using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIC.DB;
using PIC.DB.Dao;

namespace UpdateMasterData.Dao;

/// <summary>
/// 調班額外開放班別
/// </summary>
public class BaseWorktypeExtraMDao : BaseDao
{

    public BaseWorktypeExtraMDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 重建 調班額外開放班別
    /// </summary>
    public void RebuildWorktype(DateTime jobDate)
    {
        var param = new
        {
            jobDate,
            now = DateTime.Now
        };

        var sql = da.GetCallSPSQL("fn_rebuild_worktype_extra", param);
        
        da.DapperExecute(sql, param);
    }





}
