using PIC.DB;
using PIC.DB.Dao;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateMasterData.Dao;

/// <summary>
/// 單位主管表 DAO
/// </summary>
public class BaseTeamOwnerDao : BaseDao
{
    public BaseTeamOwnerDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 備份 主檔資料
    /// </summary>
    /// <returns></returns>
    public int BackupBaseTeamOwnerM()
    {
        var sql = @"
            INSERT INTO base_teamowner_bak
                (serverip, versionid, createuser, createip, createtime, updateuser, updateip, updatetime, teamid, teamownerid, teamownername, 
                agentid, agentname, agentstarttime, agentendtime, backuptime)
            SELECT 
                serverip, versionid, createuser, createip, createtime, updateuser, updateip, updatetime, teamid, teamownerid, teamownername, 
                agentid, agentname, agentstarttime, agentendtime, @backupTime
            FROM base_teamowner_m";
        return da.DapperExecute(sql, new { backupTime = DateTime.Now });
    }

    /// <summary>
    /// 清空 暫存表資料
    /// </summary>
    /// <returns></returns>
    public int TruncateBaseTeamOwnerTmp()
    {
        var sql = @" truncate table base_teamowner_tmp ";
        return da.DapperExecute(sql);
    }

    /// <summary>
    /// 清空 主表資料
    /// </summary>
    /// <returns></returns>
    public int TruncateBaseTeamOwnerM()
    {
        var sql = @" truncate table base_teamowner_m ";
        return da.DapperExecute(sql);
    }

    /// <summary>
    /// 匯入 彙報關係表彙整資料 至 暫存表
    /// </summary>
    /// <param name="reportDate"></param>
    /// <param name="serverIP"></param>
    /// <param name="createUser"></param>
    /// <param name="createIP"></param>
    /// <param name="createTime"></param>
    /// <returns></returns>
    public int ImportEmpReportingToBaseTeamOwnerTmp(DateTime reportDate, string serverIP, string createUser, string createIP, DateTime createTime)
    {
        var sql = @"
            with tmp_source as (
                SELECT 
                    a.*
                FROM (
                    SELECT 
                        emp.teamid, teamowner.empid as teamownerid, teamowner.empname as teamownername
                    FROM ( 
                        SELECT * FROM base_emp_reporting WHERE reportdate = @reportdate 
                    ) rep 
                    LEFT JOIN base_employee_m emp ON rep.empid = emp.empid 
                    LEFT JOIN base_employee_m teamowner ON rep.reporting_empid = teamowner.empid            
                    GROUP BY emp.teamid, teamowner.empid, teamowner.empname
                    order by teamid
                ) a where teamid is not null
                union --額外設定Teamowner表
                select bte.teamid, bte.empid as teamownerid, emp.empname as teamownername
                from base_teamowner_extra bte
                join base_employee_m emp ON bte.empid = emp.empid
                where bte.isactive = 'Y' and @reportDate between bte.startdate and bte.enddate
            )
            INSERT INTO base_teamowner_tmp
                (serverip, versionid, createuser, createip, createtime, updateuser, updateip, updatetime, teamid, teamownerid, teamownername)
            SELECT 
                @serverip as serverip, 0 as versionid, @createuser as createuser, @createip as createip, @createtime as createtime, 
                @createuser as updateuser, @createip as updateip, @createtime as updatetime, a.*
            FROM tmp_source a ";

        var param = new
        {
            serverip = serverIP,
            createuser = createUser,
            createip = createIP,
            createtime = createTime,
            reportdate = reportDate
        };

        return da.DapperExecute(sql, param);
    }

