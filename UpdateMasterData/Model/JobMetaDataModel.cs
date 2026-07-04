using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateMasterData.Model
{
    /// <summary>
    /// Job 紀錄資料
    /// </summary>
    public class JobMetaDataModel
    {
        public DateTime JobDate { get; set; }
        public string EventGroup { get; set; }
        public string EventMoudule { get; set; }
        public string ServerIp { get; set; }
        public string CreateUser { get; set; }
        public string CreateIp { get; set; }
        public DateTime CreateTime { get; set; }

        public string DBType { get; set; }

        public string DBPrefix { get; set; }
    }
}
