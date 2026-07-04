namespace NotifyMonitorJob.Model;

public class MailNormalLogModel
{
    public string? MailTo { get; set; }
    public string? DisplayName { get; set; }
    public string? Subject { get; set; }
    public string? CC { get; set; }
    public string? BCC { get; set; }
    public string? MailContent { get; set; }
    public string? AttachmentPath { get; set; }
    public string? DataFrom { get; set; }
    public DateTime? CreateTime { get; set; }
    public int Times { get; set; }
    public bool IsSent { get; set; }
    public DateTime? SentTime { get; set; }
    public DateTime? LogTime { get; set; }
    public string? LogStatus { get; set; }
    public string? Note { get; set; }
    public string? MailSource { get; set; }
}

