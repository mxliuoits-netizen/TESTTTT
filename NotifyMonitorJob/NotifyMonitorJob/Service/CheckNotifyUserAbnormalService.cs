using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NotifyMonitorJob.Model.View;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using PIC.DB;

namespace NotifyMonitorJob.Service;

public class CheckNotifyUserAbnormalService
{
    IDataAccess da;
    Action<List<string>, int, string?> GreatestLengthAction;

    public CheckNotifyUserAbnormalService(IDataAccess da) 
    {
        this.da = da;

        // 判別欄位最長資料內容
        this.GreatestLengthAction = (list, count, v) =>
        {
            if (v == null) return;
            list[count] = list[count].Length < v.Length ? v : list[count];
        };
    }

    /// <summary>
    /// 取得 Excel資料
    /// </summary>
    /// <param name="jobDate"></param>
    /// <returns></returns>
    public byte[] GetNotifyUserAbnormalMailExcel(List<NotifyUserAbnormalMailModel> mailList, List<NotifyUserAbnormalMailModel> blackMailList, List<NotifyAbTimesReportVM> notifyAbTimesReportList)
    {
        
        byte[] b = null;
        try
        {
            using MemoryStream stream = new MemoryStream();
            using IWorkbook workbook = new XSSFWorkbook();

            // using IWorkbook workbook = new HSSFWorkbook();
            // ((HSSFWorkbook)workbook).WriteProtectWorkbook(pwd, "admin");
            int sheetRowIndex = 0;
            int sheetColumnIndex = 0;

            //異常考勤通知信件分頁
            { 
                var sheetMail = workbook.CreateSheet("異常信件寄信紀錄");
                //if (!string.IsNullOrEmpty(pwd)) sheetMail.ProtectSheet(pwd);
                var sheetMailTitleRow = sheetMail.CreateRow(sheetRowIndex);

                NotifyUserAbnormalMailModel ColName = new NotifyUserAbnormalMailModel();
                var sheetMailTitleList = new List<ItemReflect<NotifyUserAbnormalMailModel>>() {
                    new (ColName, nameof(ColName.MailToName)),
                    new (ColName, nameof(ColName.CCName)),
                    new (ColName, nameof(ColName.BCCName)),
                    new (ColName, nameof(ColName.Subject)),
                    new (ColName, nameof(ColName.SentTime)),
                };
                sheetColumnIndex = 0;
                foreach (var titleItem in sheetMailTitleList)
                {
                    setCellValue(sheetMailTitleRow.CreateCell(sheetColumnIndex++), titleItem.DisplayName);
                }

                var compareList = sheetMailTitleList.Select(x => x.DisplayName).ToList();
                for (int i = 0; i < mailList.Count; i++)
                {
                    var item = mailList[i];
                    var row = sheetMail.CreateRow(sheetRowIndex + i + 1);
                    int count = 0;

                    foreach (var itemTitle in sheetMailTitleList)
                    {
                        var v = itemTitle.PropInfo?.GetValue(item);
                        GreatestLengthAction.Invoke(compareList, count, v as string);
                        setCellValue(row.CreateCell(count++), v);
                    }
                }
                AutoSizeColumnChinese(sheetMail, compareList.ToArray(), true, true);//自動欄寬(中文)
            }

            //異常考勤通知黑名單信件分頁
            {
                var sheetBlackMail = workbook.CreateSheet("異常信件寄信黑名單紀錄");
                //if(!string.IsNullOrEmpty(pwd)) sheetBlackMail.ProtectSheet(pwd);
                sheetRowIndex = 0;
                sheetColumnIndex = 0;
                var sheetBlackMailTitleRow = sheetBlackMail.CreateRow(sheetRowIndex);

                NotifyUserAbnormalMailModel ColName = new NotifyUserAbnormalMailModel();
                var sheetBlackMailTitleList = new List<ItemReflect<NotifyUserAbnormalMailModel>>() {
                    new (ColName, nameof(ColName.MailToName)),
                    new (ColName, nameof(ColName.CCName)),
                    new (ColName, nameof(ColName.BCCName)),
                    new (ColName, nameof(ColName.Subject)),
                    new (ColName, nameof(ColName.SentTime)),
                };
                sheetColumnIndex = 0;
                foreach (var titleItem in sheetBlackMailTitleList)
                {
                    setCellValue(sheetBlackMailTitleRow.CreateCell(sheetColumnIndex++), titleItem.DisplayName);
                }

                var blackCompareList = sheetBlackMailTitleList.Select(x => x.DisplayName).ToList();
                for (int i = 0; i < blackMailList.Count; i++)
                {
                    var item = blackMailList[i];
                    var row = sheetBlackMail.CreateRow(sheetRowIndex + i + 1);
                    int count = 0;

                    foreach (var itemTitle in sheetBlackMailTitleList)
                    {
                        var v = itemTitle.PropInfo?.GetValue(item);
                        GreatestLengthAction.Invoke(blackCompareList, count, v as string);
                        setCellValue(row.CreateCell(count++), v);
                    }
                }
                AutoSizeColumnChinese(sheetBlackMail, blackCompareList.ToArray(), true, true);//自動欄寬(中文)
            }

            //異常考勤通知次數分頁
            if(notifyAbTimesReportList!= null && notifyAbTimesReportList.Count > 0)
            {
                var sheetNotifyAbMail = workbook.CreateSheet("異常考勤通知次數");

                sheetRowIndex = 0;
                sheetColumnIndex = 0;
                var sheetNotifyAbMailTitleRow = sheetNotifyAbMail.CreateRow(sheetRowIndex);

                NotifyAbTimesReportVM ColName = new NotifyAbTimesReportVM();
                var sheetNotifyAbMailTitleList = new List<ItemReflect<NotifyAbTimesReportVM>>() {
                    new (ColName, nameof(ColName.AllTeamName)),
                    new (ColName, nameof(ColName.EmpId)),
                    new (ColName, nameof(ColName.EmpName)),
                    new (ColName, nameof(ColName.EffectivedateText)),
                    new (ColName, nameof(ColName.ExpiredateText)),
                    new (ColName, nameof(ColName.EarlyAccessStartDateText)),
                    new (ColName, nameof(ColName.EarlyAccessNote)),
                    new (ColName, nameof(ColName.Notify1stSubject)),
                    new (ColName, nameof(ColName.Notify1stTimeText)),
                    new (ColName, nameof(ColName.Notify2ndSubject)),
                    new (ColName, nameof(ColName.Notify2ndTimeText)),
                    new (ColName, nameof(ColName.Notify3rdSubject)),
                    new (ColName, nameof(ColName.Notify3rdTimeText)),
                    new (ColName, nameof(ColName.Notify4thSubject)),
                    new (ColName, nameof(ColName.Notify4thTimeText)),
                    new (ColName, nameof(ColName.NotifyResultText)),
                };
                sheetColumnIndex = 0;
                foreach (var titleItem in sheetNotifyAbMailTitleList)
                {
                    setCellValue(sheetNotifyAbMailTitleRow.CreateCell(sheetColumnIndex++), titleItem.DisplayName);
                }

                var notifyAbCompareList = sheetNotifyAbMailTitleList.Select(x => x.DisplayName).ToList();
                for (int i = 0; i < notifyAbTimesReportList.Count; i++)
                {
                    var item = notifyAbTimesReportList[i];
                    var row = sheetNotifyAbMail.CreateRow(sheetRowIndex + i + 1);
                    int count = 0;

                    foreach (var itemTitle in sheetNotifyAbMailTitleList)
                    {
                        var v = itemTitle.PropInfo?.GetValue(item);
                        GreatestLengthAction.Invoke(notifyAbCompareList, count, v as string);
                        setCellValue(row.CreateCell(count++), v);
                    }
                }
                AutoSizeColumnChinese(sheetNotifyAbMail, notifyAbCompareList.ToArray(), true, true);//自動欄寬(中文)
            }

            // 輸出
            workbook.Write(stream);
            workbook.Close();
            b = stream.ToArray();
        }
        catch (Exception ex)
        {
            throw;
        }
        return b;
    }

