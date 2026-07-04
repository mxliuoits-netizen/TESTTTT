using PIC.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UpdateMasterData.Model.Organization;

namespace UpdateMasterData.Dao
{
    /// <summary>
    /// 單位組織表 DAO
    /// </summary>
    public class BaseTeamDao
    {
        IDataAccess da;

        public BaseTeamDao(IDataAccess da) {
            this.da = da;
        }

        /// <summary>
        /// 取得現有組織
        /// </summary>
        /// <returns></returns>
        public List<BaseEmployeeModel> GetBaseTeamMData()
        {
            var sql = @"
                select 
                    OT.CostId as cost_id, OT.TeamId as team_id, OT.TeamName as team_name, OD.TeamId as parent_team_Id, OD.TeamName as parent_team_name, OT.SapTeamId as sap_team_id,
                    OT.hr_sub_area_id, OT.manager_emp_id, OT.team_type, OT.isActive as is_active
                from Base_Team_m OT
                left join Base_Team_m OD on OT.ParentId = OD.TeamId and OT.IsActive = 'Y'
                where OT.TeamId not in (select TeamId from Base_Team_exception_m where IsActive = 'Y')";
            var result = da.DapperQuery<BaseEmployeeModel>(sql).ToList();
            return result;
        }

        /// <summary>
        /// 清空暫存組織主檔 Base_Team_tmp
        /// </summary>
        public void TruncateBaseTeamTmp()
        {
            var sql = "TRUNCATE TABLE Base_Team_tmp;";
            da.DapperExecute(sql);
        }

        /// <summary>
        /// 例外名單 寫入至 [Base_Team_temp]
        /// </summary>
        /// <param name="serverIP"></param>
        /// <param name="createUser"></param>
        /// <param name="createIp"></param>
        /// <param name="createTime"></param>
        public void CopyBaseTeamMExceptionToTemp(string serverIP, string createUser, string createIp, DateTime createTime)
        {
            var sql = @"
                INSERT INTO Base_Team_tmp
                (ServerIP ,VersionId ,CreateUser ,CreateIP ,CreateTime, UpdateUser ,UpdateIP ,UpdateTime, 
                IsActive ,ParentId ,TeamId ,TeamName ,CostId ,SapTeamId, hr_sub_area_id, manager_emp_id, team_type)
                select @serverIP, '0', @createUser, @createIp, @createTime,  @createUser, @createIp, @createTime,
                        'Y' as IsActive, T.ParentId, T.TeamId, T.TeamName, T.CostId, T.SapTeamId,
                        T.hr_sub_area_id, T.manager_emp_id, T.team_type
                from Base_Team_m T
                inner join Base_Team_exception_m TExp on T.TeamId = TExp.TeamId and TExp.IsActive = 'Y' "; //and T.IsActive = 'Y' 
            var param = new
            {
                serverIP,
                createUser,
                createIp,
                createTime,
            };
            da.DapperExecute(sql, param);
        }


        /// <summary>
        /// 同步現有組織主檔(Base_Team_m)至暫存組織資料(Base_Team_tmp)
        /// </summary>
        public void BackupBaseTeamMToTemp()
        {
            var sql = @"
                INSERT INTO Base_Team_tmp
                (ServerIP ,VersionId ,CreateUser ,CreateIP ,CreateTime ,UpdateUser ,UpdateIP ,UpdateTime, 
                IsActive ,ParentId ,TeamId ,TeamName ,CostId ,SapTeamId,hr_sub_area_id, manager_emp_id, team_type)
                SELECT 
                    ServerIP ,VersionId ,CreateUser ,CreateIP ,CreateTime ,UpdateUser ,UpdateIP ,UpdateTime,
                    IsActive ,ParentId ,TeamId ,TeamName ,CostId ,SapTeamId,hr_sub_area_id, manager_emp_id, team_type
			    FROM Base_Team_m 
                WHERE teamid not in ( select teamid from Base_Team_exception_m where isactive = 'Y' ) ";
            da.DapperExecute(sql);
        }

        /// <summary>
        /// 已失效組織主檔 (@tmp_newOrganization有、因為在舊有主檔中已失效，未被抓進 @tmp_oldOrganization 中的)
        /// </summary>
        /// <param name="teamIdList"></param>
        /// <param name="serverIp"></param>
        /// <param name="createUser"></param>
        /// <param name="createIp"></param>
        /// <param name="createTime"></param>
        public void UpdateBaseTeamTempWithFakeDelete(List<string> teamIdList, string serverIp, string createUser, string createIp, DateTime createTime)
        {
            var sql = @"
                UPDATE Base_Team_tmp
	            SET ServerIP = @serverIp,
                    UpdateUser = @createUser,
                    UpdateIP = @createIp,
                    UpdateTime = @createTime,
                    IsActive = 'Y',
                    Note = to_char(@createTime, 'yyyyMMdd') || '恢復生效'
	            WHERE Base_Team_tmp.TeamId = @teamId ";

            var paramList = new List<dynamic>(); 
            foreach(string teamId in teamIdList)
            {
                paramList.Add(new
                {
                    serverIp,
                    createUser,
                    createIp,
                    createTime,
                    teamId
                });
            }
            da.DapperExecute(sql, paramList);
        }

        /// <summary>
        /// 新增組織主檔資料
        /// </summary>
        /// <param name="addList"></param>
        /// <param name="serverIp"></param>
        /// <param name="createUser"></param>
        /// <param name="createIp"></param>
        /// <param name="createTime"></param>

