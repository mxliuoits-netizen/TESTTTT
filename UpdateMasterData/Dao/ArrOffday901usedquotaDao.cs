using Dapper;
using PIC.DB;
using PIC.DB.Dao;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateMasterData.Model.Quota;

namespace UpdateMasterData.Dao;

public class ArrOffday901usedquotaDao : BaseDao
{
    public ArrOffday901usedquotaDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 重建 代休使用紀錄
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public int Rebuild(List<ArrOffday901usedquotaModel> list)
    {
        if (list == null || list.Count == 0) return 0;

        var deleteSql = "delete from arr_offday_901usedquota where leaveid in ";
        var idList = list.Select(x=>x.Leaveid).Distinct().ToList();
        var param = new DynamicParameters();
        deleteSql = this.BuildInSQL(idList, param, x => deleteSql + $"({x})");
        da.DapperExecute(deleteSql, param);

        var sql = @" INSERT INTO arr_offday_901usedquota(leaveid, usedatefrom, usedateto, usehours, isdelete, createtime, updatetime) 
            VALUES (@leaveid, @usedatefrom, @usedateto, @usehours, @isdelete, @createtime, @updatetime)";
        return da.DapperExecute(sql, list);
    }




}
