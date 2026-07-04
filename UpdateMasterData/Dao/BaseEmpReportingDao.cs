using PIC.DB;
using PIC.DB.Dao;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateMasterData.Dao;

/// <summary>
/// 彙報關係主檔 DAO
/// </summary>
public class BaseEmpReportingDao : BaseDao
{
    public BaseEmpReportingDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 刪除 同一報告日期資料
    /// </summary>
    /// <param name="reportDate"></param>
    /// <returns></returns>
    public int DeleteByReportDate(DateTime reportDate)
    {
        var sql = @"DELETE FROM base_emp_reporting WHERE reportdate = @REPORTDATE";
        return da.DapperExecute(sql, new { REPORTDATE = reportDate });
    }

    /// <summary>
    /// 新增 從JobEmpReportingTmp
    /// </summary>
    /// <param name="serverIP"></param>
    /// <param name="creatorIP"></param>
    /// <param name="createUser"></param>
    /// <param name="createTime"></param>
    /// <returns></returns>
    public int InsertFromJobEmpReportingTmp(string serverIP, string creatorIP, string createUser, DateTime createTime)
    {
        var sql = @"INSERT INTO base_emp_reporting (serverip, versionid, createuser, createip, createtime, updateuser, updateip, updatetime, empid, reporting_empid, reportdate)
            SELECT
                @ServerIP, 0, @CreateUser, @CreatorIP,  @CreateTime, @CreateUser, @CreatorIP,  @CreateTime, empid, reporting_empid, reportdate
            FROM job_emp_reporting_tmp";
        var param = new
        {
            ServerIP = serverIP,
            CreatorIP = creatorIP,
            CreateUser = createUser,
            CreateTime = createTime,
        };
        return da.DapperExecute(sql, param);
    }

}
