using NLog;
using PIC.DB;
using PIC.Job;
using PIC.Libary.Dao;
using NotifyMonitorJob.Dao;
using NotifyMonitorJob.Model;
using NotifyMonitorJob.Model.View;
using NotifyMonitorJob.Service;
using NotifyMonitorJob.Tool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SystemJobEventLogDao = NotifyMonitorJob.Dao.SystemJobEventLogDao;
using SystemJobExecLogDao = NotifyMonitorJob.Dao.SystemJobExecLogDao;

namespace NotifyMonitorJob;

/// <summary>
/// 發送排程執行結果Email JOB
/// </summary>
public class Job_CheckAbnormalData : BaseJob
{
    string MAIL_SERVERHOST;
    int MAIL_SERVERPORT;
    int MAXTRETRY;
    string MAIL_FORM;
    string MAIL_DISPLAYNAME;
    string MAIL_SUBJECT;
    string MAIL_MSG;
    string MAIL_TO;
    string MAIL_CC;
    string EVENT_MAILFROM;
    string EVENT_MAILSUBJECT;
    string EVENT_MAILTO;
    string SEND_BATCH_NO;
    const string HTML_WARPLINE = "<br />";

    public Job_CheckAbnormalData(string jobName, Logger logger, params string[] args) : base(jobName, BaseJob.GROUP_NAME_JOB, BaseJob.keyToday, logger, args)
    {
        MAIL_SERVERHOST = Environment.GetEnvironmentVariable("PIC_JOB_SMTPServer") ?? "";
        MAIL_SERVERPORT = int.Parse(Environment.GetEnvironmentVariable("PIC_JOB_SMTPServer_Port") ?? "1025");
        MAXTRETRY = int.Parse(Environment.GetEnvironmentVariable("PIC_JOB_RETRY") ?? "1");
        MAIL_FORM = Environment.GetEnvironmentVariable("PIC_JOB_MAILFROM") ?? "";
        MAIL_DISPLAYNAME = Environment.GetEnvironmentVariable("PIC_JOB_MAILDISPLAYNAME") ?? "";
        MAIL_SUBJECT = Environment.GetEnvironmentVariable("PIC_JOB_MAILSUBJECT") ?? "";
        MAIL_MSG = Environment.GetEnvironmentVariable("PIC_JOB_MAILMSG") ?? "";
        MAIL_TO = Environment.GetEnvironmentVariable("PIC_JOB_MAILTO") ?? "";
        MAIL_CC = Environment.GetEnvironmentVariable("PIC_JOB_MAILCC") ?? "";
        EVENT_MAILFROM = Environment.GetEnvironmentVariable("PIC_JOB_EVENT_MAILFROM") ?? "";
        EVENT_MAILSUBJECT = Environment.GetEnvironmentVariable("PIC_JOB_EVENT_MAILSUBJECT") ?? "";
        EVENT_MAILTO = Environment.GetEnvironmentVariable("PIC_JOB_EVENT_MAILTO") ?? "";
        MAIL_SUBJECT = Environment.GetEnvironmentVariable("PIC_JOB_MAILSUBJECT") ?? "";
        SEND_BATCH_NO = Environment.GetEnvironmentVariable("PIC_JON_CONFIG_BATCH_NO") ?? "B";
    }

