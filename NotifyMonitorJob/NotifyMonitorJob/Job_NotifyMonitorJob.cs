using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NotifyMonitorJob.Dao;
using NotifyMonitorJob.Service;
using NotifyMonitorJob.Tool;
using PIC.DB;
using PIC.Job;

namespace NotifyMonitorJob;

/// <summary>
/// 監控排程通知 Job
/// </summary>
public class Job_NotifyMonitorJob : BaseJob
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
    string NOTIFYUSERABNORMALTIMES_DATES_TEXT;

    public Job_NotifyMonitorJob(string jobName, Logger logger, params string[] args) : base(jobName, BaseJob.GROUP_NAME_JOB, BaseJob.keyToday, logger, args)
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
        NOTIFYUSERABNORMALTIMES_DATES_TEXT = Environment.GetEnvironmentVariable("PIC_JOB_NOTIFYUSERABNORMALTIMES_DATES") ?? "23,28,5,9";
    }

    /// <summary>
    /// 執行
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public override void Execute()
    {
        const string HTML_WARPLINE = "<br />";
        using var da = DBDataAccessFactory.GetInstance(this.dbType, this.dbPrefix);

        try
        {
            var systemJobExecLogDao = new SystemJobExecLogDao(da);
            var notifyMonitorJobService = new NotifyMonitorJobService();

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
            var jobExecuteList = systemJobExecLogDao.GetDistinctJobExecLogList();
            jobExecuteList = jobExecuteList.Where(x=> x.Jobmodulename!= "NotifyMonitorJob.Job_NotifyMonitorJob").ToList(); //排除自己

            var notReportStatusList = new List<string>() { "成功", "本日無檔(非工作日)" };
            var reportMonitorJobList = jobExecuteList.Where(x => x.Jobversion != this.jobVersion || !notReportStatusList.Contains(x.Execstatustext)).ToList();
            notifyMonitorJobService.MemoExecuteDate(reportMonitorJobList, NOTIFYUSERABNORMALTIMES_DATES_TEXT); //加工下次執行日期
            this.WriteLog("取得SystemJobExecLog資料結束", false);

            // 組合 信件內容
            this.CurrentJobStepText = "組合信件內容資料";
            this.WriteLog("組合信件內容資料資料開始", false);
            var sb = new StringBuilder(HTML_WARPLINE);
            if (reportMonitorJobList == null || reportMonitorJobList.Count == 0)
            {
                sb.Append(MAIL_MSG);
            }
            else
            {
                sb.Append(HtmlTool.GetModelToHTMLTable(reportMonitorJobList));
                sb.Append(HtmlTool.GetTableCSS());
            }
            this.WriteLog("組合信件內容資料資料結束", false);


            // 寄送通知信
            this.CurrentJobStepText = "寄送通知信";
            this.WriteLog("寄送通知信開始", false);
            string[] mailCC = string.IsNullOrEmpty(MAIL_CC) ? null : MAIL_CC.Split(',');
            string[] mailTO = MAIL_TO.Split(',');
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


            //更新 ODataFix.Job_FixMappingAttend IP超過12小時的資料
            this.CurrentJobStepText = "更新ODataFix.Job_FixMappingAttend IP超過12小時的資料";
            this.WriteLog("更新開始", false);
            systemJobExecLogDao.UpdateODataFixJobIP();
            this.WriteLog("更新結束", false);
        }
        catch (Exception ex)
        {
            throw;
        }
    }





}