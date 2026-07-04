using PIC.DB;
using PIC.DB.Dao;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateMasterData.Model.EmployeeDiff;
using UpdateMasterData.Model.EmployeeVice;

namespace UpdateMasterData.Dao;

/// <summary>
/// 員工職位異動表
/// </summary>
public class BaseEmployeeTransferDao : BaseDao
{

    public BaseEmployeeTransferDao(IDataAccess da) { 
        this.da = da;
    }

    /// <summary>
    /// 取得 有離職日期並且尚未處理資料
    /// </summary>
    /// <returns></returns>
    public List<BaseEmployeeTransferTmpModel> GetOutNotDoneList()
    {
        var sql = @" 
            select 
                empid, max(transfer_out_date) as transfer_out_date
            from base_employee_transfer_tmp 
            where flag_done = 'N' and transfer_out_date is not null
            group by empid
            order by transfer_out_date
        ";
        return da.DapperQuery<BaseEmployeeTransferTmpModel>(sql, new { NowDate = DateTime.Now }).ToList();
    }

    /// <summary>
    /// 更新 有離職日期並且尚未處理資料 為 已完成
    /// </summary>
    /// <returns></returns>
    public int UpdateFlagDoneSuccess()
    {
        var sql = @" 
                update base_employee_transfer_tmp set flag_done = 'Y' 
                where flag_done = 'N' and transfer_out_date is not null ";
        return da.DapperExecute(sql, new { NowDate = DateTime.Now });
    }

    /// <summary>
    /// 更新 未處理資料 為 失敗
    /// </summary>
    /// <returns></returns>
    public int UpdateFlagDoneFail()
    {
        var sql = @" update base_employee_transfer_tmp set flag_done = 'F' where flag_done ='N' ";
        return da.DapperExecute(sql, new { NowDate = DateTime.Now });
    }


    /// <summary>
    /// 合併 更新員工職位異動
    /// </summary>
    /// <param name="jobEmployeeList"></param>
    /// <param name="serverip"></param>
    /// <param name="createuser"></param>
    /// <param name="createip"></param>
    /// <returns></returns>
    public int MergeDiff(List<JobEmployeeDiffModel> jobEmployeeList, string serverip, string createuser, string createip)
    {
        var sql = @"
                MERGE INTO base_employee_transfer_tmp AS tgt
                USING (
                    VALUES @Data 
                ) AS src (
                    serverip, createuser, createip, createtime, 
                    diff_status, empname, costid, empid, ptft, sapaname, effectivedate, expiredate, sex, sapteamid, 
                    identification, identification_type, sap_transfer_code, transfer_in_date, transfer_out_date, 
                    realonboarddate
                ) 
                ON (
                    tgt.diff_status = src.diff_status and tgt.empid = src.empid and tgt.sap_transfer_code = src.sap_transfer_code 
                    and (
                        tgt.transfer_in_date = src.transfer_in_date::date 
                        or coalesce(tgt.transfer_in_date ,'1900-01-01'::date) = coalesce(src.transfer_in_date::date,'1900-01-01'::date)
                    )
                    and (
                        tgt.transfer_out_date = src.transfer_out_date::date
                        or coalesce(tgt.transfer_out_date ,'1900-01-01'::date) = coalesce(src.transfer_out_date::date,'1900-01-01'::date)
                    )
                    and flag_done = 'N'
                )
                WHEN NOT MATCHED
                THEN INSERT (
                    serverip, versionid, createuser, createip, createtime, updateuser, updateip, updatetime, 
                    diff_status, empname, costid, empid, ptft, sapaname, effectivedate, expiredate, 
                    sex, sapteamid, identification, identification_type, sap_transfer_code, transfer_in_date, 
                    transfer_out_date, realonboarddate, flag_done
                )
                VALUES (
                    src.serverip, 0, src.createuser, src.createip, src.createtime, src.createuser, src.createip, src.createtime, 
                    src.diff_status, src.empname, src.costid, src.empid, src.ptft, src.sapaname, src.effectivedate::date, src.expiredate::date,
                    src.sex, src.sapteamid, src.identification, src.identification_type, src.sap_transfer_code, src.transfer_in_date::date, 
                    src.transfer_out_date::date, src.realonboarddate::date, 'N'
                )";

        var paramList = jobEmployeeList.Select(x => new
        {
            serverip,
            createuser,
            createip,
            createtime = DateTime.Now,
            x.DiffStatus,
            x.Empname,
            x.Costid,
            x.Empid,
            x.Ptft,
            x.Sapaname,
            x.Effectivedate,
            x.Expiredate,
            x.Sex,
            x.Sapteamid,
            x.Identification,
            x.IdentificationType,
            x.SapTransferCode,
            x.TransferInDate,
            x.TransferOutDate,
            x.Realonboarddate
        });

        return this.BatchExecute(sql, paramList, combineSql: (executeSql, valuesSql) => sql.Replace("@Data", valuesSql));
    }
}