    /// <summary>
    /// 實作JOB
    /// </summary>
    /// <exception cref="Exception"></exception>
    public override void Execute()
    {
        this.WriteLog("開始發送排程執行結果Email", false);
        using var da = DBDataAccessFactory.GetInstance(this.dbType, this.dbPrefix);
        try
        {
            var maillService = new SMTPClientService();
            var systemJobExecLogDao = new SystemJobExecLogDao(da);
            var systemJobEventLogDao = new SystemJobEventLogDao(da);
            var jobCheckabnormalNotifyDao = new JobCheckabnormalNotifyDao(da);
            var systemConfigDao = new SystemConfigDao(da);
            var mailNormalLogDao = new MailNormalLogDao(da);

            DateTime jobDate = DateTime.ParseExact(this.jobVersion, BaseJob.VER_DATE_FORMAT, null);

            // 檢查 [整合性資料查詢結果寄出MAIL變數]
            this.CurrentJobStepText = "檢查變數";
            if (string.IsNullOrEmpty(MAIL_SERVERHOST) || string.IsNullOrEmpty(MAIL_SUBJECT) || string.IsNullOrEmpty(MAIL_MSG) || string.IsNullOrEmpty(MAIL_FORM)
                || string.IsNullOrEmpty(MAIL_TO))
            {
                throw new Exception("設定[整合性資料查詢結果寄出MAIL變數]錯誤，設定值可能有空值.");
            }

            // 取得 信件資料
            this.CurrentJobStepText = "取得SystemJobExecLog資料";
            this.WriteLog("取得SystemJobExecLog資料開始", false);

            var jobNotifyConifgList = jobCheckabnormalNotifyDao.GetList();
            var jobExecLogList = systemJobExecLogDao.GetJobExecLogList(jobDate);

            // 批次名單過濾資料
            var whiteList = jobNotifyConifgList.Where(x => x.Batchno != null).ToList();
            if ("B".Equals(this.SEND_BATCH_NO))
            {
                //有設定批次之外的排程才通知
                var jobCodeList = whiteList.Select(x => x.JobCode).ToList();
                jobExecLogList = jobExecLogList.Where(x => !jobCodeList.Contains(x.Jobmodulename)).ToList();
            }
            else
            {
                // 取得 排程批次與設定批次一致才通知
                int sendBatchNo = int.Parse(this.SEND_BATCH_NO);
                var jobCodeList = whiteList.Where(x => x.Batchno == sendBatchNo).Select(x => x.JobCode).ToList();
                jobExecLogList = jobExecLogList.Where(x => jobCodeList.Contains(x.Jobmodulename)).ToList();
            }

            string[] mailCC = string.IsNullOrEmpty(MAIL_CC) ? null : MAIL_CC.Split(',');
            string[] mailTO = MAIL_TO.Split(',');
            this.WriteLog("取得SystemJobExecLog資料結束", false);

            // 組合 信件內容
            this.CurrentJobStepText = "組合信件內容資料";
            this.WriteLog("組合信件內容資料資料開始", false);
            var sb = new StringBuilder(HTML_WARPLINE);
            if (jobExecLogList == null || jobExecLogList.Count == 0)
            {
                sb.Append(MAIL_MSG);
            }
            else
            {
                sb.Append(HtmlTool.GetModelToHTMLTable(jobExecLogList));
                sb.Append(HtmlTool.GetTableCSS());
            }
            this.WriteLog("組合信件內容資料資料結束", false);

            // 寄送通知信
            this.CurrentJobStepText = "寄送通知信";
            this.WriteLog("寄送通知信開始", false);
            int sendTimes = 0;
            while (true)
            {
                try
                {
                    SMTPClientService.SendEmail(MAIL_SUBJECT, MAIL_FORM, mailTO, mailCC, null, sb.ToString(), MAIL_SERVERHOST, MAIL_SERVERPORT, MAIL_DISPLAYNAME);
                    break;
                }
                catch (Exception ex)
                {
                    sendTimes++;
                    Thread.Sleep(1000);
                    this.WriteLog($"重新寄送信件(重試次數:{sendTimes})", true);
                    if (sendTimes >= MAXTRETRY) throw;
                }
            }
            this.WriteLog("寄送通知信結束", false);

            //寄送異常事件通知信
            this.CurrentJobStepText = "寄送異常事件通知信";
            this.WriteLog("寄送異常事件通知信開始", false);
            var notifyJobList = jobNotifyConifgList.Where(x => x.IsNotifyOp == "Y").ToList();
            var notifyJobNameList = notifyJobList.Select(x => x.JobCode).ToList();

            var notNotifyStatusList = new List<string>() { "成功", "CS", "CS(非工作日)", "IP" };
            var errorExecList = jobExecLogList.Where(x => !notNotifyStatusList.Contains(x.Execstatus) && notifyJobNameList.Contains(x.Jobmodulename)).ToList();
            foreach (var execItem in errorExecList)
            {
                string datafrom = this.GetType().Name + "_" + execItem.Jobmodulename;

                sb.Clear();
                var errorEventlist = systemJobEventLogDao.GetErrorList(execItem.Jobmodulename, execItem.Createtime);
                var bomDataList = new List<SystemJobEventLogViewModel>(){
                    new SystemJobEventLogViewModel()
                    {
                        Jobmodulename = execItem.Jobmodulename,
                        Jobversion = execItem.Jobversion,
                        Createtime = execItem.Createtime,
                        Serverip = execItem.Serverip,
                        EventMessage = String.Join("<br /><br />", errorEventlist.Select(x => x.Logmsg))
                    }
                };

                sb.Append(HtmlTool.GetModelToHTMLTable(bomDataList));
                sb.Append(HtmlTool.GetTableCSS());

                // 寄送通知信
                sendTimes = 0;
                var iskeepSend = true;
                while (iskeepSend)
                {
                    try
                    {
                        var notifyJobData = notifyJobList.Where(x => x.JobCode == execItem.Jobmodulename).First();
                        var mailSubject = string.Format(EVENT_MAILSUBJECT, notifyJobData?.Id.ToString() ?? "", notifyJobData?.JobSubjectName ?? "");

                        SMTPClientService.SendEmail(mailSubject, EVENT_MAILFROM, EVENT_MAILTO.Split(','), null, null, sb.ToString(), MAIL_SERVERHOST, MAIL_SERVERPORT, "統一資訊考勤系統");
                        iskeepSend = false;

                        //LOG MAIL
                        var logMailData = new MailNormalLogModel()
                        {
                            MailTo = EVENT_MAILTO,
                            DisplayName = "統一資訊考勤系統",
                            Subject = mailSubject,
                            MailContent = sb.ToString(),
                            IsSent = true,
                            SentTime = DateTime.Now,
                            Times = 1,
                            CreateTime = DateTime.Now,
                            LogStatus = "Y",
                            LogTime = DateTime.Now,
                            DataFrom = datafrom,
                            MailSource = $"{this.jobVersion}",
                        };
                        mailNormalLogDao.CreateBatch([logMailData]);
                    }
                    catch (Exception ex)
                    {
                        sendTimes++;
                        Thread.Sleep(1000);
                        this.WriteLog($"重新寄送信件(重試次數:{sendTimes})", true);
                        if (sendTimes >= MAXTRETRY) throw;
                    }
                }

            }
            this.WriteLog("寄送異常事件信結束", false);
        }
        catch (Exception ex)
        {
            this.WriteLog("發送排程執行結果Email失敗", true);
            throw;
        }
        finally
        {
            this.WriteLog("結束發送排程執行結果Email", false);
        }
    }




}
