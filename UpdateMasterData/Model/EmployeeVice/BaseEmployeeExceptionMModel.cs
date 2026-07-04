using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateMasterData.Model.EmployeeVice
{
    public class BaseEmployeeExceptionMModel
    {
        public Int32 Id { get; set; }
        public string Createuser { get; set; }
        public string Createip { get; set; }
        public DateTime Createtime { get; set; }
        public string Excepttype { get; set; }
        public string Empid { get; set; }
        public string Empname { get; set; }
        public string Sapaname { get; set; }
        public Int32 AId { get; set; }
        public string AName { get; set; }
        public string AArea { get; set; }
        public string Note { get; set; }
        public DateTime Effectivedate { get; set; }
        public DateTime Expiredate { get; set; }
        public string Teamid { get; set; }
        public string HrZone { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }
}
