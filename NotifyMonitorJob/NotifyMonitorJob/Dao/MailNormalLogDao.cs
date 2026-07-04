using PIC.DB;
using PIC.DB.Dao;
using NotifyMonitorJob.Model;
using NotifyMonitorJob.Model.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotifyMonitorJob.Dao;

public class MailNormalLogDao : BaseDao
{
    public MailNormalLogDao(IDataAccess da)
    {
        this.da = da;
    }

    public void CreateBatch(List<MailNormalLogModel> logList)
    {
        string sqlStmt = @"INSERT INTO sethq.mail_normal_log
                   (mail_to, displayname, subject, cc, bcc, mail_content, attachmentpath, datafrom, createtime, times, is_sent, senttime, logtime, logstatus, note, mail_source)
                   VALUES ";

        this.BatchExecute(sqlStmt, logList);
    }

    /// <summary>
    /// 取得 該日發送之異常信件資料
    /// </summary>
    /// <param name="jobDate"></param>
    /// <returns></returns>
    public List<NotifyUserAbnormalMailModel> GetNotifyUserAbnormalMailList(DateTime jobDate, List<string> subjectList)
    {
        var sql = @"
            with tmp_maillog as (
		        select distinct on (mnl.mail_source, mnl.createtime, mnl.subject, mnl.senttime) *
		        from mail_normal_log mnl
		        where senttime::date = @jobDate
		        order by mnl.mail_source, mnl.createtime, mnl.subject, mnl.senttime, mnl.logtime desc
	        )
	        select 
		        tm.subject, tm.mail_to, tm.cc, tm.bcc, tm.senttime
	        from tmp_maillog tm 
	        where exists (
                select 1 from mail_normal_finish mnf 
                where tm.mail_source = mnf.mail_source and tm.createtime = mnf.createtime and tm.subject = mnf.subject and mnf.is_sent = true and mnf.times < 99
            )
        ";

        if(subjectList != null && subjectList.Count > 0)
        {
            var partialSql = subjectList.Select(x => $" tm.subject like '%{x}%' ").ToList();
            sql += " and ("+ string.Join(" or ", partialSql) +")";
        }

        return da.DapperQuery<NotifyUserAbnormalMailModel>(sql, new { jobDate }).ToList();
    }

    /// <summary>
    /// 取得 該日發送之異常信件 通知人包含黑名單資料
    /// </summary>
    /// <param name="jobDate"></param>
    /// <returns></returns>
    public List<NotifyUserAbnormalMailModel> GetNotifyUserAbnormalBlackMailList(DateTime jobDate, List<string> subjectList)
    {
        var sql = @"
            with tmp_maillog as (
		        select distinct on (mnl.mail_source, mnl.createtime, mnl.subject) *
		        from mail_normal_log mnl
		        where senttime::date = @jobDate
		        order by mnl.mail_source, mnl.createtime, mnl.subject, mnl.logtime desc
	        )
            select 
		        tm.subject, tm.mail_to, tm.cc, tm.bcc, tm.senttime
	        from mail_black_m a
	        join base_employee_m bem on bem.empid = a.empid
	        join mail_normal_finish mnf on mnf.senttime between a.starttime and a.endtime 
		        --and (mnf.mail_to like '%'||bem.email||'%' or mnf.cc like '%'||bem.email||'%' or mnf.bcc like '%'||bem.email||'%') --這是原始發送內容
	        join tmp_maillog tm on tm.mail_source = mnf.mail_source and tm.createtime = mnf.createtime and tm.subject = mnf.subject
                and (tm.mail_to like '%'||bem.email||'%' or tm.cc like '%'||bem.email||'%' or tm.bcc like '%'||bem.email||'%') --這是發信移除黑名單後實際發送紀錄
            where mnf.is_sent = true
        ";

        if (subjectList != null && subjectList.Count > 0)
        {
            var partialSql = subjectList.Select(x => $" mnf.subject like '%{x}%' ").ToList();
            sql += " and (" + string.Join(" or ", partialSql) + ")";
        }

        return da.DapperQuery<NotifyUserAbnormalMailModel>(sql, new { jobDate }).ToList();
    }

    /// <summary>
    /// 取得 有效人員考勤異常通知次數報表
    /// </summary>
    /// <param name="attendStartDate"></param>
    /// <param name="attendEndDate"></param>
    /// <returns></returns>
    public List<NotifyAbTimesReportVM> GetNotifyAbTimesReportList(DateTime attendStartDate, DateTime attendEndDate)
    {
        var sql = @"
            with tmp_team as (
                SELECT 
                    vc.allteamname, vc.teamid, vc.teamname, vc.empid, vc.empname, 
                    vc.effectivedate, vc.expiredate, vc.deplowerlevel,  vc.sortlevel
                FROM fn_vc_query('CHILDTEAMALL', '0000000002', 'picteam') vc
                where vc.isactive = 'Y' AND vc.expiredate > @attendStartDate
                order by sortlevel
            )
            , tmp_notifyabtimes_mail as (
	            select mnl.mail_source, mnl.subject, mnl.senttime, 
	            case 
		            when position('【考勤異常通知信-第一' in mnl.subject) > 0 then 1
		            when position('【考勤異常通知信-第二' in mnl.subject) > 0 then 2
		            when position('【考勤異常通知信-第三' in mnl.subject) > 0 then 3
		            when position('【考勤異常通知信-第四' in mnl.subject) > 0 then 4
	            end as notifytimes
	            from mail_normal_log mnl
	            where mnl.senttime between @attendStartDate and @attendEndDate and mnl.is_sent = true
	            and mnl.subject like '【考勤異常通知信-第%' 
            )
            , tmp_distinct_mail as (
	            select distinct on (tnm.mail_source, tnm.notifytimes)
		            tnm.*
	            from tmp_notifyabtimes_mail tnm
	            order by tnm.mail_source, tnm.notifytimes, tnm.senttime desc
            )
            select 
	            tt.*, 
	            tnm1st.subject as Notify1stSubject, tnm1st.senttime as Notify1stTime,
	            tnm2nd.subject as Notify2ndSubject, tnm2nd.senttime as Notify2ndTime,
	            tnm3rd.subject as Notify3rdSubject, tnm3rd.senttime as Notify3rdTime,
	            tnm4th.subject as Notify4thSubject, tnm4th.senttime as Notify4thTime,
	            bwea.start_date as EarlyAccessStartDate, bwea.note as EarlyAccessNote
            from tmp_team tt
            left join base_work_exception_early_access bwea on bwea.empid = tt.empid
            left join tmp_distinct_mail tnm1st on tnm1st.notifytimes = 1 and tnm1st.mail_source = tt.empid
            left join tmp_distinct_mail tnm2nd on tnm2nd.notifytimes = 2 and tnm2nd.mail_source = tt.empid
            left join tmp_distinct_mail tnm3rd on tnm3rd.notifytimes = 3 and tnm3rd.mail_source = tt.empid
            left join tmp_distinct_mail tnm4th on tnm4th.notifytimes = 4 and tnm4th.mail_source = tt.empid
            ";

        var param = new { 
            attendStartDate = attendStartDate.Date, 
            attendEndDate = attendEndDate.Date.AddDays(1).Date 
        };

        return da.DapperQuery<NotifyAbTimesReportVM>(sql, param).ToList();
    }




}
