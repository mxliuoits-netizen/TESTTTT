using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateMasterData.Model.Quota;

public class ArrOffday901usedquotaModel
{
    public Int32 Leaveid { get; set; }
    public DateTime Usedatefrom { get; set; }
    public DateTime Usedateto { get; set; }
    public double Usehours { get; set; }
    public string Isdelete { get; set; }
    public DateTime Createtime { get; set; }
    public DateTime Updatetime { get; set; }
}
