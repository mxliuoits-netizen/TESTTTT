using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateMasterData.Tool;

/// <summary>
/// 員工主檔解密
/// 1.依據當天日期下載FTP檔案
/// 
///   
/// 2.讀取FTP檔案(UTF16)
///   讀取固定字串(UTF16)
///   
/// 3.利用密碼算出固定字串分割位置
///   -將每個位數當成個位數將其加總
///   
/// 4.重組固定字串
/// 
/// 5.解密(置換字串)(Unicode)
/// 
/// 6.產生解密文件於資料夾下
/// </summary>
public class DecodeTool
{
    #region Attribute
    /// <summary>
    /// job名稱
    /// </summary>
    private string strAppName = "Decode";
    /// <summary>
    /// 密碼
    /// </summary>
    string strPassword = string.Empty;

    /// <summary>
    /// 密碼加總
    /// </summary>
    int iSumPassword = 0;

    /// <summary>
    /// (移動後)固定字串
    /// </summary>
    string strOldFix = string.Empty;

    /// <summary>
    /// (移動後)固定字串
    /// </summary>
    string strNewFix = string.Empty;

    /// <summary>
    /// 解密後字串
    /// </summary>
    string strDecode = string.Empty;

    /// <summary>
    /// 加密字串
    /// </summary>
    string strEncode = string.Empty;

    /// <summary>
    /// (置換前)字元
    /// </summary>
    string strOld = string.Empty;

    /// <summary>
    /// (置換後)字元
    /// </summary>
    string strNew = string.Empty;

    /// <summary>
    /// 來源檔位置及名稱
    /// </summary>
    string s_Src_FilePath = string.Empty;

    /// <summary>
    /// 目的檔位置及名稱
    /// </summary>
    string s_Desti_FilePath = string.Empty;

    /// <summary>
    /// 系統時間
    /// </summary>
    string SysDate = DateTime.Now.ToString("yyyyMMddHHmmss");

    /// <summary>
    /// 解密後位元byte
    /// </summary>
    byte[] decodebyte;
    #endregion

    #region Construct
    public DecodeTool(string decodeCode)
    {
        // 解密檔路徑
        strOldFix = decodeCode;
    }

    #endregion

    #region Main Process
    /// <summary>
    /// 開始工作排程
    /// </summary>
    public void RunJob(string s_FilePath, string s_Src_File, string s_Desti_File)
    {
        try
        {
            // 設定路徑
            GetFilePath_Name(s_FilePath, s_Src_File, s_Desti_File);

            // 解密檔案
            DecodeFile();
        }
        catch (Exception ex)
        {
            throw new Exception(string.Format("文件解密因：{0}", ex.Message));
        }
    }

    #endregion

    #region Private Method

    #region 加總密碼
    /// <summary>
		/// 加總密碼
		/// </summary>
		/// <param name="password">密碼</param>
		/// <returns></returns>
		public void SumPassword(string password)
    {
        try
        {
            int iSum = 0;
            for (int i = 0; i < password.Length; i++)
            {
                iSum = iSum + int.Parse(password.Substring(i, 1));
            }
            iSumPassword = iSum;
        }
        catch (Exception ex)
        {
            throw new Exception(string.Format("{0}加總密碼失敗，因為：{1}", strAppName, ex.Message));
        }
    }

    #endregion

    #region 變換固定字串位置
    /// <summary>
    /// 變換固定字串位置
    /// </summary>
    /// <returns></returns>
    public void ChanegeLocation()
    {
        try
        {
            string string1 = strOldFix.Substring(0, iSumPassword);
            string string2 = strOldFix.Substring(iSumPassword, strOldFix.Length - iSumPassword);
            strNewFix = @string2 + string1;
        }
        catch (Exception ex)
        {
            throw new Exception(string.Format("{0}變換固定字串位置失敗，因為：{1}", strAppName, ex.Message));
        }
    }

    #endregion

