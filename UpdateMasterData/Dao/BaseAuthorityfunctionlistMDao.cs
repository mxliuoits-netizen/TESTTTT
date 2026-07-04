using Dapper;
using PIC.DB;
using PIC.DB.Dao;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace UpdateMasterData.Dao;

/// <summary>
/// 職稱授權功能項目 DAO
/// </summary>
public class BaseAuthorityfunctionlistMDao : BaseDao
{

    public BaseAuthorityfunctionlistMDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 新增 職稱授權功能項目
    /// </summary>
    /// <param name="aId"></param>
    /// <param name="funcFIDList"></param>
    /// <param name="severIP"></param>
    /// <param name="clientIP"></param>
    /// <param name="clientId"></param>
    /// <returns></returns>
    public int Insert(int aId, List<int> funcFIDList,string severIP, string clientIP, string clientId)
    {
        if (funcFIDList == null || funcFIDList.Count == 0) return 0;

        var sql = @"
            INSERT INTO base_authorityfunctionlist_m
            (serverip, versionid, createuser, createip, createtime, f_id, a_id)
            select 
            @severIP, 0, @clientId, @clientIP, @createTime, f_id, @aId
            from base_function_m 
            where f_id in 
            ";

        var param = new DynamicParameters();
        param.Add("@aId", aId);
        param.Add("@severIP", severIP);
        param.Add("@clientIP", clientIP);
        param.Add("@clientId", clientId);
        param.Add("@createTime", DateTime.Now);

        
        var paramList = funcFIDList.Select((x, i) => new { key = $"@fid" + i, value = x }).ToList();
        sql += "(" + string.Join(",", paramList.Select(x => x.key).ToList()) + ")";
        foreach (var item in paramList)
        {
            param.Add(item.key, item.value);
        }

        return da.DapperExecute(sql, param);
    }

    /// <summary>
    /// 新增 設定功能代碼權限 給所有人員
    /// </summary>
    /// <param name="funcIdList"></param>
    /// <param name="dataFrom"></param>
    /// <returns></returns>
    public int InsertAllEmployeeByFuncId(List<int> funcIdList,string serverIP, string clientIP, string createUser)
    {
        if (funcIdList == null && funcIdList.Count == 0) return 0;
        var sql = @"
            INSERT INTO base_authorityfunctionlist_m
            (serverip, versionid, createuser, createip, createtime, f_id, a_id)
            select 
                @serverip as serverip, 0 as versionid, @createuser as createuser, @createip as createip,
                @createtime as createtime, a.f_id, a.a_id
            from (
                select distinct bem.a_id , bfm.f_id 
                from base_employee_m bem 
                join base_function_m bfm on bfm.f_id in (@inSQL)
                left join base_authorityfunctionlist_m bam on bam.a_id = bem.a_id and bam.f_id =bfm.f_id 
                where bam.a_id is null and bem.role = 'user'
                order by a_id , f_id 
            ) a";

        var param = new DynamicParameters();
        param.Add("@serverip", serverIP);
        param.Add("@createip", clientIP);
        param.Add("@createuser", createUser);
        param.Add("@createtime", DateTime.Now);

        var keyValueList = funcIdList.Select((x, i) => new {key = "@p"+i, value = x}).ToList();
        sql = sql.Replace("@inSQL", string.Join(",", keyValueList.Select(x=> x.key).ToList()));

        foreach(var item in keyValueList)
        {
            param.Add(item.key, item.value);
        }

        return da.DapperExecute(sql, param);
    }

    /// <summary>
    /// 新增 主管功能項目
    /// </summary>
    /// <param name="funcIdList"></param>
    /// <param name="serverIP"></param>
    /// <param name="clientIP"></param>
    /// <param name="createUser"></param>
    /// <returns></returns>
    public int InsertManagerEmployeeByFuncId(List<int> funcIdList, string serverIP, string clientIP, string createUser)
    {
        if (funcIdList == null && funcIdList.Count == 0) return 0;
        var sql = @"
            INSERT INTO base_authorityfunctionlist_m
            (serverip, versionid, createuser, createip, createtime, f_id, a_id)
            select 
                @serverip as serverip, 0 as versionid, @createuser as createuser, @createip as createip,
                @createtime as createtime, a.f_id, a.a_id
            from (
                select distinct bem.a_id , bfm.f_id 
                from base_employee_m bem 
                join base_function_m bfm on bfm.f_id in (@inSQL)
                left join base_authorityfunctionlist_m bam on bam.a_id = bem.a_id and bam.f_id =bfm.f_id 
                where bam.a_id is null
                and bem.empid in (
                    select distinct teamownerid from base_teamowner_m
                )
                order by a_id , f_id 
            ) a";

        var param = new DynamicParameters();
        param.Add("@serverip", serverIP);
        param.Add("@createip", clientIP);
        param.Add("@createuser", createUser);
        param.Add("@createtime", DateTime.Now);

        var keyValueList = funcIdList.Select((x, i) => new { key = "@p" + i, value = x }).ToList();
        sql = sql.Replace("@inSQL", string.Join(",", keyValueList.Select(x => x.key).ToList()));

        foreach (var item in keyValueList)
        {
            param.Add(item.key, item.value);
        }

        return da.DapperExecute(sql, param);
    }

    /// <summary>
    /// 刪除 已非主管移除主管預設功能
    /// </summary>
    /// <param name="funcIdList"></param>
    /// <returns></returns>
    public int DeleteNotManagerEmpid(List<int> funcIdList)
    {
        var sql = @"
            delete from base_authorityfunctionlist_m 
            where f_id in (@inSQL)
            and a_id in (
                select a_id from base_employee_m where empid in (
                    select 
	                    distinct btb.teamownerid 
                    from base_teamowner_bak btb
                    left join base_teamowner_m btm on btb.teamownerid = btm.teamownerid
                    where btb.backuptime = (select max(backuptime) from base_teamowner_bak)
                    and btm.teamownerid is null
                )
            ) ";

        var param = new DynamicParameters();

        var keyValueList = funcIdList.Select((x, i) => new { key = "@p" + i, value = x }).ToList();
        sql = sql.Replace("@inSQL", string.Join(",", keyValueList.Select(x => x.key).ToList()));

        foreach (var item in keyValueList)
        {
            param.Add(item.key, item.value);
        }

        return da.DapperExecute(sql, param);
    }






}
