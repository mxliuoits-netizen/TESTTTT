using PIC.DB;
using PIC.DB.Dao;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateMasterData.Dao;

public class ApplyLeaverecordTDao : BaseDao
{
    public ApplyLeaverecordTDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 取得 未結算代休假請假單清單
    /// </summary>
    /// <param name="jobDate"></param>
    /// <returns></returns>
    public List<ApplyLeave> Get00901LeaveList(DateTime jobDate)
    {
        var sql = @"
with tmp_emp as (
	select 
		bem.empid,
		coalesce (
			(select max(a.checktime) from settle_checkpoint_t a 
	        where a.filetype in ('Leave', 'OvertimeDetail') 
	        and a.checktime <= (select max(datecurrent) from arr_offday_t od where od.empid = bem.empid and od.wt_id = '00901' and od.datecurrent <= @jobDate)),
			(select max(datecurrent) from arr_offday_t od 
				where od.empid = bem.empid and od.wt_id = '00901' and od.datecurrent <= @jobDate)::timestamp, 
			(@jobDate - interval '1 months')::timestamp
		) as max_checktime_00901
	from base_employee_m bem 
	where bem.expiredate > @jobDate and bem.role = 'user' 
)
, tmp_leave as (
	select
    	alt.*
	FROM apply_leaverecord_t alt 
	where alt.wt_id = '00901' and alt.updatetime >= (select min(max_checktime_00901) from tmp_emp a) and alt.updatetime < @jobDate
	and alt.applytype = 'apply' AND alt.applystatus = 'finish'  AND alt.isdelete = 'N' and alt.cancelapplyid is null
)
select
    alt.id,
	alt.empid,
	alt.leavedate,
	sum(aldt.applyleavehours) AS leavehours
FROM tmp_emp te
join tmp_leave alt 
on te.empid = alt.empid and alt.wt_id = '00901' and alt.updatetime >= te.max_checktime_00901 and alt.updatetime < @jobDate
and alt.applytype = 'apply' AND alt.applystatus = 'finish'  AND alt.isdelete = 'N' and alt.cancelapplyid is null
join apply_leaverecorddetail_t aldt on aldt.apply_leaverecord_id = alt.id
group by alt.id, alt.empid, alt.leavedate
order by alt.empid, alt.leavedate";

        return da.DapperQuery<ApplyLeave>(sql, new { jobDate }).ToList();
    }




}

public class ApplyLeave
{
    public int Id { get; set; }
    public string Empid { get; set; }    
    public DateTime Leavedate { get; set; }
    public decimal Leavehours { get; set; }
}