    protected void setCellValue(ICell cell, dynamic? value, ICellStyle? style = null, ISheet? sheet = null, CellRangeAddress? mergedRegion = null)
    {
        if (value == null)
            value = "";
        Type type = value.GetType();

        if (type == typeof(int) || type == typeof(Int16) || type == typeof(Int32) || type == typeof(Int64))
            cell.SetCellValue(Convert.ToInt32(value));
        else if (type == typeof(float) || type == typeof(double) || type == typeof(Double))
            cell.SetCellValue((Double)value);
        else if (type == typeof(DateTime))
            cell.SetCellValue(((DateTime)value).ToString("yyyy/MM/dd HH:mm"));
        else if (type == typeof(bool) || type == typeof(Boolean))
            cell.SetCellValue((bool)value);
        else
            cell.SetCellValue(value.ToString());
        if (style != null)
            cell.CellStyle = style;
        if (sheet != null && mergedRegion != null)
            sheet.AddMergedRegion(mergedRegion);
    }

    protected void AutoSizeColumnChinese(ISheet sheet, string[] compareArray, bool isMSJhengHei, bool hadFilter)
    {
        //獲取當前列的寬度，然後對比本列的長度，取最大值
        for (int columnNum = 0; columnNum < compareArray.Length; columnNum++)
        {
            int columnWidth = sheet.GetColumnWidth(columnNum) / 256;
            int length = IsContainHanString(compareArray[columnNum].ToString(), isMSJhengHei, hadFilter);
            if (columnWidth < length)
            {
                columnWidth = length;
            }

            if (columnWidth > 100) columnWidth = 100;

            sheet.SetColumnWidth(columnNum, columnWidth * 256);
        }
    }

    protected int IsContainHanString(string str, bool isMSJhengHei, bool hadFilter)
    {
        double length = hadFilter ? 4 : 0;
        for (int i = 0; i < str.Length; i++)
        {
            if (IsHanCode(str, i))
                length += isMSJhengHei ? 3 : 2;
            else
                length += isMSJhengHei ? 1.5 : 1;
        }

        return (int)length;
    }

    protected bool IsHanCode(string str, int index)
    {
        int code = 0;
        int chFrom = Convert.ToInt32("4e00", 16);
        int chEnd = Convert.ToInt32("9fff", 16);

        if (str != "")
        {
            // 取得字串中指定索引處的字元 Unicode 編碼
            code = Char.ConvertToUtf32(str, index);

            if (code >= chFrom && code <= chEnd)
                return true;
            return false;
        }
        return false;
    }


}
