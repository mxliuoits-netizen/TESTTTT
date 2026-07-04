using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NotifyMonitorJob.Model.View;
using PIC.Job;

namespace NotifyMonitorJob.Service;

public class NotifyMonitorJobService
{

    /// <summary>
    /// 取得 下一次指定星期日期
    /// </summary>
    /// <param name="dt"></param>
    /// <param name="dayOfWeek"></param>
    /// <returns></returns>
    public DateTime GetNextDateWithDayOfWeekDay(DateTime dt, DayOfWeek dayOfWeek)
    {
        return Enumerable.Range(1, 7).Select(x => dt.AddDays(x)).Where(x=> x.DayOfWeek == dayOfWeek).Order().FirstOrDefault();
    }

    /// <summary>
    /// 取得 下一次日期依照設定值
    /// </summary>
    /// <param name="dt"></param>
    /// <param name="dateList"></param>
    /// <param name="dayOfWeek"></param>
    /// <returns></returns>
    public DateTime GetNextDate(DateTime dt, List<int> dateList, DayOfWeek? dayOfWeek = null)
    {
        return Enumerable.Range(1, 30).Select(x => dt.AddDays(x)).Where(x => dateList.Contains(x.Day) || x.DayOfWeek == dayOfWeek).Order().FirstOrDefault();
    }

    /// <summary>
    /// 補上下次執行日描述文字
    /// </summary>
    /// <param name="reportMonitorJobList"></param>
    /// <param name="datesText"></param>
    public void MemoExecuteDate(List<MonitorJobViewModel> reportMonitorJobList, string datesText)
    {
        var dateList = datesText.Split(",").Select(x => int.Parse(x)).ToList();
        var dayOfWeek = DayOfWeek.Tuesday;

        reportMonitorJobList.ForEach(item => {
            var itemJobDate = DateTime.ParseExact(item.Jobversion, BaseJob.VER_DATE_FORMAT, null);
            if (item.Jobmodulename == "NotifyUserAbnormal.Job_NotifyUserAbnormalWeek")
            {
                var nextExecuteDate = GetNextDateWithDayOfWeekDay(itemJobDate, dayOfWeek);
                item.Execstatustext += "(" + nextExecuteDate.ToString(BaseJob.VER_DATE_FORMAT) + ")";
            }

            if (item.Jobmodulename == "NotifyUserAbnormal.Job_NotifyUserAbnormalTimes")
            {
                var nextExecuteDate = GetNextDate(itemJobDate, dateList);
                item.Execstatustext += "(" + nextExecuteDate.ToString(BaseJob.VER_DATE_FORMAT) + ")";

            }

            if(item.Jobmodulename == "NotifyMonitorJob.Job_CheckNotifyUserAbnormal")
            {
                var nextExecuteDate = GetNextDate(itemJobDate, dateList, dayOfWeek);
                item.Execstatustext += "(" + nextExecuteDate.ToString(BaseJob.VER_DATE_FORMAT) + ")";
            }
        });
    }




}
