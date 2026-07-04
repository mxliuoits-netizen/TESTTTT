using PIC.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateMasterData.Model.EmployeeVice;

namespace UpdateMasterData.Dao
{
    /// <summary>
    /// 員工例外主表 DAO
    /// </summary>
    public class BaseEmployeeExceptionMDao
    {
        IDataAccess da;

        public BaseEmployeeExceptionMDao(IDataAccess da)
        {
            this.da = da;
        }

        /// <summary>
        /// 取得 BaseEmployeeException 雜湊表
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, BaseEmployeeExceptionMModel> GetMap()
        {
            var sql = " select * from Base_Employee_exception_m ";
            return da.DapperQuery<BaseEmployeeExceptionMModel>(sql).ToDictionary(x=>x.Empid, x=>x);
        }
    }
}
