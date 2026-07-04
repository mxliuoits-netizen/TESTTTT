using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NotifyMonitorJob.Model.View;
using PIC.DB;
using PIC.DB.Dao;

namespace NotifyMonitorJob.Dao;

public class BaseEmployeeMDao : BaseDao
{
    public BaseEmployeeMDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 取得 腳色為User的員工信箱資料
    /// </summary>
    /// <returns></returns>
    public List<MailMapEmpIdModel> GetMailEmpidList()
    {
        var sql = @" 
        select distinct on (bem.email)
            bem.empid, bem.empname, bem.email, isactive 
        from base_employee_m bem
        where bem.role = 'user'
        order by bem.email, bem.isactive desc, bem.updatetime desc
        ";

        return da.DapperQuery<MailMapEmpIdModel>(sql).ToList();
    }

}


