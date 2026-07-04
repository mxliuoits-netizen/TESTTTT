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
    /// Authority主表 DAO
    /// </summary>
    public class BaseAuthorityMDao
    {
        IDataAccess da;

        public BaseAuthorityMDao(IDataAccess da)
        {

            this.da = da;
        }

        /// <summary>
        /// 取得 資料
        /// </summary>
        /// <returns></returns>
        public IEnumerable<BaseAuthorityMModel> GetIEnumerable()
        {
            var sql = @"select * from base_authority_m";
            return da.DapperQuery<BaseAuthorityMModel>(sql);
        }

        /// <summary>
        /// 取得 當前A_ID最大值(預設前100號保留)
        /// </summary>
        /// <returns></returns>
        public int GetMaxAId()
        {
            var sql = @"SELECT coalesce(MAX(A_Id), 0) FROM Base_Authority_m";
            var maxId = da.DapperQuery<int>(sql).First();
            if(maxId <= 100) maxId = 101; //前100號保留
            return maxId;
        }

        /// <summary>
        /// 新增 
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public int InsertByJobEmployeeData(dynamic param)
        {
            var sql = @"
                    INSERT INTO base_authority_m(serverip, versionid, createuser, createip, createtime, sapteamid, sapaname, teamid, a_id, a_name, a_area, a_sort, costcenter)
                    VALUES(@ServerIp, @VersionId, @CreateUser, @CreatorIp, @CreateTime, @SapTeamId, @SapAname, @Teamid, @A_Id, @A_Name, @A_Area, @A_Sort, @CostCenter)";
            return da.DapperExecute(sql, param);
        }

        /// <summary>
        /// 合併 employee_exception_m 至 Authority主表
        /// </summary>
        /// <param name="serverIp"></param>
        /// <param name="createUser"></param>
        /// <param name="createTime"></param>
        /// <returns></returns>
        public int MergeByExceptionEmployee(string serverIp, string createUser, string createIp, DateTime createTime)
        {
            var sql = @"
                MERGE INTO base_authority_m AS tgt
                USING (
                    select distinct TeamId as sapteamid , sapaname, teamid, a_id, a_name, a_area, 999 as a_sort, '9999999999' as costcenter
                    from base_employee_exception_m 
                    where id in (
                        select id from(select  a_id, max(id) as id from base_employee_exception_m group by a_id, id)
                    )
                    order by a_id 
                ) AS src
                ON (tgt.a_id = src.a_id)
                WHEN MATCHED
                THEN UPDATE SET
                    sapteamid=src.sapteamid, sapaname=src.sapaname, teamid=src.teamid, a_name=src.a_name, a_sort=src.a_sort, costcenter=src.costcenter
                WHEN NOT MATCHED
                THEN INSERT (
                    serverip, versionid, createuser, createip, createtime, sapteamid, sapaname, teamid, a_id, a_name, a_area, a_sort, costcenter
                ) VALUES (
                    @serverip, 0, @createuser, @createip, @createtime, src.sapteamid, src.sapaname, src.teamid, src.a_id, src.a_name, src.a_area,
                    src.a_sort, src.costcenter
                )";
            var param = new
            {
                serverip = serverIp,
                createuser = createUser,
                createip = createIp,
                createtime = createTime,
            };
            return da.DapperExecute(sql, param);
        }

        /// <summary>
        /// 更新 A_AREA = 3
        /// </summary>
        /// <returns></returns>
        public int UpdateAAreaByBaseTeamOwnerM()
        {
            var sql = @"
                UPDATE base_authority_m set a_area = '3' WHERE a_id IN (
                    SELECT DISTINCT a_id FROM base_employee_m WHERE empid IN ( SELECT teamownerid FROM base_teamowner_m )
                )";
            return da.DapperExecute(sql);
        }





    }
}
