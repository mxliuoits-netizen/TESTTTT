using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.Xml;
using System.Collections;
using NLog;
using PIC.Job;
using PIC.Tools;
using PIC.Libary.Dao;
using UpdateMasterData.Tool;
using UpdateMasterData.Service;
using PIC.DB;
using UpdateMasterData.Model;
using UpdateMasterData.Dao;
using System.Linq.Expressions;
using System.Threading;

namespace UpdateMasterData;

/// <summary>
/// 組織主檔轉入 JOB
/// </summary>
public class Job_UpdateOrganization : BaseJob
{
    #region 設定全域變數
    SFTPClient sftp;
    string s_Local_Path = string.Empty;
    string s_FTP_File_Name = string.Empty;
    string s_Handle_File_Name = string.Empty;
    string s_Decode_File_Name = string.Empty;
    string s_IsGetFromFTP = string.Empty;    
    List<string> li_SQL = new List<string>();
    bool IsSkipEmptyFile = false;
    #endregion

    RetryTool RetryTool;

    public Job_UpdateOrganization(string jobName, Logger logger, params string[] args): base(jobName, BaseJob.GROUP_NAME_JOB, BaseJob.keyToday, logger, args)
    {
        this.IsSkipEmptyFile = "Y".Equals(Environment.GetEnvironmentVariable("PIC_JOB_SKIP_EMPTY_FILE") ?? "");
        int retryCount = int.Parse(Environment.GetEnvironmentVariable("PIC_JOB_RETRY_COUNT") ?? "3");
        int retryDelaySeconds = int.Parse(Environment.GetEnvironmentVariable("PIC_JOB_RETRY_DELAY_SEC") ?? "60");
        RetryTool = new RetryTool() { RetryCount = retryCount, RetryDelaySeconds = retryDelaySeconds, LogFunc = (message) => { this.WriteLog(message, false); } };
    }

