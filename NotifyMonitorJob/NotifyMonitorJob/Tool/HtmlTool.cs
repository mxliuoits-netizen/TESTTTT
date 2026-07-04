using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotifyMonitorJob.Tool;

public class HtmlTool
{

    /// <summary>
    /// 取得 模組資料轉成HTML TABLE
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dataList"></param>
    /// <returns></returns>
    public static string GetModelToHTMLTable<T>(List<T> dataList, string className = "checktable")
    {
        string result;
        var modelType = typeof(T);
        var modelProps = modelType.GetProperties();

        var sb = new StringBuilder();
        sb.AppendLine($"<table class='{className}' border='1' cellpadding='1' cellspacing='0'> ");

        //抬頭
        sb.AppendLine("<thead style='background-color: #EEEEEE;'>");
        sb.AppendLine($"<tr>");
        foreach (var prop in modelProps)
        {
            var dispalyName = prop.GetCustomAttributes(typeof(DisplayNameAttribute), true).Cast<DisplayNameAttribute>().FirstOrDefault();
            var headText = dispalyName?.DisplayName ?? prop.Name;
            sb.AppendLine($"<th style='min-width: 100px;'>{headText}</th>");
        }
        sb.AppendLine($"</tr>");
        sb.AppendLine("</thead>");

        //內容
        sb.AppendLine("<tbody>");
        if (dataList == null || dataList.Count == 0)
        {
            sb.AppendLine($"<td colspan='{modelProps.Length}'>查無資料</td>");
        }
        else
        {
            foreach (var data in dataList)
            {
                sb.AppendLine("<tr>");
                foreach (var prop in modelProps)
                {
                    string bodyText = "N/A";
                    var obj = prop.GetValue(data);

                    if (obj?.GetType() == typeof(string) || obj?.GetType() == typeof(int)) bodyText = obj?.ToString() ?? "";
                    if (obj?.GetType() == typeof(DateTime)) bodyText = ((DateTime)obj).ToString("yyyy-MM-dd HH:mm:ss") ?? "";

                    sb.AppendLine($"<td>{bodyText}</td>");
                }
                sb.AppendLine("</tr>");
            }

        }
        sb.AppendLine("</tbody>");

        //結束
        sb.AppendLine("</table>");
        result = sb.ToString();

        return result;
    }


    /// <summary>
    /// TABEL CSS樣式
    /// </summary>
    /// <returns></returns>
    public static string GetTableCSS()
    {
		string cssText = @"
<style>
.checktable{
	border-collapse: collapse; 
	border: black 1px solid;
	
	thead {
		background-color: #EEEEEE;
		border: black 1px solid;
	}
	
	thead th {
		border: black 1px solid;
		padding: 4px;
		font-size: small;
		text-align: left; 
		vertical-align: text-top;
	}

	tbody tr:nth-child(even){
		background-color: #F8F8F8;
	}
	
	tbody td {
        border: black 1px solid;
		padding: 4px;
		font-size: small;
		text-align: left; 
		vertical-align: text-top;
	}
}
</style>";
		return cssText;
    }
}
