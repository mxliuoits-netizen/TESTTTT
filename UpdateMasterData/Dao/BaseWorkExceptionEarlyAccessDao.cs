using Dapper;
using PIC.DB;
using PIC.DB.Dao;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateMasterData.Dao;

public class BaseWorkExceptionEarlyAccessDao : BaseDao
{
    public BaseWorkExceptionEarlyAccessDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 取得 已提早開放人員清單以及非一般使用者 白名單資料
    /// </summary>
    /// <param name="jobDate"></param>
    /// <returns></returns>
    public List<string> GetAllowSendEmpIdList(DateTime jobDate)
    {
        var sql = @"
            select empid from base_work_exception_early_access where @jobDate >= start_date
            union
            select empid from base_employee_m where role != 'user'";
        return da.DapperQuery<string>(sql, new { jobDate }).ToList();
    }



}
