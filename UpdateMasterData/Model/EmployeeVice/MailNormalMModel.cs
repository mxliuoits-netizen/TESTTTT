using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateMasterData.Model.EmployeeVice;

public class MailNormalMModel
{
    public Int32 Id { get; set; }
    public string MailTo { get; set; }
    public string? Displayname { get; set; }
    public string? Subject { get; set; }
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string MailContent { get; set; }
    public string? Attachmentpath { get; set; }
    public string Datafrom { get; set; }
    public DateTime Createtime { get; set; }
    public Int32 Times { get; set; }
    public bool IsSent { get; set; }
    public DateTime? Senttime { get; set; }
    public string? MailSource { get; set; }
}
