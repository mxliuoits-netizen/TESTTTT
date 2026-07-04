using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace UpdateMasterData.Model.EmployeeVice
{
    public class BaseAuthorityMModel
    {
        public int Id { get; set; }
        public string Serverip { get; set; }
        public int? Versionid { get; set; }
        public string Createuser { get; set; }
        public string Createip { get; set; }
        public DateTime? Createtime { get; set; }
        public string Updateuser { get; set; }
        public string Updateip { get; set; }
        public DateTime? Updatetime { get; set; }
        public string Sapteamid { get; set; }
        public string Sapaname { get; set; }
        public string Teamid { get; set; }
        public int? AId { get; set; }
        public string AName { get; set; }
        public double? ASort { get; set; }
        public string Costcenter { get; set; }
    }
}
