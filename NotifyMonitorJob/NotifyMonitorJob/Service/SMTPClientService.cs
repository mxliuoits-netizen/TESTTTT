using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MimeKit;
using MailKit.Net.Smtp;
using NPOI;
using NPOI.SS.UserModel;
using NPOI.XSSF.Streaming;
using System.Linq.Expressions;

namespace NotifyMonitorJob.Service;

public class SMTPClientService
{
    public static void SendEmail(string subject, string mailFrom, string mailTo, string[] cc, string[] bcc,
        string mailConent, string mailServer, int port = 587, string displayName = "", List<AttachementFile> attchementList = null)
    {
        SendEmail(subject, mailFrom, new string[] { mailTo }, cc, bcc,
        mailConent, mailServer, port, displayName, attchementList);
    }

    public static void SendEmail(string subject, string mailFrom, string[] mailTo, string[] cc, string[] bcc,
        string mailConent, string mailServer, int port = 587 , string displayName="", List<AttachementFile> attchementList = null)
    {
        var mailMessage = new MimeMessage();
        mailMessage.Subject = subject;
        mailMessage.From.Add(new MailboxAddress(displayName, mailFrom));
        if(mailTo != null && mailTo.Length > 0)
        {
            foreach (var to in mailTo)
            {
                mailMessage.To.Add(new MailboxAddress(to, to));
            }
        }
        if (cc != null && cc.Length > 0)
        {
            foreach (var cctarget in cc)
            {
                mailMessage.Cc.Add(new MailboxAddress(cctarget, cctarget));
            }
        }
        if (bcc != null && bcc.Length > 0)
        {
            foreach (var bcctarget in bcc)
            {
                mailMessage.Bcc.Add(new MailboxAddress(bcctarget, bcctarget));
            }
        }

        /*mailMessage.Body = new TextPart("html")
        {
            Text = mailConent
        };*/

        var builder = new BodyBuilder();
        builder.HtmlBody = mailConent;
        if(attchementList!= null && attchementList.Count > 0)
        {
            foreach(var item in attchementList)
            {
                builder.Attachments.Add(item.FileName, item.bytes);
            }
        }      
        mailMessage.Body = builder.ToMessageBody();

        using (var smtpClient = new SmtpClient())
        {
            try
            {
                //如果有安全設定變化 也要參數化
                smtpClient.Connect(mailServer, port, MailKit.Security.SecureSocketOptions.Auto);
                //如果mail Server需要帳號密碼
                //Todo !! 參數化
                //smtpClient.Authenticate("your-email@example.com", "your-password");
                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                throw ex;               
            }
            finally
            {
                smtpClient.Disconnect(true);
            }
        }
    }
}

public class AttachementFile
{
    public string FileName { get; set; } = "";

    public byte[] bytes { get; set; } = Array.Empty<byte>();
}