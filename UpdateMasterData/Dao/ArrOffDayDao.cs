using PIC.DB;
using PIC.DB.Dao;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateMasterData.Model;

namespace UpdateMasterData.Dao;

/// <summary>
/// 結算剩餘假明細表 DAO
/// </summary>
public class ArrOffDayDao : BaseDao
{

    public ArrOffDayDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 備份資料
    /// </summary>
    /// <returns></returns>

    public int Backup()
    {
        var sql = @"INSERT INTO arr_offday_bak
                (serverip, createuser, createip, createtime, sap_uk, empid, wt_id, datecurrent, datefrom, dateto, usedatefrom, usedateto, saprestnumber, 
                tkrestnumber, restnumber, note, backuptime)
                select 
                    serverip, createuser, createip, createtime, sap_uk, empid, wt_id, datecurrent, datefrom, dateto, usedatefrom, usedateto, saprestnumber, 
                    tkrestnumber, restnumber, note, now()
                from arr_offday_t";
        return da.DapperExecute(sql);
    }

    /// <summary>
    /// 新增資料
    /// </summary>
    /// <param name="logData"></param>
    /// <returns></returns>
    public int MergeByArrOffDayRecordT(JobMetaDataModel logData, DateTime dateCurrent)
    {
        var maxDateCurrent = da.DapperQuery<DateTime?>("select max(datecurrent) from arr_offdayrecord_t").FirstOrDefault();

        var count = 0;

        // 新增 非代休
        var sql = @"
                MERGE INTO arr_offday_t AS tgt
                USING (
                    select empid, datefrom, dateto, wt_id, sap_uk, totalnumber, usednumber, restnumber, datecurrent, note
                    from arr_offdayrecord_t
                    where sap_uk in (select sap_uk from job_quota_tmp) and wt_id is not null and sapid not in ('12','13','14','15')
                ) AS src
                ON (tgt.datecurrent = src.datecurrent and tgt.sap_uk = src.sap_uk and tgt.empid = src.empid)
                WHEN MATCHED
                THEN UPDATE SET
                    sap_uk=src.sap_uk, empid=src.empid, wt_id=src.wt_id, datecurrent=src.datecurrent, datefrom=src.datefrom, dateto=src.dateto, useDateFrom=src.datefrom, useDateTo=src.dateto,  
                    saprestnumber = src.totalnumber, tkrestnumber=src.usednumber, restnumber=src.restnumber, note=src.note
                WHEN NOT MATCHED
                THEN INSERT (
                    serverip, createuser, createip, createtime, sap_uk, empid, wt_id, datecurrent, datefrom, dateto, usedatefrom, usedateto,
                    saprestnumber, tkrestnumber, restnumber, note
                ) VALUES (
                    @serverip, @createuser, @createip, @createtime, src.sap_uk, src.empid, src.wt_id, src.datecurrent, src.datefrom, src.dateto, src.datefrom, src.dateto,
                    src.totalnumber, src.usednumber, src.restnumber, src.note
                )";

        var param = new
        {
            serverip = logData.ServerIp,
            createuser = logData.CreateUser,
            createip = logData.CreateIp,
            createtime = logData.CreateTime,
            jobDate = maxDateCurrent ?? dateCurrent
        };

        count += da.DapperExecute(sql, param);

        // 新增 代休類(sapid in 12~15) 加總計算剩餘假以及時間區間
        sql = @"
                MERGE INTO arr_offday_t AS tgt
                USING (
                    select empid, datefrom, dateto, '00901' as wt_id, 
                    max(sap_uk) as sap_uk, sum(totalnumber) as totalnumber, sum(usednumber) as usednumber, sum(restnumber) as restnumber, datecurrent, 'SAP' as note
                    from arr_offdayrecord_t
                    where datecurrent = @jobDate and sapid in ('12', '13', '14', '15')
                    group by empid, datefrom, dateto, datecurrent
                ) AS src
                ON (tgt.datecurrent = src.datecurrent and tgt.empid = src.empid and tgt.wt_id = src.wt_id and tgt.datefrom = src.datefrom and tgt.dateto = src.dateto)
                WHEN MATCHED
                THEN UPDATE SET
                    sap_uk=src.sap_uk, empid=src.empid, wt_id=src.wt_id, datecurrent=src.datecurrent, datefrom=src.datefrom, dateto=src.dateto, useDateFrom=src.datefrom, useDateTo=src.dateto,  
                    saprestnumber = src.totalnumber, tkrestnumber=src.usednumber, restnumber=src.restnumber, note=src.note
                WHEN NOT MATCHED
                THEN INSERT (
                    serverip, createuser, createip, createtime, sap_uk, empid, wt_id, datecurrent, datefrom, dateto, usedatefrom, usedateto,
                    saprestnumber, tkrestnumber, restnumber, note
                ) VALUES (
                    @serverip, @createuser, @createip, @createtime, src.sap_uk, src.empid, src.wt_id, src.datecurrent, src.datefrom, src.dateto, src.datefrom, src.dateto,
                    src.totalnumber, src.usednumber, src.restnumber, src.note
                )";

        count += da.DapperExecute(sql, param);
        return count;
    }

