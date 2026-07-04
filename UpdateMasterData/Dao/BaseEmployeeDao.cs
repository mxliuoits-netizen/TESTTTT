using PIC.DB;
using PIC.DB.Dao;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UpdateMasterData.Model.EmployeeVice;

namespace UpdateMasterData.Dao;

/// <summary>
/// 員工主檔 DAO
/// </summary>
public class BaseEmployeeDao : BaseDao
{
    public BaseEmployeeDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 取得 員工主檔資料
    /// </summary>
    /// <returns></returns>
    public List<BaseEmployeeModel> GetBaseEmployeeMList()
    {
        var sql = @"
                SELECT 
                    NULL AS DataFlag, NULL AS Log_CreateTime, 
                    ServerIP, VersionId, CreateUser, CreateIP, CreateTime, UpdateUser, UpdateIP, UpdateTime, EmpId, EmpName, EmpNickName, EmpPwd, IsActive, Role, 
                    TeamId, A_Id, A_Name, Effectivedate, ExpireDate, IsManager, IsManageArr, IsNoCheckArr, DataSource, Sex, PTFT, Birthday, Note, ReportTeamId, 
                    ReportA_Id, CostId, AttendCard1, AttendCard1_UpdateUser, AttendCard1_UpdateTime, AttendCard2, AttendCard2_UpdateUser, AttendCard2_UpdateTime, RealOnBoardDate,
                    transfer_update_time, position_id, hr_zone, outside, email, group_type, job_code
                FROM Base_Employee_m ";
        return da.DapperQuery<BaseEmployeeModel>(sql).ToList();
    }

    /// <summary>
    /// 清空 員工主檔
    /// </summary>
    public void TruncateBaseEmployeeM()
    {
        var sql = " TRUNCATE TABLE Base_Employee_m ";
        da.DapperExecute(sql);
    }

    /// <summary>
    /// 新增 員工主檔
    /// </summary>
    /// <param name="dataList"></param>
    /// <returns></returns>
    public int InsertBaseEmployeeM(List<BaseEmployeeModel> dataList)
    {
        var insertHeaderSql = @"
                INSERT INTO Base_Employee_m(
                    ServerIP, VersionId, CreateUser, CreateIP, CreateTime, UpdateUser, UpdateIP, UpdateTime, EmpId, EmpName, EmpNickName, EmpPwd, IsActive, 
                    Role, TeamId, A_Id, A_Name, Effectivedate, ExpireDate, IsManager, IsManageArr, IsNoCheckArr, DataSource, Sex, PTFT, Birthday, 
                    Note, ReportTeamId, ReportA_Id, CostId, AttendCard1, AttendCard1_UpdateUser, AttendCard1_UpdateTime, AttendCard2, AttendCard2_UpdateUser, 
                    AttendCard2_UpdateTime, RealOnBoardDate, transfer_update_time, position_id, hr_zone, outside, email, group_type, job_code) values ";
        var paramAllList = dataList.Where(x => x.Empid != null & x.Emppwd != null & x.Isactive != null & x.Role != null & x.AId != null & x.Effectivedate != null
                & x.Expiredate != null & x.Ismanager != null & x.Ismanagearr != null & x.Isnocheckarr != null & x.Datasource != null & x.Sex != null & x.Ptft != null)
            .Select(x => new
            {
                ServerIP = x.Serverip,
                VersionId = x.Versionid,
                CreateUser = x.Createuser,
                CreateIP = x.Createip,
                CreateTime = x.Createtime,
                UpdateUser = x.Updateuser,
                UpdateIP = x.Updateip,
                UpdateTime = x.Updatetime,
                EmpId = x.Empid,
                EmpName = x.Empname,
                EmpNickName = x.Empnickname,
                EmpPwd = x.Emppwd,
                IsActive = x.Isactive,
                Role = x.Role,
                TeamId = x.Teamid,
                A_Id = x.AId,
                A_Name = x.AName,
                Effectivedate = x.Effectivedate,
                ExpireDate = x.Expiredate,
                IsManager = x.Ismanager,
                IsManageArr = x.Ismanagearr,
                IsNoCheckArr = x.Isnocheckarr,
                DataSource = x.Datasource,
                Sex = x.Sex,
                PTFT = x.Ptft,
                Birthday = x.Birthday,
                Note = x.Note,
                ReportTeamId = x.Reportteamid,
                ReportA_Id = x.ReportaId,
                CostId = x.Costid,
                AttendCard1 = x.Attendcard1,
                AttendCard1_UpdateUser = x.Attendcard1Updateuser,
                AttendCard1_UpdateTime = x.Attendcard1Updatetime,
                AttendCard2 = x.Attendcard2,
                AttendCard2_UpdateUser = x.Attendcard1Updateuser,
                AttendCard2_UpdateTime = x.Attendcard2Updatetime,
                RealOnBoardDate = x.Realonboarddate,
                TransferUpdateTime = x.TransferUpdateTime,
                PositionId = x.PositionId,
                HrZone = x.HrZone,
                Outside = x.Outside,
                Email = x.Email,
                GroupType = x.GroupType,
                JobCode = x.JobCode
            }).ToList();
        return this.BatchExecute(insertHeaderSql, paramAllList);
    }

