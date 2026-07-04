using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;
using PIC.Tools;
using UpdateMasterData.Model;

namespace UpdateMasterData
{
    class CheckFile
    {
        #region 設定變數

        string s_LogMsg = string.Empty;

        Dictionary<string, string> SettingDictionary;
        string s_Data_Body;

        string s_FTP_File_Name;
        string s_Data_HeadEnd;
        int i_ActualCount;
        DateTime d_JOB_VersionDate;
        bool b_IsChkFile;

        #endregion

        /// <summary>
        /// 檢查檔身
        /// </summary>
        /// <param name="settingDictionary"></param>
        /// <param name="s_Input_Data_Body"></param>
        public CheckFile(Dictionary<string, string> settingDictionary, string s_Input_Data_Body)
        {
            this.SettingDictionary = settingDictionary;
            this.s_Data_Body = s_Input_Data_Body;
            this.b_IsChkFile = (settingDictionary.GetValueOrDefault("IsChkFile", "").ToUpper() != "N") ? true : false;
        }

        /// <summary>
        /// 檢查檔頭檔尾
        /// </summary>
        /// <param name="xml_Input_Node">XML規則</param>
        /// <param name="s_Input_Handle_File_Name">處理的檔案名稱</param>
        /// <param name="s_Input_Data_HeadEnd">判斷的字串</param>
        /// <param name="i_Input_ActualCount">實際檔身筆數</param>
        /// <param name="d_Input_JOB_VersionDate">處理日期</param>
        public CheckFile(Dictionary<string, string> settingDictionary, string s_Input_FTP_File_Name, string s_Input_Data_HeadEnd, int i_Input_ActualCount, DateTime d_Input_JOB_VersionDate)
        {
            this.SettingDictionary = settingDictionary;
            this.s_FTP_File_Name = s_Input_FTP_File_Name;
            this.s_Data_HeadEnd = s_Input_Data_HeadEnd.Trim();//2012/08/07 sap給的資料會有他媽的空白，所以要trim掉空白
            this.i_ActualCount = i_Input_ActualCount;
            this.d_JOB_VersionDate = d_Input_JOB_VersionDate;
        }

