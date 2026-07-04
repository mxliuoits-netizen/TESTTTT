using PIC.DB;
using PIC.DB.Dao;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateMasterData.Dao;

/// <summary>
/// 工作彙報關係 DAO
/// </summary>
public class JobEmpReportingDao : BaseDao
{
    public JobEmpReportingDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 備份 同一報告日期資料
    /// </summary>
    /// <param name="reportDate"></param>
    /// <returns></returns>
    public int BackupBaseEmpReporting(DateTime reportDate)
    {
        var sql = @"
            INSERT INTO job_emp_reporting_bak(serverip, versionid, createuser, createip, createtime, updateuser, updateip, updatetime, empid, reporting_empid, reportdate, backuptime)
            SELECT
                ServerIP, VersionId, CreateUser ,CreateIP, CreateTime, UpdateUser, UpdateIP, UpdateTime, EmpId, REPORTING_EmpId, REPORTDATE, @backuptime
            FROM Base_Emp_Reporting
            WHERE REPORTDATE = @REPORTDATE";
        return da.DapperExecute(sql, new { backuptime = DateTime.Now, REPORTDATE = reportDate });
    }


}
