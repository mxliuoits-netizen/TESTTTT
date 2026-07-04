using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotifyMonitorJob.Model.View;

public class SystemJobEventLogViewModel
{
    [DisplayName("作業名稱")]
    public string Jobmodulename { get; set; }
    [DisplayName("版本日")]
    public string Jobversion { get; set; }
    [DisplayName("執行時間")]
    public DateTime Createtime { get; set; }
    [DisplayName("主機")]
    public string Serverip { get; set; }
    [DisplayName("事件描述")]
    public string EventMessage { get; set; }

}