    /// <summary>
    /// 主程式
    /// </summary>
    public override void Execute()
    {
        CommonTool commonTool = null;
        var settingDictionary = new Dictionary<string, string>();
        string jobSubStepText = "";
        try
        {
            using var da = DBDataAccessFactory.GetInstance(this.dbType, this.dbPrefix);
            JobMetaDataModel jobMetaData = new JobMetaDataModel {
                EventGroup = this.GROUP_NAME, EventMoudule = this.jobModuleName, ServerIp = this.GetServerIP(), CreateIp = this.creatorIP, CreateUser = this.creator,
                CreateTime = this.sysDate, DBType = this.dbType, DBPrefix = this.dbPrefix
            };
            commonTool = new CommonTool(da);
            var jobConfigSettingDao = new JobConfigSettingDao(da);
            //寫 info 到 event log
            this.WriteLog("更新[組織]主檔開始", false);

            #region 取得JobConfigSetting資料
            this.CurrentJobStepText = "取得JobConfigSetting資料";
            this.WriteLog("取得JobConfigSetting資料開始", false);
            settingDictionary = jobConfigSettingDao.GetList(this.moduleName);
            this.WriteLog("取得JobConfigSetting資料結束", false);
            #endregion

            bool b_ALL_Right;
            ArrayList arl_ChkResult = new ArrayList();

            #region 檢查人工轉入區域是否有檔案
            DateTime d_VersionDate = DateTime.ParseExact(this.jobVersion, BaseJob.VER_DATE_FORMAT, null);
            this.CurrentJobStepText = "檢查人工轉入區域是否有檔案";
            this.WriteLog("檢查人工轉入區域是否有檔案開始", false);
            var hasManualFile = commonTool.CheckManualFileExist(out jobSubStepText, settingDictionary, d_VersionDate, out this.s_Local_Path,out this.s_Handle_File_Name);
            this.WriteLog("檢查人工轉入區域是否有檔案結束", false);
            #endregion

            //是否至FTP抓取組織主檔
            s_IsGetFromFTP = commonTool.GetSysConfigValue(SystemConfigDao.GetItemValue("UpdateOrganization", "IsGetFromFTP"), "UpdateOrganization", "IsGetFromFTP");

            //人工轉入區域是否有檔案
            if(hasManualFile)
            {
                #region 檢查人工轉入區域檔案格式

                this.CurrentJobStepText = "檢查人工轉入區域檔案格式";

                this.WriteLog("檢查人工轉入區域檔案格式開始", false);
                commonTool.CheckManualFile(out jobSubStepText, settingDictionary, this.s_Local_Path, this.s_Handle_File_Name, d_VersionDate,out li_SQL,out arl_ChkResult,out b_ALL_Right, IsSkipEmptyFile: IsSkipEmptyFile);
                if (b_ALL_Right == false)
                {
                    foreach (object obj in arl_ChkResult)
                    {
                        this.WriteLog(obj.ToString(), true); 
                    }
                    throw new Exception("文字檔檔案格式不是完全正確");
                }

                this.WriteLog("檢查人工轉入區域檔案格式結束", false);

                #endregion
            }//是否至FTP抓取組織主檔，Y:抓取，N:直接進行[組織]資料轉入正式資料表，且不進行備份組織主檔檔案
            else if (s_IsGetFromFTP == "Y")
            {
                #region 將FTP上的檔案下載至Server端
                this.CurrentJobStepText = "將FTP上的檔案下載至Server端";
                this.WriteLog("將FTP上的檔案下載至Server端開始", false);
                commonTool.GetFTPFile(out jobSubStepText, settingDictionary, d_VersionDate, this.sysDate, out this.s_FTP_File_Name, out this.s_Local_Path, out this.s_Handle_File_Name, out this.sftp);
                this.WriteLog("將FTP上的檔案下載至Server端結束", false);
                #endregion

                #region 解密從FTP下載下來的檔案
                this.CurrentJobStepText = "解密從FTP下載下來的檔案";
                this.WriteLog("解密從FTP下載下來的檔案開始", false);
                commonTool.DecodeFTPFile(out jobSubStepText, settingDictionary, this.s_Local_Path, this.s_Handle_File_Name, out this.s_Decode_File_Name);
                this.WriteLog("解密從FTP下載下來的檔案結束", false);
                #endregion

                #region 檢查檔案格式
                this.CurrentJobStepText = "檢查檔案格式";
                this.WriteLog("檢查檔案格式開始", false);
                commonTool.CheckFile(out jobSubStepText, settingDictionary, this.s_Local_Path,this.s_FTP_File_Name,this.s_Decode_File_Name, d_VersionDate, out li_SQL, out arl_ChkResult, out b_ALL_Right, IsSkipEmptyFile: IsSkipEmptyFile);

                if (b_ALL_Right == false)
                {
                    foreach (object obj in arl_ChkResult)
                    {
                        this.WriteLog(obj.ToString(), true); 
                    }
                    throw new Exception("文字檔檔案格式不是完全正確");
                }
                this.WriteLog("檢查檔案格式結束", false);
                #endregion
            }

            if(hasManualFile || s_IsGetFromFTP=="Y")
            {
                #region 將文字檔資料寫入Temp Table
                this.CurrentJobStepText = "將文字檔資料寫入Temp Table";
                this.WriteLog("將文字檔資料寫入Temp Table開始", false);
                commonTool.ImportFileTOTempDB(out jobSubStepText, settingDictionary, li_SQL);
                this.WriteLog("將文字檔資料寫入Temp Table結束", false);
                #endregion
            }

            #region 將[組織]資料轉入正式資料表
            this.CurrentJobStepText = "將[組織]資料轉入正式資料表";
            var service = new OrganizationService(da, jobMetaData);
            RetryTool.Execute(service.Import);            
            this.WriteLog("將[組織]資料轉入正式資料表結束", false);
            #endregion

            #region 建立OK檔
            this.CurrentJobStepText = "建立OK檔";
            this.WriteLog("建立OK檔開始", false);
            commonTool.SetOKFile(out jobSubStepText, this.s_Local_Path, this.s_Handle_File_Name);
            this.WriteLog("建立OK檔結束", false);

            #endregion
        }
        catch (Exception ex)
        {
            this.CurrentJobSubStepText = jobSubStepText ?? "";
            throw;
        }
        finally
        {
            #region 備份檔案
            this.CurrentJobStepText = "備份檔案";
            this.WriteLog("備份檔案開始", false);
            if(commonTool != null) commonTool.BackFile(s_DBLogMsg: out jobSubStepText, settingDictionary, this.sftp, this.s_Local_Path, this.s_FTP_File_Name);
            this.WriteLog("備份檔案結束", false);
            #endregion
        }
    }






}