    #region  置換字串
    /// <summary>
    /// 置換字串
    /// </summary>
    /// <returns></returns>
    public void ReplaceString()
    {
        try
        {
            int iIndex = 0;
            strDecode = string.Empty;

            #region 解密
            //char[] c = strEncode.ToCharArray();
            byte[] encodebyte = System.Text.Encoding.Unicode.GetBytes(strEncode);
            decodebyte = new byte[encodebyte.Length];
            char tochar = '\0';
            for (int i = 0; i < encodebyte.Length; i++)
            {

                //BYTE轉字元
                tochar = (char)encodebyte[i];

                if (encodebyte[i] >= 32 && encodebyte[i] <= 126)
                {
                    iIndex = strNewFix.IndexOf(tochar);

                    //依Index尋找舊固定字串的字元
                    strNew = strOldFix.Substring(iIndex, 1);
                }
                else
                {
                    strNew = tochar.ToString();
                }

                decodebyte[i] = (byte)strNew.ToCharArray()[0]; //置換後BYTE

                strDecode = System.Text.Encoding.Unicode.GetString(decodebyte); //byte轉成字串(字串無使用

            }
            #endregion
        }
        catch (Exception ex)
        {
            throw new Exception(string.Format("{0}置換字串失敗，置換字元：''{1}''，因為：{2}", strAppName, strOld, ex.Message));
        }
    }
    #endregion

    #region 設定路徑
    /// <summary>
    /// 設定路徑
    /// </summary>
    private void GetFilePath_Name(string s_FilePath, string s_Src_File, string s_Desti_File)
    {
        try
        {
            // 來源檔路徑及名稱
            s_Src_FilePath = s_FilePath + s_Src_File;

            // 解密後的路徑及名稱
            s_Desti_FilePath = s_FilePath + s_Desti_File;
        }
        catch (Exception ex)
        {
            throw new Exception(string.Format("{0}設定路徑失敗，因為：{1}", strAppName, ex.Message));
        }
    }
    #endregion

    #region 解密檔案
    /// <summary>
    /// 解密檔案
    /// </summary>
    /// <returns></returns>
    private void DecodeFile()
    {
        try
        {
            FileStream file = File.Open(s_Src_FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            BinaryReader myReader = new BinaryReader(file, Encoding.Unicode);
            StringBuilder sb = new StringBuilder();

            // 存放Encode文件的陣列
            List<string> GetData = new List<string>();

            // 指定DECODE文件放置路徑&檔名
            FileStream myFile = File.Open(s_Desti_FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            BinaryWriter myWriter = new BinaryWriter(myFile);


            int dl = System.Convert.ToInt32(file.Length);
            byte[] databyte = myReader.ReadBytes(dl);

            byte[] code = { 0x00FF, 0x00FE }; //定義編碼FF FE
            byte[] next = { 0x000D, 0, 0x000A, 0 }; //須另外定義換行
            myWriter.Write(code);

            int j = 2, k = 0; //去除FE FF
            for (int i = 0; i < databyte.Length;)
            {

                if (databyte[i] == 13 && databyte[i + 1] == 00 && databyte[i + 2] == 10 && databyte[i + 3] == 00) //判斷換行
                {
                    GetData.Add(System.Text.Encoding.Unicode.GetString(databyte, j, i - j));
                    strPassword = GetData[k].ToString().Substring(0, 6);
                    strEncode = GetData[k].ToString().Substring(6, GetData[k].ToString().Length - 6);

                    //加總密碼
                    SumPassword(strPassword);

                    //變換固定字串位置
                    ChanegeLocation();

                    //置換字串
                    ReplaceString();

                    // 將置換文字寫入文件
                    myWriter.Write(decodebyte);
                    myWriter.Write(next);
                    k++;
                    j = i + 4;
                    i = i + 2;
                }
                i = i + 2;
            }


            myFile.Close();
            myWriter.Close();


            file.Close();
            myReader.Close();

        }
        catch (Exception ex)
        {
            throw new Exception(string.Format("{0}解密檔案失敗，因為：{1}加密字串：''{2}''", strAppName, ex.Message, (strPassword + strEncode)));
        }
    }
    #endregion

    #endregion

    private string GetSysConfigValue(string s_Value, string s_GROUPNAME, string s_ITEM)
    {
        string s_ErrMsg = string.Empty;

        if (s_Value == null ||
            s_Value.Trim() == string.Empty)
        {
            s_ErrMsg += "取得參數檔 T_SYSCONFIG 發生異常：GROUPNAME = " + s_GROUPNAME + "，ITEM = " + s_ITEM;
            throw new Exception(s_ErrMsg);
        }

        return s_Value.Trim();
    }







}
