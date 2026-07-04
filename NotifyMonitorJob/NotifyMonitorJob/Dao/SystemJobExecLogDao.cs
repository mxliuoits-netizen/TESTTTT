using PIC.DB;
using PIC.DB.Dao;
using NotifyMonitorJob.Model.View;

namespace NotifyMonitorJob.Dao;

/// <summary>
/// 排程執行紀錄DAO
/// </summary>
public class SystemJobExecLogDao : BaseDao
{
    public SystemJobExecLogDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 取得 排程執行紀錄
    /// </summary>
    /// <returns></returns>
    public List<SystemJobExecLogViewModel> GetJobExecLogList(DateTime dt)
    {
        var sql = @"
            with pre_date_job as (
                select max(sjl.id) as id, sjl.jobmodulename 
                from system_jobexec_log sjl 
                where sjl.createtime >= @queryDate and sjl.jobversion = @jobdDate and sjl.execstatus != 'DP'
                group by sjl.jobmodulename 
            )
            select 
                sjl.jobmodulename, sjl.jobversion, sjl.createtime, sjl.serverip,
                CASE 
                    WHEN sjl.execstatus = 'OK' THEN '成功'
                    WHEN sjl.execstatus = 'NG' THEN '失敗.請注意'
                    WHEN sjl.execstatus = 'CS' and sjl.jobmodulename = 'UpdateAttendLog.Job_UpdateAttendLogHQ' 
                        THEN sjl.execstatus || case when bcm.wt_id = '00000' then '(工作日)' else '(非工作日)' end
                    WHEN sjl.execstatus = 'CS' and ( sjl.jobmodulename = 'UpdateAttendLog.Job_UpdateAttendLogHQNH' or sjl.jobmodulename = 'UpdateAttendLog.Job_UpdateAttendLogHQCL')
                        THEN sjl.execstatus || case when bcm_pre.wt_id = '00000' then '(工作日)' else '(非工作日)' end
                    ELSE sjl.execstatus
                END AS execstatus
            from system_jobexec_log sjl
            join pre_date_job pdj on pdj.id = sjl.id
            left join base_calendar_m bcm_pre on bcm_pre.arrdate = (sjl.jobversion::date - interval '1 days')
            left join base_calendar_m bcm on bcm.arrdate = sjl.jobversion::date
            order by sjl.createtime";

        var param = new
        {
            queryDate = dt.AddDays(-1).Date,
            jobdDate = dt.ToString("yyyyMMdd")
        };

        return da.DapperQuery<SystemJobExecLogViewModel>(sql, param).ToList();
    }

    /// <summary>
    /// 取得 排程執行紀錄
    /// </summary>
    /// <returns></returns>
    public List<SystemJobExecLogViewModel> GetChtNameJobExecLogList(DateTime dt)
    {
        var sql = @"
            with pre_date_job as (
	            select max(sjl.id) as id, sjl.jobmodulename 
	            from system_jobexec_log sjl 
	            where sjl.createtime >= @queryDate and sjl.jobversion = @jobdDate and sjl.execstatus != 'DP'
	            group by sjl.jobmodulename 
            )
            select 
	            coalesce(jcn.job_name, sjl.jobmodulename) as jobmodulename, sjl.jobversion, sjl.createtime, sjl.serverip,
	            CASE 
		            WHEN sjl.execstatus = 'OK' THEN '成功'
                    WHEN sjl.execstatus = 'NG' THEN '失敗.請注意'
                    WHEN sjl.execstatus = 'CS' and sjl.jobmodulename = 'UpdateAttendLog.Job_UpdateAttendLogHQ' 
                        THEN '本日無檔' || case when bcm.wt_id = '00000' then '(工作日)' else '(非工作日)' end
                    WHEN sjl.execstatus = 'CS' and ( sjl.jobmodulename = 'UpdateAttendLog.Job_UpdateAttendLogHQNH' or sjl.jobmodulename = 'UpdateAttendLog.Job_UpdateAttendLogHQCL')
                        THEN '本日無檔' || case when bcm_pre.wt_id = '00000' then '(工作日)' else '(非工作日)' end
                     WHEN sjl.execstatus = 'CS' THEN '本日無檔'
                    WHEN sjl.execstatus = 'IP' THEN '作業進行中'
                    ELSE sjl.execstatus
                END AS execstatus
            from system_jobexec_log sjl
            join pre_date_job pdj on pdj.id = sjl.id
            join job_checkabnormal_notify jcn on jcn.job_code = sjl.jobmodulename
            left join base_calendar_m bcm_pre on bcm_pre.arrdate = (sjl.jobversion::date - interval '1 days')
            left join base_calendar_m bcm on bcm.arrdate = sjl.jobversion::date
            order by sjl.createtime";

        var param = new
        {
            queryDate = dt.AddDays(-1).Date,
            jobdDate = dt.ToString("yyyyMMdd")
        };

        return da.DapperQuery<SystemJobExecLogViewModel>(sql, param).ToList();
    }