    /// <summary>
    /// 新增 職稱白名單對應之主管資料
    /// </summary>
    /// <param name="serverIP"></param>
    /// <param name="createUser"></param>
    /// <param name="createIP"></param>
    /// <param name="createTime"></param>
    /// <returns></returns>
    public int ImportWhiteANameToBaseTeamOwnerTmp(DateTime reportDate, string serverIP, string createUser, string createIP, DateTime createTime)
    {
        var sql = @"
             INSERT INTO base_teamowner_tmp
                (serverip, versionid, createuser, createip, createtime, updateuser, updateip, updatetime, teamid, teamownerid, teamownername)
            select
                @serverip as serverip, 0 as versionid, @createuser as createuser, @createip as createip, @createtime as createtime, 
                @createuser as updateuser, @createip as updateip, @createtime as updatetime,
                bem.teamid, bem.empid as teamownerid, bem.empname as teamownername
            from base_employee_m bem
            join base_teamowner_white_list btwl on btwl.a_name = bem.a_name
            where @reportdate between btwl.start_date and btwl.end_date
            --過濾黑名單人員
            and bem.empid not in (
                select empid from base_teamowner_black_list a
                where @reportdate between a.start_date and a.end_date
            )
            --排除已在匯入暫存檔
            and(
                select count(1) from base_teamowner_tmp btt
                where btt.teamownerid = bem.empid and btt.teamid = bem.teamid
            ) = 0";

        var param = new
        {
            serverip = serverIP,
            createuser = createUser,
            createip = createIP,
            createtime = createTime,
            reportdate = reportDate
        };

        return da.DapperExecute(sql, param);
    }

    /// <summary>
    /// 匯入 主管當Jobcode=00000131且該員工單位不為自己單位主管時 設為主管
    /// </summary>
    /// <param name="serverIP"></param>
    /// <param name="createUser"></param>
    /// <param name="createIP"></param>
    /// <param name="createTime"></param>
    /// <returns></returns>
    public int ImportJobCode00000131(string serverIP, string createUser, string createIP, DateTime createTime)
    {
        var sql = @"
            INSERT INTO base_teamowner_tmp
                (serverip, versionid, createuser, createip, createtime, updateuser, updateip, updatetime, teamid, teamownerid, teamownername)
            select
                @serverip as serverip, 0 as versionid, @createuser as createuser, @createip as createip, @createtime as createtime, 
                @createuser as updateuser, @createip as updateip, @createtime as updatetime,
                bem.teamid, bem.empid as teamownerid, bem.empname as teamownername
            from base_employee_m bem 
            where bem.isactive = 'Y' and @now between bem.effectivedate and bem.expiredate and bem.job_code = '00000131' 
                and not exists (select 1 from base_teamowner_tmp btt where btt.teamid = bem.teamid and btt.teamownerid = bem.empid)
        ";

        var param = new
        {
            serverip = serverIP,
            createuser = createUser,
            createip = createIP,
            createtime = createTime,
            now = DateTime.Now
        };

        return da.DapperExecute(sql, param);
    }

    /// <summary>
    /// 匯入 暫存表 至 主表
    /// </summary>
    /// <param name="reportDate"></param>
    /// <param name="serverIP"></param>
    /// <param name="createUser"></param>
    /// <param name="createIP"></param>
    /// <param name="createTime"></param>
    /// <returns></returns>
    public int ImportBaseTeamOwnerTmpToBaseTeamOwnerM()
    {
        var sql = @"
            INSERT INTO base_teamowner_m
                (serverip, versionid, createuser, createip, createtime, updateuser, updateip, updatetime, teamid, teamownerid, teamownername)
            SELECT 
                serverip, versionid, createuser, createip, createtime, updateuser, updateip, updatetime, teamid, teamownerid, teamownername
            FROM base_teamowner_tmp ";

        return da.DapperExecute(sql);
    }

    /// <summary>
    /// 取得 與最新匯報紀錄比對不再是任何單位主管的員工編號清單
    /// </summary>
    /// <returns></returns>
    public List<string> GetEmpIdWhoNotManagerList()
    {
        var sql = @"
            select 
	            distinct btb.teamownerid 
            from base_teamowner_bak btb
            left join base_teamowner_m btm on btb.teamownerid = btm.teamownerid
            where btb.backuptime = (select max(backuptime) from base_teamowner_bak)
            and btm.teamownerid is null ";

        return da.DapperQuery<string>(sql).ToList();
    }


}
