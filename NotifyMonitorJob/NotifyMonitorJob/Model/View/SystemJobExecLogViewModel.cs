using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotifyMonitorJob.Model.View;

/// <summary>
/// 排程執行結果資料
/// </summary>
public class SystemJobExecLogViewModel
{
    [DisplayName("作業名稱")]
    public string Jobmodulename { get; set; }
    [DisplayName("版本日")]
    public string Jobversion { get; set; }
    [DisplayName("執行時間")]
    public DateTime Createtime { get; set; }
    [DisplayName("主機")]
    public string Serverip { get; set; }
    [DisplayName("狀態")]
    public string Execstatus { get; set; }
    
}

/// <summary>
/// 排程生死監控 VeiwModel
/// </summary>
public class MonitorJobViewModel
{
    [DisplayName("作業名稱")]
    public string Jobmodulename { get; set; }
    [DisplayName("版本日")]
    public string Jobversion { get; set; }
    [DisplayName("執行開始時間")]
    public DateTime Createtime { get; set; }
    [DisplayName("執行更新時間")]
    public DateTime? Updatetime { get; set; }
    [DisplayName("執行總時間")]
    public string Runningtimetext { get; set; }
    [DisplayName("狀態")]
    public string Execstatustext { get; set; }

}
