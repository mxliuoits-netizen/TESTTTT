using NLog;
using PIC.DB;
using PIC.Job;
using PIC.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using UpdateMasterData.Dao;
using UpdateMasterData.Model;
using UpdateMasterData.Service;
using UpdateMasterData.Tool;

namespace UpdateMasterData
{
    /// <summary>
    /// 剩餘假主檔 JOB
    /// </summary>
    public class Job_UpdateQuota : BaseJob
    {
        #region 設定全域變數
        SFTPClient sftp;
        bool hasManualFile = false;
        string s_Local_Path = string.Empty;
        string s_FTP_File_Name = string.Empty;
        string s_Handle_File_Name = string.Empty;
        string s_Decode_File_Name = string.Empty;
        bool IsSkipEmptyFile = false;
        List<string> li_SQL = new List<string>();
        #endregion

        int LOOP_COUNT = 1;

        public Job_UpdateQuota(string jobName, Logger logger, params string[] args)
            : base(jobName, BaseJob.GROUP_NAME_JOB, BaseJob.keyToday, logger, args) {
            this.IsSkipEmptyFile = "Y".Equals(Environment.GetEnvironmentVariable("PIC_JOB_SKIP_EMPTY_FILE") ?? "");
            this.LOOP_COUNT = Convert.ToInt32(Environment.GetEnvironmentVariable("PIC_JOB_LOOP_COUNT") ?? "1");
        }

        #region 主程式

