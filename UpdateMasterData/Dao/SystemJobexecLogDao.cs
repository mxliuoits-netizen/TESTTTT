using PIC.DB;
using PIC.DB.Dao;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateMasterData.Dao;

public class SystemJobexecLogDao : BaseDao
{
    public SystemJobexecLogDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 總數 
    /// </summary>
    /// <param name="jobModuleName"></param>
    /// <param name="startDT"></param>
    /// <param name="endDT"></param>
    /// <returns></returns>
    public int CountOK(string jobModuleName, DateTime startDT, DateTime endDT)
    {
        var sql = @"
            select count(1) 
            from system_jobexec_log 
            where jobmodulename = @jobModuleName and executetime between @startDT and @endDT and execstatus = 'OK'";

        var param = new
        {
            jobModuleName,
            startDT, 
            endDT
        };
        return da.DapperQuery<int>(sql, param).FirstOrDefault();
    }

}
