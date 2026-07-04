using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Collections;
using System.Data.SqlClient;
using NLog;
using PIC.Job;
using PIC.Tools;
using PIC.DB;
using PIC.Libary;
using System.Net;
using System.Globalization;
using System.Text.RegularExpressions;
using UpdateMasterData.Tool;
using System.Management;

namespace UpdateMasterData
{
    public class Job_UpdateQuotaQualify : BaseJob
    {
        #region 設定全域變數

        /// <summary>
        /// 設定版本日期 
        /// </summary>
        private String strVersionDate = string.Empty;

        /// <summary>
        /// Log資訊
        /// </summary>
        String strLogFlag = string.Empty;

        string s_Modul_DBLogMsg = string.Empty;//Log Message(大模組)
        string s_DBLogMsg = string.Empty;//Log Message(細項)

        XmlNode xml_Node;
        SFTPClient ftp;

        string s_Local_Path = string.Empty;
        string s_FTP_File_Name = string.Empty;
        string s_Handle_File_Name = string.Empty;
        string s_Decode_File_Name = string.Empty;

        List<string> li_SQL = new List<string>();

        SqlParameter[] sql_Par = new SqlParameter[0];

        #endregion

        public Job_UpdateQuotaQualify(string jobName, Logger logger, params string[] args)
            : base(jobName, BaseJob.GROUP_NAME_JOB, BaseJob.keyToday, logger, args) { }

        #region 主程式

        /// <summary>
        /// 主程式
        /// </summary>
        public override void Execute()
        {
            using var da = DBDataAccessFactory.GetInstance(this.dbType, this.dbPrefix);
            CommonTool com_fuction = new CommonTool(da);
            strLogFlag = "更新[剩餘假]生效日期及額度";
            
            //寫 info 到 event log
            WriteLog(string.Format("{0}開始", strLogFlag), false);

            try
            {
                #region 更新[剩餘假生效日期]資料

                s_Modul_DBLogMsg = "更新剩餘假生效日期及額度開始";

                var param = new
                {
                    ServerIP = this.GetServerIP(),
                    CreateUser = this.creator,
                    CreatorIP = this.creatorIP,
                    CreateTime = this.sysDate,
                    JobVersionDate = this.jobVersion
                };
                var sql = da.GetCallSPSQL("sp_UpdateQuotaQualify", param);
                da.DapperExecute(sql, param);

                /*PIC.DB.DataAccess da = new PIC.DB.DataAccess();

                sql_Par = new SqlParameter[5];
                sql_Par[0] = new SqlParameter("ServerIP", this.GetServerIP());
                sql_Par[1] = new SqlParameter("CreateUser", this.creator);
                sql_Par[2] = new SqlParameter("CreatorIP", this.creatorIP);
                sql_Par[3] = new SqlParameter("CreateTime", this.sysDate);
                sql_Par[4] = new SqlParameter("JobVersionDate", this.jobVersion);  //Job執行的版本日(TODAY本日、指定日期)

                da.ExecuteProcdure("sp_UpdateQuotaQualify", sql_Par);*/
                this.WriteLog("更新剩餘假生效日期及額度結束", false);

                #endregion

            }
            catch (Exception ex)
            { 
                throw new Exception(s_Modul_DBLogMsg + "，" + this.s_DBLogMsg + "，" + ex.Message); 
            }
        }
        #endregion 主程式

    }
}