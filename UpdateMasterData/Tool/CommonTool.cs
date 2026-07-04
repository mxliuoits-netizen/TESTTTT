using PIC.DB;
using PIC.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace UpdateMasterData.Tool;

public class CommonTool
{
    IDataAccess da;
    public string FileHeaderRowText { get; private set; }


    public CommonTool(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 將FTP上的檔案下載至Server端
    /// </summary>
    /// <param name="s_DBLogMsg">Log Message(細項)</param>
    /// <param name="xml_Node">XML資料</param>
    /// <param name="d_JOB_VersionDate">JOB處理的日期</param>
    /// <param name="d_JOB_CreateDate">JOB的createdate</param>
    /// <param name="s_FTP_File_Name">FTP上的檔案名稱</param>
    /// <param name="s_Local_Path">FTP上的檔案下載至Server端的路徑</param>
    /// <param name="s_Handle_File_Name">FTP上的檔案下載至Server端的檔名</param>
    /// <param name="s_Handle_File_Name">FTP</param>
    public void GetFTPFile(out string s_DBLogMsg, Dictionary<string, string> settingDictionary, DateTime d_JOB_VersionDate, DateTime d_JOB_CreateDate,
        out string s_FTP_File_Name,
        out string s_Local_Path,
        out string s_Handle_File_Name,
        out SFTPClient sftp)
    {
        #region 設定變數

        s_DBLogMsg = "設定變數";

        s_FTP_File_Name = string.Empty;
        s_Local_Path = string.Empty;
        s_Handle_File_Name = string.Empty;

        string s_FTP_IP = string.Empty;
        string s_FTP_Port = string.Empty;
        string s_FTP_ID = string.Empty;
        string s_FTP_Pwd = string.Empty;
        string s_FTP_Path = string.Empty;
        string s_fn_FTP_File_Name = string.Empty;
        string s_UnZip_FileName = string.Empty;
        bool s_FTP_IsDecrypt = true;
        //JOB_CreateDate = d_JOB_CreateDate;//方便Decode檔名使用

        #endregion

        #region 取得 SAP FTP 相關資訊

        s_DBLogMsg = "取得 SAP FTP 相關資訊";
        s_FTP_IP = settingDictionary.GetValueOrDefault("SFTP_IP", "");
        s_FTP_Port = settingDictionary.GetValueOrDefault("SFTP_PORT", "22");
        s_FTP_ID = settingDictionary.GetValueOrDefault("SFTP_ID", "");
        s_FTP_Pwd = settingDictionary.GetValueOrDefault("SFTP_PWD", "");
        s_FTP_Path = settingDictionary.GetValueOrDefault("SFTP_Folder", "");         
        s_fn_FTP_File_Name = string.Format(settingDictionary.GetValueOrDefault("FTPFileName", ""), d_JOB_VersionDate.ToString("yyyyMMdd")).ToUpper();
        var localPath = settingDictionary.GetValueOrDefault("LocalPath", "");
        s_Local_Path = localPath.EndsWith(Path.DirectorySeparatorChar) ? localPath : localPath + Path.DirectorySeparatorChar;
        s_FTP_IsDecrypt = "Y".Equals(settingDictionary.GetValueOrDefault("SFTP_IsNeedDecrypt", ""));

        #endregion

        #region 檢查Local的路徑是否存在，不存在則建立

        s_DBLogMsg = "檢查Local的路徑是否存在，不存在則建立";

        DirectoryInfo dir = new DirectoryInfo(s_Local_Path);
        if (dir.Exists == false)
        { dir.Create(); }

        #endregion

        #region 將FTP檔案下載至Server端

        #region 設定FTP連線資訊

        s_DBLogMsg = "設定FTP連線資訊";

        sftp = new SFTPClient(s_FTP_IP, s_FTP_Port, s_FTP_ID, s_FTP_Pwd, s_FTP_Path, s_FTP_IsDecrypt);

        #endregion

        #region 取得系統日當天最新的檔案名稱

        s_DBLogMsg = "取得系統日當天最新的檔案名稱";

        var list = sftp.GetFileList();

        Console.WriteLine($"FTP路徑({sftp.CurrentPath})下，檔案數量:{list?.Count}，並尋找檔案{s_fn_FTP_File_Name + ".TAR"}");
        if(list != null)
        {
            Console.WriteLine("檔案清單:");
            foreach (var f in list)
            {
                Console.WriteLine(f);
            }
        }
        if(list == null || list.Count == 0) throw new Exception($"FTP路徑({sftp.CurrentPath})下，沒有任何檔案!");

        var query = from FileName in sftp.GetFileList()
                    where FileName.ToUpper().Equals(s_fn_FTP_File_Name + ".TAR")
                    select FileName;

        #endregion

        #region 下載檔案至Server端

        s_DBLogMsg = "下載檔案至Server端";

        if (query == null || query.Count() == 0)
        { throw new Exception(d_JOB_VersionDate.ToString("yyyy/MM/dd") + "：FTP上沒有新的檔案"); }
        else if (query.Count() > 1)
        { throw new Exception(d_JOB_VersionDate.ToString("yyyy/MM/dd") + "：FTP上有多筆資料"); }
        else
        {
            try
            {

                s_Handle_File_Name = query.First().ToUpper();
                s_fn_FTP_File_Name = query.First();

                s_DBLogMsg = "檢查FTP上是否有FILEOK的註解檔";

                var queryOK = from FileName in sftp.GetFileList()
                              where FileName.Equals(s_fn_FTP_File_Name + ".FILEOK")
                              select FileName;
                if (queryOK == null || queryOK.Count() == 0)
                { throw new Exception(d_JOB_VersionDate.ToString("yyyy/MM/dd") + "：FTP上沒有當日.FILEOK的檔案"); }

                else
                {
                    s_Handle_File_Name = s_Handle_File_Name.Insert(s_Handle_File_Name.IndexOf(".TXT"), "_" + d_JOB_CreateDate.ToString("yyyyMMddHHmmss"));
                    sftp.DownloadFile(s_Local_Path , s_fn_FTP_File_Name, s_Handle_File_Name);
                    s_FTP_File_Name = s_fn_FTP_File_Name;

                    s_Handle_File_Name = UnzipFile(out s_DBLogMsg, settingDictionary, d_JOB_VersionDate, d_JOB_CreateDate, s_Local_Path, s_Handle_File_Name, s_FTP_File_Name);
                    s_FTP_File_Name = s_Handle_File_Name.Replace(".TXT_" + d_JOB_CreateDate.ToString("yyyyMMddHHmmss"), ".TXT"); //CHECKfile時確認用
                }
            }
            catch (Exception ex)
            {
                s_FTP_File_Name = string.Empty;
                s_Handle_File_Name = string.Empty;

                throw new Exception("從FTP下載檔案：" + s_FTP_File_Name + "發生錯誤：" + ex.Message);
            }
        }




        #endregion

        #endregion
    }


    /// <summary>
    /// 將下載的檔案解壓縮
    /// </summary>
    public string UnzipFile(out string s_DBLogMsg,
                             Dictionary<string, string> settingDictionary, DateTime d_JOB_VersionDate,
                             DateTime d_JOB_CreateDate,
                                string s_Local_Path, string s_FTP_FileName, string s_Unzip_FileName)
    {
        #region 設定變數
        s_DBLogMsg = "設定變數";
        string s_Unzip_File_Name = string.Empty; //解壓縮檔案名稱
        #endregion


        #region 解壓縮檔

        #region sever路徑無檔案
        string[] V_strFiles = Directory.GetFiles(s_Local_Path);
        if (V_strFiles.Length == 0)
        {
            s_DBLogMsg = "SEVER路徑無檔案!";
            throw new Exception(s_DBLogMsg);
        }
        #endregion

        #region 資料夾內有.tar壓縮檔

        string[] FileList = Directory.GetFiles(s_Local_Path, s_FTP_FileName);//找出目錄底下壓縮檔案


        if (FileList.Length != 0)
        {


            try
            {
                foreach (string item in FileList)
                {
                    #region 解壓縮後的檔案存在
                    if (Path.GetFileNameWithoutExtension(item) == s_Unzip_FileName.ToUpper().Replace(".TAR", string.Empty))
                    {
                        File.Delete(s_Local_Path + "\\" + Path.GetFileNameWithoutExtension(item));

                        ZipTool.ExtractFiles(item, s_Local_Path);

                    }
                    #endregion

                    #region 解壓縮後的檔案不存在
                    else
                    {
                        ZipTool.ExtractFiles(item, s_Local_Path);
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                throw new Exception("解壓縮檔案：" + s_FTP_FileName + "發生錯誤：" + ex.Message);
            }
        }
        #endregion

        #region 資料夾內無.tar壓縮檔
        else
        {
            s_DBLogMsg = string.Format("資料夾內無.tar壓縮檔");
            throw new Exception(s_DBLogMsg);
        }
        #endregion

        #endregion

        s_Unzip_File_Name = string.Format(settingDictionary.GetValueOrDefault("FTPFileName", ""), d_JOB_VersionDate.ToString("yyyyMMdd")).ToUpper();

        #region 取得解壓縮後檔案名稱

        s_DBLogMsg = "取得系統日當天最新的檔案名稱";

        //LinQ語法
        var query = (from FileName in Directory.GetFiles(s_Local_Path)
                     where FileName.ToUpper().Contains(s_Unzip_File_Name) && FileName.ToUpper().EndsWith(".TXT")
                     select Path.GetFileName(FileName)).Max();


        if (query == null)
        {
            throw new Exception(d_JOB_VersionDate.ToString("yyyy/MM/dd") + "：沒有新的檔案"); 
        }
        else
        {
            try
            {
                s_Unzip_File_Name = query;
            }
            catch (Exception ex)
            { throw new Exception("取得檔案名稱：" + s_Unzip_File_Name + "發生錯誤：" + ex.Message); }
        }



        File.Move(s_Local_Path + s_Unzip_File_Name, s_Local_Path + s_Unzip_File_Name + "_" + d_JOB_CreateDate.ToString("yyyyMMddHHmmss"));

        s_Unzip_File_Name = s_Unzip_File_Name + "_" + d_JOB_CreateDate.ToString("yyyyMMddHHmmss");

        return s_Unzip_File_Name;

        #endregion



    }



    /// <summary>
    /// 解密從FTP下載下來的檔案
    /// </summary>
    /// <param name="s_DBLogMsg">Log Message(細項)</param>
    /// <param name="xml_Node">XML資料</param>
    /// <param name="s_Local_Path">FTP上的檔案下載至Server端的路徑</param>
    /// <param name="s_Handle_File_Name">FTP上的檔案下載至Server端的檔名</param>
    /// <param name="s_Decode_File_Name">解碼後的檔名</param>
    public void DecodeFTPFile(out string s_DBLogMsg,
                                Dictionary<string, string> settingDictionary,
                                string s_Local_Path,
                                string s_Handle_File_Name,
                                out string s_Decode_File_Name)
    {
        #region 設定變數

        s_DBLogMsg = "設定變數";

        #endregion

        #region 設定解碼後的檔案名稱

        s_DBLogMsg = "設定解碼後的檔案名稱";

        s_Decode_File_Name = string.Format(settingDictionary.GetValueOrDefault("SFTP_DecodeFileName", ""), s_Handle_File_Name);

        #endregion

        FileInfo fi = new FileInfo(s_Local_Path + s_Handle_File_Name);

        var decodeCode = settingDictionary.GetValueOrDefault("SFTP_DecodeCode", "");
        DecodeTool dec_File = new DecodeTool(decodeCode);

        switch (settingDictionary.GetValueOrDefault("IsDecode", "Y"))
        {
            //需要解碼
            case "Y":
                //這邊是暫時的，先複製檔案，代替解碼過程
                dec_File.RunJob(s_Local_Path, s_Handle_File_Name, s_Decode_File_Name);
                break;

            //不需要解碼
            case "N":
                //這邊是暫時的，先複製檔案，代替解碼過程
                fi.CopyTo(s_Local_Path + s_Decode_File_Name, true);
                break;
        }

    }

    /// <summary>
    /// 檢查檔案格式
    /// </summary>
    /// <param name="s_DBLogMsg">Log Message(細項)</param>
    /// <param name="xml_Node">XML資料</param>
    /// <param name="s_Local_Path">FTP上的檔案下載至Server端的路徑</param>
    /// <param name="s_FTP_File_Name">FTP上的檔案名稱</param>
    /// <param name="s_Decode_File_Name">解碼後的檔名</param>
    /// <param name="d_JOB_VersionDate">JOB處理的日期</param>
    /// <param name="li_SQL">新增至Temp Table的資料</param>
    /// <param name="arl_ChkResult">訊息</param>
    /// <param name="b_ALL_Right">檢查是否全部正確</param>
    /// <param name="IsContainHead">是否包含檔頭</param>
    public void CheckFile(out string s_DBLogMsg,
                           Dictionary<string, string> settingDictionary,
                           string s_Local_Path,
                           string s_FTP_File_Name,
                           string s_Decode_File_Name,
                           DateTime d_JOB_VersionDate,
                           out List<string> li_SQL,
                           out ArrayList arl_ChkResult,
                           out bool b_ALL_Right,
                           bool IsContainHead = true, bool IsSkipEmptyFile = false)
    {
        #region 設定變數

        s_DBLogMsg = "設定變數";

        string s_Path_XML_File = string.Empty;
        string s_ErrMsg = string.Empty;//錯誤訊息
        string s_Insert = string.Empty;//SQL字串

        FileInfo fi_Chk = new FileInfo(s_Local_Path + s_Decode_File_Name);
        b_ALL_Right = true;
        li_SQL = new List<string>();
        arl_ChkResult = new ArrayList();

        #endregion

        #region 檢查文字檔

        #region 設定變數

        int i_Row = 0;//紀錄執行到哪一行
        int i_Row_Body = 0;//記錄檔身有幾筆資料
        string sr_String_Head = string.Empty;//檔頭
        string sr_String_Body = string.Empty;//檔身
        string sr_String_End = string.Empty;//檔身

        #endregion
        using StreamReader sr = new StreamReader(fi_Chk.FullName);
        while ((sr_String_Body = sr.ReadLine()) != null)
        {
            #region 檔頭
            if (i_Row == 0 && IsContainHead)
            {
                s_DBLogMsg = "檔頭";
                sr_String_Head = sr_String_Body;
                i_Row++;
                continue;
            }
            #endregion

            #region 檔身 && 檔尾
            s_DBLogMsg = "檔身";

            CheckFile cfd_Body = new CheckFile(settingDictionary, sr_String_Body);
            if (cfd_Body.ChkBodyResult(out s_ErrMsg, out s_Insert) == false)
            {
                b_ALL_Right = false;
                arl_ChkResult.Add("第" + Convert.ToString(i_Row + 1) + "行資料錯誤：" + s_ErrMsg);
            }
            else
            {
                li_SQL.Add(s_Insert);
            }
            #endregion

            i_Row_Body++;//記錄檔身有幾筆資料
            i_Row++;//紀錄執行到哪一行
        }

        if (i_Row == 0 && !IsSkipEmptyFile)
        {
            b_ALL_Right = false;
            arl_ChkResult.Add("文字檔無資料");
        }



        #region 檢查檔頭
        if (IsContainHead && i_Row != 0 )
        {
            s_DBLogMsg = "檢查檔頭";
            CheckFile cfd_Head = new CheckFile(settingDictionary, s_FTP_File_Name, sr_String_Head, i_Row_Body, d_JOB_VersionDate);

            if (cfd_Head.ChkHeadEndResult(out s_ErrMsg) == false)
            {
                b_ALL_Right = false;
                arl_ChkResult.Add("檔頭資料錯誤：" + s_ErrMsg);
            }
            else
            {
                this.FileHeaderRowText = sr_String_Head;
            } 
        }

        #endregion
        #endregion
    }



    /// <summary>
    /// 將文字檔資料寫入Temp Table
    /// </summary>
    /// <param name="s_DBLogMsg">Log Message(細項)</param>
    /// <param name="xml_Node">XML資料</param>
    /// <param name="li_SQL">新增至Temp Table的資料</param>
    public void ImportFileTOTempDB(out string s_DBLogMsg,
                                      Dictionary<string,string> settingDictionary,
                                      List<string> li_SQL)
    {
        #region 設定變數

        s_DBLogMsg = "設定變數";

        string s_SQL = string.Empty;
        int i_Commit_Count = 200;//每幾筆資料commit一次

        #endregion

        #region 刪除Temp Table資料

        s_DBLogMsg = "刪除Temp Table資料";

        #region 組合SQL

        s_SQL = string.Empty;
        s_SQL += @"Truncate Table " + settingDictionary["Table"];

        #endregion

        #region 連接資料庫

        da.DapperExecute(s_SQL);

        #endregion

        #endregion

        #region 將資料批次寫入Table

        s_DBLogMsg = "將資料批次寫入Table";

        da.BeginTransaction();
        try
        {
            int i = 0;
            foreach(string sql in li_SQL)
            {
                da.DapperExecute(sql);
                i++;
                if (i % i_Commit_Count == 0)
                {
                    da.Commit();
                    da.BeginTransaction();
                }
            }
            da.Commit();
        }catch
        {
            da.Rollback();
            throw;
        }
        finally
        {
            da.CloseTransactin();
        }
        /*
        s_SQL = string.Empty;
        for (int i = 0; i < li_SQL.Count; i++)
        {
            s_SQL = s_SQL.Insert(s_SQL.Length, li_SQL[i] + ";");

            //設定每幾筆資料commit一次
            //最後一筆的時候也要commit
            if (i % i_Commit_Count == 0 ||
                i == li_SQL.Count - 1)
            {
                s_SQL = s_SQL.Insert(0, "Begin ");
                s_SQL = s_SQL.Insert(s_SQL.Length, "End;");

                da.ExecuteNonQuery(s_SQL);
                s_SQL = string.Empty;
            }
        }*/

        #endregion
    }

    /// <summary>
    /// 備份檔案
    /// </summary>
    /// <param name="s_DBLogMsg">Log Message(細項)</param>
    /// <param name="xml_Node">XML資料</param>
    /// <param name="sftp">FTP</param>
    /// <param name="s_Local_Path">FTP上的檔案下載至Server端的路徑</param>
    /// <param name="s_FTP_File_Name">FTP上的檔案名稱</param>
    public void BackFile(out string s_DBLogMsg,
                          Dictionary<string, string> settingDictionary,
                          SFTPClient sftp,
                          string s_Local_Path,
                          string s_FTP_File_Name)
    {
        #region 設定變數

        s_DBLogMsg = "設定變數";

        string s_Bak_Path = settingDictionary.GetValueOrDefault("BakPath", "");//備份檔案路徑
        s_Bak_Path = (s_Bak_Path.EndsWith(Path.DirectorySeparatorChar) == true) ? s_Bak_Path : s_Bak_Path + Path.DirectorySeparatorChar;//看後面有沒有"\"，沒有自動補上
        DirectoryInfo dir_Handle;

        #endregion

        #region 設定處理目錄

        if (s_Local_Path == string.Empty)
        { return; }
        else
        { dir_Handle = new DirectoryInfo(s_Local_Path); }

        #endregion

        #region 檢查備份目錄是否存在，不存在則建立

        s_DBLogMsg = "檢查備份目錄是否存在，不存在則建立";

        DirectoryInfo dir_Bak = new DirectoryInfo(s_Bak_Path);
        if (dir_Bak.Exists == false)
        { dir_Bak.Create(); }

        #endregion

        #region 將處理目錄內的檔案移至備份目錄

        s_DBLogMsg = "將處理目錄內的檔案移至備份目錄";

        var suffixDateText = DateTime.Now.ToString("yyyyMMddHHmmss");
        if (dir_Handle.Exists == true)
        {
            foreach (FileInfo fi_Dtl in dir_Handle.GetFiles())
            { fi_Dtl.MoveTo(dir_Bak.FullName + fi_Dtl.Name + "_" + suffixDateText); }
        }

        #endregion

        #region 刪除FTP上的檔案

        //s_DBLogMsg = "刪除FTP上的檔案";

        //if (s_FTP_File_Name != string.Empty)
        //{
        //    try
        //    {
        //        ftp.DeleteFile(s_FTP_File_Name + ".tar");

        //        ftp.DeleteFile(s_FTP_File_Name + ".tar.FILEOK");

        //    }
        //    catch (Exception ex)
        //    { throw new Exception("刪除FTP上的檔案：" + s_FTP_File_Name + "發生錯誤：" + ex.Message); }
        //}

        #endregion
    }

    /// <summary>
    /// 設定OK檔
    /// </summary>
    public void SetOKFile(out string s_DBLogMsg, string s_Local_Path, string s_Handle_File_Name)
    {
        s_DBLogMsg = "建立OK檔";
        StreamWriter sw = new StreamWriter(s_Local_Path + s_Handle_File_Name + ".OK");
        sw.Close();
        sw.Dispose();
    }

    /// <summary>
    /// 取得SysConfig的Value
    /// </summary>
    /// <param name="s_Value"></param>
    /// <param name="s_GROUPNAME"></param>
    /// <param name="s_ITEM"></param>
    /// <returns></returns>
    public string GetSysConfigValue(string s_Value, string s_GROUPNAME, string s_ITEM)
    {
        string s_ErrMsg = string.Empty;
        if (s_Value == null || s_Value.Trim() == string.Empty)
        {
            s_ErrMsg += "取得參數檔 m_SysConfig 發生異常：GROUPNAME = " + s_GROUPNAME + "，ITEM = " + s_ITEM;
            throw new Exception(s_ErrMsg);
        }
        return s_Value.Trim();
    }

    /// <summary>
    /// 檢查人工轉入區域是否有檔案
    /// </summary>
    /// <param name="s_DBLogMsg">Log Message(細項)</param>
    /// <param name="xml_Node">XML資料</param>
    /// <param name="d_JOB_VersionDate">JOB處理的日期</param>
    /// <param name="s_Local_Path">人工轉入區域的路徑</param>
    /// <param name="s_Handle_File_Name">人工轉入區域的檔案名稱</param>
    /// <returns></returns>
    public bool CheckManualFileExist(out string s_DBLogMsg,
                                        Dictionary<string, string> settingDictionary,
                                        DateTime d_JOB_VersionDate,
                                        out string s_Local_Path,
                                        out string s_Handle_File_Name)
    {
        #region 設定變數

        s_DBLogMsg = "設定變數";
        var manualFilePath = settingDictionary.GetValueOrDefault("ManualFilePath", "");
        var manualFileName = settingDictionary.GetValueOrDefault("ManualFileName", "");
        s_Local_Path = manualFilePath.EndsWith(Path.DirectorySeparatorChar) ? manualFilePath : manualFilePath + Path.DirectorySeparatorChar;

        string s_Ex_File_Name = string.Format(manualFileName, d_JOB_VersionDate.ToString("yyyyMMdd"));

        s_Handle_File_Name = s_Ex_File_Name;

        #endregion

        #region 檢查Local的路徑是否存在，不存在則建立

        s_DBLogMsg = "檢查Local的路徑是否存在，不存在則建立";

        DirectoryInfo dir = new DirectoryInfo(s_Local_Path);
        if (dir.Exists == false)
        { dir.Create(); }

        #endregion

        return dir.GetFiles().Count(x => x.Name == s_Ex_File_Name) > 0;
    }

    /// <summary>
    /// 檢查人工轉入區域檔案格式
    /// </summary>
    /// <param name="s_DBLogMsg">Log Message(細項)</param>
    /// <param name="xml_Node">XML資料</param>
    /// <param name="s_Local_Path">人工轉入區域的路徑</param>
    /// <param name="s_Handle_File_Name">人工轉入區域的檔案名稱</param>
    /// <param name="d_JOB_VersionDate">JOB處理的日期</param>
    /// <param name="li_SQL">新增至Temp Table的資料</param>
    /// <param name="arl_ChkResult">訊息</param>
    /// <param name="b_ALL_Right">檢查是否全部正確</param>
    /// <param name="IsContainHead">是否包含檔頭</param>
    public void CheckManualFile(out string s_DBLogMsg,
                                  Dictionary<string, string> settingDictionary,
                                  string s_Local_Path,
                                  string s_Handle_File_Name,
                                  DateTime d_JOB_VersionDate,
                                  out List<string> li_SQL,
                                  out ArrayList arl_ChkResult,
                                  out bool b_ALL_Right,
                                  bool IsContainHead = true, bool IsSkipEmptyFile = false)
    {
        #region 設定變數

        s_DBLogMsg = "設定變數";

        string s_Path_XML_File = string.Empty;
        string s_ErrMsg = string.Empty;//錯誤訊息
        string s_Insert = string.Empty;//SQL字串

        FileInfo fi_Chk = new FileInfo(s_Local_Path + s_Handle_File_Name);
        b_ALL_Right = true;
        li_SQL = new List<string>();
        arl_ChkResult = new ArrayList();

        #endregion

        #region 檢查文字檔

        #region 設定變數

        int i_Row = 0;//紀錄執行到哪一行
        int i_Row_Body = 0;//記錄檔身有幾筆資料
        string sr_String_Head = string.Empty;//檔頭
        string sr_String_Body = string.Empty;//檔身
        string sr_String_End = string.Empty;//檔身

        #endregion

        using StreamReader sr = new StreamReader(fi_Chk.FullName);
        while ((sr_String_Body = sr.ReadLine()) != null)
        {
            #region 檔頭            
            if (i_Row == 0 && IsContainHead)
            {
                s_DBLogMsg = "檔頭";
                sr_String_Head = sr_String_Body;
                i_Row++;
                continue;
            }
            #endregion

            #region 檔身 && 檔尾
            s_DBLogMsg = "檔身";

            CheckFile cfd_Body = new CheckFile(settingDictionary, sr_String_Body);
            if (cfd_Body.ChkBodyAllowResult(out s_ErrMsg))
            {
                if (cfd_Body.ChkBodyResult(out s_ErrMsg, out s_Insert) == false)
                {
                    b_ALL_Right = false;
                    arl_ChkResult.Add("第" + Convert.ToString(i_Row + 1) + "行資料錯誤：" + s_ErrMsg);
                }
                else
                {
                    li_SQL.Add(s_Insert);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(s_ErrMsg))
                {
                    b_ALL_Right = false;
                    arl_ChkResult.Add("第" + Convert.ToString(i_Row + 1) + "行資料錯誤：" + s_ErrMsg);
                }
            }
            #endregion
            i_Row_Body++;//記錄檔身有幾筆資料
            i_Row++;//紀錄執行到哪一行
        }

        if (i_Row == 0 && !IsSkipEmptyFile)
        {
            b_ALL_Right = false;
            arl_ChkResult.Add("文字檔無資料");
        }

        #region 檢查檔頭
        if (IsContainHead && i_Row != 0)
        {
            s_DBLogMsg = "檢查檔頭";
            CheckFile cfd_Head = new CheckFile(settingDictionary, s_Handle_File_Name, sr_String_Head, i_Row_Body, d_JOB_VersionDate);

            //20171024 for test
            if (cfd_Head.ChkHeadEndResult(out s_ErrMsg) == false)
            {
                b_ALL_Right = false;
                arl_ChkResult.Add("檔頭資料錯誤：" + s_ErrMsg);
            }
            else
            {
                this.FileHeaderRowText = sr_String_Head;
            }
        }

        #endregion

        #endregion
    }




}