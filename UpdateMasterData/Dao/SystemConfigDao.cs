using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using PIC.DB;
using PIC.DB.Dao;
using PIC.Libary.Model;

namespace UpdateMasterData.Dao;

internal class SystemConfigDao : BaseDao
{
    private IDataAccess da;
    private Dictionary<string, List<SystemConfigModel>> GroupConfig;

    public SystemConfigDao(IDataAccess da)
    {
        this.da = da;
        this.GroupConfig = new Dictionary<string, List<SystemConfigModel>>();
    }

    /// <summary>
    /// 查詢 設定值
    /// </summary>
    /// <param name="groupKey"></param>
    /// <param name="itemKey"></param>
    /// <returns></returns>
    public string GetValueWithName(string groupKey, string itemKey, string defaultValue = "")
    {
        var sql = @" select value from system_config_m where groupname = @groupKey and item = @itemKey ";
        var value = da.DapperQuery<string>(sql, new { groupKey, itemKey }).FirstOrDefault();
        return value ?? defaultValue;
    }

    /// <summary>
    /// 查詢 設定值
    /// </summary>
    /// <param name="groupKey"></param>
    /// <param name="itemKey"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public int GetValueWithName(string groupKey, string itemKey, int defaultValue)
    {
        int result = 0;
        var value = GetValueWithName(groupKey, itemKey);
        bool b = int.TryParse(value, out result);
        if (!b) result = defaultValue;

        return result;
    }

    /// <summary>        
    /// 傳入 Group 裡所有的數值項目
    /// </summary>
    /// <param name="groupName"></param>
    /// <returns>Hashtable(數值集合)</returns>
    public List<SystemConfigModel> GetConfigGroup(string groupName)
    {
        if (GroupConfig.ContainsKey(groupName)) return GroupConfig[groupName];
        string sql = @"select * from System_Config_m where UPPER(GroupName) = UPPER(@groupName)";
        List<SystemConfigModel> systemConfigList = da.DapperQuery<SystemConfigModel>(sql, new { groupName = groupName }).ToList();
        GroupConfig[groupName] = systemConfigList;
        return systemConfigList;
    }

    /// <summary>
    /// 依 groupName 及 itemName 傳回數值 Default: String.Empty
    /// </summary>
    /// <param name="groupName"></param>
    /// <param name="itemName"></param>
    /// <returns>
    /// 來源：m_SysConfig
    /// </returns>
    public string GetItemValue(string groupName, string itemName)
    {
        return this.GetConfigGroup(groupName)?.Where(x => x.item == itemName).Select(x => x.value).FirstOrDefault() ?? "";
    }

}
