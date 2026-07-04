using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateMasterData.Model.Quota;

public class Cal00901ArrOffdayT
{
    public string? Serverip { get; set; }
    public string? Createuser { get; set; }
    public string? Createip { get; set; }
    public DateTime? Createtime { get; set; }
    public string Empid { get; set; }
    public string WtId { get; set; }
    public DateTime Datecurrent { get; set; }
    public DateTime Datefrom { get; set; }
    public DateTime Dateto { get; set; }
    public string SapUk { get; set; }
    public decimal Restnumber { get; set; }
    public string Note { get; set; }
}
