using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotifyMonitorJob.Model;

public class JobCheckabnormalNotifyModel
{
    public Int32 Id { get; set; }
    public string JobCode { get; set; }
    public string JobName { get; set; }
    public string IsNotifyOp { get; set; }
    public string? Serverip { get; set; }
    public Int32 Versionid { get; set; }
    public string? Createuser { get; set; }
    public string? Createip { get; set; }
    public DateTime Createtime { get; set; }
    public string? Updateuser { get; set; }
    public string? Updateip { get; set; }
    public DateTime? Updatetime { get; set; }
    public string JobSubjectName { get; set; }

    public int? Batchno { get; set; }
}
