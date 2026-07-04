using PIC.DB;
using PIC.Libary.Dao;
using System;
using System.Collections.Generic;
using System.Linq;
using UpdateMasterData.Dao;
using UpdateMasterData.Model;
using UpdateMasterData.Model.EmployeeVice;
using UpdateMasterData.Model.Organization;
using SystemConfigDao = UpdateMasterData.Dao.SystemConfigDao;

namespace UpdateMasterData.Service
{
    /// <summary>
    /// 處理暫存資料轉入
    /// </summary>
    public class OrganizationService : BaseService
    {
        public OrganizationService(IDataAccess da, JobMetaDataModel jobMetaData) {
            this.JobMetaData = jobMetaData;         
            SystemJobEventLogDao = new SystemJobEventLogDao(da);
        }

        /// <summary>
        /// 匯入組織主檔
        /// 翻寫來源:sp_UpdateOrganization
        /// </summary>
        public void Import()
        {
            string checkFlag = string.Empty;
            string maxDiffRate = string.Empty;
            string teamTypeWhite = string.Empty;
            bool isCanUpdate = true;
            using var businessDa = DBDataAccessFactory.GetInstance(JobMetaData.DBType, JobMetaData.DBPrefix);
            //test
            //businessDa.IsEnableLog = true;
            try
            {
                businessDa.BeginTransaction();               
                var SystemConfigDao = new SystemConfigDao(businessDa);
                var BaseTeamDao = new BaseTeamDao(businessDa);
                var JobOrganizationTempDao = new JobOrganizationDao(businessDa);

                LogInfo("OrganizationService.Import 開始");
                checkFlag = SystemConfigDao.GetItemValue("UpdateOrganization", "CheckFlag");
                maxDiffRate = SystemConfigDao.GetItemValue("UpdateOrganization", "MaxDiffRate");
                teamTypeWhite = SystemConfigDao.GetItemValue("UpdateOrganization", "TeamTypeWhiteList");

                // 取得現有組織
                LogInfo("取得現有組織");
                var originTeamList = BaseTeamDao.GetBaseTeamMData();

                //取得本次最新組織資料
                LogInfo("取得本次最新組織資料");             
                var unFilterNewTeamList = JobOrganizationTempDao.GetJobOrganizationList();

                LogInfo("篩掉不需處理的例外名單");
                //排除(is_import=false) teamtype = null 以及 其子公司 
                var emptyTeamTypeList = unFilterNewTeamList.Where(x => String.IsNullOrEmpty(x.TeamType)).Select(x => x.TeamId).ToList();
                var notImportTeamidList = GetNotImportTeamidList(emptyTeamTypeList, unFilterNewTeamList);

                //篩掉不需處理的例外名單
                var teamTypeWhiteList = teamTypeWhite.Split(",").ToList();
                var newTeamList = unFilterNewTeamList.Where(x => teamTypeWhiteList.Contains(x.TeamType) && !notImportTeamidList.Contains(x.TeamId)).ToList();

                //更新紀錄至JobOranizationTmp IsImport
                JobOrganizationTempDao.UpdateIsImport(newTeamList.Select(x => x.Id).ToList());

                

                //例外名單 從originTeamList、@tmp_newOrganization 中移除，之後不處理
                //先前查詢時已排除

                //取得本次新組織(@tmp_newOrganization有、@tmp_oldOrganization沒有的，為新增的組織)
                LogInfo("取得本次新組織");
                var oldTeamIdList = originTeamList.Select(x => x.TeamId).ToList();
                var addTeamList = newTeamList.Where(x => !oldTeamIdList.Contains(x.TeamId)).ToList();

                //取得本次異動組織 (@tmp_newOrganization、@tmp_oldOrganization都有的，即目前生效，但資料不一樣)
                LogInfo("取得本次異動組織");
                var editTeamList = (from nd in newTeamList
                                   join old in originTeamList on nd.TeamId equals old.TeamId
                                   where (nd.TeamName ?? "") != (old.TeamName ?? "") 
                                   || (nd.ParentTeamId ?? "") != (old.ParentTeamId ?? "")
                                   || (nd.CostId ?? "") != (old.CostId ?? "")
                                   || (nd.SapTeamId ?? "") != (old.SapTeamId ?? "")
                                   || (nd.HrSubAreaId ?? "") != (old.HrSubAreaId ?? "")
                                   || (nd.ManagerEmpId ?? "") != (old.ManagerEmpId ?? "")
                                   || (nd.TeamType ?? "") != (old.TeamType ?? "")
                                    select nd).ToList();
                //CheckWitchDiff(newTeamList, originTeamList); //debug use.

                //取得本次失效組織 (@tmp_oldOrganization有、@tmp_newOrganization沒有的，為失效的組織)
                LogInfo("取得本次失效組織");
                var newTeamIdList = newTeamList.Select(x => x.TeamId).ToList();
                var deleteTeamList = originTeamList.Where(x => !newTeamIdList.Contains(x.TeamId) && x.isActive == "Y").Select(x=>x.TeamId).ToList();

                //更新暫存組織主檔 Base_Team_tmp【開始】
                LogInfo("更新暫存組織主檔[Base_Team_m]至[Base_Team_tmp]");
                BaseTeamDao.TruncateBaseTeamTmp();//清空暫存組織主檔 Base_Team_tmp
                BaseTeamDao.CopyBaseTeamMExceptionToTemp(JobMetaData.ServerIp, JobMetaData.CreateUser, JobMetaData.CreateIp, JobMetaData.CreateTime); //寫入例外名單之組織主檔, 且必須永遠生效(update active='Y') 
                BaseTeamDao.BackupBaseTeamMToTemp();//同步現有組織主檔(Base_Team_m)至暫存組織資料(Base_Team_tmp);

                //(備份)上一版組織主檔[Job_Organization_tmp]
                LogInfo("上一版組織主檔[Job_Organization_tmp]");
                JobOrganizationTempDao.BackupJonOrganizationTempToBak(JobMetaData.CreateTime);

                //(更新)已失效組織主檔 (@tmp_newOrganization有、因為在舊有主檔中已失效，未被抓進 @tmp_oldOrganization 中的)
                LogInfo("(更新)已失效組織主檔");
                var addTeamIdList = addTeamList.Select(x=>x.TeamId).ToList();
                BaseTeamDao.UpdateBaseTeamTempWithFakeDelete(addTeamIdList, JobMetaData.ServerIp, JobMetaData.CreateUser, JobMetaData.CreateIp, JobMetaData.CreateTime);

                //(新增)新增組織主檔資料
                LogInfo("(新增)新增組織主檔資料");
                BaseTeamDao.AddBaseTeamTemp(addTeamList, JobMetaData.ServerIp, JobMetaData.CreateUser, JobMetaData.CreateIp, JobMetaData.CreateTime);

                //(更新)既有組織主檔資料
                LogInfo("(更新)既有組織主檔資料");
                BaseTeamDao.UpdateBaseTeamTemp(editTeamList, JobMetaData.ServerIp, JobMetaData.CreateUser, JobMetaData.CreateIp, JobMetaData.CreateTime);

                //(更新)失效組織主檔資料
                LogInfo("(更新)失效組織主檔資料");
                BaseTeamDao.UpdateDeleteMarkBaseTeamTemp(deleteTeamList, JobMetaData.ServerIp, JobMetaData.CreateUser, this.JobMetaData.CreateIp, JobMetaData.CreateTime);

                //更新暫存組織主檔 Base_Team_tmp 【結束】
                LogInfo("更新暫存組織主檔結束");

                //檢核異動比例
                LogInfo($"檢核異動比例: {checkFlag} 最大可異動比例: {maxDiffRate}");
                var countOldData = BaseTeamDao.CountBaseTeamM(); // 644
                var countAdd = addTeamList.Count(); // 5
                var countDiff = editTeamList.Count(); //0
                var countDelete = deleteTeamList.Count(); // 644
                var countNewData = countAdd + countDiff + countDelete;
                var per = Convert.ToDecimal((countNewData * 100.0) / (countOldData > 0 ? countOldData:1));
                LogInfo($"原資料總筆數:{countOldData}筆，本次新增筆數:{countAdd}筆，異動筆數:{countDiff}筆，刪除筆數:{countDelete}筆" + 
                    $"，本次異動比例:{per.ToString("#0.00")}'%!");

                if("Y".Equals(checkFlag) || "y".Equals(checkFlag)) //要檢查資料
                {
                    if(per > Convert.ToDecimal(maxDiffRate)) //資料異動比例大於設定值，停止更新組織資料
                    {
                        LogInfo("本次異動比例超過最大可異動比例!，因此不進行更新正式組織");
                        isCanUpdate = false;
                    }
                }

                //若本次異動比例無超過最大可異動比例，或不需檢核異動比例，則更新組織資料
                if (isCanUpdate)
                {
                    //(備份)上一版正式組織資料[Base_Team_m]至[Base_Team_bak]
                    LogInfo("上一版正式組織資料");
                    BaseTeamDao.BackupBaseTeamMToBak(JobMetaData.CreateTime);
                    BaseTeamDao.TruncateBaseTeamM(); //(清空)正式組織資料 [Base_Team_m]

                    //(更新)正式組織資料
                    LogInfo("更新正式組織資料開始");
                    BaseTeamDao.AddBaseTeamMFromTemp();
                }

                LogInfo("OrganizationService.Import結束");
                businessDa.Commit();
            }catch (Exception)
            {
                businessDa.Rollback();
                throw;
            }
        }

        /// <summary>
        /// 遞迴 取得子類所有不匯入Empid
        /// </summary>
        /// <param name="nextList"></param>
        /// <param name="originList"></param>
        /// <returns></returns>

        private List<string> GetNotImportTeamidList(List<string> nextList, List<JobOrganizationModel> originList)
        {
            var result = new List<string>();
            result.AddRange(nextList);
            var toNextList = originList.Where(x => nextList.Contains(x.ParentTeamId)).Select(x=>x.TeamId).ToList();          
            if (toNextList == null || toNextList.Count == 0)
            {
                return nextList;
            }
            else
            {
                var reutrnList = GetNotImportTeamidList(toNextList, originList);               
                result.AddRange(reutrnList);
                return result;
            }
            
        }

        /// <summary>
        /// 除錯用
        /// </summary>
        /// <param name="newTeamList"></param>
        /// <param name="originTeamList"></param>

        private void CheckWitchDiff(List<JobOrganizationModel> newTeamList, List<JobOrganizationModel> originTeamList)
        {
            var testList = (from nd in newTeamList
                            join old in originTeamList on nd.TeamId equals old.TeamId
                            where (nd.TeamName ?? "") != (old.TeamName ?? "")
                            || (nd.ParentTeamId ?? "") != (old.ParentTeamId ?? "")
                            || (nd.CostId ?? "") != (old.CostId ?? "")
                            || (nd.SapTeamId ?? "") != (old.SapTeamId ?? "")
                            || (nd.HrSubAreaId ?? "") != (old.HrSubAreaId ?? "")
                            || (nd.ManagerEmpId ?? "") != (old.ManagerEmpId ?? "")
                            || (nd.TeamType ?? "") != (old.TeamType ?? "")
                            select new { nd, old }).ToList();

            foreach (var m in testList)
            {
                var nd = m.nd;
                var old = m.old;
                var a = "";
                if ((nd.TeamName ?? "") != (old.TeamName ?? ""))
                {
                    a = "";
                }
                if ((nd.ParentTeamId ?? "") != (old.ParentTeamId ?? ""))
                {
                    a = "";
                }
                if ((nd.CostId ?? "") != (old.CostId ?? ""))
                {
                    a = "";
                }
                if ((nd.SapTeamId ?? "") != (old.SapTeamId ?? ""))
                {
                    a = "";
                }
                if ((nd.HrSubAreaId ?? "") != (old.HrSubAreaId ?? ""))
                {
                    a = "";
                }
                if ((nd.ManagerEmpId ?? "") != (old.ManagerEmpId ?? ""))
                {
                    a = "";
                }
                if ((nd.TeamType ?? "") != (old.TeamType ?? ""))
                {
                    a = "";
                }
                a += a;
            }
            
        }




    }
}