        public void AddBaseTeamTemp(List<JobOrganizationModel> addList, string serverIp, string createUser, string createIp, DateTime createTime)
        {
            var sql = @"select teamId from base_team_tmp";
            var teamIdInTempList = da.DapperQuery<string>(sql).ToList();
            var realAddList = addList.Where(x => !teamIdInTempList.Contains(x.TeamId)).ToList();

            var insertSQl = @"
                INSERT INTO Base_Team_tmp
                (ServerIP ,VersionId ,CreateUser ,CreateIP ,CreateTime, IsActive , ParentId , TeamId , TeamName , CostId , SapTeamId, hr_sub_area_id, manager_emp_id, team_type)
                values 
                (@serverIp, '0', @createUser, @createIp, @createTime, 'Y', @ParentTeamId, @TeamId, @TeamName, @CostId, @SapTeamId, @HrSubAreaId, @ManagerEmpId, @TeamType)
                ";

            var paramList = new List<dynamic>();
            foreach (JobOrganizationModel m in realAddList)
            {
                paramList.Add(new
                {
                    serverIp,
                    createUser,
                    createIp,
                    createTime,
                    m.ParentTeamId,
                    m.TeamId,
                    m.TeamName,
                    m.CostId,
                    m.SapTeamId,
                    m.HrSubAreaId,
                    m.ManagerEmpId,
                    m.TeamType
                });             
            }
            da.DapperExecute(insertSQl, paramList);
        }

        /// <summary>
        /// (更新)既有組織主檔資料
        /// </summary>
        /// <param name="editList"></param>
        /// <param name="serverIp"></param>
        /// <param name="createUser"></param>
        /// <param name="createIp"></param>
        /// <param name="createTime"></param>
        public void UpdateBaseTeamTemp(List<JobOrganizationModel> editList, string serverIp, string createUser, string createIp, DateTime createTime)
        {
            var sql = @"
                UPDATE Base_Team_tmp 
                SET 
                    ServerIP = @serverIp, UpdateUser = @createUser, UpdateIP = @createIp, UpdateTime = @createTime, IsActive = 'Y',
                    ParentId = @ParentTeamId, TeamName = @TeamName, CostId = @CostId, SapTeamId = @SapTeamId, hr_sub_area_id = @HrSubAreaId, manager_emp_id = @ManagerEmpId, team_type = @TeamType
                WHERE Base_Team_tmp.TeamId = @TeamId";

            var paramList = new List<dynamic>();
            foreach (var m in editList){
                paramList.Add(new
                {
                    serverIp,
                    createUser,
                    createIp,
                    createTime,
                    m.ParentTeamId,
                    m.TeamName,
                    m.CostId,
                    m.SapTeamId,
                    m.HrSubAreaId,
                    m.ManagerEmpId,
                    m.TeamType,
                    m.TeamId
                });
            }
            da.DapperExecute(sql, paramList);
        }

        public int UpdateDeleteMarkBaseTeamTemp(List<string> deleteList, string serverIp, string createUser, string createIp, DateTime createTime)
        {
            var sql = @"
                UPDATE Base_Team_tmp
                SET 
                    ServerIP = @serverIp, UpdateUser = @createUser, UpdateIP = @createIp, UpdateTime = @createTime, IsActive = 'N'
                WHERE TeamId = @teamId";

            var paramList = new List<dynamic>();
            foreach(var teamId in deleteList)
            {
                paramList.Add(new
                {
                    serverIp,
                    createUser,
                    createIp,
                    createTime,
                    teamId
                });
            }
            var i = da.DapperExecute(sql, paramList);
            return i;
        }

        /// <summary>
        /// 資料筆數
        /// </summary>
        /// <returns></returns>
        public int CountBaseTeamM()
        {
            return da.DapperQuery<int>("select count(1) from base_team_m").First();
        }

        /// <summary>
        /// (備份)上一版正式組織資料[Base_Team_m]至[Base_Team_bak]
        /// </summary>
        /// <param name="createTime"></param>
        public void BackupBaseTeamMToBak(DateTime createTime)
        {
            var sql = @"
                INSERT INTO Base_Team_bak
                (ServerIP, VersionId, CreateUser, CreateIP, CreateTime, UpdateUser, UpdateIP, UpdateTime, IsActive,
                ParentId, TeamId, TeamName, CostId, SapTeamId, hr_sub_area_id, manager_emp_id, team_type, BackupTime)
                SELECT 
                    ServerIP, VersionId, CreateUser, CreateIP, CreateTime, UpdateUser, UpdateIP, UpdateTime, IsActive, 
                    ParentId, TeamId ,TeamName ,CostId, SapTeamId, hr_sub_area_id, manager_emp_id, team_type, @createTime
				FROM Base_Team_m";
            da.DapperExecute(sql, new { createTime });
        }

        /// <summary>
        /// 清空暫存組織主檔 base_team_m
        /// </summary>
        public void TruncateBaseTeamM()
        {
            var sql = "TRUNCATE TABLE base_team_m;";
            da.DapperExecute(sql);
        }

        /// <summary>
        /// (更新)正式組織資料
        /// </summary>

        public void AddBaseTeamMFromTemp()
        {
            var sql = @"
                INSERT INTO Base_Team_m
                (ServerIP ,VersionId, CreateUser, CreateIP, CreateTime, UpdateUser, UpdateIP, UpdateTime, 
                IsActive, ParentId, TeamId, TeamName, CostId, SapTeamId, hr_sub_area_id, manager_emp_id, team_type)
			    SELECT 
                    ServerIP, VersionId, CreateUser, CreateIP, CreateTime, UpdateUser, UpdateIP, UpdateTime, 
                    IsActive , ParentId, TeamId, TeamName, TRIM(CostId), SapTeamId, hr_sub_area_id, manager_emp_id, team_type
				FROM Base_Team_tmp";
            da.DapperExecute(sql);
        }


    }
}