    /// <summary>
    /// 取得 排程執行紀錄
    /// </summary>
    /// <returns></returns>
    public List<MonitorJobViewModel> GetDistinctJobExecLogList()
    {
        var sql = @"
            with tmp_distinct_joblog as (
                select 
                    distinct on (sjl.jobmodulename) *
                from system_jobexec_log sjl 
                where sjl.executetime >= @now::date - interval '1 month'
                order by sjl.jobmodulename, sjl.executetime desc
            )
            select 
                sjl.jobmodulename, sjl.jobversion, sjl.createtime, sjl.updatetime, 
                rt.runningtimetext,
                CASE 
                    WHEN sjl.execstatus = 'OK' THEN '成功'
                    WHEN sjl.execstatus = 'NG' THEN '失敗.請注意'
                    WHEN sjl.execstatus = 'CS' and sjl.jobmodulename = 'UpdateAttendLog.Job_UpdateAttendLogHQ' 
                        THEN '本日無檔' || case when bcm.wt_id = '00000' then '(工作日)' else '(非工作日)' end
                    WHEN sjl.execstatus = 'CS' and ( sjl.jobmodulename = 'UpdateAttendLog.Job_UpdateAttendLogHQNH' or sjl.jobmodulename = 'UpdateAttendLog.Job_UpdateAttendLogHQCL')
                        THEN '本日無檔' || case when bcm_pre.wt_id = '00000' then '(工作日)' else '(非工作日)' end
                    WHEN sjl.execstatus = 'CS' 
                        THEN '相同作業進行中(' 
                            || (
                                select to_char(max(a.createtime), 'yyyy-mm-dd hh24:mi:ss') 
                                from system_jobexec_log a 
                                where a.jobmodulename = sjl.jobmodulename and a.execstatus = 'IP' and a.executetime < sjl.executetime
                            ) 
                            || ')'
                    WHEN sjl.execstatus = 'IP' THEN '作業進行中'
                    ELSE sjl.execstatus
                END AS execstatustext
            from tmp_distinct_joblog sjl
            left join base_calendar_m bcm_pre on bcm_pre.arrdate = (sjl.jobversion::date - interval '1 days')
            left join base_calendar_m bcm on bcm.arrdate = sjl.jobversion::date
            left join lateral (
	            SELECT
	                CONCAT(
	                    FLOOR(a.total_seconds / 3600), '小時',
	                    FLOOR(a.total_seconds % 3600 / 60), '分',
	                    FLOOR(a.total_seconds % 60), '秒'
	                ) AS runningtimetext
	            FROM (
	                SELECT EXTRACT(
                        EPOCH FROM (
                            date_trunc('second', (case when sjl.updatetime is null then @now else sjl.updatetime end) )
                            - date_trunc('second', sjl.createtime)
                        )
                    ) AS total_seconds
	            ) a
            ) rt on 1=1
            order by sjl.createtime";

        var param = new
        {
            now = DateTime.Now
        };

        return da.DapperQuery<MonitorJobViewModel>(sql, param).ToList();
    }

    /// <summary>
    /// 更新 ODataFix.Job_FixMappingAttend IP超過12小時的資料
    /// </summary>
    /// <returns></returns>
    public int UpdateODataFixJobIP()
    {
        var sql = @"
            update system_jobexec_log set 
                execstatus = 'NG', note = system_jobexec_log.execstatus || '_' || to_char(@now, 'yyyy-mm-dd hh24:mi')
            where jobmodulename ='OldDataFix.Job_FixMappingAttend' and execstatus = 'IP' and createtime <= @now - interval '12 hours' ";

        return da.DapperExecute(sql, new {now = DateTime.Now});
    }





}
