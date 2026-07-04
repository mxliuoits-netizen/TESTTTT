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
/// 排程通知設定表
/// </summary>
public class JobCheckabnormalNotifyDao : BaseDao
{

    public JobCheckabnormalNotifyDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 取得 排程通知設定資料
    /// </summary>
    /// <returns></returns>
    public List<JobCheckabnormalNotifyModel> GetList()
    {
        var sql = @" select * from job_checkabnormal_notify ";
        return da.DapperQuery<JobCheckabnormalNotifyModel>(sql).ToList();
    }



}