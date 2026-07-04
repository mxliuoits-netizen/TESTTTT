using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateMasterData.Model.EmployeeDiff
{
    public class JobEmployeeDiffModel
    {
        public Int32 Id { get; set; }
        public string? DiffStatus { get; set; }
        public string? Empname { get; set; }
        public string? Costid { get; set; }
        public string? Empid { get; set; }
        public string? Ptft { get; set; }
        public string? Sapaname { get; set; }
        public DateTime? Effectivedate { get; set; }
        public DateTime? Expiredate { get; set; }
        public string? Sex { get; set; }
        public string? Sapteamid { get; set; }
        public string? Identification { get; set; }
        public string? IdentificationType { get; set; }
        public string? SapTransferCode { get; set; }
        public DateTime? TransferInDate { get; set; }
        public DateTime? TransferOutDate { get; set; }
        public DateTime? Realonboarddate { get; set; }
    }
}
