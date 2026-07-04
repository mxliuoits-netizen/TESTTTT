using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using PIC.DB;
using PIC.Job;
using NotifyMonitorJob.Dao;
using NotifyMonitorJob.Service;
using NotifyMonitorJob.Tool;

namespace NotifyMonitorJob;

public class Job_CheckCustomerAbnormalData : BaseJob
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
    const string HTML_WARPLINE = "<br />";

    public Job_CheckCustomerAbnormalData(string jobName, Logger logger, params string[] args) : base(jobName, BaseJob.GROUP_NAME_JOB, BaseJob.keyToday, logger, args)
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
    }

    /// <summary>
    /// 實作JOB
    /// </summary>
    /// <exception cref="Exception"></exception>
    public override void Execute()
    {
        this.WriteLog("開始發送通知客戶排程執行結果Email", false);
        using var da = DBDataAccessFactory.GetInstance(this.dbType, this.dbPrefix);
        try
        {
            var sMTPClientService = new SMTPClientService();
            var systemJobExecLogDao = new SystemJobExecLogDao(da);

            DateTime jobDate = DateTime.ParseExact(this.jobVersion, BaseJob.VER_DATE_FORMAT, null);

            // 檢查 [整合性資料查詢結果寄出MAIL變數]
            this.CurrentJobStepText = "檢查變數";
            if (string.IsNullOrEmpty(MAIL_SERVERHOST) || string.IsNullOrEmpty(MAIL_SUBJECT) || string.IsNullOrEmpty(MAIL_MSG) || string.IsNullOrEmpty(MAIL_FORM)
                || string.IsNullOrEmpty(MAIL_TO))
            {
                throw new Exception("設定[整合性資料查詢結果寄出MAIL變數]錯誤，設定值可能有空值.");
            }
            string[] mailCC = string.IsNullOrEmpty(MAIL_CC) ? null : MAIL_CC.Split(',');
            string[] mailTO = MAIL_TO.Split(',');

            // 取得 信件資料
            this.CurrentJobStepText = "取得SystemJobExecLog資料";
            this.WriteLog("取得SystemJobExecLog資料開始", false);
            var jobExecLogList = systemJobExecLogDao.GetChtNameJobExecLogList(jobDate);           
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

        }
        catch (Exception ex)
        {
            this.WriteLog("發送通知客戶排程執行結果Email失敗", true);
            throw;
        }
        finally
        {
            this.WriteLog("結束發送通知客戶排程執行結果Email", false);
        }
    }




}
