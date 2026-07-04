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
/// 員工調入調出履歷表 DAO
/// </summary>
public class BaseEmployeeTransferHistoryDao : BaseDao
{
    public BaseEmployeeTransferHistoryDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 合併 員工主檔調入資料
    /// </summary>
    /// <param name="jobEmployeeList"></param>
    /// <param name="serverip"></param>
    /// <param name="createuser"></param>
    /// <param name="createip"></param>
    /// <returns></returns>
    public int MergeTransferIn(List<JobEmployeeViceModel> jobEmployeeList, string serverip, string createuser, string createip)
    {
        // 取出 到職/復職/調入日 資料
        var dataList = jobEmployeeList.Where(x => x.TransferDate != null && x.Empid != null)
            .Select(x => new { x.Empid, x.TransferDate }).Distinct().ToList();
        var nullDt = DateTime.Parse("9999-12-31");
        var now = DateTime.Now;

        //如果調入資料 對應 emp_id & (transfer_date相同 或 end_transfer_date='9999-12-31') 無對應 則 新增, 有對應 則 當調入日期不同時更新
        var sql = @"
            MERGE INTO base_employee_transfer_history AS tgt
            USING (
                VALUES @Data
            ) AS src(emp_id, transfer_date)
            ON (tgt.emp_id = src.emp_id and (tgt.start_transfer_date = src.transfer_date or tgt.end_transfer_date = @nullDt))
            WHEN MATCHED AND tgt.start_transfer_date != src.transfer_date
                THEN UPDATE SET
                    start_transfer_date = src.transfer_date, updateuser = @createuser, updateip = @createip, updatetime = @createtime
            WHEN NOT MATCHED
            THEN INSERT (serverip, versionid, createuser, createip, createtime, emp_id, start_transfer_date, end_transfer_date)
            VALUES (@serverip, 0, @createuser, @createip, @createtime, src.emp_id, src.transfer_date, @nullDt)";

        var i = this.BatchExecute(sql, dataList, 
            combineSql:(sql, paramSql) => { 
                return sql.Replace("@Data", paramSql);
            },
            addParamFunc:(param) =>
            {
                param.Add("@serverip", serverip);
                param.Add("@createuser", createuser);
                param.Add("@createip", createip);
                param.Add("@createtime", now);
                param.Add("@nullDt", nullDt);
            });
        return i;
    }

    /// <summary>
    /// 合併 員工主檔調出資料
    /// </summary>
    /// <param name="jobEmployeeList"></param>
    /// <param name="serverip"></param>
    /// <param name="createuser"></param>
    /// <param name="createip"></param>
    /// <returns></returns>
    public int MergeTransferOut(List<BaseEmployeeModel> jobEmployeeList, string serverip, string createuser, string createip)
    {
        var dataList = jobEmployeeList.Select(x => new { x.Empid, TransferOutDate = x.TransferUpdateTime }).Distinct().ToList();
        var nullDt = DateTime.Parse("9999-12-31");
        var now = DateTime.Now;

        //如果調出資料 對應 emp_id & max(end_transfer_date) 有對應 則 更新調出日
        var sql = @"
            MERGE INTO base_employee_transfer_history AS tgt
            USING (
                VALUES @Data
            ) AS src(emp_id, transfer_date)
            ON (tgt.emp_id = src.emp_id and tgt.end_transfer_date = (select max(end_transfer_date) from base_employee_transfer_history where emp_id = src.emp_id))
            WHEN MATCHED AND (tgt.end_transfer_date = @nullDt or tgt.end_transfer_date != src.transfer_date)
                THEN UPDATE SET
                    end_transfer_date = src.transfer_date, updateuser = @createuser, updateip = @createip, updatetime = @createtime";

        var i = this.BatchExecute(sql, dataList,
            combineSql: (sql, paramSql) => {
                return sql.Replace("@Data", paramSql);
            },
            addParamFunc: (param) =>
            {
                param.Add("@serverip", serverip);
                param.Add("@createuser", createuser);
                param.Add("@createip", createip);
                param.Add("@createtime", now);
                param.Add("@nullDt", nullDt);
            });
        return i;
    }




}
