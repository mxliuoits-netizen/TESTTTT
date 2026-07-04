using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotifyMonitorJob.Model.View;

public class NotifyUserAbnormalMailModel
{
    [DisplayName("主旨")]
    public string Subject { get; set; } = "";

    public string MailTo { get; set; } = "";

    [DisplayName("MAILTO")]
    public string MailToName { get; set; } = "";

    public string CC { get; set; } = "";

    [DisplayName("CC")]
    public string CCName { get; set; } = "";

    public string BCC { get; set; } = "";

    [DisplayName("BCC")]
    public string BCCName { get; set; } = "";

    [DisplayName("寄送時間")]
    public DateTime SentTime { get; set; }
}

public class NotifyUserAbnormalCountModel
{
    [DisplayName("主旨名稱")]
    public string Subject { get; set; } = "";

    [DisplayName("寄出總數")]
    public int SendCount { get; set; }

    [DisplayName("黑名單寄出總數")]
    public int BlackSendCount { get; set; }
}