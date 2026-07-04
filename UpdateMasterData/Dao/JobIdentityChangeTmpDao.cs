using PIC.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateMasterData.Model.EmployeeDiff;

namespace UpdateMasterData.Dao
{
    public class JobIdentityChangeTmpDao
    {
        IDataAccess da;

        public JobIdentityChangeTmpDao(IDataAccess da)
        {
            this.da = da;
        }

        public List<JobIdentityChangeTmpModel> Query()
        {
            string sql = @"select * from job_identitychange_tmp";
            return da.DapperQuery<JobIdentityChangeTmpModel>(sql, new { }).ToList();
        }

        public void Insert(JobIdentityChangeTmpModel model)
        {
            string sql = @"INSERT INTO sethq.job_identitychange_tmp
( empid, changetype, isdel, a_id, teamid,  createdate, createuser, updatedate, updateuser)
VALUES(@EmpId, @ChangeType, @IsDel, @A_Id, @TeamId,  @CreateDate, @CreateUser, @UpdateDate, @UpdateUser)";
            da.DapperExecute(sql, new 
            { 
                EmpId = model.EmpId, 
                ChangeType = model.ChangeType,
                IsDel = model.IsDel,
                A_Id = model.A_Id,
                TeamId = model.TeamId,
                CreateDate = model.CreateDate,
                CreateUser = model.CreateUser,
                UpdateDate = model.UpdateDate,
                UpdateUser = model.UpdateUser
            });
        }

        public void Delete(int id, DateTime updateDate, string updateUser)
        {
            string sql = @"UPDATE sethq.job_identitychange_tmp
SET  isdel='Y' updatedate=@UpdateDate, updateuser=@UpdateUser
WHERE id=@Id";
            da.DapperExecute(sql, new
            {
                UpdateDate = updateDate,
                UpdateUser = updateUser,
                Id = id
            });
        }

        public void DeleteAll(int id, DateTime updateDate, string updateUser)
        {
            string sql = @"UPDATE sethq.job_identitychange_tmp
SET  isdel='Y' updatedate=@UpdateDate, updateuser=@UpdateUser";
            da.DapperExecute(sql, new
            {
                UpdateDate = updateDate,
                UpdateUser = updateUser
            });
        }
        public void Truncate()
        {
            string sql = @"TRUNCATE TABLE job_identitychange_tmp";
            da.DapperExecute(sql, new
            {
            });
        }
    }
}
