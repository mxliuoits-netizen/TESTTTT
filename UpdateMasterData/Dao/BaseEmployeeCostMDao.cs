using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIC.DB;
using PIC.DB.Dao;

namespace UpdateMasterData.Dao;

public class BaseEmployeeCostMDao : BaseDao
{
    public BaseEmployeeCostMDao(IDataAccess da)
    {
        this.da = da;
    }

    public List<EmployeeCostM> GetList()
    {
        string sql = " select * from base_employee_cost_m ";
        return da.DapperQuery<EmployeeCostM>(sql).ToList();
    }




}

public class EmployeeCostM
{
    public string costid { get; set; }

    public string wt_id { get; set; }
}