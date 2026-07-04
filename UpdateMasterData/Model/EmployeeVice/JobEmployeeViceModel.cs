using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateMasterData.Model.EmployeeVice
{
    /// <summary>
    /// 在職員工主檔轉
    /// </summary>
    public class JobEmployeeViceModel
    {
        public int Id { get; set; }
        public string Empname { get; set; }
        public string Sapteamname { get; set; }
        public string Empid { get; set; }
        public string Ptft { get; set; }
        public string Sapaname { get; set; }
        public DateTime? Effectivedate { get; set; }
        public DateTime? Expiredate { get; set; }
        public string Sex { get; set; }
        public string Sapteamid { get; set; }
        public DateTime? Birthday { get; set; }
        public DateTime? Realonboarddate { get; set; }       
        public string Costid { get; set; }
        public DateTime? TransferDate { get; set; }
        public DateTime? TransferUpdateTime { get; set; }
        public string PositionId { get; set; }
        public string HrZone { get; set; }
        public string Email { get; set; }
        public string GroupType { get; set; }
        public string JobCode { get; set; }
        public string Outside { get; set; }
        public string IsImport { get; set; }

        public DateTime? Backuptime { get; set; }
    }
}
