using PIC.DB;
using PIC.DB.Dao;
using System;
using System.Linq;
using UpdateMasterData.Model;

namespace UpdateMasterData.Dao
{
    /// <summary>
    /// 剩餘假明細表 DAO
    /// </summary>
    public class ArrOffDayRecordDao : BaseDao
    {

        public ArrOffDayRecordDao(IDataAccess da) {
            this.da = da;
        }

        /// <summary>
        /// 備份資料
        /// </summary>
        /// <returns></returns>

        public int Backup()
        {
            var sql = @"insert into arr_offdayrecord_bak 
                (serverip, createuser, createip, createtime, empid, empname, datefrom, dateto, wt_id, sap_uk, sapid, sapname, totalnumber, usednumber, restnumber, datecurrent, note, backuptime)
                select 
                    serverip, createuser, createip, createtime, empid, empname, datefrom, dateto, wt_id, sap_uk, sapid, sapname, totalnumber, usednumber, restnumber, datecurrent, note, now()
                from arr_offdayrecord_t";

            return da.DapperExecute(sql);
        }

        /// <summary>
        /// 刪除資料
        /// </summary>
        /// <param name="dateCurrent"></param>
        /// <returns></returns>
        public int DeleteByDateCurrent(DateTime dateCurrent)
        {
            var sql = @"delete from arr_offdayrecord_t where datecurrent = @datecurrent ";
            return da.DapperExecute(sql, new { datecurrent = dateCurrent });
        }

        /// <summary>
        /// 新增資料
        /// </summary>
        /// <param name="logData"></param>
        /// <param name="dateCurrent"></param>
        /// <returns></returns>
        public int InsertByJobQuotaTmp(JobMetaDataModel logData, DateTime dateCurrent)
        {
            var sql = @"insert into arr_offdayrecord_t 
                    (serverip, createuser, createip, createtime, sap_uk, empid, empname, datefrom, dateto, wt_id, sapid, sapname, totalnumber, usednumber, restnumber, datecurrent, note)
                    select 
                        @serverip as serverip, @createuser as createuser, @createip as createip, @createtime as createtime, 
                        jqt.sap_uk, jqt.empid, jqt.empname, jqt.datefrom, jqt.dateto, 
                        bwm.wt_id, jqt.sapid, jqt.sapname, jqt.totalnumber, jqt.usednumber, jqt.restnumber, @datecurrent as datecurrent, 'SAP' as note
                    from job_quota_tmp jqt
                    left join base_worktype_m bwm on jqt.sapid = bwm.sapid and bwm.workgroup = 'leave' and bwm.isactive = 'Y'
                    where jqt.data_operation = 'U'
                ";

            var param = new
            {
                serverip = logData.ServerIp,
                createuser = logData.CreateUser,
                createip = logData.CreateIp,
                createtime = logData.CreateTime,
                datecurrent = dateCurrent
            };

            return da.DapperExecute(sql, param);
        }

        /// <summary>
        /// 新增 當全建時刪除 SAP_UK不存在於匯入暫存表SAP_UK的資料 將createtime=jobDate, 總額度=0, DateCurrent=jobDate 補回去
        /// </summary>
        /// <param name="jobDate"></param>
        /// <returns></returns>
        public int RebuildNotExistsWithSapUK(DateTime jobDate)
        {
            int i = 0;
            const string noteText = "Job_NotExist";
            var param = new { jobDate = jobDate, noteText };

            var sql = @"
            INSERT INTO arr_offdayrecord_t
             (serverip, createuser, createip, createtime, sap_uk, empid, empname, datefrom, dateto, wt_id, sapid, sapname, totalnumber, usednumber, restnumber, datecurrent, note)
            select distinct on (sap_uk)
                aot.serverip, aot.createuser, aot.createip, @jobDate as createtime, 
                aot.sap_uk, aot.empid, aot.empname, aot.datefrom, aot.dateto, aot.wt_id, aot.sapid, aot.sapname, 0 as totalnumber, aot.usednumber, aot.restnumber, 
                @jobDate as datecurrent, @noteText as note
            from arr_offdayrecord_t aot 
            where @jobDate between aot.datefrom and aot.dateto and aot.sap_uk not in (select jqt.sap_uk from job_quota_tmp jqt)
            order by aot.sap_uk, aot.datecurrent desc";
            i += da.DapperExecute(sql, new { jobDate = jobDate, noteText });
            return i;
        }

