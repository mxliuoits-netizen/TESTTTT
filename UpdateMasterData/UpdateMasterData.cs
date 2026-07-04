using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PIC.DB;
using PIC.Job;
using PIC.Libary;
using PIC.Tools;
using NLog;
using System.IO;

namespace UpdateMasterData;

/// <summary>
/// 執行參數：args[0] - 模組名稱, args[1] - 模組代號, args[2] - 作業版本年月日(Y, M, D), args[3] - TODAY或給定年月日, args[4] - ConnectionType(POSTGRESQL,MSSQL,ORACLESQL)
/// </summary>
class UpdateMasterData
{
    public static Logger logger = LogManager.GetCurrentClassLogger();
    static void Main(string[] args)
    {
        //EncryptPWD();
        //AddViceJobCode();
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true; //啟用自動對應駝峰式命名轉換
        Dapper.SqlMapper.Settings.CommandTimeout = 600; //預設超時為10分鐘

        string jobName = "UpdateMasterData";
        string moduleName = Environment.GetEnvironmentVariable("PIC_JOB_MODULE_CODE");
        string[] envArgs = {
            Environment.GetEnvironmentVariable("PIC_JOB_MODULE_CODE"),
            Environment.GetEnvironmentVariable("PIC_JOB_MODULE_NAME"),
            Environment.GetEnvironmentVariable("PIC_JOB_VERSION_TYPE"),
            Environment.GetEnvironmentVariable("PIC_JOB_JOB_VERSION"),
            Environment.GetEnvironmentVariable("PIC_JOB_DATABASE_TYPE"),
            Environment.GetEnvironmentVariable("PIC_JOB_DATABASE_PREFIX"),
            Environment.GetEnvironmentVariable("PIC_JOB_CREATOR"),
            Environment.GetEnvironmentVariable("PIC_JOB_CREATOR_IP")
        };
        BaseJob job;
        switch (moduleName)
        {
            case "Job_UpdateOrganization": // 更新組織主檔
                job = new Job_UpdateOrganization(jobName, logger, envArgs);
                job.RunJob();
                break;
            case "Job_UpdateEmployee": // 更新員工主檔
                job = new Job_UpdateEmployee(jobName, logger, envArgs);
                job.RunJob();
                break;
            case "Job_UpdateEmployeeDiff": // 更新員工異動主檔
                job = new Job_UpdateEmployeeDiff(jobName, logger, envArgs);
                job.RunJob();
                break;
            case "Job_UpdateEmployeeVice": // 更新員工在職主檔
                job = new Job_UpdateEmployeeVice(jobName, logger, envArgs);
                job.RunJob();
                break;
            case "Job_UpdateQuota": // 更新剩餘假主檔
                job = new Job_UpdateQuota(jobName, logger, envArgs);
                job.RunJob();
                break;
            case "Job_UpdateReporting": // 更新匯報關係主檔
                job = new Job_UpdateReporting(jobName, logger, envArgs);
                job.RunJob();
                break;
                //case "Job_UpdateQuotaQualify": // 更新剩餘假生效日期及額度
                //    job = new Job_UpdateQuotaQualify(jobName, logger, args);
                //    job.RunJob();
                //    break;
        }



    }

    /*private static void EncryptPWD()
    {
        var decryptPWD = "!Pic1106";
        var encryptPWD = EncryptTool.EncryptSHA512(decryptPWD);
        Console.WriteLine($"[{decryptPWD}]:{encryptPWD}");
    }*/

    /*private static void AddViceJobCode()
    {
        try
        {
            var originFS = new StreamReader("C:\\Nick\\Decode_rs_EMP_ACTIVE20241024.TXT");
            var codeFS = new StreamReader("C:\\Nick\\員工主檔JOB_CODE_MAPPING.txt");
            string line;
            Dictionary<string, string> jobCodeMap = new Dictionary<string, string>();
            while ((line = codeFS.ReadLine()) != null)
            {
                string[] s = line.Split(",");
                if (s.Length == 2)
                {
                    jobCodeMap.Add(s[0], s[1]);
                }

            }

            var newFile = new StreamWriter("C:\\Nick\\rs_EMP_ACTIVE20241024_maped_jobcode.TXT");
            line = "";
            int i = 0;
            while ((line = originFS.ReadLine()) != null)
            {
                string[] s = line.Split("|");
                if (s.Length == 19)
                {
                    var empid = s[2].Trim();
                    string jobCode = "";
                    jobCodeMap.TryGetValue(empid, out jobCode);
                    newFile.WriteLine(line + "|" + jobCode);
                }
                else
                {
                    newFile.WriteLine(line);
                }
                i++;
                if (i % 100 == 0) newFile.Flush();
            }
            newFile.Flush();
            newFile.Close();
            newFile.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());

        }
    }*/




}
