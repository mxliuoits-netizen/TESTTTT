using PIC.DB;
using PIC.DB.Dao;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace UpdateMasterData.Dao;

/// <summary>
/// 組織群組 DAO
/// </summary>
public class BaseArrangeGroupDao: BaseDao
{

    public BaseArrangeGroupDao(IDataAccess da) {
        this.da = da;
    }

    /// <summary>
    /// 合併 區顧問人員組織資料
    /// </summary>
    /// <param name="createuser"></param>
    /// <returns></returns>
    public int MergeDistrictConsultant(string createuser)
    {
        var sql = @"
                MERGE INTO base_arrangegroup AS tgt
                USING (
                    select sapteamid || '_' || a_id as ts_id  from base_authority_m where sapaname like '%區顧問%'
                ) AS src  
                ON (tgt.ts_id = src.ts_id)
                WHEN NOT MATCHED
                THEN INSERT (g_id, g_name, ts_id, is_del, createuser, createtime, updateuser, updatetime)
                VALUES (@g_id, @g_name, src.ts_id, @is_del, @createuser, @createtime, @createuser, @createtime)";

        var param = new
        {
            g_id = "03",
            g_name = "區顧問",
            is_del = 'N',
            createuser,
            createtime = DateTime.Now,
        };

        return da.DapperExecute(sql, param);
    }

    /// <summary>
    /// 合併 TeamType = 2 聯服人員組織資料
    /// </summary>
    /// <param name="createuser"></param>
    /// <returns></returns>
    public int MergeTeamTypeJointServer(string createuser)
    {
        var sql = @"
                MERGE INTO base_arrangegroup AS tgt
                USING (
                    select distinct bam.sapteamid || '_' || bam.a_id as ts_id  
                    from base_authority_m bam
                    left join base_employee_m bem on bam.a_id = bem.a_id 
                    where bem.group_type ='2'
                ) AS src  
                ON (tgt.ts_id = src.ts_id)
                WHEN NOT MATCHED
                THEN INSERT (g_id, g_name, ts_id, is_del, createuser, createtime, updateuser, updatetime)
                VALUES (@g_id, @g_name, src.ts_id, @is_del, @createuser, @createtime, @createuser, @createtime)";

        var param = new
        {
            g_id = "04",
            g_name = "聯服",
            is_del = 'N',
            createuser,
            createtime = DateTime.Now,
        };

        return da.DapperExecute(sql, param);
    }

    /// <summary>
    /// 合併 TeamType = 1 商場人員組織資料
    /// </summary>
    /// <param name="createuser"></param>
    /// <returns></returns>

    public int MergeTeamTypeMallPersonnel(string createuser)
    {
        var sql = @"
                MERGE INTO base_arrangegroup AS tgt
                USING (
                    select distinct bam.sapteamid || '_' || bam.a_id as ts_id  
                    from base_authority_m bam
                    left join base_employee_m bem on bam.a_id = bem.a_id 
                    where bem.group_type ='1'
                ) AS src  
                ON (tgt.ts_id = src.ts_id)
                WHEN NOT MATCHED
                THEN INSERT (g_id, g_name, ts_id, is_del, createuser, createtime, updateuser, updatetime)
                VALUES (@g_id, @g_name, src.ts_id, @is_del, @createuser, @createtime, @createuser, @createtime)";

        var param = new
        {
            g_id = "05",
            g_name = "商場人員",
            is_del = 'N',
            createuser,
            createtime = DateTime.Now,
        };

        return da.DapperExecute(sql, param);
    }

    /// <summary>
    /// 合併 TeamType = 3,9 外勤後勤人員組織資料
    /// </summary>
    /// <param name="createuser"></param>
    /// <returns></returns>

    public int MergeTeamTypeOtherPersonnel(string createuser)
    {
        //當組織中 有外勤人員時(ba_count > 0) 則為外勤人員 無則為後勤人員
        var sql = @"
                MERGE INTO base_arrangegroup AS tgt
                USING (
                    select distinct bam.sapteamid || '_' || bam.a_id as ts_id, 
                    (
                        select count(1) from base_arrangegroup ba where ba.ts_id like bam.sapteamid || '_%' and g_id = '02'
                    ) as ba_count
                    from base_authority_m bam
                    left join base_employee_m bem on bam.a_id = bem.a_id 
                    where bem.group_type in ('3', '9')
                ) AS src  
                ON (tgt.ts_id = src.ts_id)
                WHEN NOT MATCHED AND src.ba_count > 0
                THEN
                    INSERT (g_id, g_name, ts_id, is_del, createuser, createtime, updateuser, updatetime)
                    VALUES (@g_id_field_personnel, @g_name_field_personnel, src.ts_id, @is_del, @createuser, @createtime, @createuser, @createtime)
                WHEN NOT MATCHED
                THEN 
                    INSERT (g_id, g_name, ts_id, is_del, createuser, createtime, updateuser, updatetime)
                    VALUES (@g_id_logistics, @g_name_logistics, src.ts_id, @is_del, @createuser, @createtime, @createuser, @createtime)";

        var param = new
        {
            g_id_field_personnel = "02",
            g_name_field_personnel = "其他人員(外勤)",
            g_id_logistics = "01",
            g_name_logistics = "其他人員(後勤)",
            is_del = 'N',
            createuser,
            createtime = DateTime.Now,
        };

        return da.DapperExecute(sql, param);
    }



}
