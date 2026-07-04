using PIC.DB;
using PIC.DB.Dao;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateMasterData.Model;

namespace UpdateMasterData.Dao;

/// <summary>
/// 作業設定 DAO
/// </summary>
public class JobConfigSettingDao : BaseDao
{
    public JobConfigSettingDao(IDataAccess da)
    {
        this.da = da;
    }

    /// <summary>
    /// 查詢 作業設定
    /// </summary>
    /// <param name="jobCode"></param>
    /// <returns></returns>
    public Dictionary<string, string> GetList(string jobCode)
    {
        var sql = @" select setting_key, setting_value from job_config_setting_dev1 where job_code = @job_code order by setting_key ";
        return da.DapperQuery<JobConfgSettingModel>(sql, new {job_code = jobCode}).ToDictionary(x=> x.settingKey, x=>x.settingValue);
    }




}
