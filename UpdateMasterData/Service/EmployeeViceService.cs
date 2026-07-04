using PIC.DB;
using PIC.Tools;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using UpdateMasterData.Dao;
using UpdateMasterData.Model;
using UpdateMasterData.Model.EmployeeDiff;
using UpdateMasterData.Model.EmployeeVice;
using UpdateMasterData.Model.Enum;

namespace UpdateMasterData.Service;

/// <summary>
/// 更新員工主檔 服務
/// ref: sp_UpdateEmployee_CosmedHQ
/// </summary>
public class EmployeeViceService : BaseService
{
    MailTemplateService MailTemplateService;

    public EmployeeViceService(IDataAccess da, JobMetaDataModel jobMetaData)
    {
        this.JobMetaData = jobMetaData;
        this.SystemJobEventLogDao = new PIC.Libary.Dao.SystemJobEventLogDao(da);
        this.MailTemplateService = new MailTemplateService(da);
    }

    /// <summary>
    /// 更新員工主表
    /// </summary>
    public void UpdateEmployee()
    {
        string checkFlag = string.Empty;
        string maxDiffRate = string.Empty;
        string teamTypeWhite = string.Empty;
        bool isCanUpdate = true;
        int minPWDLength = 8;

        using var businessDa = DBDataAccessFactory.GetInstance(JobMetaData.DBType, JobMetaData.DBPrefix);
        var systemConfigDao = new SystemConfigDao(businessDa);
        var baseAuthorityMDao = new BaseAuthorityMDao(businessDa);
        var baseEmployeeDao = new BaseEmployeeDao(businessDa);
        var baseEmployeeExceptionMDao = new BaseEmployeeExceptionMDao(businessDa);
        var jobEmployeeViceDao = new JobEmployeeViceDao(businessDa);
        //var jobEmployeeDiffDao = new JobEmployeeDiffDao(businessDa);
        //var baseEmployeeTransferDao = new BaseEmployeeTransferDao(businessDa);
        var baseArrangeGroupDao = new BaseArrangeGroupDao(businessDa);
        var baseEmployeeTransferHistoryDao = new BaseEmployeeTransferHistoryDao(businessDa);
        var jobIdentityChangeTmpDao = new JobIdentityChangeTmpDao(businessDa);
        var maiNormalMDao = new MaiNormalMDao(businessDa);
        var baseAuthorityfunctionlistMDao = new BaseAuthorityfunctionlistMDao(businessDa);
        var baseTeamOwnerDao = new BaseTeamOwnerDao(businessDa);
        var baseAttendPersonTypeMDao = new BaseAttendPersonTypeMDao(businessDa);
        var baseWorkExceptionEarlyAccessDao = new BaseWorkExceptionEarlyAccessDao(businessDa);
        var jobEmployeeviceTDao = new JobEmployeeviceTDao(businessDa);
        var baseEmployeeCostMDao = new BaseEmployeeCostMDao(businessDa);
        //test
        //businessDa.IsEnableLog = true;
        try
        {
            businessDa.BeginTransaction();

            LogInfo("EmployeeViceService.UpdateEmployee開始");
            checkFlag = systemConfigDao.GetItemValue("UpdateEmployee", "CheckFlag");
            maxDiffRate = systemConfigDao.GetItemValue("UpdateEmployee", "MaxDiffRate");
            teamTypeWhite = systemConfigDao.GetItemValue("UpdateEmployee", "TeamTypeWhiteList");
            var minPWDLengthText = systemConfigDao.GetItemValue("RecoverPwd", "MinPwdLength") ?? "8";
            var officialOnlineDateText = systemConfigDao.GetItemValue("fn_work_exception", "official_online_date") ?? "2025-04-21";
            var officialOnlineDate = DateTime.ParseExact(officialOnlineDateText, "yyyy-MM-dd", null);
            int.TryParse(minPWDLengthText, out minPWDLength);
            if (minPWDLength < 4) minPWDLength = 4;
            var empTempLogList = new List<BaseEmployeeModel>();

            //紀錄變更表, 包括身分轉換

            // 1. 過濾白名單 不需要匯入update is_import = 'N'
            LogInfo($"過濾白名單({teamTypeWhite}), 其他不匯入");
            var whiteList = new List<string>();
            if (!string.IsNullOrEmpty(teamTypeWhite)) whiteList = teamTypeWhite.Split(",").ToList();
            jobEmployeeViceDao.UpdateByWhiteList(whiteList);

            // 2.EMP寫進TMP 
            LogInfo("Base_Employee_m資料寫進@tmp_Emp");
            var empTempList = baseEmployeeDao.GetBaseEmployeeMList();
            var origEmpList = baseEmployeeDao.GetBaseEmployeeMList();

            // 3 處理base_employee_exception_m 重新加入匯入暫存表 以及 base_authority_m
            LogInfo("處理base_employee_exception_m 重新加入匯入暫存表 以及 base_authority_m");
            var employeeExceptionMap = baseEmployeeExceptionMDao.GetMap();
            baseAuthorityMDao.MergeByExceptionEmployee(JobMetaData.ServerIp, JobMetaData.CreateUser, JobMetaData.CreateIp, JobMetaData.CreateTime);
            jobEmployeeViceDao.MergeByExceptionEmployee();

            // 3.1 取出匯入資料 格式化邏輯
            var baseAuthrorityList = baseAuthorityMDao.GetIEnumerable().ToList();
            var jobEmployeeTempList = jobEmployeeViceDao.GetJobEmployeeViceTmpList();

            //取出 diff 資料 20260629
            var jobEmployeeDiffDao = new JobEmployeeDiffDao(businessDa);
            var diffList = jobEmployeeDiffDao.GetJobEmployeeDiffList();
            var diffMap = diffList.GroupBy(x => x.Empid).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var x in jobEmployeeTempList)
            {

                if (diffMap.ContainsKey(x.Empid))
                {
                }

            }

            jobEmployeeTempList.ForEach(x =>
            {
                x.Sapteamid = x.Sapteamid?.PadLeft(10, '0'); //組織代碼 不足10碼 補0

                //20250407 發生SAPNAME 為NULL問題, Sapteamid=0000000000
                x.Sapaname = string.IsNullOrEmpty(x.Sapaname) ? "" : x.Sapaname;

                if (!String.IsNullOrEmpty(x.PositionId) && x.Sapaname.StartsWith(x.PositionId)) x.Sapaname = x.Sapaname.Replace(x.PositionId, "");// 職稱名稱 要replace 職稱代碼
                if (JobMetaData.JobDate >= x.TransferDate)
                {
                    x.Effectivedate = x.Effectivedate > (x.TransferDate ?? DateTime.MinValue) ? x.Effectivedate : x.TransferDate; //到職日 = MAX(到職/復職/調入日 || 到職日)
                }
            });

            // 4.更新VICE到TMP ，避免十天內無DIFF的資料，到第十一天要更新的資料在VICE中
            LogInfo("更新Job_EmployeeVice_tmp資料到@tmp_Emp");
            #region Linq 資料處裡
            // CheckDiff(jobEmployeeTempList, empTempList, baseAuthrorityList); //測試用檢視差異細節
            var editEmpList = (from EV in jobEmployeeTempList
                               join tmp_Emp in empTempList on new { EV.Empid } equals new { tmp_Emp.Empid }
                               join b in baseAuthrorityList on new { EV.Sapaname, EV.Sapteamid } equals new { b.Sapaname, b.Sapteamid } into bb
                               from A in bb.DefaultIfEmpty()
                               where EV.Empname != tmp_Emp.Empname || A?.AId != tmp_Emp.AId || A?.AName != tmp_Emp.AName || EV.Sex != tmp_Emp.Sex
                                 || EV.Ptft != tmp_Emp.Ptft //|| EV.Birthday != tmp_Emp.Birthday //此欄位拿來存PTFT轉換日
                                 || EV.Effectivedate != tmp_Emp.Effectivedate
                                 || EV.Expiredate != tmp_Emp.Expiredate || EV.Realonboarddate != tmp_Emp.Realonboarddate || EV.Costid != tmp_Emp.Costid
                                 || EV.TransferUpdateTime != tmp_Emp.TransferUpdateTime || EV.PositionId != tmp_Emp.PositionId || EV.HrZone != tmp_Emp.HrZone
                                 || EV.Outside != tmp_Emp.Outside || EV.Email != tmp_Emp.Email || EV.GroupType != tmp_Emp.GroupType || EV.JobCode != tmp_Emp.JobCode
                               select new BaseEmployeeModel(tmp_Emp)
                               {
                                   Dataflag = "4",
                                   LogCreatetime = DateTime.Now,
                                   Versionid = tmp_Emp.Versionid + 1,
                                   Teamid = EV.Sapteamid,
                                   Empname = EV.Empname,
                                   AId = A?.AId,
                                   AName = EV.Sapaname,
                                   Sex = EV.Sex,
                                   Ptft = EV.Ptft,
                                   Birthday = tmp_Emp.Ptft != EV.Ptft ? JobMetaData.JobDate : tmp_Emp.Birthday,
                                   Effectivedate = EV.Effectivedate,
                                   Isactive = EV.Expiredate == null || DateTime.Now <= EV.Expiredate ? "Y" : "N",
                                   Expiredate = EV.Expiredate ?? DateTime.Parse("9999/12/31"),
                                   Realonboarddate = EV.Realonboarddate,
                                   Costid = EV.Costid,
                                   TransferUpdateTime = EV.TransferUpdateTime,
                                   PositionId = EV.PositionId,
                                   HrZone = EV.HrZone,
                                   //純記錄原始資料
                                   Outside = EV.Outside,
                                   Email = EV.Email ?? "",
                                   GroupType = EV.GroupType,
                                   JobCode = EV.JobCode,
                                   Updateuser = JobMetaData.CreateUser,
                                   Updateip = JobMetaData.CreateIp,
                                   Updatetime = JobMetaData.CreateTime,
                                   Role = employeeExceptionMap.GetValueOrDefault(tmp_Emp.Empid, null)?.Role ?? tmp_Emp.Role
                               }).ToList();

            // 找到信件地址更新上來的
            var updateEmailEmpList = (from EV in jobEmployeeTempList
                                      join e in empTempList on new { EV.Empid } equals new { e.Empid }
                                      join b in baseAuthrorityList on new { EV.Sapaname, EV.Sapteamid } equals new { b.Sapaname, b.Sapteamid } into bb
                                      from A in bb.DefaultIfEmpty()
                                      where string.IsNullOrWhiteSpace(e.Email) && !string.IsNullOrWhiteSpace(EV.Email)
                                      select new BaseEmployeeModel()
                                      {
                                          Empid = EV.Empid,
                                          Empname = EV.Empname,
                                          Emppwd = GeneratePWDTool.GeneratePWD(minPWDLength),
                                          Email = EV.Email ?? "",
                                          Effectivedate = EV.Effectivedate
                                      }).ToList();
            var mapEmpDencryptPWD2 = updateEmailEmpList.Select(x => new { x.Empid, x.Emppwd }).ToDictionary(x => x.Empid, x => x.Emppwd);

            // 將補上EMAIL人員的新密碼壓回去
            updateEmailEmpList.ForEach(x =>
            {
                var tempEmp = editEmpList.FirstOrDefault(y => y.Empid == x.Empid);
                tempEmp.Emppwd = EncryptTool.EncryptSHA512(x.Emppwd);
            });

            #endregion
            empTempLogList.AddRange(editEmpList);
            empTempList = empTempList.Where(x => !editEmpList.Any(y => y.Empid == x.Empid)).ToList();
            empTempList.AddRange(editEmpList);



            // 5.新增VICE到TMP，從VICE新增的成員
            LogInfo("新增Job_EmployeeVice_tmp資料到@tmp_Emp");
            #region Linq 資料處裡
            var addEmpList = (from EV in jobEmployeeTempList
                              join e in empTempList on new { EV.Empid } equals new { e.Empid } into et
                              from tmp_Emp in et.DefaultIfEmpty()
                              join b in baseAuthrorityList on new { EV.Sapaname, EV.Sapteamid } equals new { b.Sapaname, b.Sapteamid } into bb
                              from A in bb.DefaultIfEmpty()
                              where tmp_Emp?.Empid == null
                              select new BaseEmployeeModel()
                              {
                                  Dataflag = "5",
                                  LogCreatetime = DateTime.Now,
                                  Serverip = JobMetaData.ServerIp,
                                  Versionid = 0,
                                  Createuser = JobMetaData.CreateUser,
                                  Createip = JobMetaData.CreateIp,
                                  Createtime = JobMetaData.CreateTime,
                                  Empid = EV.Empid,
                                  Empname = EV.Empname,
                                  Emppwd = GeneratePWDTool.GeneratePWD(minPWDLength),
                                  Isactive = "Y",
                                  Role = employeeExceptionMap.GetValueOrDefault(EV.Empid, null)?.Role ?? "user",
                                  Teamid = EV.Sapteamid,
                                  AId = A?.AId,
                                  AName = EV.Sapaname,
                                  Effectivedate = EV.Effectivedate,
                                  Expiredate = EV.Expiredate ?? DateTime.Parse("9999/12/31"),
                                  Ismanager = "N",
                                  Ismanagearr = "N",
                                  Isnocheckarr = "N",
                                  Datasource = "job",
                                  Sex = EV.Sex,
                                  Ptft = EV.Ptft,
                                  Birthday = EV.Birthday,
                                  Realonboarddate = EV.Realonboarddate,
                                  Costid = EV.Costid,
                                  TransferUpdateTime = EV.TransferUpdateTime,
                                  PositionId = EV.PositionId,
                                  HrZone = EV.HrZone,
                                  Outside = EV.Outside,
                                  Email = EV.Email ?? "",
                                  GroupType = EV.GroupType,
                                  JobCode = EV.JobCode
                              }).ToList();
            var mapEmpDencryptPWD = addEmpList.Select(x => new { x.Empid, x.Emppwd }).ToDictionary(x => x.Empid, x => x.Emppwd);
            addEmpList.ForEach(x =>
            {
                x.Emppwd = EncryptTool.EncryptSHA512(x.Emppwd);
            });
            #endregion
            empTempLogList.AddRange(addEmpList);
            empTempList.AddRange(addEmpList);

            // 6. 紀錄身分變更相關資料
            #region 紀錄身分變更相關資料
            //外勤線上打卡身分表
            var baseAttendPersonTypeList = baseAttendPersonTypeMDao.Query();

            //到職, 真正到職日等於生效日 而且empid不存在於原始資料
            jobIdentityChangeTmpDao.Truncate();
            DateTime addChangeNow = DateTime.Now.Date;

            foreach (var addEmp in addEmpList)
            {
                JobIdentityChangeTmpModel newEmpChange = new JobIdentityChangeTmpModel();
                newEmpChange.EmpId = addEmp.Empid;
                newEmpChange.IsDel = "N";
                //1 到職
                newEmpChange.ChangeType = "1";
                newEmpChange.CreateDate = addChangeNow;
                newEmpChange.CreateUser = "JOB";
                newEmpChange.UpdateDate = addChangeNow;
                newEmpChange.UpdateUser = "JOB";
                newEmpChange.A_Id = addEmp.AId;
                newEmpChange.TeamId = addEmp.Teamid;
                jobIdentityChangeTmpDao.Insert(newEmpChange);
            }

            var costList = baseEmployeeCostMDao.GetList();
            var costIdList = costList.Select(x => x.costid).ToList();
            //紀錄是否有發生需要重新排班的身分轉換
            foreach (var reEmp in empTempList)
            {
                if (reEmp.Isactive.ToUpper() == "N")
                {
                    LogInfo("非啟用資料:" + reEmp.Empid);
                    continue;
                }
                var origEmp = origEmpList.Where(x => x.Empid == reEmp.Empid).FirstOrDefault();
                if (origEmp == null)
                {
                    LogInfo("編輯資料不存在於原始資料:" + reEmp.Empid);
                    continue;
                }
                //1 到職, 2 復職(總部->商場聯服), 3 復職(商場聯服->總部), 4 身分轉換(總部->商場聯服), 5 身分轉換(商場－> 總部), (PT－> FT), 6 身分轉換(商場－> 總部), (FT－> PT)
                //發生身分轉換
                if (origEmp.GroupType != reEmp.GroupType)
                {
                    string changeType = "";
                    //復職
                    bool isReOnBoard = (origEmp.Isactive.ToUpper() == "N" &&
                        reEmp.Isactive.ToUpper() == "Y");

                    //如果group_type 從任何其他變成1,商場, 2.聯服
                    if (reEmp.GroupType == "1" || reEmp.GroupType == "2" &&
                        (origEmp.GroupType != "1" && origEmp.GroupType != "2"))
                    {
                        if (isReOnBoard)
                        {
                            //2 復職(總部->商場聯服)
                            changeType = "2";
                        }
                        else
                        {
                            //4 身分轉換(總部->商場聯服)
                            changeType = "4";
                        }
                    }
                    //如果group_type 從1,商場, 2.聯服變成任何其他type
                    else if (origEmp.GroupType == "1" || origEmp.GroupType == "2" &&
                        (reEmp.GroupType != "1" && reEmp.GroupType != "2"))
                    {
                        if (isReOnBoard)
                        {
                            // 3 復職(商場->總部)
                            changeType = "3";
                        }
                        else
                        {
                            //5 身分轉換(商場－> 總部)
                            changeType = "5";
                        }
                    }
                    else if (origEmp.GroupType == "9" && reEmp.GroupType == "9")
                    {
                        changeType = "3";
                    }

                    if (changeType != "")
                    {
                        JobIdentityChangeTmpModel reEmpChange = new JobIdentityChangeTmpModel();
                        reEmpChange.EmpId = reEmp.Empid;
                        reEmpChange.IsDel = "N";
                        reEmpChange.ChangeType = changeType;
                        reEmpChange.A_Id = reEmp.AId;
                        reEmpChange.TeamId = reEmp.Teamid;
                        reEmpChange.CreateDate = addChangeNow;
                        reEmpChange.CreateUser = "JOB";
                        reEmpChange.UpdateDate = addChangeNow;
                        reEmpChange.UpdateUser = "JOB";
                        jobIdentityChangeTmpDao.Insert(reEmpChange);
                    }
                }

                // 後勤成本中心轉換 C.後勤成本中心轉換
                var isChange = costIdList.Contains(origEmp.Costid) != costIdList.Contains(reEmp.Costid);
                if ((reEmp.GroupType == "3" || reEmp.GroupType == "9") && isChange)
                {
                    JobIdentityChangeTmpModel reEmpChange = new JobIdentityChangeTmpModel();
                    reEmpChange.EmpId = reEmp.Empid;
                    reEmpChange.IsDel = "N";
                    reEmpChange.ChangeType = "C";
                    reEmpChange.A_Id = reEmp.AId;
                    reEmpChange.TeamId = reEmp.Teamid;
                    reEmpChange.CreateDate = addChangeNow;
                    reEmpChange.CreateUser = "JOB";
                    reEmpChange.UpdateDate = addChangeNow;
                    reEmpChange.UpdateUser = "JOB";
                    jobIdentityChangeTmpDao.Insert(reEmpChange);
                }


            }
            #endregion

            // 7. 更新ISACTIVE='N' 將原資料表中 不存在當日匯入主檔 更新IsActive='N'
            LogInfo("更新ISACTIVE = N");
            var deleteEmpList = (from tmp_Emp in empTempList
                                 where tmp_Emp.Isnocheckarr == "N" && tmp_Emp.Isactive == "Y" && !jobEmployeeTempList.Any(y => y.Empid == tmp_Emp.Empid)
                                 select new BaseEmployeeModel(tmp_Emp)
                                 {
                                     Dataflag = "6",
                                     LogCreatetime = DateTime.Now,
                                     Versionid = tmp_Emp.Versionid + 1,
                                     Isactive = "N",
                                     Expiredate = jobEmployeeviceTDao.GetTransferUpdateTime(tmp_Emp.Empid, JobMetaData.JobDate),
                                     TransferUpdateTime = jobEmployeeviceTDao.GetTransferUpdateTime(tmp_Emp.Empid, JobMetaData.JobDate),
                                     Updateuser = JobMetaData.CreateUser,
                                     Updateip = JobMetaData.CreateIp,
                                     Updatetime = JobMetaData.CreateTime
                                 }).ToList();


            empTempLogList.AddRange(deleteEmpList);
            empTempList = empTempList.Where(x => !deleteEmpList.Any(y => y.Empid == x.Empid)).ToList();
            empTempList.AddRange(deleteEmpList);

            //檢核是否有重複資料
            var unUniqueList = empTempList.GroupBy(x => x.Empid).Select(x => new { Empid = x.Key, Count = x.Count() }).Where(x => x.Count > 1).ToList();
            if (unUniqueList != null && unUniqueList.Count > 0)
            {
                unUniqueList.ForEach(x =>
                {
                    var data = empTempList.Where(y => y.Empid == x.Empid).ToList();
                    var msg = $"有UK重複資料 EmpId={x.Empid}, Count={x.Count}, Data={data.ToString()}";
                    LogInfo(msg);
                });
            }

            //檢核異動比例
            LogInfo($"檢核異動比例: {checkFlag} 最大可異動比例: {maxDiffRate}");
            var countOldData = empTempList.Count();
            var countViceUpdate = empTempList.Where(x => x.Dataflag == "4").Count();
            var countViceInsert = empTempList.Where(x => x.Dataflag == "5").Count();
            var countViceDelete = empTempList.Where(x => x.Dataflag == "6").Count();
            var countNewData = countViceUpdate + countViceInsert + countViceDelete;
            var updateRatio = Convert.ToDecimal((countNewData * 100.0) / (countOldData > 0 ? countOldData : 1));
            var message = $"本次Vice[更新]筆數:{countViceUpdate}筆，本次Vice[新增]筆數:{countViceInsert}筆，本次Vice[刪除]筆數:{countViceDelete}筆，" +
                $"原資料總筆數:{countOldData}筆，本次異動比例:{updateRatio.ToString("#0.00")}";
            LogInfo(message);

            if ("Y".Equals(checkFlag) || "y".Equals(checkFlag))
            {
                if (updateRatio > Convert.ToDecimal(maxDiffRate)) //若本次異動比例超過最大可異動比例，則不可更新員工資料
                {
                    LogInfo($"本次異動比例為{updateRatio.ToString("#0.00")}，超過最大可異動比例{Convert.ToDecimal(maxDiffRate).ToString("#0.00")}'%!，因此不進行更新正式員工");
                    isCanUpdate = false;
                }
            }

            // 若本次異動比例無超過最大可異動比例，或不需檢核異動比例，則更新員工資料
            if (isCanUpdate)
            {
                // 8 更新Base_Authority_m
                LogInfo("權限更新到Base_Authority_m");
                var flags = new List<string>() { "4", "5" }; // 4: update 5: insert
                var maxAid = baseAuthorityMDao.GetMaxAId();
                var param = (from E in empTempList.Where(x => flags.Contains(x.Dataflag))
                             join b in baseAuthrorityList on new { Sapaname = E.AName, Sapteamid = E.Teamid } equals new { b.Sapaname, b.Sapteamid } into bb
                             from A in bb.DefaultIfEmpty()
                             where E.AId == null && E.AName != null
                             orderby E.AName
                             select new
                             {
                                 Teamid = E.Teamid,
                                 SapAname = E.AName,
                                 CostId = E.Costid,
                             }).Distinct()
                            .Select((x, i) => new
                            {
                                A_Id = maxAid + i + 1,
                                ServerIp = JobMetaData.ServerIp,
                                VersionId = 0,
                                CreateUser = JobMetaData.CreateUser,
                                CreatorIp = JobMetaData.CreateIp,
                                CreateTime = JobMetaData.CreateTime,
                                SapTeamId = x.Teamid,
                                SapAname = x.SapAname,
                                x.Teamid,
                                A_Name = x.SapAname,
                                A_Sort = 999,
                                A_Area = "4",
                                CostCenter = x.CostId
                            }).ToList();
                baseAuthorityMDao.InsertByJobEmployeeData(param);

                var baseAuthrorityMap = baseAuthorityMDao.GetIEnumerable().ToDictionary(x => $"'{x.Teamid}_{x.AName}'", x => x.AId);
                empTempList.ForEach(x =>
                {
                    x.AId = baseAuthrorityMap.GetValueOrDefault($"'{x.Teamid}_{x.AName}'", x.AId);
                });

                // 9. 更新外勤身分
                foreach (var emp in empTempList)
                {
                    UpdateOutside(emp, baseAttendPersonTypeList, baseAttendPersonTypeMDao);
                }

                // 10. TRUNCATE EMP
                LogInfo("清空Base_Employee_m");
                baseEmployeeDao.TruncateBaseEmployeeM();

                // 11. TMP 寫進 EMP
                LogInfo("將@tmp_Emp資料寫到Base_Employee_m");
                baseEmployeeDao.InsertBaseEmployeeM(empTempList); //有條件 某些欄位不可同時為空才能匯入

                // 12. 把LOG_TMP寫進LOG主檔
                LogInfo("將Employee Course暫存表資料寫到Base_Employee_m_Log");
                baseEmployeeDao.InsertBaseEmployeeLog(empTempLogList);

                // 13. 統整 base_authority_m, base_employee_m 對應轉換至 base_arrangegroup (僅新增 已有對應不處理)
                LogInfo("更新 對應預設組織群組base_arrangegroup");
                var baCount = baseArrangeGroupDao.MergeDistrictConsultant(JobMetaData.CreateUser);
                baCount += baseArrangeGroupDao.MergeTeamTypeJointServer(JobMetaData.CreateUser);
                baCount += baseArrangeGroupDao.MergeTeamTypeMallPersonnel(JobMetaData.CreateUser);
                baCount += baseArrangeGroupDao.MergeTeamTypeOtherPersonnel(JobMetaData.CreateUser);

                // 14. 將 修正員工資料 彙整至 base_employee_transfer_history
                /*LogInfo(" 將 修正員工資料 彙整至 base_employee_transfer_history");
                var transInList = jobEmployeeTempList.Where(x => x.Empid != null && JobMetaData.JobDate >= x.TransferDate?.Date).ToList();
                var inCount = baseEmployeeTransferHistoryDao.MergeTransferIn(transInList, JobMetaData.ServerIp, JobMetaData.CreateUser, JobMetaData.CreateIp);
                var transOutList = jobEmployeeTempList.Where(x => x.Empid != null && JobMetaData.JobDate == x.TransferUpdateTime?.Date)
                    .Select(x => new BaseEmployeeModel()
                    {
                        Empid = x.Empid,
                        TransferUpdateTime = x.TransferUpdateTime.Value.Date
                    }).ToList();
                var outCount = baseEmployeeTransferHistoryDao.MergeTransferOut(transOutList, JobMetaData.ServerIp, JobMetaData.CreateUser, JobMetaData.CreateIp);
                outCount += baseEmployeeTransferHistoryDao.MergeTransferOut(deleteEmpList, JobMetaData.ServerIp, JobMetaData.CreateUser, JobMetaData.CreateIp);

                // 15. 將 base_employee_transfer_history 回壓 base_employee_m effectivedate, expiredate
                LogInfo("將 base_employee_transfer_history 回壓 base_employee_m effectivedate, expiredate");
                var updateTransDateCount = baseEmployeeDao.UpdateEmployeeByEmployeeTransferHistory(JobMetaData.CreateUser, JobMetaData.CreateIp);*/

                // 16. 人員有少預設功能清單 補上預設功能權限
                LogInfo("人員有少預設功能清單 補上預設功能權限");
                var defaultFuncText = systemConfigDao.GetItemValue("UpdateEmployee", "DefaultFuncList") ?? "202,204,205,206,1001,1022";
                var defaultFuncList = defaultFuncText.Split(",").Select(x => Convert.ToInt32(x)).ToList();
                baseAuthorityfunctionlistMDao.InsertAllEmployeeByFuncId(defaultFuncList, JobMetaData.ServerIp, JobMetaData.CreateIp, JobMetaData.CreateUser);

                // 17. 寄送新進人員 帳號啟用密碼通知信件
                #region 寄送新進人員 帳號啟用密碼通知信件
                string MailPWD = systemConfigDao.GetValueWithName("UpdateEmployee", "IsMailPWD", "Y");
                var IsMailPWD = "Y" == MailPWD;
                LogInfo("寄送新進人員 帳號啟用密碼通知信件(Y:寄N:不寄):" + MailPWD);
                if (IsMailPWD)
                {
                    int shiftMailDay = systemConfigDao.GetValueWithName("UpdateEmployee", "MailPWDShiftDay", 1);
                    var mailBomList = addEmpList.Where(x => !String.IsNullOrEmpty(x.Email)) //當員工無MAIL排除不發信
                    .Select(x =>
                    {
                        var empPWD = mapEmpDencryptPWD.GetValueOrDefault(x.Empid);
                        var mail = new MailData<dynamic>()
                        {
                            MailTo = x.Email,
                            MailTemplateType = MailTemplateType.帳號啟用密碼通知,
                            DataFrom = this.GetType().Name,
                            MailSource = x.Empid,
                            Createtime = JobMetaData.JobDate > x.Effectivedate.Value ? DateTime.Now : x.Effectivedate.Value.AddDays(shiftMailDay),
                            Data = new
                            {
                                NotifyName = x.Empname,
                                Account = x.Empid,
                                NewPWD = empPWD
                            }
                        };
                        return mail;
                    }).ToList();

                    mailBomList.AddRange(updateEmailEmpList.Where(x => !string.IsNullOrEmpty(x.Email)) //當員工無MAIL排除不發信
                        .Select(x =>
                        {
                            var empPWD = mapEmpDencryptPWD2.GetValueOrDefault(x.Empid);
                            var mail = new MailData<dynamic>()
                            {
                                MailTo = x.Email,
                                MailTemplateType = MailTemplateType.帳號啟用密碼通知,
                                DataFrom = this.GetType().Name,
                                MailSource = x.Empid,
                                // 已超過到職日隔天時 則 立即發送, 尚未到到職日時 則 到職日+1D發送 
                                Createtime = JobMetaData.JobDate > x.Effectivedate.Value ? DateTime.Now : x.Effectivedate.Value.AddDays(shiftMailDay),
                                Data = new
                                {
                                    NotifyName = x.Empname,
                                    Account = x.Empid,
                                    NewPWD = empPWD
                                }
                            };
                            return mail;
                        }).ToList());

                    // 當尚未到系統正式上線日 執行過濾信件
                    if (JobMetaData.JobDate < officialOnlineDate)
                    {
                        var mailWhiteList = baseWorkExceptionEarlyAccessDao.GetAllowSendEmpIdList(JobMetaData.JobDate);
                        if (mailWhiteList != null && mailWhiteList.Count > 0)
                        {
                            mailBomList = mailBomList.Where(x => mailWhiteList.Contains(x.MailSource)).ToList();
                        }
                    }

                    var mailList = MailTemplateService.BuildMailContent(mailBomList);
                    maiNormalMDao.BatchInsert(mailList);
                }
                #endregion

                // 18. 紀錄本次匯入主檔(移除同日資料重新新增)
                LogInfo("紀錄本次匯入主檔(移除同日資料重新新增)");
                jobEmployeeviceTDao.DeleteByDateCurrent(JobMetaData.JobDate);
                jobEmployeeviceTDao.ImportData(JobMetaData.JobDate);

            }

            LogInfo("EmployeeViceService.UpdateEmployee結束");
            businessDa.Commit();


        }
        catch (Exception ex)
        {
            businessDa.Rollback();
            Console.WriteLine(ex.ToString());
            throw;
        }

    }

