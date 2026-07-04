using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace NotifyMonitorJob.Tool;

public static class ZipTool
{
    /// <summary>
    /// 壓縮檔案
    /// </summary>
    /// <param name="formFileList"></param>
    /// <param name="prefix"></param>
    /// <param name="applyId"></param>
    /// <returns></returns>
    public static byte[] ZipFile(List<PreZipFile> attachmentList, string encryptText = "")
    {
        if (attachmentList.Count == 0) return null;

        using var memoryStream = new MemoryStream();
        using ZipOutputStream zipOutputStream = new ZipOutputStream(memoryStream);
        zipOutputStream.SetLevel(3);

        if (!String.IsNullOrEmpty(encryptText)) zipOutputStream.Password = encryptText;
        foreach (var file in attachmentList)
        {
            ZipEntry entry = new ZipEntry(file.FileName);
            entry.IsUnicodeText = true; //unicode 處裡檔名以及內文
            zipOutputStream.PutNextEntry(entry);
            zipOutputStream.Write(file.FileBytes, 0 , file.FileBytes.Length);
            zipOutputStream.CloseEntry();
        }

        // 關閉上層流
        zipOutputStream.IsStreamOwner = false;
        zipOutputStream.Close();

        memoryStream.Position = 0;
        return memoryStream.ToArray();
    }
}

public class PreZipFile
{
    public string FileName { get; set; }

    public byte[] FileBytes { get; set; }
}
