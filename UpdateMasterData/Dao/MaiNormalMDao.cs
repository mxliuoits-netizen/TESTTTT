using PIC.DB;
using PIC.DB.Dao;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateMasterData.Model.EmployeeVice;

namespace UpdateMasterData.Dao;

public class MaiNormalMDao : BaseDao
{
    public MaiNormalMDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 批次新增
    /// </summary>
    /// <param name="dataList"></param>
    /// <returns></returns>
    public int BatchInsert(List<MailNormalMModel> mailNormalList)
    {
        var sql = " INSERT INTO mail_normal_m(mail_to, displayname, subject, cc, bcc, mail_content, attachmentpath, datafrom, createtime, times, is_sent, mail_source) VALUES ";

        var paramList = mailNormalList.Select(x => new
        {
            x.MailTo,
            x.Displayname,
            x.Subject,
            x.Cc,
            x.Bcc,
            x.MailContent,
            x.Attachmentpath,
            x.Datafrom,
            x.Createtime,
            Times = 0,
            IsSent = false,
            x.MailSource
        }).ToList();

        return this.BatchExecute(sql, paramList);
    }





}
