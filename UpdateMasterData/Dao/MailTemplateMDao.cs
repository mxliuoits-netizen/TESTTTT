using PIC.DB.Dao;
using PIC.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateMasterData.Model;
using System.Web;

namespace UpdateMasterData.Dao;

public class MailTemplateMDao : BaseDao
{
    public MailTemplateMDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 查詢 信件樣板
    /// </summary>
    /// <param name="templateName"></param>
    /// <returns></returns>
    public Dictionary<string, MailTemplateMModel> GetList()
    {
        var sql = @"select * from mail_template_m where Isactive = true ";
        return da.DapperQuery<MailTemplateMModel>(sql).ToDictionary(x => x.Templatename ?? "", x =>
        {
            x.Templatesubject = HttpUtility.HtmlDecode(x.Templatesubject);
            x.Templatecontent = HttpUtility.HtmlDecode(x.Templatecontent);
            return x;
        });
    }


}

