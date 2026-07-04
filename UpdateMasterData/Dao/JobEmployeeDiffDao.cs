using Dapper;
using PIC.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateMasterData.Model.EmployeeDiff;

namespace UpdateMasterData.Dao;

/// <summary>
/// 員工差異匯入表 DAO
/// </summary>
public  class JobEmployeeDiffDao
{
    IDataAccess da;

    public JobEmployeeDiffDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 查詢 資料 
    /// </summary>
    /// <returns></returns>
    public List<JobEmployeeDiffModel> GetJobEmployeeDiffList()
    {
        var sql = @" 
                SELECT jet.* 
                FROM Job_EmployeeDiff_tmp jet 
                join base_employee_m bem on bem.empid = jet.empid
                where jet.diff_status = 'D' or ( jet.diff_status = 'U' and jet.sap_transfer_code in ('B8', 'C1', 'B5'))
            ";
        return da.DapperQuery<JobEmployeeDiffModel>(sql).ToList();
    }


    public Dictionary<string, List<JobEmployeeDiffModel>> GetJobEmployeeDiffMap()
    {
        var sql = @"
        SELECT
            id,
            diff_status,
            empname,
            costid,
            empid,
            ptft,
            sapaname,
            effectivedate,
            expiredate,
            sex,
            sapteamid,
            identification,
            identification_type,
            sap_transfer_code,
            transfer_in_date,
            transfer_out_date,
            realonboarddate
        FROM Job_EmployeeDiff_tmp
        WHERE diff_status IN ('D','U')
        ORDER BY
            empid,
            COALESCE(
                transfer_in_date,
                transfer_out_date,
                effectivedate,
                realonboarddate
            ),
            id;
    ";

        return da.DapperQuery<JobEmployeeDiffModel>(sql)
                 .GroupBy(x => x.Empid!)
                 .ToDictionary(
                     g => g.Key,
                     g => g.ToList());
    }
}