        /// <summary>
        /// 檢查驗證檔身字串
        /// </summary>
        /// <param name="s_ErrMsg">驗證錯誤訊息</param>
        /// <returns></returns>
        public bool ChkBodyResult(out string s_ErrMsg, out string s_Insert)
        {
            #region 設定變數

            s_ErrMsg = string.Empty;
            s_Insert = string.Empty;
            string[] s_Array_Data = new string[0];
            var colCount = 0;
            #endregion

            try
            {
                #region 依照分隔符號，分隔驗證字串

                s_LogMsg = "依照分隔符號，分隔驗證字串";
                var splitText = SettingDictionary.GetValueOrDefault("Split", "");
                s_Array_Data = this.s_Data_Body.Split(char.Parse(splitText));//依照分隔符號，分隔驗證字串

                #endregion

                #region 檢查驗證字串Split後的欄位數目和XML規則的數目相符

                s_LogMsg = "檢查驗證字串Split後的欄位數目和XML規則的數目相符";

                //如果不相符，直接錯誤，後面不在繼續
                colCount = SettingDictionary.Keys.Where(x => x.StartsWith("Col") && x.EndsWith(".Name")).Count();
                if (s_Array_Data.Length != colCount)
                {
                    s_ErrMsg = "驗證字串Split後的欄位數目和XML規則的數目不相符";
                    goto Result_Error;
                }

                #endregion

                //如果需要驗證才驗證，不需要驗證就直接組SQL
                if (this.b_IsChkFile == true)
                {
                    for (int i = 0; i < s_Array_Data.Length; i++)
                    {
                        #region 設定XML變數

                        s_LogMsg = "設定XML變數";
                        string s_ColumnData = s_Array_Data[i].Trim();//驗證的資料
                        string s_xml_Name = SettingDictionary.GetValueOrDefault($"Col{i + 1}.Name", "");//對應到 的欄位名稱，一定有值
                        string s_xml_Len = SettingDictionary.GetValueOrDefault($"Col{i + 1}.Len", "");//欄位大小(單位：Byte)，一定有值
                        string s_xml_Type = SettingDictionary.GetValueOrDefault($"Col{i + 1}.Type", "");//欄位格式，一定有值
                        string s_xml_Rule = SettingDictionary.GetValueOrDefault($"Col{i + 1}.Rule", "");//欄位格式=custom，依照自訂的規則，不一定會有值
                        string s_xml_Fix = SettingDictionary.GetValueOrDefault($"Col{i + 1}.Fix", "");//欄位固定值為甚麼，不一定會有值

                        #endregion

                        #region 檢查欄位固定值與傳入資料是否相符

                        s_LogMsg = "檢查欄位固定值與傳入資料是否相符";

                        if (s_xml_Fix != string.Empty)
                        {
                            if (s_ColumnData != s_xml_Fix)
                            { s_ErrMsg += "欄位：" + s_xml_Name + "為[" + s_ColumnData + "]，該欄位設定必須固定為[" + s_xml_Fix + "]。"; }
                        }

                        #endregion

                        #region 檢查欄位長度

                        s_LogMsg = "檢查欄位長度";

                        if (s_xml_Len != string.Empty)
                        {
                            //檢查是否有超過XML所設定的長度
                            //int i_Data_Len2= System.Text.Encoding.Default.GetBytes(s_ColumnData).Length;
                            int i_Data_Len = s_ColumnData.Length;
                            int i_Rule_Len = int.Parse(s_xml_Len);

                            if (i_Data_Len > i_Rule_Len)
                            { 
                                s_ErrMsg += "欄位：" + s_xml_Name + "長度為[" + i_Data_Len.ToString() + "]大於設定長度[" + i_Rule_Len.ToString() + "]。"; 
                            }
                        }

                        #endregion

                        #region 依照Type來驗證資料

                        //驗證資料必須不為空值才做驗證，空值就直接跳過
                        if (s_ColumnData != string.Empty)
                        {
                            switch (s_xml_Type)
                            {
                                #region 數字型態

                                case "num":

                                    s_LogMsg = "數字型態";

                                    //檢查驗證資料的每一個字是否為數目字
                                    if (!DataValidator.IsNumber(s_ColumnData, false))
                                    { s_ErrMsg += "欄位：" + s_xml_Name + "為[" + s_ColumnData + "]，該欄位設定必須為數字型態。"; }

                                    break;

                                #endregion

                                #region 整數型態

                                case "int":

                                    s_LogMsg = "整數型態";

                                    //檢查驗證資料是否為整數
                                    if (!DataValidator.IsInteger(s_ColumnData, false))
                                    { s_ErrMsg += "欄位：" + s_xml_Name + "為[" + s_ColumnData + "]，該欄位設定必須為整數型態。"; }

                                    break;

                                #endregion

                                #region 小數點型態

                                case "float":

                                    s_LogMsg = "小數點型態";

                                    string s_Pattern = @"^([-]?)([0-9]*)((.[0,5]{1,1}[0]{1,1})?)$";

                                    Regex reg_double = new Regex(s_Pattern);

                                    if (reg_double.IsMatch(s_ColumnData.Trim()) == false)
                                    { s_ErrMsg += "欄位：" + s_xml_Name + "為[" + s_ColumnData + "]，該欄位設定必須為小數點型態。"; }

                                    break;

                                #endregion

                                #region 日期型態

                                case "date":

                                    s_LogMsg = "日期型態";

                                    //SAP給予的調入日與調出日如果是NULL則為00000000 
                                    if(s_ColumnData=="00000000")
                                    {
                                        s_Array_Data[i] = string.Empty;
                                        break;
                                    }
 
                                    if (s_xml_Name == "ExpireDate" && s_ColumnData.Trim()=="")
                                    {
                                        s_Array_Data[i] = string.Empty;
                                        break;
                                    }

                                    if (!DataValidator.IsDate(s_ColumnData, s_xml_Rule))
                                    { s_ErrMsg += "欄位：" + s_xml_Name + "為[" + s_ColumnData + "]，該欄位日期不正確，格式為[" + s_xml_Rule + "]。"; }

                                    break;

                                #endregion

                                #region 自訂型態

                                case "custom":

                                    switch (s_xml_Rule)
                                    {
                                        #region 英文型態

                                        case "en":

                                            s_LogMsg = "英文型態";

                                            //檢查驗證資料的每一個字是否為英文字
                                            if (!DataValidator.IsEnglish(s_ColumnData, false))
                                            { s_ErrMsg += "欄位：" + s_xml_Name + "為[" + s_ColumnData + "]，該欄位設定必須為英文型態。"; }

                                            break;

                                        #endregion

                                        #region 英文或數字

                                        case "en,int":

                                            s_LogMsg = "英文或數字";

                                            //檢查驗證資料的每一個字是否為英文字或數字
                                            if (!DataValidator.IsEngAndNum(s_ColumnData, false))
                                            { s_ErrMsg += "欄位：" + s_xml_Name + "為[" + s_ColumnData + "]，該欄位設定必須為英文或數字型態。"; }

                                            break;

                                        #endregion

                                        #region PT或FT

                                        case "PT,FT":

                                            s_LogMsg = "PT或FT";

                                            if (s_ColumnData != "PT" && s_ColumnData != "FT")
                                            { s_ErrMsg += "欄位：" + s_xml_Name + "為[" + s_ColumnData + "]，該欄位設定必須為PT或FT。"; }

                                            break;

                                        #endregion

                                        #region F或M

                                        case "F,M":

                                            s_LogMsg = "F或M";

                                            if (s_ColumnData != "F" && s_ColumnData != "M")
                                            { s_ErrMsg += "欄位：" + s_xml_Name + "為[" + s_ColumnData + "]，該欄位設定必須為F或M。"; }

                                            break;

                                        #endregion

                                        #region I或U或D

                                        case "I,U,D":

                                            s_LogMsg = "I或U或D";

                                            if (s_ColumnData != "I" && s_ColumnData != "U" && s_ColumnData != "D")
                                            { s_ErrMsg += "欄位：" + s_xml_Name + "為[" + s_ColumnData + "]，該欄位設定必須為I或U或D。"; }

                                            break;

                                        #endregion
                                    }

                                    break;

                                #endregion
                            }
                        }

                        #endregion
                    }
                }
            }
            catch (Exception ex)
            { s_ErrMsg = "class CheckFile發生錯誤，" + this.s_LogMsg + "，錯誤原因：" + ex.ToString(); }

            if (s_ErrMsg != string.Empty)
            { goto Result_Error; }
            else
            {
                #region 組合SQL字串

                s_Insert = s_Insert.Insert(s_Insert.Length, "insert into");
                s_Insert = s_Insert.Insert(s_Insert.Length, " " + SettingDictionary.GetValueOrDefault("Table", "") + " (");

                for (int i = 0; i < colCount; i++)
                {
                    s_Insert = s_Insert.Insert(s_Insert.Length, ((i == 0) ? string.Empty : ",") + SettingDictionary.GetValueOrDefault($"Col{i + 1}.Name")); 
                }

                s_Insert = s_Insert.Insert(s_Insert.Length, ") values (");

                for (int i = 0; i < s_Array_Data.Length; i++)
                { s_Insert = s_Insert.Insert(s_Insert.Length, ((i == 0) ? string.Empty : ",") + ((s_Array_Data[i].Trim()) == string.Empty ? "null" : "'" + s_Array_Data[i].Trim() + "'")); }

                s_Insert = s_Insert.Insert(s_Insert.Length, ")");

                #endregion

                goto Result_Right;
            }

        Result_Error:
            return false;

        Result_Right:
            return true;
        }