        /// <summary>
        /// 合併 欲新增修正資料
        /// </summary>
        /// <param name="logData"></param>
        /// <param name="dateCurrent"></param>
        /// <returns></returns>
        public int MergeByJobQuotaTmp(JobMetaDataModel logData, DateTime dateCurrent)
        {
            var maxDateCurrent = da.DapperQuery<DateTime?>("select max(datecurrent) from arr_offdayrecord_t").FirstOrDefault();

            // 20260605 差件只修改 起訖日 總額度 三個欄位
            var sql = @"
                MERGE INTO arr_offdayrecord_t AS tgt
                USING (
                    select 
                        jqt.empid, jqt.empname, jqt.datefrom, jqt.dateto,
                        bwm.wt_id, jqt.sap_uk, jqt.sapid, jqt.sapname, jqt.totalnumber, jqt.usednumber, jqt.restnumber, @datecurrent as datecurrent, 'SAP' as note
                    from job_quota_tmp jqt
                    left join base_worktype_m bwm on jqt.sapid = bwm.sapid and bwm.workgroup = 'leave' and bwm.isactive = 'Y'
                    where jqt.data_operation = 'U'
                ) AS src
                ON (tgt.datecurrent = src.datecurrent and tgt.sap_uk = src.sap_uk and tgt.empid = src.empid)
                WHEN MATCHED
                THEN UPDATE SET
                    datefrom=src.datefrom, dateto=src.dateto, totalnumber=src.totalnumber, note=src.note || to_char(@createtime, '_MMdd')
                    /*empid = src.empid, empname=src.empname, datefrom=src.datefrom, dateto=src.dateto, wt_id=src.wt_id, sapid=src.sapid, sapname=src.sapname,
                    totalnumber=src.totalnumber, usednumber=src.usednumber, restnumber=src.restnumber*/
                WHEN NOT MATCHED
                THEN INSERT (
                    serverip, createuser, createip, createtime, sap_uk, empid, empname, datefrom, dateto, wt_id, sapid, sapname, 
                    totalnumber, usednumber, restnumber, datecurrent, note
                ) VALUES (
                    @serverip, @createuser, @createip, @createtime, src.sap_uk, src.empid, src.empname, src.datefrom, src.dateto, src.wt_id, src.sapid, src.sapname,
                    src.totalnumber, src.usednumber, src.restnumber, src.datecurrent, src.note
                )";

            var param = new
            {
                serverip = logData.ServerIp,
                createuser = logData.CreateUser,
                createip = logData.CreateIp,
                createtime = logData.CreateTime,
                datecurrent = maxDateCurrent ?? dateCurrent
            };

            return da.DapperExecute(sql, param);
        }

        /// <summary>
        /// 合併 欲刪除資料
        /// </summary>
        /// <param name="dateCurrent"></param>
        /// <returns></returns>
        public int SetDeleteByJobQuotaTmp(DateTime dateCurrent)
        {
            var maxDateCurrent = da.DapperQuery<DateTime?>("select max(datecurrent) from arr_offdayrecord_t").FirstOrDefault();

            var sql = @"
                update arr_offdayrecord_t set totalnumber = 0, usednumber = 0, restnumber = 0
                where datecurrent = @dateCurrent 
                and Exists(
                    select 1
                    from ( select
                        jqt.empid, jqt.sap_uk
                    from job_quota_tmp jqt
                    left join base_worktype_m bwm on jqt.sapid = bwm.sapid and bwm.workgroup = 'leave' and bwm.isactive = 'Y'
                    where jqt.data_operation = 'D' ) a
                    where a.empid = arr_offdayrecord_t.empid and a.sap_uk = arr_offdayrecord_t.sap_uk
                )";

            var param = new
            {
                datecurrent = maxDateCurrent ?? dateCurrent
            };

            return da.DapperExecute(sql, param);
        }


    }
}