    /// <summary>
    /// 取得 滾動後新增代休假清單(尚未扣除請假)
    /// * 類似邏輯 於fn_apply_getoffdayleave 以及 查詢剩餘假功能 
    /// * 有類似區段SQL(tmp_query_data, tmp_overtime_to_ip, tmp_overtime_cancel_ip,tmp_cancel_leave_00901)
    /// </summary>
    /// <returns></returns>
    /*public List<Cal00901ArrOffdayT> GetIncrease00901List(DateTime jobDate)
    {
        var sql = @"
with tmp_emp as (
	select 
		bem.empid, 
		@jobDate as job_datecurrent,
		coalesce(bem.realonboarddate, bem.effectivedate) as realonboarddate,
		(DATE_PART('year', @jobDate) - DATE_PART('year',realonboarddate))::integer as addyear,
		--onboarddate: 02/29問題 需要用年份差異加至JOB執行年度
		(
			bem.realonboarddate + 
			(DATE_PART('year', @jobDate) - DATE_PART('year',realonboarddate)) * interval '1 year'
		)::date as year_onboard_date,
        coalesce(
			(select max(datecurrent) from arr_offday_t od 
			where od.empid = bem.empid and od.wt_id = '00901' and od.datecurrent <= @jobDate),
			(@jobDate - interval '1 months')::date
		) as max_datecurrent_00901 
	from base_employee_m bem 
)
, tmp_query_data as ( --各類結算日計算
	select 
		emp.empid, emp.job_datecurrent, emp.realonboarddate, emp.max_datecurrent_00901,
		--前一年到職日起迄日期範圍(jobdate:2025-01-01, onboarddate:12-30 => 2023-12-30~2024-12-29)
		(year_onboard_date + interval '-2 year')::date as emp_lastyear_start,
		(year_onboard_date + interval '-1 year')::date - 1 as emp_lastyear_end,
		--當前到職日起迄日期範圍(jobdate:2025-06-01, onboarddate:06-30 => 2024-06-30~2025-06-29)
		(year_onboard_date + interval '-1 year')::date as emp_year_start,
		year_onboard_date ::date - 1 as emp_year_end,
		--次年到職日起迄日期範圍(jobdate:2025-12-01, onboarddate:01-30 => 2025-01-30~2026-01-29)
		year_onboard_date ::date as emp_nextyear_start,
		(year_onboard_date + interval '1 year')::date - 1 as emp_nextyear_end,
		--當前最大版次 最近結算日 ( jobdate: 2024-12-01, 當前最大版次(max_datecurrent_00901): 2024-11-01)
        coalesce (
			(select max(a.checktime) from settle_checkpoint_t a 
            where a.filetype in ('Leave', 'OvertimeDetail') and a.checktime <= emp.max_datecurrent_00901),
			emp.max_datecurrent_00901::timestamp, 
			(emp.job_datecurrent - interval '1 months')::timestamp
		) as max_last_checktime,
        --本次版次 最近結算日 (jobdate: 2024-12-01)
        coalesce (
			(select max(a.checktime) from settle_checkpoint_t a 
            where a.filetype in ('Leave', 'OvertimeDetail') and a.checktime <= emp.job_datecurrent),
			emp.job_datecurrent::timestamp
		) as max_checktime
	from tmp_emp emp
)
, tmp_overtime_to_ip as ( --取得已核可的代休時數(+)
	SELECT
        o.empid, '00901' as wt_id, tqd.job_datecurrent as datecurrent,
        o.overdate as datefrom,
        case 
	    	when o.overdate between tqd.emp_lastyear_start and tqd.emp_lastyear_end
	        then tqd.emp_lastyear_end
	        when o.overdate between tqd.emp_year_start and tqd.emp_year_end
	        then tqd.emp_year_end
	        when o.overdate between tqd.emp_nextyear_start and tqd.emp_nextyear_end
	        then tqd.emp_nextyear_end
        end as dateto,
        o.checkhours AS restnumber
    FROM apply_overtimerecord_t o
    JOIN tmp_query_data tqd ON o.empid = tqd.empid
    WHERE o.updatetime >= tqd.max_last_checktime and o.updatetime < tqd.max_checktime
    AND o.note = 'IP' AND o.applytype = 'actual' AND o.applystatus = 'finish' AND o.isdelete = 'N'
)
, tmp_overtime_cancel_ip as (-- 取得已銷單的代休時數(-)
	SELECT
        o.empid, '00901' as wt_id, tqd.job_datecurrent as datecurrent,
        o.overdate as datefrom,
        case 
	    	when o.overdate between tqd.emp_lastyear_start and tqd.emp_lastyear_end
	        then tqd.emp_lastyear_end
	        when o.overdate between tqd.emp_year_start and tqd.emp_year_end
	        then tqd.emp_year_end
	        when o.overdate between tqd.emp_nextyear_start and tqd.emp_nextyear_end
	        then tqd.emp_nextyear_end
        end as dateto,
        o.checkhours * -1 AS restnumber
    FROM apply_overtimerecord_t o
    JOIN tmp_query_data tqd ON o.empid = tqd.empid
    WHERE o.updatetime >= tqd.max_last_checktime and o.updatetime < tqd.max_checktime
    AND o.note = 'IP' AND o.applytype = 'cancel' AND o.applystatus = 'finish' AND o.isdelete = 'N' 
)
, tmp_cancel_leave_00901 as ( --銷假已結算的代休假(+)
	select
		alt.empid, alt.wt_id, tqd.job_datecurrent as datecurrent,
		ao9.usedatefrom as datefrom,
        ao9.usedateto as dateto,
		ao9.usehours AS restnumber
    FROM apply_leaverecord_t alt 
	JOIN tmp_query_data tqd ON alt.empid = tqd.empid
	join arr_offday_901usedquota ao9 on alt.cancelapplyid = ao9.leaveid
	where alt.updatetime >= tqd.max_last_checktime and alt.updatetime < tqd.max_checktime
    AND alt.applytype = 'cancel' AND alt.applystatus = 'finish'  AND alt.isdelete = 'N'
    AND alt.wt_id = '00901'
)
, tmp_settle_00901 as (
	select tqd.empid, wt_id, tqd.job_datecurrent as datecurrent, aot.datefrom, aot.dateto, aot.restnumber
	from tmp_query_data tqd
	join arr_offday_t aot on aot.empid= tqd.empid and aot.wt_id = '00901' and aot.datecurrent = tqd.max_datecurrent_00901
)
, tmp_all_data as (
	select 'arr_offdat_t' as name,* from tmp_settle_00901
	union all
	select 'apply_overtime' as name,* from tmp_overtime_to_ip
	union all
	select 'cancel_overtime' as name,* from tmp_overtime_cancel_ip
	union all
	select 'cancel_leave' as name, * from tmp_cancel_leave_00901
)
select 
	empid, wt_id, datecurrent, datefrom, dateto, sum(restnumber) as restnumber
from tmp_all_data
group by empid, wt_id, datecurrent, datefrom, dateto
order by empid, datefrom, dateto
";
        return da.DapperQuery<Cal00901ArrOffdayT>(sql, new { jobDate }, commandTimeout: 0).ToList();
    }*/

