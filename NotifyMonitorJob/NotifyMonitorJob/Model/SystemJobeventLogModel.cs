using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotifyMonitorJob.Model;

public class SystemJobeventLogModel
{
    public Int32 Id { get; set; }
    public string? Serverip { get; set; }
    public string? Createuser { get; set; }
    public string? Createip { get; set; }
    public DateTime? Createtime { get; set; }
    public string? Infoclass { get; set; }
    public string? Jobexecgroup { get; set; }
    public string? Jobmodulename { get; set; }
    public string? Logmsg { get; set; }
    public DateTime Executetime { get; set; }
    public string? Note { get; set; }

}
