using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateMasterData.Model.Organization
{
    public class BaseEmployeeModel
    {
        /// <summary>
        /// 成本中心
        /// </summary>
        public string CostId { get; set; }

        /// <summary>
        /// 組織單位代碼
        /// </summary>
        public string TeamId { get; set; }

        /// <summary>
        /// 組織名稱
        /// </summary>
        public string TeamName { get; set; }

        /// <summary>
        /// 上一層組織單位代碼
        /// </summary>
        public string ParentTeamId { get; set; }

        /// <summary>
        /// 上一層組織單位名稱
        /// </summary>
        public string ParentTeamName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string SapTeamId { get; set; }

        /// <summary>
        /// 人事子範圍
        /// </summary>
        public string HrSubAreaId { get; set; }

        /// <summary>
        /// 主管員編
        /// </summary>
        public string ManagerEmpId { get; set; }

        /// <summary>
        /// 分類
        /// </summary>
        public string TeamType { get; set; }

        /// <summary>
        /// 是否啟用
        /// </summary>
        public string isActive { get; set; }
    }
}
