using PIC.DB;
using PIC.DB.Dao;
using SharpCompress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateMasterData.Model.EmployeeVice;

namespace UpdateMasterData.Dao
{
    public class BaseAttendPersonTypeMDao : BaseDao
    {
        public BaseAttendPersonTypeMDao(IDataAccess da)
        {
            this.da = da;
        }
        public List<BaseAttendPersonTypeMModel> Query()
        {
            string sql = @"select * FROM sethq.base_attendpersontype_m";
            return da.DapperQuery<BaseAttendPersonTypeMModel>(sql, new { }).ToList();
        }
        public void Update(BaseAttendPersonTypeMModel model)
        {
            string sql = @"UPDATE sethq.base_attendpersontype_m
                SET empid=@EmpId, ""type""=@Type, startdate=@StartDate, enddate=@EndDate
                WHERE id=@Id;";

            da.DapperExecute(sql, new { 
                EmpId = model.EmpId,
                Type = model.Type,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Id = model.Id
            });
        }
        public void Insert(BaseAttendPersonTypeMModel model)
        {
            string sql = @"INSERT INTO sethq.base_attendpersontype_m
                            (empid, ""type"", startdate, enddate)
                            VALUES(@EmpId, @Type, @StartDate, @EndDate);";

            da.DapperExecute(sql, new
            {
                EmpId = model.EmpId,
                Type = model.Type,
                StartDate = model.StartDate,
                EndDate = model.EndDate
            });

        }
    }
}