    /// <summary>
    /// 新增 員工主檔紀錄
    /// </summary>
    /// <param name="dataList"></param>
    /// <returns></returns>
    public int InsertBaseEmployeeLog(List<BaseEmployeeModel> dataList)
    {
        var insertHeaderSql = @" INSERT INTO Base_Employee_m_Log(
                    DataFlag, Log_CreateTime, ServerIP, VersionId, CreateUser, CreateIP, CreateTime, UpdateUser, UpdateIP, UpdateTime, EmpId, 
                    EmpName, EmpNickName, EmpPwd, IsActive, Role, TeamId, A_Id, A_Name, Effectivedate, ExpireDate, IsManager, IsManageArr, 
                    IsNoCheckArr, DataSource, Sex, PTFT, Birthday, Note, ReportTeamId, ReportA_Id, CostId, AttendCard1, AttendCard1_UpdateUser, 
                    AttendCard1_UpdateTime, AttendCard2, AttendCard2_UpdateUser, AttendCard2_UpdateTime, RealOnBoardDate,
                    transfer_update_time, position_id, hr_zone, outside, email, group_type, job_code
                ) values ";

        var paramAllList = dataList.Select(x => new
        {
            DataFlag = x.Dataflag,
            Log_CreateTime = x.LogCreatetime,
            ServerIP = x.Serverip,
            VersionId = x.Versionid,
            CreateUser = x.Createuser,
            CreateIP = x.Createip,
            CreateTime = x.Createtime,
            UpdateUser = x.Updateuser,
            UpdateIP = x.Updateip,
            UpdateTime = x.Updatetime,
            EmpId = x.Empid,
            EmpName = x.Empname,
            EmpNickName = x.Empnickname,
            EmpPwd = x.Emppwd,
            IsActive = x.Isactive,
            Role = x.Role,
            TeamId = x.Teamid,
            A_Id = x.AId,
            A_Name = x.AName,
            Effectivedate = x.Effectivedate,
            ExpireDate = x.Expiredate,
            IsManager = x.Ismanager,
            IsManageArr = x.Ismanagearr,
            IsNoCheckArr = x.Isnocheckarr,
            DataSource = x.Datasource,
            Sex = x.Sex,
            PTFT = x.Ptft,
            Birthday = x.Birthday,
            Note = x.Note,
            ReportTeamId = x.Reportteamid,
            ReportA_Id = x.ReportaId,
            CostId = x.Costid,
            AttendCard1 = x.Attendcard1,
            AttendCard1_UpdateUser = x.Attendcard1Updateuser,
            AttendCard1_UpdateTime = x.Attendcard1Updatetime,
            AttendCard2 = x.Attendcard2,
            AttendCard2_UpdateUser = x.Attendcard2Updateuser,
            AttendCard2_UpdateTime = x.Attendcard2Updatetime,
            RealOnBoardDate = x.Realonboarddate,
            TransferUpdateTime = x.TransferUpdateTime,
            PositionId = x.PositionId,
            HrZone = x.HrZone,
            Outside = x.Outside,
            Email = x.Email,
            GroupType = x.GroupType,
            JobCode = x.JobCode
        });
        return this.BatchExecute(insertHeaderSql, paramAllList);
    }

    /// <summary>
    /// 更新 排班不檢核標示
    /// </summary>
    /// <returns></returns>
    public int UpdateNoCheckArrange()
    {
        var sql = @"
            with temp_empid as (
                select 
                    bed.empid, 
                    case when @par_date between bed.startdate and bed.enddate and bed.isactive = 'Y' then 'Y' else 'N' end as isnocheckarr
                from base_employee_department_isnocheckarr bed 
            )
            update base_employee_m set isnocheckarr = te.isnocheckarr
            from temp_empid te
            where base_employee_m.empid = te.empid ";
        return da.DapperExecute(sql, new { par_date = DateTime.Now.Date });
    }

    /// <summary>
    /// 更新 base_employee_m 從base_employee_transfer_history 回壓
    /// </summary>
    /// <param name="createuser"></param>
    /// <param name="createip"></param>
    /// <returns></returns>
    public int UpdateEmployeeByEmployeeTransferHistory(string createuser, string createip)
    {
        var sql = @"
                MERGE INTO base_employee_m AS tgt
                USING (
                    select emp_id, max(start_transfer_date) as start_transfer_date, max(end_transfer_date) as end_transfer_date 
                    from base_employee_transfer_history
                    group by emp_id
                ) AS src
                ON (tgt.empid = src.emp_id)
                WHEN MATCHED AND (tgt.effectivedate != src.start_transfer_date or src.end_transfer_date != '9999-12-31'::date)
                THEN UPDATE SET
                    effectivedate = src.start_transfer_date, expiredate = src.end_transfer_date, updateuser = @createuser, updateip = @createip, updatetime = @createtime";

        var param = new
        {
            createuser,
            createip,
            createtime = DateTime.Now
        };

        return da.DapperExecute(sql, param);
    }





}
