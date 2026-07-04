using PIC.Libary.Dao;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateMasterData.Model;

namespace UpdateMasterData.Service
{
    /// <summary>
    /// 服務基類
    /// </summary>
    public class BaseService
    {
        /// <summary>
        /// JOB LOG DATA
        /// </summary>
        protected JobMetaDataModel JobMetaData;

        /// <summary>
        /// LOG DAO
        /// </summary>
        protected SystemJobEventLogDao SystemJobEventLogDao;

        /// <summary>
        /// 紀錄LOG
        /// </summary>
        /// <param name="message"></param>
        protected void LogInfo(string message)
        {
            if(SystemJobEventLogDao != null) SystemJobEventLogDao.WriteDatabaseLog(JobMetaData.EventGroup, JobMetaData.EventMoudule, message, JobMetaData.CreateUser, JobMetaData.CreateIp, DateTime.Now, false);
            Console.Write(message+"\r\n");
        }
    }
}
