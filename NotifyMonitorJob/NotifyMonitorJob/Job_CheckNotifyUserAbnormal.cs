using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NotifyMonitorJob.Dao;
using NotifyMonitorJob.Model.View;
using NotifyMonitorJob.Service;
using NotifyMonitorJob.Tool;
using NPOI.SS.Formula.Functions;
using PIC.DB;
using PIC.Job;

namespace NotifyMonitorJob;


public class Job_CheckNotifyUserAbnormal : BaseJob
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

    public Job_CheckNotifyUserAbnormal(string jobName, Logger logger, params string[] args) : base(jobName, BaseJob.GROUP_NAME_JOB, BaseJob.keyToday, logger, args)
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

    public override void Execute()
    {
        using var da = DBDataAccessFactory.GetInstance(this.dbType, this.dbPrefix);
       
        try
        {
            var mailNormalLogDao = new MailNormalLogDao(da);
            var baseEmployeeMDao = new BaseEmployeeMDao(da);


            DateTime jobDate = DateTime.ParseExact(this.jobVersion, BaseJob.VER_DATE_FORMAT, null);
            DateTime attendStartDate, attendEndDate;
            if (jobDate.Day > 20) // 21~3x
            {
                var nextMonthDT = jobDate.AddMonths(1);
                attendStartDate = new DateTime(jobDate.Year, jobDate.Month, 21);
                attendEndDate = new DateTime(nextMonthDT.Year, nextMonthDT.Month, 20);
            }
            else // 1~20
            {
                var preMonthDT = jobDate.AddMonths(-1);
                attendStartDate = new DateTime(preMonthDT.Year, preMonthDT.Month, 21);
                attendEndDate = new DateTime(jobDate.Year, jobDate.Month, 20);
            }
            var isShowYear = "Y" == (Environment.GetEnvironmentVariable("PIC_JOB_IS_PWD_APPEND_YEAR_SUFFIX") ?? "");
            var yearSuffix = isShowYear ? jobDate.Year + "" : "";
            var encryptPwd = (Environment.GetEnvironmentVariable("PIC_JOB_EXCEL_PWD") ?? "Sethq") + yearSuffix;
            var mailSubjectText = Environment.GetEnvironmentVariable("PIC_JOB_FILTER_SUBJECT") ?? "";
            var mailSubjectList = mailSubjectText.Split(",").ToList();
            var isShowNotifyAbTimesReport = "Y" == (Environment.GetEnvironmentVariable("PIC_JOB_IS_SHOW_NOTIFYABTIMES_REPORT") ?? "");

            var checkNotifyUserAbnormalService = new CheckNotifyUserAbnormalService(da);

            // 取得內文附件資料
            this.CurrentJobStepText = "取得信件內文材料";
            this.WriteLog("取得異常信件紀錄開始", false);
            var mailMapList = baseEmployeeMDao.GetMailEmpidList();
            var mailList = mailNormalLogDao.GetNotifyUserAbnormalMailList(jobDate, mailSubjectList);
            var blackMailList = mailNormalLogDao.GetNotifyUserAbnormalBlackMailList(jobDate, mailSubjectList);
            var notifyAbTimesList = new List<NotifyAbTimesReportVM>();
            if (isShowNotifyAbTimesReport)
            {
                notifyAbTimesList = mailNormalLogDao.GetNotifyAbTimesReportList(attendStartDate, attendEndDate);
            }

            SwitchMailToName(mailList, mailMapList);
            SwitchMailToName(blackMailList, mailMapList);
            
            // 附件1 
            var fileName = $"NotifyUserMail_{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";
            var fileBytes = checkNotifyUserAbnormalService.GetNotifyUserAbnormalMailExcel(mailList, blackMailList, notifyAbTimesList);
            var preZipFileList = new List<PreZipFile>() { new PreZipFile() { FileName = fileName, FileBytes = fileBytes } };

            var zipFileName = $"ZipNotifyUserMail_{DateTime.Now.ToString("yyyyMMddHHmmss")}.zip";
            var zipFileBytes = ZipTool.ZipFile(preZipFileList, encryptPwd);
            var fileAttachmentList = new List<AttachementFile>()
            {
                new AttachementFile() { FileName = zipFileName, bytes = zipFileBytes },
            };

            var bodyList = mailSubjectList.Select(x => new NotifyUserAbnormalCountModel()
            {
                Subject = x,
                SendCount = mailList.Where(y => y.Subject.Contains(x)).Count(),
                BlackSendCount = blackMailList.Where(y=>y.Subject.Contains(x)).Count()
            }).ToList();
            var sb = new StringBuilder(HTML_WARPLINE);
            sb.Append(HtmlTool.GetModelToHTMLTable(bodyList));
            sb.Append(HtmlTool.GetTableCSS());
            this.WriteLog("取得異常信件紀錄結束", false);


            // 檢查 [整合性資料查詢結果寄出MAIL變數]
            this.CurrentJobStepText = "檢查信件參數";
            if (string.IsNullOrEmpty(MAIL_SERVERHOST) || string.IsNullOrEmpty(MAIL_SUBJECT) || string.IsNullOrEmpty(MAIL_MSG) || string.IsNullOrEmpty(MAIL_FORM)
                || string.IsNullOrEmpty(MAIL_TO))
            {
                throw new Exception("設定[整合性資料查詢結果寄出MAIL變數]錯誤，設定值可能有空值.");
            }
            string[] mailCC = string.IsNullOrEmpty(MAIL_CC) ? null : MAIL_CC.Split(',');
            string[] mailTO = MAIL_TO.Split(',');

            // 寄送通知信
            this.CurrentJobStepText = "寄送通知信";
            this.WriteLog("寄送通知信開始", false);
            int sendTimes = 0;
            while (true)
            {
                try
                {
                    SMTPClientService.SendEmail(MAIL_SUBJECT, MAIL_FORM, mailTO, mailCC, null, sb.ToString(), MAIL_SERVERHOST, MAIL_SERVERPORT, MAIL_DISPLAYNAME, attchementList: fileAttachmentList);
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
            throw;
        }
    }

    public void SwitchMailToName(List<NotifyUserAbnormalMailModel> mailList, List<MailMapEmpIdModel> mailMapList)
    {
        foreach (var item in mailList)
        {
            var mailToNameList = item.MailTo.Split(",").ToList().Select(x => mailMapList.Where(y => y.Email == x).FirstOrDefault()?.MailToName ?? x).ToList();
            item.MailToName = string.Join(",", mailToNameList);

            if (!string.IsNullOrEmpty(item.CC))
            {
                var CCNameList = item.CC.Split(",").ToList().Select(x => mailMapList.Where(y => y.Email == x).FirstOrDefault()?.MailToName ?? x).ToList();
                item.CCName = string.Join(",", CCNameList);
            }

            if (!string.IsNullOrEmpty(item.BCC))
            {
                var BCCNameList = item.BCC.Split(",").ToList().Select(x => mailMapList.Where(y => y.Email == x).FirstOrDefault()?.MailToName ?? x).ToList();
                item.BCCName = string.Join(",", BCCNameList);
            }
        }
    }




}