    /// <summary>
    /// 新增 代休結算資料
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    /*public int Insert00901List(List<Cal00901ArrOffdayT> list)
    {
        var sql = @"
            INSERT INTO arr_offday_t(
                serverip, createuser, createip, createtime, empid, wt_id, datecurrent, datefrom, dateto, usedatefrom, usedateto,
                saprestnumber, tkrestnumber, restnumber, note
            ) VALUES (
                @serverip, @createuser, @createip, @createtime, @empid, @wtid, @datecurrent, @datefrom, @dateto, @datefrom, @dateto,
                @restnumber, @restnumber, @restnumber, @note
            )";

        return da.DapperExecute(sql, list);
    }*/

    /// <summary>
    /// 刪除 JobDate代休結算資料
    /// </summary>
    /// <param name="jobDate"></param>
    /// <returns></returns>
    public int DeleteNeedRecal00901Data(DateTime dateCurrent)
    {
        var maxDateCurrent = da.DapperQuery<DateTime?>("select max(datecurrent) from arr_offdayrecord_t").FirstOrDefault();

        var sql = @" delete from arr_offday_t where wt_id = '00901' and datecurrent = @jobDate 
            and empid in (select distinct empid from job_quota_tmp where sapid in ('12', '13', '14', '15'))";
        return da.DapperExecute(sql, new { jobDate = maxDateCurrent ?? dateCurrent });
    }