    /// <summary>
    /// 更新 排班不檢核標示
    /// </summary>
    /// <returns></returns>
    public int UpdateNoCheckArrange()
    {
        int count = 0;
        using var businessDa = DBDataAccessFactory.GetInstance(JobMetaData.DBType, JobMetaData.DBPrefix);
        try
        {
            LogInfo("EmployeeViceService.UpdateNoCheckArrange開始");
            businessDa.BeginTransaction();
            var baseEmployeeDao = new BaseEmployeeDao(businessDa);
            count = baseEmployeeDao.UpdateNoCheckArrange();
            businessDa.Commit();
            LogInfo("EmployeeViceService.EncryptEmployeePWD結束");
        }
        catch (Exception)
        {
            businessDa.Rollback();
            throw;
        }
        return count;
    }

    /// <summary>
    /// 重建 調班額外開放班別
    /// </summary>
    /// <returns></returns>
    public void RebuildWorktypeExtra()
    {
        using var businessDa = DBDataAccessFactory.GetInstance(JobMetaData.DBType, JobMetaData.DBPrefix);
        try
        {
            LogInfo("EmployeeViceService.RebuildWorktypeExtra開始");
            businessDa.BeginTransaction();
            var baseWorktypeExtraMDao = new BaseWorktypeExtraMDao(businessDa);
            baseWorktypeExtraMDao.RebuildWorktype(JobMetaData.JobDate);
            businessDa.Commit();
            LogInfo("EmployeeViceService.RebuildWorktypeExtra結束");
        }
        catch (Exception)
        {
            businessDa.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 測試用 檢視差異
    /// </summary>
    /// <param name="jobEmployeeTempList"></param>
    /// <param name="empTempList"></param>
    /// <param name="baseAuthrorityList"></param>
    public void CheckDiff(List<JobEmployeeViceModel> jobEmployeeTempList, List<BaseEmployeeModel> empTempList, List<BaseAuthorityMModel> baseAuthrorityList)
    {
        var editEmpList = (from EV in jobEmployeeTempList
                           join tmp_Emp in empTempList on new { EV.Empid } equals new { tmp_Emp.Empid }
                           join b in baseAuthrorityList on new { EV.Sapaname, EV.Sapteamid } equals new { b.Sapaname, b.Sapteamid } into bb
                           from A in bb.DefaultIfEmpty()
                           where EV.Empname != tmp_Emp.Empname || A?.AId != tmp_Emp.AId || A?.AName != tmp_Emp.AName || EV.Sex != tmp_Emp.Sex
                             || EV.Ptft != tmp_Emp.Ptft || EV.Birthday != tmp_Emp.Birthday || EV.Birthday != tmp_Emp.Birthday || EV.Effectivedate != tmp_Emp.Effectivedate
                             || EV.Expiredate != tmp_Emp.Expiredate || EV.Realonboarddate != tmp_Emp.Realonboarddate || EV.Costid != tmp_Emp.Costid
                             || EV.TransferUpdateTime != tmp_Emp.TransferUpdateTime || EV.PositionId != tmp_Emp.PositionId || EV.HrZone != tmp_Emp.HrZone
                             || EV.Outside != tmp_Emp.Outside || EV.Email != tmp_Emp.Email || EV.GroupType != tmp_Emp.GroupType || EV.JobCode != tmp_Emp.JobCode
                           select new { import = EV, origin = tmp_Emp, auth = A }).ToList();

        foreach (var m in editEmpList)
        {
            var EV = m.import;
            var tmp_Emp = m.origin;
            var A = m.auth;
            Console.WriteLine($"compare EV.Empid:{EV.Empid}:::");
            if (EV.Empname != tmp_Emp.Empname)
            {
                Console.WriteLine($"EV.Empname={EV.Empname}, tmp_Emp.Empname={tmp_Emp.Empname}");
            }
            if (A?.AId != tmp_Emp.AId)
            {
                Console.WriteLine($"A?.AId={A?.AId}, tmp_Emp.AId={tmp_Emp.AId}");
            }
            if (A?.AName != tmp_Emp.AName)
            {
                Console.WriteLine($"A?.AName={A?.AName}, tmp_Emp.AId={tmp_Emp.AName}");
            }
            if (EV.Sex != tmp_Emp.Sex)
            {
                Console.WriteLine($"EV.Sex={EV.Sex}, tmp_Emp.Sex={tmp_Emp.Sex}");
            }
            if (EV.Ptft != tmp_Emp.Ptft)
            {
                Console.WriteLine($"EV.Ptft={EV.Ptft}, tmp_Emp.Ptft={tmp_Emp.Ptft}");
            }
            if (EV.Birthday != tmp_Emp.Birthday)
            {
                Console.WriteLine($"EV.Birthday={EV.Birthday}, tmp_Emp.Birthday={tmp_Emp.Birthday}");
            }
            if (EV.Effectivedate != tmp_Emp.Effectivedate)
            {
                Console.WriteLine($"EV.Birthday={EV.Effectivedate}, tmp_Emp.Birthday={tmp_Emp.Effectivedate}");
            }
            if (EV.Effectivedate != tmp_Emp.Effectivedate)
            {
                Console.WriteLine($"EV.Birthday={EV.Effectivedate}, tmp_Emp.Birthday={tmp_Emp.Effectivedate}");
            }
            if (EV.Expiredate != tmp_Emp.Expiredate)
            {
                Console.WriteLine($"EV.Expiredate={EV.Expiredate}, tmp_Emp.Expiredate={tmp_Emp.Expiredate}");
            }
            if (EV.Realonboarddate != tmp_Emp.Realonboarddate)
            {
                Console.WriteLine($"EV.Realonboarddate={EV.Realonboarddate}, tmp_Emp.Realonboarddate={tmp_Emp.Realonboarddate}");
            }
            if (EV.Costid != tmp_Emp.Costid)
            {
                Console.WriteLine($"EV.Costid={EV.Costid}, tmp_Emp.Costid={tmp_Emp.Costid}");
            }
            if (EV.TransferUpdateTime != tmp_Emp.TransferUpdateTime)
            {
                Console.WriteLine($"EV.TransferUpdateTime={EV.TransferUpdateTime}, tmp_Emp.TransferUpdateTime={tmp_Emp.TransferUpdateTime}");
            }
            if (EV.PositionId != tmp_Emp.PositionId)
            {
                Console.WriteLine($"EV.PositionId={EV.PositionId}, tmp_Emp.PositionId={tmp_Emp.PositionId}");
            }
            if (EV.HrZone != tmp_Emp.HrZone)
            {
                Console.WriteLine($"EV.HrZone={EV.HrZone}, tmp_Emp.HrZone={tmp_Emp.HrZone}");
            }
            if (EV.Outside != tmp_Emp.Outside)
            {
                Console.WriteLine($"EV.Outside={EV.Outside}, tmp_Emp.Outside={tmp_Emp.Outside}");
            }
            if (EV.Email != tmp_Emp.Email)
            {
                Console.WriteLine($"EV.Email={EV.Email}, tmp_Emp.Email={tmp_Emp.Email}");
            }
            if (EV.GroupType != tmp_Emp.GroupType)
            {
                Console.WriteLine($"EV.GroupType={EV.GroupType}, tmp_Emp.GroupType={tmp_Emp.GroupType}");
            }
            if (EV.JobCode != tmp_Emp.JobCode)
            {
                Console.WriteLine($"EV.JobCode={EV.JobCode}, tmp_Emp.JobCode={tmp_Emp.JobCode}");
            }
        }
    }
    private void UpdateOutside(BaseEmployeeModel emp, List<BaseAttendPersonTypeMModel> baseAttendPersonTypeList, BaseAttendPersonTypeMDao baseAttendPersonTypeMDao)
    {
        const string LocalConsultantJobCode = "00000107";
        DateTime today = DateTime.Now.Date;

        //若是刪除此員工
        if (emp.Isactive.ToUpper() == "N")
        {
            //讓所有身分過期
            var personTypeList = baseAttendPersonTypeList.Where(x => x.EmpId == emp.Empid).ToList();
            if (personTypeList != null && personTypeList.Count() > 0)
            {
                foreach (var personType in personTypeList)
                {
                    //讓身分過期
                    personType.EndDate = today.AddDays(-1);
                    baseAttendPersonTypeMDao.Update(personType);
                }
            }
        }
        //否則更新
        else
        {
            //如果jobcode為00000107(區顧問) 停止外勤
            if (emp.JobCode == LocalConsultantJobCode)
            {
                //取得外勤身分設定
                var outsideList = baseAttendPersonTypeList.Where(x => x.EmpId == emp.Empid && x.Type.ToUpper() == "H").ToList();
                if (outsideList != null && outsideList.Count() > 0)
                {
                    foreach (var outside in outsideList)
                    {
                        //如果外勤身分存在 保留
                        //否則讓外勤身分過期
                        if (outside.EndDate.Date < today)
                        {
                            outside.EndDate = today.AddDays(-1);
                            baseAttendPersonTypeMDao.Update(outside);
                        }
                    }
                }
            }
            //否則預設為外勤
            else
            {
                var outsideList = baseAttendPersonTypeList.Where(x => x.EmpId == emp.Empid && x.Type.ToUpper() == "H").ToList();
                if (outsideList != null && outsideList.Count() > 0)
                {
                    foreach (var outside in outsideList)
                    {
                        //將起始日期設為到職日 將結束日期設為9999/12/31
                        outside.StartDate = emp.Effectivedate.Value.Date;
                        outside.EndDate = new DateTime(9999, 12, 31).Date;
                        baseAttendPersonTypeMDao.Update(outside);
                    }
                }
                else
                {
                    //新增一筆
                    var newOutside = new BaseAttendPersonTypeMModel()
                    {
                        EmpId = emp.Empid,
                        Type = "H",
                        StartDate = emp.Effectivedate.Value.Date,
                        EndDate = new DateTime(9999, 12, 31).Date
                    };
                    baseAttendPersonTypeMDao.Insert(newOutside);
                }
            }
        }
    }
}
