using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateMasterData.Model.EmployeeDiff
{
    public class JobIdentityChangeTmpModel
    {
        public int Id { get; set; }
        public string EmpId { get; set; }
        //1 到職 ,
        //2 復職(總部->商場聯服)
        //3 復職(商場聯服->總部)
        //4.身分轉換(總部->商場聯服)
        //5.身分轉換(商場－> 總部)、 (PT－>FT)
        //6.身分轉換(商場－> 總部)、 (FT－>PT)
        //例外人員排班時判斷
        public string ChangeType { get; set; }
        public int? A_Id { get; set; }
        public string? TeamId { get; set; }
        public string IsDel { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreateUser { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string UpdateUser { get; set; }
    }
}
