using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIC.DB;
using PIC.DB.Dao;

namespace UpdateMasterData.Dao;

/// <summary>
/// 員工主檔轉入歷程表 
/// </summary>
public class JobEmployeeviceTDao : BaseDao
{
    public JobEmployeeviceTDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 取得 最新的異動日
    /// </summary>
    /// <param name="empId"></param>
    /// <returns></returns>
    public DateTime GetTransferUpdateTime(string empId, DateTime jobDate)
    {
        var sql = @" 
select 
    jet.transfer_update_time 
from job_employeevice_t jet
join (
    select a.empid, max(a.datecurrent) as datecurrent 
    from job_employeevice_t a where a.empid = @empId
    group by a.empid
) mjet on mjet.empid =jet.empid and mjet.datecurrent = jet.datecurrent
where jet.empid = @empId
";
        var resultDate = da.DapperQuery<DateTime?>(sql, new { empId }).FirstOrDefault();

        return resultDate ?? jobDate;
    }

    /// <summary>
    /// 匯入 主檔資料
    /// </summary>
    /// <param name="jobDate"></param>
    /// <returns></returns>
    public int ImportData(DateTime jobDate)
    {
        var sql = @"
INSERT INTO job_employeevice_t(datecurrent, 
empname, sapteamname, empid, ptft, sapaname, effectivedate, expiredate, sex, sapteamid, birthday, 
realonboarddate, costid, transfer_date, transfer_update_time, position_id, hr_zone, email, group_type, 
job_code, outside, identification, identification_type)
SELECT  
    @jobDate as datecurrent,
    empname, sapteamname, empid, ptft, sapaname, effectivedate, expiredate, sex, sapteamid, birthday, 
    realonboarddate, costid, transfer_date, transfer_update_time, position_id, hr_zone, email, group_type, 
    job_code, outside, identification, identification_type
FROM Job_EmployeeVice_tmp where is_import = 'Y'
";
        return da.DapperExecute(sql, new { jobDate });
    }

    /// <summary>
    /// 刪除 資料日期相關員工主檔紀錄
    /// </summary>
    /// <param name="jobDate"></param>
    /// <returns></returns>
    public int DeleteByDateCurrent(DateTime jobDate)
    {
        var sql = "delete from job_employeevice_t where datecurrent = @jobDate ";
        return da.DapperExecute(sql, new { jobDate });
    }


}