    /*
    public int FillDefaultby00901(DateTime jobDate, JobMetaDataModel logData)
    {
        var sql = @"

with tmp_config as (
	select to_char(
		((to_char(now(),'yyyy') || '-02-28')::date + interval '1 days'),
		'dd'
	) = '29' as isleapyear
)
, tmp_employee_m as (
	SELECT 
		empid, empname,
		case
			when (select isleapyear from tmp_config) = false and to_char(effectivedate,'MMdd') = '0229' 
			then (to_char(@JobDate::date,'yyyy') || to_char(effectivedate + interval '1 days' ,'-MM-dd') )::date 
			else (to_char(@JobDate::date,'yyyy') || to_char(effectivedate,'-MM-dd'))::date
		end as effectivedate
	FROM base_employee_m WHERE ""role"" = 'user' and isactive = 'Y' AND expiredate >= @JobDate::date 
)
, tmp_shift_effectivedate as (
	select 
		empid, empname,
		case 
			when effectivedate < @JobDate then effectivedate
			else effectivedate - interval '1 years'
		end as effectivedate		
	from tmp_employee_m
)
INSERT INTO arr_offday_t(
    serverip, createuser, createip, createtime, empid, wt_id, datecurrent, datefrom, dateto, usedatefrom, usedateto,
    saprestnumber, tkrestnumber, restnumber, note
)
SELECT 
    @serverip as serverip, @createuser as createuser, @createip as createip, @createtime as createtime,
    e.empid, '00901' as wt_id, @JobDate as datecurrent, 
    e.effectivedate as datefrom,  e.effectivedate + interval '1 years' - interval '1 days' as dateto,
    e.effectivedate as usedatefrom,  e.effectivedate  + interval '1 years' - interval '1 days' as usedateto,
    0 as saprestnumber,0 as tkrestnumber,0 as restnumber, null as note
FROM tmp_shift_effectivedate e
LEFT JOIN arr_offday_t a on e.empid=a.empid and a.wt_id='00901'
WHERE a.empid is null; ";

        var param = new
        {
            jobDate,
            serverip = logData.ServerIp,
            createuser = logData.CreateUser,
            createip = logData.CreateIp,
            createtime = logData.CreateTime,
        };

        return da.DapperExecute(sql, param);
    }
    */

    /// <summary>
    /// 新增 當全建時刪除 SAP_UK不存在於匯入暫存表SAP_UK的資料 將createtime=jobDate, 總額度=0, DateCurrent=jobDate 補回去
    /// </summary>
    /// <param name="jobDate"></param>
    /// <returns></returns>
    public int RebuildNotExistsWithSapUK(DateTime jobDate)
    {
        int i = 0;
        const string noteText = "Job_NotExist";
        var param = new { jobDate = jobDate, noteText };

        var sql = @"
            INSERT INTO arr_offday_t
            (serverip, createuser, createip, createtime, 
            empid, wt_id, datecurrent, datefrom, dateto, usedatefrom, usedateto, saprestnumber, tkrestnumber, restnumber, note, sap_uk)
            select distinct on (sap_uk)
                aot.serverip, aot.createuser, aot.createip, @jobDate as createtime, 
                aot.empid, aot.wt_id, @jobDate as datecurrent, aot.datefrom, aot.dateto, aot.usedatefrom, aot.usedateto, 0 as saprestnumber, aot.tkrestnumber, aot.restnumber, 
                @noteText as note, aot.sap_uk
            from arr_offday_t aot 
            where @jobDate between aot.usedatefrom and aot.usedateto and aot.sap_uk not in (select jqt.sap_uk from job_quota_tmp jqt)
            order by aot.sap_uk, aot.datecurrent desc";
        i += da.DapperExecute(sql, new { jobDate = jobDate, noteText });
        return i;
    }

    /// <summary>
    /// 刪除資料
    /// </summary>
    /// <param name="dateCurrent"></param>
    /// <returns></returns>
    public int DeleteByDateCurrent(DateTime dateCurrent)
    {
        var sql = @"delete from arr_offday_t where datecurrent = @datecurrent ";
        return da.DapperExecute(sql, new { datecurrent = dateCurrent });
    }



}