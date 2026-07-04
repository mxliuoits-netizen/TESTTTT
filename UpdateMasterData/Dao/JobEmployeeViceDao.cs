using Dapper;
using PIC.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateMasterData.Model.EmployeeVice;

namespace UpdateMasterData.Dao;

/// <summary>
/// 員工主檔匯入暫存檔 DAO
/// </summary>
public class JobEmployeeViceDao
{
    IDataAccess da;

    public JobEmployeeViceDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 更新 非白名單不匯入
    /// </summary>
    /// <param name="whiteList"></param>
    /// <returns></returns>
    public int UpdateByWhiteList(List<string> whiteList)
    {
        var sql = @" update job_employeevice_tmp set is_import = 'N' where  ";
        var param = new DynamicParameters();
        if(whiteList.Count > 0)
        {
            var i = 0;
            foreach (var item in whiteList)
            {
                param.Add("@p" + i++, item);
            }
            sql += " group_type not in (" + string.Join(",", param.ParameterNames.Select(x => "@" + x)) + ") ";
        }
        else
        {
            sql += " 1=2 ";
        }           
        return da.DapperExecute(sql, param);
    }

    /// <summary>
    /// 取得 匯入暫存表
    /// </summary>
    /// <returns></returns>
    public List<JobEmployeeViceModel> GetJobEmployeeViceTmpList()
    {
        var sql = @" SELECT * FROM Job_EmployeeVice_tmp where is_import = 'Y' ";
        return da.DapperQuery<JobEmployeeViceModel>(sql).ToList();
    }

    /// <summary>
    /// 合併 忽略的Employee(exception_m or role=admin) 至 匯入暫存表
    /// </summary>
    /// <param name="empIdList"></param>
    /// <returns></returns>
    public int MergeByExceptionEmployee()
    {
        var sql = @"
                MERGE INTO Job_EmployeeVice_tmp AS tgt
                USING (
                    select EmpName, EmpId, 'FT' as PTFT, SapAname AS SapAname, Effectivedate, ExpireDate, 'M' as Sex, TeamId as SapTeamId, Effectivedate as RealOnBoardDate, 
                    '9999999999' as costid, hr_zone, email, '999' as group_type
                    from base_employee_exception_m 
                ) AS src
                ON (tgt.EmpId = src.EmpId)
                WHEN MATCHED
                THEN UPDATE SET
                    EmpName=src.EmpName, EmpId=src.EmpId, PTFT=src.PTFT, SapAname=src.SapAname, Effectivedate=src.Effectivedate, ExpireDate=src.ExpireDate, 
                    Sex=src.Sex, SapTeamId=src.SapTeamId, RealOnBoardDate=src.RealOnBoardDate, costid = src.costid, hr_zone = src.hr_zone, email = src.email, group_type = src.group_type
                WHEN NOT MATCHED
                THEN INSERT (
                    EmpName, EmpId, PTFT, SapAname, Effectivedate, ExpireDate, Sex, SapTeamId, RealOnBoardDate, costid, hr_zone, email, group_type
                ) VALUES (
                    src.EmpName, src.EmpId, src.PTFT, src.SapAname, src.Effectivedate, src.ExpireDate, src.Sex, src.SapTeamId, src.RealOnBoardDate, src.costid,
                    src.hr_zone, src.email, src.group_type
                )";
        return da.DapperExecute(sql);
    }





}
