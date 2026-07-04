using PIC.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateMasterData.Dao;
using UpdateMasterData.Model;

namespace UpdateMasterData.Service;

/// <summary>
/// 結算剩餘假 服務
/// </summary>
public class QuotaService : BaseService
{
    IDataAccess da;

    public QuotaService(IDataAccess da, JobMetaDataModel jobMetaData, IDataAccess logDa)
    {
        this.da = da;
        this.JobMetaData = jobMetaData;
        this.SystemJobEventLogDao = new PIC.Libary.Dao.SystemJobEventLogDao(logDa);
    }

    /// <summary>
    /// 匯入剩餘假
    /// </summary>
    /// <param name="dateCurrent"></param>
    /// <param name="isRebuild"></param>
    public void Import(DateTime dateCurrent, bool isRebuild, string jobDateText)
    {
        //test
        //businessDa.IsEnableLog = true;
        try
        {
            LogInfo("QuotaService.Import 開始");
            var arrOffDayRecordDao = new ArrOffDayRecordDao(da);
            var arrOffDayDao = new ArrOffDayDao(da);

            // 備份資料
            LogInfo("備份 剩餘假明細以及結算剩餘假明細資料");
            arrOffDayRecordDao.Backup();
            arrOffDayDao.Backup();

            if (isRebuild) //全件
            {
                LogInfo("重建 剩餘假明細資料");
                // 刪除 DateCurrent = 檔案結算日 + 1日
                int deleteCount = arrOffDayRecordDao.DeleteByDateCurrent(dateCurrent);
                deleteCount += arrOffDayDao.DeleteByDateCurrent(dateCurrent);

                // 新增 匯入資料
                arrOffDayRecordDao.InsertByJobQuotaTmp(JobMetaData, dateCurrent);

                // 新增 暫停額度 在可用其間資料且SAPUK不再這次全件檔中
                // @meno 兩邊需要都補不然 首次全件 只建REOCORD不會轉入OFFDAY(因SAPUK不再匯入檔中) 2.差件如果沒有Record紀錄會更新不到資料
                arrOffDayRecordDao.RebuildNotExistsWithSapUK(dateCurrent);
                arrOffDayDao.RebuildNotExistsWithSapUK(dateCurrent); 

            }
            else //插件
            {
                LogInfo("更新或新增 剩餘假明細資料");
                int modifyCount = arrOffDayRecordDao.MergeByJobQuotaTmp(JobMetaData, dateCurrent);

                LogInfo("設定清除 剩餘假明細資料");
                modifyCount += arrOffDayRecordDao.SetDeleteByJobQuotaTmp(dateCurrent);

            }

            // 結算假別資料
            LogInfo($"{jobDateText}_統計前刪除代休假統計資料");
            int delete00901 = arrOffDayDao.DeleteNeedRecal00901Data(dateCurrent);

            LogInfo($"{jobDateText}_統計結算剩餘假明細資料並更新");
            int summaryCount = arrOffDayDao.MergeByArrOffDayRecordT(JobMetaData, dateCurrent);

            LogInfo("QuotaService.Import 結束");
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 結算 代休假
    /// </summary>
    /// <param name="jobDate"></param>
    /*public void Cal00901ArrOffDayData(DateTime jobDate)
    {
        var arrOffDayDao = new ArrOffDayDao(da);
        var applyLeaverecordTDao = new ApplyLeaverecordTDao(da);
        var arrOffday901usedquotaDao = new ArrOffday901usedquotaDao(da);

        try {
            LogInfo("QuotaService.Cal00901ArrOffDayData 開始");
            arrOffDayDao.Delete00901Data(jobDate);

            LogInfo("查詢 代休假計算資料");
            var increaseList = arrOffDayDao.GetIncrease00901List(jobDate);
            LogInfo("查詢 代休假使用請假資料");
            var reduceList = applyLeaverecordTDao.Get00901LeaveList(jobDate);
            var useRecordList = new List<ArrOffday901usedquotaModel>();

            //計算 請代休假 使用代休到職範圍
            LogInfo("計算 請代休假 使用代休到職範圍");
            int countReduce = reduceList.Count();
            foreach (var item in reduceList)
            {
                LogInfo($"計算 Empid:{item.Empid} {reduceList.IndexOf(item) + 1}/{countReduce}");
                var useableList = increaseList.Where(x => x.Empid == item.Empid && item.Leavedate >= x.Datefrom && item.Leavedate <= x.Dateto)
                    .OrderBy(x => x.Datefrom).OrderBy(x => x.Dateto).ToList();

                int count = useableList.Count();
                int i = 0;
                decimal calValue = item.Leavehours;
                foreach (var useableItem in useableList)
                {
                    LogInfo($"分配使用時數 Empid:{item.Empid} LeaveDate:{item.Leavedate}, {useableList.IndexOf(useableItem)}/{count}");
                    i++;
                    var restValue = 0 + useableItem.Restnumber;
                    calValue = useableItem.Restnumber - calValue;
                    useableItem.Restnumber = calValue < 0 ? 0 : calValue; //使用後的時數(負數時歸零)

                    var useRecordData = new ArrOffday901usedquotaModel()
                    {
                        Leaveid = item.Id,
                        Usedatefrom = useableItem.Datefrom,
                        Usedateto = useableItem.Dateto,
                        Usehours = (double)(restValue - useableItem.Restnumber),
                        Isdelete = "N",
                        Createtime = DateTime.Now,
                        Updatetime = DateTime.Now
                    };
                    useRecordList.Add(useRecordData);

                    if (calValue < 0) //不夠扣除使用時數
                    {
                        //如果是最後一組 則使用到負數則不歸零
                        if (i == count) useableItem.Restnumber = calValue;
                        calValue = calValue * -1; //繼續使用下一組
                    }
                    else
                    {
                        break;
                    }
                }

                if(count == 0) //無對應代休資料
                {
                    string msg = $"請假無對應代休資料(id:{item.Id}, leavedate:{item.Leavedate.ToString("yyyy-MM-dd")}, empid:{item.Empid})";
                    LogInfo(msg);
                }
            }

            //紀錄 假別使用紀錄
            LogInfo("紀錄 代休假使用紀錄");
            arrOffday901usedquotaDao.Rebuild(useRecordList);

            //新增 代休結算資料
            LogInfo("新增 代休結算資料");
            increaseList.ForEach(item => {
                item.Serverip = JobMetaData.ServerIp;
                item.Createip = JobMetaData.CreateIp;
                item.Createuser = JobMetaData.CreateUser;
                item.Createtime = DateTime.Now;
                item.Note = "JOB";
            });
            arrOffDayDao.Insert00901List(increaseList);

            //補代休空資料
            arrOffDayDao.FillDefaultby00901(jobDate, JobMetaData);

            LogInfo("QuotaService.Cal00901ArrOffDayData 結束");
        }
        catch (Exception ex) 
        {
            throw;
        } 
    }*/





}
