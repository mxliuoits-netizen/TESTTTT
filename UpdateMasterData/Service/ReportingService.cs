using PIC.DB;
using PIC.Libary.Dao;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateMasterData.Dao;
using UpdateMasterData.Model;
using SystemConfigDao = UpdateMasterData.Dao.SystemConfigDao;

namespace UpdateMasterData.Service;

/// <summary>
/// 彙報關係 服務
/// </summary>
public class ReportingService : BaseService
{

    public ReportingService(IDataAccess da, JobMetaDataModel jobMetaData) 
    {
        this.JobMetaData = jobMetaData;
        SystemJobEventLogDao = new SystemJobEventLogDao(da);
    }

    /// <summary>
    /// 匯入彙報關係
    /// </summary>
    /// <param name="reportDate"></param>
    public void Import(DateTime reportDate)
    {
        using IDataAccess businessDa = DBDataAccessFactory.GetInstance(JobMetaData.DBType, JobMetaData.DBPrefix);
        //test
        //businessDa.IsEnableLog = true;
        try
        {
            LogInfo("ReportingService.Import 開始");
            businessDa.BeginTransaction();

            JobEmpReportingDao jobEmpReportingDao = new JobEmpReportingDao(businessDa);
            BaseEmpReportingDao baseEmpReportingDao = new BaseEmpReportingDao(businessDa);

            // 1.刪除Base_Emp_Reporting存在今天的資料，如果有資料就將資料備份到JOB_Emp_Reporting_bak。
            LogInfo("1.刪除Base_Emp_Reporting存在今天的資料，如果有資料就將資料備份到JOB_Emp_Reporting_bak。");
            jobEmpReportingDao.BackupBaseEmpReporting(reportDate);
            baseEmpReportingDao.DeleteByReportDate(reportDate);

            // 2.將JOB_Emp_Reporting_tmp的資料Insert到Base_Emp_Reporting。
            LogInfo("2.將JOB_Emp_Reporting_tmp的資料Insert到Base_Emp_Reporting。");
            baseEmpReportingDao.InsertFromJobEmpReportingTmp(JobMetaData.ServerIp, JobMetaData.CreateIp, JobMetaData.CreateUser, JobMetaData.CreateTime);

            businessDa.Commit();
            LogInfo("ReportingService.Import 結束");
        }
        catch(Exception)
        {
            businessDa.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 重建 BaseTeamOwner
    /// </summary>
    /// <param name="reportDate"></param>

    public void RebuildBaseTeamOwner(DateTime reportDate)
    {
        using IDataAccess businessDa = DBDataAccessFactory.GetInstance(JobMetaData.DBType, JobMetaData.DBPrefix);
        //test
        //businessDa.IsEnableLog = true;
        try
        {
            LogInfo("ReportingService.RebuildBaseTeamOwner 開始");

            var baseTeamOwnerDao = new BaseTeamOwnerDao(businessDa);
            var baseAuthorityMDao = new BaseAuthorityMDao(businessDa);
            var systemConfigDao = new SystemConfigDao(businessDa);
            var baseAuthorityfunctionlistMDao = new BaseAuthorityfunctionlistMDao(businessDa);

            // 1. 備份base_teamowner_m至base_teamowner_bak
            LogInfo("備份base_teamowner_m至base_teamowner_bak");
            baseTeamOwnerDao.BackupBaseTeamOwnerM();

            // 2. 清空base_teamowner_tmp
            LogInfo("清空base_teamowner_tmp");
            baseTeamOwnerDao.TruncateBaseTeamOwnerTmp();

            // 3. 彙整單位主管資料至base_teamowner_tmp
            LogInfo("彙整單位主管資料至base_teamowner_tmp");
            baseTeamOwnerDao.ImportEmpReportingToBaseTeamOwnerTmp(reportDate, JobMetaData.ServerIp, JobMetaData.CreateUser, JobMetaData.CreateIp, JobMetaData.CreateTime);

            // 4. 新增職稱白名單對應之主管資料至base_teamowner_tmp
            LogInfo("新增職稱白名單對應之主管資料至base_teamowner_tmp");
            baseTeamOwnerDao.ImportWhiteANameToBaseTeamOwnerTmp(reportDate, JobMetaData.ServerIp, JobMetaData.CreateUser, JobMetaData.CreateIp, JobMetaData.CreateTime);

            // 5. 新增人員Jobcode=00000131且該人員單位無主管之資料至base_teamowner_tmp (應在base_teamowner_tmp處理最後一步)
            baseTeamOwnerDao.ImportJobCode00000131(JobMetaData.ServerIp, JobMetaData.CreateUser, JobMetaData.CreateIp, JobMetaData.CreateTime);

            // 6. 執行重建base_teamowner_m
            LogInfo("執行重建base_teamowner_m");
            businessDa.BeginTransaction();
            baseTeamOwnerDao.TruncateBaseTeamOwnerM();
            baseTeamOwnerDao.ImportBaseTeamOwnerTmpToBaseTeamOwnerM();

            // 7. 更新 base_authority_m 設定  A_AREA = '3'
            LogInfo("更新 base_authority_m 設定 A_AREA = '3'");
            baseAuthorityMDao.UpdateAAreaByBaseTeamOwnerM();

            // 8 主管人員 補上 主管預設功能清單
            var mangerDefaultFuncText = systemConfigDao.GetItemValue("UpdateEmployee", "ManagerDefaultFuncList") ?? "203,207";
            var mangerDefaultFuncList = mangerDefaultFuncText.Split(",").Select(x => Convert.ToInt32(x)).ToList();
            int c18 = baseAuthorityfunctionlistMDao.InsertManagerEmployeeByFuncId(mangerDefaultFuncList, JobMetaData.ServerIp, JobMetaData.CreateIp, JobMetaData.CreateUser);

            // 8-1. 非主管人員 移除 主管預設功能清單
            int d18 = baseAuthorityfunctionlistMDao.DeleteNotManagerEmpid(mangerDefaultFuncList);

            businessDa.Commit();
            LogInfo("ReportingService.RebuildBaseTeamOwner 結束");
        }
        catch(Exception ex)
        {
            businessDa.Rollback();
            Console.WriteLine(ex.ToString());
            throw;
        }
    }


}