        /// <summary>
        /// 檢查驗證檔頭檔尾字串
        /// </summary>
        /// <param name="s_ErrMsg">驗證錯誤訊息</param>
        /// <returns></returns>
        public bool ChkHeadEndResult(out string s_ErrMsg)
        {
            #region 設定變數

            s_ErrMsg = string.Empty;
            this.s_FTP_File_Name = this.s_FTP_File_Name.Replace(".TXT", string.Empty);
            string s_Data_Count = string.Empty;

            #endregion

            try
            {
                #region 檢查檔頭碼數，不對就不用繼續做下去

                s_LogMsg = "檢查檔頭碼數，不對就不用繼續做下去";          
                int i_Head_Count = int.Parse(SettingDictionary.GetValueOrDefault("HeaderCount", "0"));

                if (this.s_Data_HeadEnd.Length != this.s_FTP_File_Name.Length + i_Head_Count)
                {
                    s_ErrMsg = "檔頭長度錯誤";
                    goto Result_Error;
                }

                #endregion

                #region 檢查表頭和檔案檔名是否相同

                if (this.s_Data_HeadEnd.StartsWith(this.s_FTP_File_Name) == false)
                {
                    s_ErrMsg += "文字檔表頭為[" + this.s_Data_HeadEnd + "]，檔案檔名為[" + this.s_FTP_File_Name + "]。";
                    goto Result_Error;
                }

                #endregion
 
                #region 檢查資料筆數

                s_LogMsg = "檢查資料筆數";

                s_Data_Count = this.s_Data_HeadEnd.Replace(this.s_FTP_File_Name, string.Empty);
                //2012/08/07 因為轉入剩餘假的文字檔表頭規格異動
                //判定資料筆數規則改取文字檔表頭後8碼
                s_Data_Count = s_Data_Count.Substring(s_Data_Count.Length - 8, 8);

                if (!DataValidator.IsNumber(s_Data_Count, false))
                { 
                    s_ErrMsg += "文字檔資料筆數[" + s_Data_Count + "]，不為數字型態。";
                }
                else if (int.Parse(s_Data_Count) != this.i_ActualCount)
                { 
                    s_ErrMsg += "文字檔資料筆數為[" + s_Data_Count + "]筆，實際資料筆數為[" + this.i_ActualCount.ToString() + "]筆。";
                }

                #endregion
            }
            catch (Exception ex)
            { 
                s_ErrMsg = "class CheckFile發生錯誤，" + this.s_LogMsg + "，錯誤原因：" + ex.ToString(); 
            }

            if (s_ErrMsg != string.Empty)
            { 
                goto Result_Error;
            }
            else
            { 
                goto Result_Right; 
            }

        Result_Error:
            return false;

        Result_Right:
            return true;
        }

