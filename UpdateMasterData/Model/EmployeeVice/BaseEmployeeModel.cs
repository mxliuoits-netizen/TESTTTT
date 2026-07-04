using PIC.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateMasterData.Model.EmployeeVice
{

    public class BaseEmployeeModel
    {
        public BaseEmployeeModel() { }
        public BaseEmployeeModel(object obj)
        {
            BeanUtils.Fill(obj, this);
        }

        public string Dataflag { get; set; }
        public DateTime? LogCreatetime { get; set; }
        public string Serverip { get; set; }
        public int? Versionid { get; set; }
        public string Createuser { get; set; }
        public string Createip { get; set; }
        public DateTime? Createtime { get; set; }
        public string Updateuser { get; set; }
        public string Updateip { get; set; }
        public DateTime? Updatetime { get; set; }
        public string Empid { get; set; }
        public string Empname { get; set; }
        public string Empnickname { get; set; }
        public string Emppwd { get; set; }
        public string Isactive { get; set; }
        public string Role { get; set; }
        public string Teamid { get; set; }
        public int? AId { get; set; }
        public string AName { get; set; }
        public DateTime? Effectivedate { get; set; }
        public DateTime? Expiredate { get; set; }
        public string Ismanager { get; set; }
        public string Ismanagearr { get; set; }
        public string Isnocheckarr { get; set; }
        public string Datasource { get; set; }
        public string Sex { get; set; }
        public string Ptft { get; set; }
        public DateTime? Birthday { get; set; }
        public string Note { get; set; }
        public string Reportteamid { get; set; }
        public int? ReportaId { get; set; }
        public string Costid { get; set; }
        public string Attendcard1 { get; set; }
        public string Attendcard1Updateuser { get; set; }
        public DateTime? Attendcard1Updatetime { get; set; }
        public string Attendcard2 { get; set; }
        public string Attendcard2Updateuser { get; set; }
        public DateTime? Attendcard2Updatetime { get; set; }
        public DateTime? Realonboarddate { get; set; }
        public DateTime? TransferUpdateTime { get; set; }
        public string PositionId { get; set; }
        public string HrZone { get; set; }
        public string Outside { get; set; }
        public string Email { get; set; }
        public string GroupType { get; set; }
        public string JobCode { get; set; }
    }
}
