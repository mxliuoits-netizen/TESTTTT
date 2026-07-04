// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");

using NLog;
using NotifyMonitorJob;
using PIC.Job;

Logger logger = LogManager.GetCurrentClassLogger();

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true; //啟用自動對應駝峰式命名轉換
Dapper.SqlMapper.Settings.CommandTimeout = 600; //預設超時為10分鐘

string jobName = "NotifyMonitorJob";
//string moduleName = args[0];
string moduleName = Environment.GetEnvironmentVariable("PIC_JOB_MODULE_CODE");
string[] envArgs = {
                Environment.GetEnvironmentVariable("PIC_JOB_MODULE_CODE"),
                Environment.GetEnvironmentVariable("PIC_JOB_MODULE_NAME"),
                Environment.GetEnvironmentVariable("PIC_JOB_VERSION_TYPE"),
                Environment.GetEnvironmentVariable("PIC_JOB_JOB_VERSION"),
                Environment.GetEnvironmentVariable("PIC_JOB_DATABASE_TYPE"),
                Environment.GetEnvironmentVariable("PIC_JOB_DATABASE_PREFIX"),
                Environment.GetEnvironmentVariable("PIC_JOB_CREATOR"),
                Environment.GetEnvironmentVariable("PIC_JOB_CREATOR_IP"),
                Environment.GetEnvironmentVariable("PIC_JOB_SMTPServer"),
                Environment.GetEnvironmentVariable("PIC_JOB_SMTPServer_Port"),
                Environment.GetEnvironmentVariable("PIC_JOB_RETRY"),
                Environment.GetEnvironmentVariable("PIC_JOB_MAILFROM"),
                Environment.GetEnvironmentVariable("PIC_JOB_THREAD_NO"),
                Environment.GetEnvironmentVariable("PIC_JOB_DISPLAYNAME"),
                Environment.GetEnvironmentVariable("PIC_JOB_DISPLAYNAME_MODE"),
            };
BaseJob job;
switch (moduleName)
{
    case "Job_CheckAbnormalData": // 發送通知維運人員排程執行結果 Email
        job = new Job_CheckAbnormalData(jobName, logger, envArgs);
        job.RunJob();
        break;
    case "Job_CheckAbnormalDataBlack": // 發送通知維運人員排程執行結果 Email
        job = new Job_CheckAbnormalData(jobName, logger, envArgs);
        job.RunJob();
        break;
    case "Job_CheckCustomerAbnormalData": // 發送通知客戶排程執行結果 Email
        job = new Job_CheckCustomerAbnormalData(jobName, logger, envArgs);
        job.RunJob();
        break;
    case "Job_CheckNotifyUserAbnormal":
        job = new Job_CheckNotifyUserAbnormal(jobName, logger, envArgs);
        job.RunJob();
        break;
    case "Job_NotifyMonitorJob": // 生死監控排程通知 Email
        job = new Job_NotifyMonitorJob(jobName, logger, envArgs);
        job.RunJob();
        break;
}