        /// <summary>
        /// 主程式
        /// </summary>
        public override void Execute()
        {
            string originJobVersion = this.jobVersion;

            using var da = DBDataAccessFactory.GetInstance(this.dbType, this.dbPrefix);
            using var logDa = DBDataAccessFactory.GetInstance(this.dbType, this.dbPrefix);
            CommonTool commonTool = null;
            var settingDictionary = new Dictionary<string, string>();
            var jobSubStepText = "";
            try
            {
                JobMetaDataModel jobMetaData = new JobMetaDataModel { 
                    EventGroup = this.GROUP_NAME, EventMoudule = this.jobModuleName, ServerIp = this.GetServerIP(), CreateIp = this.creatorIP, CreateUser = this.creator,
                    CreateTime = this.sysDate, DBType = this.dbType, DBPrefix = this.dbPrefix
                };
                var quotaService = new QuotaService(da, jobMetaData, logDa);
                commonTool = new CommonTool(da);
                var jobConfigSettingDao = new JobConfigSettingDao(da);
                bool b_ALL_Right;
                ArrayList arl_ChkResult = new ArrayList();

                
                for (int i = 0; i < this.LOOP_COUNT; i++)
                {
                    DateTime d_VersionDate = DateTime.ParseExact(this.jobVersion, BaseJob.VER_DATE_FORMAT, null);
                    if(i > 0) d_VersionDate = d_VersionDate.AddDays(1);
                    this.jobVersion = d_VersionDate.ToString("yyyyMMdd");

                    //寫 info 到 event log
                    WriteLog("更新[剩餘假]主檔開始", false);

                    #region 取得JobConfigSetting資料
                    this.CurrentJobStepText = "取得JobConfigSetting資料";
                    this.WriteLog("取得JobConfigSetting資料開始", false);
                    settingDictionary = jobConfigSettingDao.GetList(this.moduleName);
                    this.WriteLog("取得JobConfigSetting資料結束", false);
                    #endregion

                    #region 檢查人工轉入區域是否有檔案            
                    this.CurrentJobStepText = "檢查人工轉入區域是否有檔案";
                    this.WriteLog("檢查人工轉入區域是否有檔案開始", false);
                    hasManualFile = commonTool.CheckManualFileExist(out jobSubStepText,
                                                                        settingDictionary,
                                                                        d_VersionDate,
                                                                        out this.s_Local_Path,
                                                                        out this.s_Handle_File_Name);
                    this.WriteLog("檢查人工轉入區域是否有檔案結束", false);
                    #endregion

                    //是否至FTP抓取剩餘假主檔
                    //s_IsGetFromFTP = this.com_fuction.GetSysConfigValue(sf.GetItemValue("UpdateQuota", "IsGetFromFTP"), "UpdateQuota", "IsGetFromFTP");
                    //2014/06/26調整為由批次檔中參數控制，每月21日為N，每月1日為Y
                    //s_IsGetFromFTP = args[4];

                    if (hasManualFile)
                    {
                        #region 檢查人工轉入區域檔案格式

                        this.CurrentJobStepText = "檢查人工轉入區域檔案格式";

                        this.WriteLog("檢查人工轉入區域檔案格式開始", false);
                        commonTool.CheckManualFile(out jobSubStepText,
                                                           settingDictionary,
                                                           this.s_Local_Path,
                                                           this.s_Handle_File_Name,
                                                           d_VersionDate,
                                                           out li_SQL,
                                                           out arl_ChkResult,
                                                           out b_ALL_Right, IsSkipEmptyFile: IsSkipEmptyFile);

                        if (b_ALL_Right == false)
                        {
                            foreach (object obj in arl_ChkResult)
                            { this.WriteLog(obj.ToString(), true); }

                            throw new Exception("文字檔檔案格式不是完全正確");
                        }

                        this.WriteLog("檢查人工轉入區域檔案格式結束", false);

                        #endregion
                    }
                    else
                    {

                        #region 將FTP上的檔案下載至Server端

                        this.CurrentJobStepText = "將FTP上的檔案下載至Server端";

                        this.WriteLog("將FTP上的檔案下載至Server端開始", false);
                        commonTool.GetFTPFile(out jobSubStepText,
                                                      settingDictionary,
                                                      d_VersionDate,
                                                      this.sysDate,
                                                      out this.s_FTP_File_Name,
                                                      out this.s_Local_Path,
                                                      out this.s_Handle_File_Name,
                                                      out this.sftp);
                        this.WriteLog("將FTP上的檔案下載至Server端結束", false);

                        #endregion

                        #region 解密從FTP下載下來的檔案

                        this.CurrentJobStepText = "解密從FTP下載下來的檔案";

                        this.WriteLog("解密從FTP下載下來的檔案開始", false);
                        commonTool.DecodeFTPFile(out jobSubStepText,
                                                         settingDictionary,
                                                         this.s_Local_Path,
                                                         this.s_Handle_File_Name,
                                                         out this.s_Decode_File_Name);
                        this.WriteLog("解密從FTP下載下來的檔案結束", false);

                        #endregion

                        #region 檢查檔案格式

                        this.CurrentJobStepText = "檢查檔案格式";

                        //this.s_Local_Path = @"C:\RSJOB\hankyu\job\data\UpdateQuota\Temp\";
                        //this.s_FTP_File_Name = "QUOTA20181101.TXT";
                        //this.s_Decode_File_Name = "Decode_QUOTA20181101.TXT_20181105141321";

                        this.WriteLog("檢查檔案格式開始", false);
                        commonTool.CheckFile(out jobSubStepText,
                                                    settingDictionary,
                                                    this.s_Local_Path,
                                                    this.s_FTP_File_Name,
                                                    this.s_Decode_File_Name,
                                                    DateTime.ParseExact(this.jobVersion, BaseJob.VER_DATE_FORMAT, null),
                                                    out li_SQL,
                                                    out arl_ChkResult,
                                                    out b_ALL_Right, IsSkipEmptyFile: IsSkipEmptyFile);

                        if (b_ALL_Right == false)
                        {
                            foreach (object obj in arl_ChkResult)
                            { this.WriteLog(obj.ToString(), true); }

                            throw new Exception("文字檔檔案格式不是完全正確");
                        }

                        this.WriteLog("檢查檔案格式結束", false);

                        #endregion

                    }

                    #region 將文字檔資料寫入Temp Table
                    this.CurrentJobStepText = "將文字檔資料寫入Temp Table";
                    this.WriteLog("將文字檔資料寫入Temp Table開始", false);
                    commonTool.ImportFileTOTempDB(out jobSubStepText, settingDictionary, li_SQL);
                    this.WriteLog("將文字檔資料寫入Temp Table結束", false);
                    #endregion

                    da.BeginTransaction();

                    #region 將[剩餘假主檔]資料轉入正式資料表
                    this.CurrentJobStepText = "將[剩餘假主檔]資料轉入正式資料表";
                    // 依照設定全建日 執行 全件或插件
                    var configRebuildDate = commonTool.GetSysConfigValue(SystemConfigDao.GetItemValue("UpdateQuota", "RebuildDate"), "UpdateQuota", "RebuildDate");
                    int rebuildDate = int.Parse(configRebuildDate);
                    var dateCurrent = new DateTime(d_VersionDate.Year, d_VersionDate.Month, rebuildDate);
                    if (d_VersionDate.Day < rebuildDate) dateCurrent.AddMonths(-1); //執行日期在結算日前 應屬上個月結算DateCurrent
                    var isRebuild = d_VersionDate.Day == rebuildDate;

                    quotaService.Import(dateCurrent, isRebuild, this.jobVersion);
                    this.WriteLog("將[剩餘假主檔]資料轉入正式資料表結束", false);

                    /*if (isRebuild)
                    {
                        this.WriteLog("結算代休假資料並轉入正式資料表開始", false);
                        quotaService.Cal00901ArrOffDayData(dateCurrent);
                        this.WriteLog("結算代休假資料並轉入正式資料表結束", false);
                    }*/
                    #endregion

                    #region 建立OK檔                  
                    if (this.LOOP_COUNT == 1)
                    {
                        this.CurrentJobStepText = "建立OK檔";
                        this.WriteLog("建立OK檔開始", false);
                        commonTool.SetOKFile(out jobSubStepText, this.s_Local_Path, this.s_Handle_File_Name);
                        this.WriteLog("建立OK檔結束", false);
                    }
                    #endregion

                    da.Commit();
                }
            }
            catch (Exception ex)
            {
                da.Rollback();
                this.jobVersion = originJobVersion;
                this.CurrentJobSubStepText = jobSubStepText;
                throw;
            }
            finally
            {
                this.jobVersion = originJobVersion;
                #region 備份檔案
                if(this.LOOP_COUNT == 1)
                {
                    this.CurrentJobStepText = "備份檔案";
                    this.WriteLog("備份檔案開始", false);
                    if (commonTool != null) commonTool.BackFile(out jobSubStepText, settingDictionary, this.sftp, this.s_Local_Path, this.s_FTP_File_Name);
                    this.WriteLog("備份檔案結束", false);
                }
                #endregion
            }
        }
        #endregion 主程式

    }
}