using PIC.DB;
using PIC.DB.Dao;
using NotifyMonitorJob.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotifyMonitorJob.Dao;

/// <summary>
/// JOB執行詳細步驟紀錄 DAO
/// </summary>
public class SystemJobEventLogDao : BaseDao
{

    public SystemJobEventLogDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 取得 JOB異常資料紀錄
    /// </summary>
    /// <param name="jobMudleName"></param>
    /// <param name="execEndTime"></param>
    /// <returns></returns>
    public List<SystemJobeventLogModel> GetErrorList(string jobMudleName, DateTime execEndTime)
    {
        var sql = @"
            select * from system_jobevent_log 
            where infoclass = 'ERROR' and jobmodulename = @jobMudleName 
                and executetime between @startDateTime and @endDateTime ";
        var param = new
        {
            jobMudleName,
            startDateTime = execEndTime.AddHours(-6),
            endDateTime = execEndTime.AddHours(2),
        };
        
        return da.DapperQuery<SystemJobeventLogModel>(sql, param).ToList();
    }


}
