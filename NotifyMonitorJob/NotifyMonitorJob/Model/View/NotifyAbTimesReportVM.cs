using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotifyMonitorJob.Model.View;

public class NotifyAbTimesReportVM
{
    [DisplayName("層級單位")]
    public string AllTeamName { get; set; }
    [DisplayName("單位")]
    public string TeamName { get; set; }
    [DisplayName("員工編號")]
    public string EmpId { get; set; }
    [DisplayName("員工姓名")]
    public string EmpName { get; set; }

    public DateTime Effectivedate { get; set; }

    [DisplayName("到職日")]
    public string EffectivedateText {
        get
        {
            return Effectivedate.ToString("yyyy-MM-dd");
        }
    }

    public DateTime Expiredate { get; set; }

    [DisplayName("離職日")]
    public string ExpiredateText
    {
        get
        {
            return Expiredate.ToString("yyyy-MM-dd");
        }
    }

    public DateTime? EarlyAccessStartDate { get; set; }

    [DisplayName("異常啟用日")]
    public string EarlyAccessStartDateText
    {
        get
        {
            var result = "";
            if(EarlyAccessStartDate.HasValue) result = EarlyAccessStartDate.Value.ToString("yyyy-MM-dd");
            return result;
        }
    }

    [DisplayName("異常啟用備註")]
    public string EarlyAccessNote { get; set; }

    [DisplayName("第一次發信主旨")]
    public string Notify1stSubject { get; set; }
    public DateTime? Notify1stTime { get; set; }

    [DisplayName("第一次發信時間")]
    public string Notify1stTimeText { 
        get {
            var result = "";
            if (Notify1stTime.HasValue) result = Notify1stTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
            return result;
        } 
    }

    [DisplayName("第二次發信主旨")]
    public string Notify2ndSubject { get; set; }
    public DateTime? Notify2ndTime { get; set; }

    [DisplayName("第二次發信時間")]
    public string Notify2ndTimeText
    {
        get
        {
            var result = "";
            if (Notify2ndTime.HasValue) result = Notify2ndTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
            return result;
        }
    }

    [DisplayName("第三次發信主旨")]
    public string Notify3rdSubject { get; set; }
    public DateTime? Notify3rdTime { get; set; }

    [DisplayName("第二次發信時間")]
    public string Notify3rdTimeText
    {
        get
        {
            var result = "";
            if (Notify3rdTime.HasValue) result = Notify3rdTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
            return result;
        }
    }

    [DisplayName("第四次發信主旨")]
    public string Notify4thSubject { get; set; }
    public DateTime? Notify4thTime { get; set; }

    [DisplayName("第二次發信時間")]
    public string Notify4thTimeText
    {
        get
        {
            var result = "";
            if (Notify4thTime.HasValue) result = Notify4thTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
            return result;
        }
    }

    [DisplayName("結果")]
    public string NotifyResultText
    {
        get
        {
            var result = "";
            int n1 = Notify1stTime.HasValue ? 1 : 0;
            int n2 = Notify2ndTime.HasValue ? 1 : 0;
            int n3 = Notify3rdTime.HasValue ? 1 : 0;
            int n4 = Notify4thTime.HasValue ? 1 : 0;
            int c = n1 + n2 + n3 + n4;

            var map = new (int flag, string text)[]
            {
                (n1, "一"),
                (n2, "二"),
                (n3, "三"),
                (n4, "四")
            };

            var sendedText = string.Join(",", map.Where(x => x.flag == 1).Select(x => x.text));
            var notSendedText = string.Join(",", map.Where(x => x.flag == 0).Select(x => x.text));

            if (c == 0)
            {
                result = "";
            }
            else if(c == 1 || c == 2)
            {
                result = $"只有第{sendedText}次發信";
            }
            else if (c == 3)
            {
                result = $"只有第{notSendedText}次沒發信";
            }
            else
            {
                result = "";
            }

            return result;
        }
    }


}
