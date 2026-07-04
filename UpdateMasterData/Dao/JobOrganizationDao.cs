using Dapper;
using PIC.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UpdateMasterData.Model.Organization;

namespace UpdateMasterData.Dao
{
    /// <summary>
    /// 組織匯入資料 DAO 
    /// </summary>
    public class JobOrganizationDao
    {
        IDataAccess da;

        public JobOrganizationDao(IDataAccess da) {
            this.da = da;
        }

        /// <summary>
        /// 取得本次最新組織資料 排除Base_Team_exception_m啟用設定名單
        /// </summary>
        /// <returns></returns>

        public List<JobOrganizationModel> GetJobOrganizationList()
        {
            var sql = @"
                select 
                    OT.id, OT.cost_id, OT.team_id, OT.team_name , OP.team_id as parent_team_Id, OP.team_name as parent_team_name, OT.team_id as sap_team_id,
                    OT.hr_sub_area_id, OT.manager_emp_id, OT.team_type
                from Job_Organization_tmp OT
                left join Job_Organization_tmp OP on OT.team_parent_id = OP.team_id
                where OT.team_id not in (select TeamId from Base_Team_exception_m where IsActive = 'Y')";
 
            var result = da.DapperQuery<JobOrganizationModel>(sql).ToList();
            //將單位代碼額外補0至10碼
            result.ForEach(x => {
                x.ParentTeamId = x.ParentTeamId?.PadLeft(10, '0');
                x.TeamId = x.TeamId.PadLeft(10, '0');
                x.SapTeamId = x.SapTeamId.PadLeft(10, '0');
            });

            return result;
        }

        /// <summary>
        /// (備份)上一版組織主檔[Job_Organization_tmp]
        /// </summary>
        /// <param name="createTime"></param>

        public void BackupJonOrganizationTempToBak(DateTime createTime)
        {
            var sql = @"
                INSERT INTO job_organization_bak (cost_id, team_name, team_id, team_parent_id, hr_sub_area_id, team_type, is_import, backup_time)
                SELECT cost_id ,team_name ,team_id, team_parent_id ,hr_sub_area_id ,team_type, is_import, @createTime
                FROM Job_Organization_tmp";
            var result = da.DapperExecute(sql, new { createTime });
        }

        /// <summary>
        /// 標記 有匯入資料
        /// </summary>
        /// <param name="teamIdList"></param>
        /// <returns></returns>

        public int UpdateIsImport(List<long> teamIdList)
        {
            if (teamIdList == null || teamIdList.Count == 0) return 0;

            var sql = @" update job_organization_tmp set is_import = true where id in ";
            var param = new DynamicParameters();
            var i = 0;
            foreach( var teamId in teamIdList)
            {
                param.Add("@p" + i++, teamId);
            }
            sql += $" ({string.Join(",", param.ParameterNames.Select(x => "@" + x))})";
            i = da.DapperExecute(sql, param);
            return i;
        }

    }
}
