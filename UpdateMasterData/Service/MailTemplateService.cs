using PIC.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateMasterData.Dao;
using UpdateMasterData.Model.EmployeeVice;
using UpdateMasterData.Model.Enum;

namespace UpdateMasterData.Service;

/// <summary>
/// 信件套版 服務
/// </summary>
public class MailTemplateService : BaseService
{

    IDataAccess da;

    public MailTemplateService(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 建立 寄信資料套版結果
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="R"></typeparam>
    /// <param name="mailData"></param>
    /// <param name="mailTemplateType"></param>
    /// <returns></returns>
    public MailNormalMModel BuildMailContent<T>(MailData<T> mailData)
    {
        List<MailData<T>> mailDataList = new List<MailData<T>> { mailData };
        var result = BuildMailContent(mailDataList);
        return result.FirstOrDefault();
    }

    /// <summary>
    /// 建立 寄信資料套版結果
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="mailDataList"></param>
    /// <param name="mailTemplateType"></param>
    /// <returns></returns>
    public List<MailNormalMModel> BuildMailContent<T>(List<MailData<T>> mailDataList)
    {
        List<MailNormalMModel> result = new List<MailNormalMModel>();
        try
        {
            var dao = new MailTemplateMDao(da);
            var mailTemplateData = dao.GetList();
            if (mailTemplateData == null) throw new Exception("查無相關信件樣板!");

            foreach (var mailData in mailDataList)
            {
                var template = mailTemplateData?.GetValueOrDefault(mailData.MailTemplateType.ToString());
                if (template == null) throw new Exception($"查無相關信件樣板({mailData.MailTemplateType})!");

                var subjectText = template.Templatesubject;
                var mailText = template.Templatecontent;
                var props = mailData.Data.GetType().GetProperties();
                foreach (var p in props)
                {
                    // ex: 【@{applyName}申請單】待簽核通知-表單編號:@{applyId} => new {applyName="補打卡", applyId="20477"} 
                    // =>  【補打卡申請單】待簽核通知-表單編號:20477
                    subjectText = subjectText.Replace("@{" + p.Name + "}", $"{p.GetValue(mailData.Data)}");
                    mailText = mailText.Replace("@{" + p.Name + "}", $"{p.GetValue(mailData.Data)}");
                }
                result.Add(new MailNormalMModel
                {
                    MailTo = mailData.MailTo ?? "",
                    Subject = subjectText,
                    Cc = mailData.CC,
                    Attachmentpath = mailData.Attachmentpath,
                    MailContent = mailText,
                    MailSource = mailData.MailSource,
                    Datafrom = mailData.DataFrom,
                    Createtime = mailData.Createtime,
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
        }
        return result;
    }
}

/// <summary>
/// 信件內容材料
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="R"></typeparam>
public class MailData<T>
{

    public string MailTo { get; set; }

    public string CC { get; set; }

    public string BCC { get; set; }

    public string Attachmentpath { get; set; }

    public string DataFrom { get; set; }

    public string? MailSource { get; set; }

    public DateTime Createtime { get; set; } = DateTime.Now;

    public MailTemplateType MailTemplateType { get; set; }

    public T Data { get; set; }
}