        /// <summary>
        /// 檢查檔身字串是否允許匯入
        /// </summary>
        /// <param name="s_ErrMsg">驗證錯誤訊息</param>
        /// <returns></returns>
        public bool ChkBodyAllowResult(out string s_ErrMsg)
        {
            #region 設定變數

            s_ErrMsg = string.Empty;
            string[] s_Array_Data = new string[0];
            var colCount = 0;
            #endregion

            try
            {
                #region 依照分隔符號，分隔驗證字串

                s_LogMsg = "依照分隔符號，分隔驗證字串";
                var splitText = SettingDictionary.GetValueOrDefault("Split", "|");
                s_Array_Data = this.s_Data_Body.Split(char.Parse(splitText));//依照分隔符號，分隔驗證字串

                #endregion

                #region 檢查驗證字串Split後的欄位數目和XML規則的數目相符

                s_LogMsg = "檢查驗證字串Split後的欄位數目和XML規則的數目相符";

                //如果不相符，直接錯誤，後面不在繼續
                colCount = SettingDictionary.Keys.Where(x => x.StartsWith("Col") && x.EndsWith(".Name")).Count();
                if (s_Array_Data.Length != colCount)
                {
                    s_ErrMsg = "驗證字串Split後的欄位數目和XML規則的數目不相符";
                    goto Result_Error;
                }

                #endregion

                for (int i = 0; i < s_Array_Data.Length; i++)
                {
                    #region 設定XML變數

                    s_LogMsg = "設定XML變數";

                    string s_ColumnData = s_Array_Data[i].Trim();//驗證的資料
                    string s_xml_Name = SettingDictionary.GetValueOrDefault($"Col{i+1}.Name", "");//對應到 的欄位名稱，一定有值
                    string s_xml_Allow = SettingDictionary.GetValueOrDefault($"Col{i + 1}.Allow", "");//是否允許匯入

                    #endregion

                        #region 檢查是否允許匯入

                        //驗證資料必須不為空值才做驗證，空值就直接跳過
                    if (s_ColumnData != string.Empty && s_xml_Allow != string.Empty)
                    {
                        string[] s_Array_Allow = s_xml_Allow.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                        if (s_Array_Allow.Contains(s_ColumnData.Trim()) == false)
                        {
                            //s_ErrMsg += "欄位：" + s_xml_Name + "的資料為[" + s_ColumnData + "]，不允許匯入。";
                            goto Result_Error;
                        }
                    }

                    #endregion
                }

                return true;
            }
            catch (Exception ex)
            {
                s_ErrMsg = "class CheckFile發生錯誤，" + this.s_LogMsg + "，錯誤原因：" + ex.ToString();
            }

            Result_Error:
            return false;
        }
    }
}