# UpdateMasterData

## 1. 專案概觀

`UpdateMasterData` 是一支 **.NET 8 主控台批次程式**（非常駐服務），屬於 **PIC / Cosmed HQ 出勤排班系統** 的主檔同步元件（`Service/EmployeeViceService.cs` 註解提到「ref: sp_UpdateEmployee_CosmedHQ」）。

它的職責是把上游 **SAP HR 系統** 產出的組織、員工、剩餘假、匯報關係等主檔資料，透過 SFTP 下載、解密、格式驗證、暫存，再比對合併寫入 **PostgreSQL** 正式資料表。

### 執行模式

- `Main(string[] args)`（`UpdateMasterData.cs`）啟動一次即結束，**沒有內建排程迴圈**（無 `Timer`/`Quartz`/`while(true)`）。
- 透過環境變數 `PIC_JOB_MODULE_CODE` 決定要執行哪一個 Job 模組，`switch` 內尚有 `PIC_JOB_MODULE_NAME`、`PIC_JOB_VERSION_TYPE`、`PIC_JOB_JOB_VERSION`、`PIC_JOB_DATABASE_TYPE`、`PIC_JOB_DATABASE_PREFIX`、`PIC_JOB_CREATOR`、`PIC_JOB_CREATOR_IP` 等環境變數會一併傳入 Job 建構子。
- 因此需要仰賴**外部排程器**（推測為 Kubernetes CronJob）針對每個 Job 模組各自排程、各自帶入對應環境變數觸發一次執行。
- Dapper 全域設定：`MatchNamesWithUnderscores = true`（底線轉駝峰命名對應）、`CommandTimeout = 600` 秒。

### 部署方式

`Dockerfile`：
- Build：`mcr.microsoft.com/dotnet/sdk:8.0`
- Runtime：`mcr.microsoft.com/dotnet/aspnet:8.0`（Linux）
- 時區設為 `Asia/Taipei`
- `ENTRYPOINT ["dotnet", "UpdateMasterData.dll"]`
- `EXPOSE 8888`（註解「修改為8888 Port, JOB 使用 8888 Port」）——但程式本身是純 console app、沒有任何 HTTP listener，此 EXPOSE 應是共用 Job image 範本留下的慣例設定，非本程式實際使用。

`UpdateMasterData.csproj` 主要相依：`SharpCompress 0.38.0`（解壓縮 tar）、`System.Data.DataSetExtensions`、`System.Management`，以及專案外部函式庫 `PIC.DB`、`PIC.Job`、`PIC.Libary`、`PIC.Tools`（位於 `..\..\..\..\lib\...`）與 `NLog.dll`。

`app.config` 僅剩一個 `.NET Framework 4.5` 的 `<startup>` 宣告，屬遺留檔案，實際 TargetFramework 是 `net8.0`，此檔案未被使用，無連線字串或 appSettings。

## 2. 共通資料流程（檔案匯入型 Job）

`Job_UpdateOrganization`、`Job_UpdateEmployeeDiff`、`Job_UpdateEmployeeVice`、`Job_UpdateQuota`、`Job_UpdateReporting` 共用以下流程（邏輯集中在 `Tool/CommonTool.cs`）：

1. **確認手動檔案**：`CheckManualFileExist` 檢查是否有人工放置的檔案（`ManualFilePath`/`ManualFileName`）。
2. **否則走 SFTP 下載**：`GetFTPFile` 透過 `SFTPClient`（`PIC.Tools`）連線，尋找符合命名規則的 `.TAR` 檔與對應 `.FILEOK` 標記檔後下載，並用 `UnzipFile`（`SharpCompress`）解壓為 `.TXT`。
3. **解碼/解密**：`DecodeFTPFile` 呼叫 `DecodeTool`（若設定 `IsDecode=N` 則單純複製檔案）。
4. **逐行格式驗證**：`CheckFile`/`CheckManualFile` 讀檔逐行呼叫 `CheckFile.cs` 驗證表頭、欄位長度/型別/固定值/允許值，並組出 `insert into {Table} (...) values (...)` SQL 字串。
5. **匯入暫存表**：`ImportFileTOTempDB` 清空目標暫存表後，以 200 筆為一批次交易方式執行 insert。
6. **業務邏輯合併**：呼叫對應 `Service` 類別做暫存表與正式表的比對合併（新增/修改/刪除），通常有安全門檻檢查（最大變動比例）後才真正寫入正式表。
7. **產生完成標記**：`SetOKFile` 寫出 `{filename}.OK`。
8. **備份原始檔**：`BackFile` 將處理過的檔案搬移到 `BakPath`（加上時間戳記；FTP 端刪除的程式碼已被註解停用）。

## 3. 六個 Job 模組

`Main()` 的 `switch (moduleName)` 目前可觸發下列六個 Job，`Job_UpdateQuotaQualify` 的 `case` 已被整段註解，**目前無法透過此程式觸發**（檔案仍存在於專案中）。

### Job_UpdateOrganization — 組織主檔轉入
- 讀取 `job_config_setting_dev1` 取得 SFTP／欄位設定；可透過 `system_config_m` 的 `UpdateOrganization.IsGetFromFTP` 決定是否要從 FTP 抓檔，或沿用既有暫存資料。
- 匯入暫存表 `Job_Organization_tmp`，呼叫 `OrganizationService.Import()`（以 `RetryTool` 包裝重試，重試次數/間隔由 `PIC_JOB_RETRY_COUNT`/`PIC_JOB_RETRY_DELAY_SEC` 控制）。
- `OrganizationService.Import()` 比對 `Base_Team_m` 與新匯入的 `Job_Organization_tmp`（排除 `Base_Team_exception_m`），計算新增/修改/刪除集合（遞迴排除 `TeamType` 為空之團隊的子節點），寫入 `Base_Team_tmp`，通過 `UpdateOrganization.CheckFlag`/`MaxDiffRate` 安全門檻後才備份（`Base_Team_bak`）並正式寫入 `Base_Team_m`。
- 文件內註明「翻寫來源:sp_UpdateOrganization」。

### Job_UpdateEmployee — 更新員工主檔（多數邏輯已停用）
- 目前主要邏輯已被大量註解，僅剩呼叫 SP `sp_UpdateWorkflow`（更新簽核流程資料）。
- 功能已大致被 `Job_UpdateEmployeeVice` 取代。

### Job_UpdateEmployeeDiff — 員工異動紀錄轉入
- 依文件註解：「僅轉入資料，邏輯移至 Job_UpdateEmployeeVice 處理」。
- 下載/解碼/驗證 SAP 員工異動檔並匯入暫存表 `Job_EmployeeDiff_tmp`，本 Job 本身不做後續合併。
- 支援 `PIC_JOB_SKIP_EMPTY_FILE` 環境變數容許空檔案。

### Job_UpdateEmployeeVice — 在職員工主檔轉入（核心員工同步）
- 匯入後組出 `JobMetaDataModel`，透過 `RetryTool` 依序呼叫：
  - `EmployeeViceService.UpdateEmployee()` — 主要員工主檔比對合併
  - `EmployeeViceService.UpdateNoCheckArrange()` — 更新「不檢查排班」旗標
  - `EmployeeViceService.RebuildWorktypeExtra()` — 呼叫資料庫函式 `fn_rebuild_worktype_extra` 重建轉調人員的額外可排班種
- `UpdateEmployee()` 主要流程：
  1. 依 `UpdateEmployee.TeamTypeWhiteList` 過濾暫存資料
  2. 讀取現有 `Base_Employee_m` 到記憶體
  3. 合併例外名單員工（`Base_Employee_exception_m`）至暫存與 `base_authority_m`
  4. 以 LINQ 比對計算新增/修改/刪除，對新進或新增 email 的員工用 `GeneratePWDTool.GeneratePWD` 產密碼，並以 `EncryptTool.EncryptSHA512` 雜湊
  5. 記錄身分/群組異動事件至 `job_identitychange_tmp`（代碼 1–6：到職、復職 HQ↔門市、身分轉換 HQ↔門市/PT↔FT 等）
  6. 離職員工標記 `Isactive='N'`
  7. 通過 `UpdateEmployee.CheckFlag`/`MaxDiffRate` 安全門檻後才正式寫入
  8. 若安全：配發新 `A_Id`、更新外勤/駐點類型旗標（`base_attendpersontype_m`，job code `00000107` 區顧問特例）、重寫 `Base_Employee_m` 並寫 `Base_Employee_m_Log`、更新 `base_arrangegroup` 預設排班分組、加派預設功能權限（`UpdateEmployee.DefaultFuncList`，預設 `"202,204,205,206,1001,1022"`）、寄送帳號啟用密碼通知信（受 `UpdateEmployee.IsMailPWD`/`MailPWDShiftDay` 與上線日 `fn_work_exception.official_online_date`（預設 `2025-04-21`）及提前開放白名單控制）、並將本次結果快照存入 `job_employeevice_t`。

### Job_UpdateQuota — 剩餘假主檔
- 支援 `PIC_JOB_LOOP_COUNT` 環境變數迴圈補跑多天。
- 每次迭代：匯入暫存表 → 開交易 → 依 `system_config_m` 的 `UpdateQuota.RebuildDate`（結算日）決定 `isRebuild`（全量重建 vs 增量修補）→ 呼叫 `QuotaService.Import(dateCurrent, isRebuild, jobVersion)` → 提交/回滾。
- 只有在最後一次迴圈才寫 `.OK` 檔並備份來源檔。

### Job_UpdateQuotaQualify — 更新剩餘假生效日期及額度（目前停用）
- 邏輯很單純：直接呼叫 SP `sp_UpdateQuotaQualify`，帶入 `ServerIP, CreateUser, CreatorIP, CreateTime, JobVersionDate`。
- 在 `UpdateMasterData.cs` 的 `switch` 中該 `case` **已被整段註解**，目前無法被觸發。

### Job_UpdateReporting — 匯報關係 JOB
- 匯入暫存表後執行 `UPDATE {Table} SET REPORTDATE = '{jobVersion}'`（字串組合 SQL，見第 6 節備註），再呼叫：
  - `ReportingService.Import(jobVersionDate)` — 由 `job_emp_reporting_tmp` 重建 `base_emp_reporting`
  - `ReportingService.RebuildBaseTeamOwner(jobVersionDate)` — 重建 `base_teamowner_m`（誰管理哪個團隊），並更新主管的預設權限/功能授權

## 4. 分層架構

### Dao 層（`Dao/`，共 26 個檔案）

依功能分組列出主要職責與資料表：

**組織/團隊**
- `BaseTeamDao.cs` — `Base_Team_m`/`_tmp`/`_bak`/`_exception_m` 的查詢、暫存、備份、正式寫入
- `BaseTeamOwnerDao.cs` — `base_teamowner_m`/`_tmp`/`_bak` 主管歸屬重建（含白名單 `base_teamowner_white_list`、黑名單 `base_teamowner_black_list`、特殊 job code `00000131` 規則）
- `BaseArrangeGroupDao.cs` — `base_arrangegroup` 排班分組維護（依 `group_type` 分類：區顧問、聯服、商場人員、後勤/外勤）
- `BaseAuthorityMDao.cs` — `base_authority_m`（職稱/授權主檔）
- `BaseAuthorityfunctionlistMDao.cs` — `base_authorityfunctionlist_m`（個人功能權限指派）

**員工主檔**
- `BaseEmployeeDao.cs` — 核心 `Base_Employee_m` 讀寫、`Base_Employee_m_Log` 稽核、`UpdateNoCheckArrange`
- `BaseEmployeeExceptionMDao.cs` — `Base_Employee_exception_m` 例外/白名單覆寫
- `BaseEmployeeCostMDao.cs` — `base_employee_cost_m` 成本中心清單
- `BaseEmployeeTransferDao.cs` — `base_employee_transfer_tmp`（職位/單位異動暫存）
- `BaseEmployeeTransferHistoryDao.cs` — `base_employee_transfer_history`（異動歷史）
- `BaseAttendPersonTypeMDao.cs` — `sethq.base_attendpersontype_m`（外勤/駐點類型旗標）
- `BaseWorktypeExtraMDao.cs` — 呼叫資料庫函式 `fn_rebuild_worktype_extra`
- `JobEmployeeDiffDao.cs` — `Job_EmployeeDiff_tmp` 讀取（過濾 SAP 轉調代碼 B8/C1/B5）
- `JobEmployeeViceDao.cs` — `job_employeevice_tmp` 暫存操作（白名單過濾、例外合併）
- `JobEmployeeviceTDao.cs` — `job_employeevice_t`（每次執行快照）
- `JobIdentityChangeTmpDao.cs` — `sethq.job_identitychange_tmp`（身分/群組異動事件記錄，代碼 1–6）
- `BaseWorkExceptionEarlyAccessDao.cs` — `base_work_exception_early_access`（提前開放通知的白名單）

**剩餘假/補休**
- `ArrOffDayDao.cs` — `arr_offday_t`（已結算剩餘假明細）備份、合併、含大量已停用的舊版 00901 補休試算邏輯
- `ArrOffDayRecordDao.cs` — `arr_offdayrecord_t`（原始匯入的假別餘額紀錄）
- `ArrOffday901usedquotaDao.cs` — `arr_offday_901usedquota`（wt_id `00901` 補休使用紀錄）
- `BaseEmpReportingDao.cs` — `base_emp_reporting`（匯報關係）由 `job_emp_reporting_tmp` 重建
- `JobEmpReportingDao.cs` — `job_emp_reporting_bak` 備份
- `ApplyLeaverecordTDao.cs` — 查詢未結算的代休假申請（`apply_leaverecord_t` 等）

**設定與其他**
- `JobConfigSettingDao.cs` — `job_config_setting_dev1`（各 Job 的 SFTP/檔案/欄位設定）
- `SystemConfigDao.cs` — `system_config_m`（系統層級 key/value 設定，含群組快取）
- `SystemJobexecLogDao.cs` — `system_jobexec_log` 執行歷史計數
- `MailTemplateMDao.cs` — `mail_template_m`（郵件範本）
- `MaiNormalMDao.cs` — `mail_normal_m`（郵件佇列批次寫入）

### Model 層（`Model/`）

- **EmployeeDiff**：`JobEmployeeDiffModel`（`Job_EmployeeDiff_tmp` 對應）、`JobIdentityChangeTmpModel`（含 6 種 ChangeType 說明）
- **EmployeeVice**：`BaseEmployeeModel`（核心員工實體）、`BaseAttendPersonTypeMModel`、`BaseAuthorityMModel`、`BaseEmployeeExceptionMModel`、`BaseEmployeeTransferTmpModel`、`JobEmployeeViceModel`（暫存匯入列）、`MailNormalMModel`
- **Organization**：`BaseEmployeeModel`（命名沿用但實際是組織/團隊模型）、`JobOrganizationModel`（暫存匯入列）
- **Quota**：`ArrOffday901usedquotaModel`、`Cal00901ArrOffdayT`（供已停用的 00901 試算邏輯使用）
- **Enum**：`MailTemplateType`（目前僅一值：`帳號啟用密碼通知 = 300`）
- 其他：`JobConfgSettingModel`（`settingKey`/`settingValue`，型別名稱有拼字誤植 "Confg"）、`JobMetaDataModel`（Job 執行期共用內容：`JobDate`、`EventGroup`、`ServerIp`、`CreateUser`、`DBType`、`DBPrefix` 等）、`MailTemplateMModel`

### Service 層（`Service/`）

- `BaseService.cs` — 所有 Service 的抽象基底，持有 `JobMetaDataModel` 與 `SystemJobEventLogDao`，提供 `LogInfo()` 同時寫 DB 與主控台
- `OrganizationService.cs` — 組織架構完整比對合併（見第 3 節）
- `EmployeeViceService.cs` — 最大最複雜的 Service，員工主檔同步核心邏輯（見第 3 節），另含私有輔助 `UpdateOutside()` 管理 `base_attendpersontype_m`
- `QuotaService.cs` — 剩餘假全量重建/增量修補、`00901` 補休彙總，含大段已停用的舊版試算方法
- `ReportingService.cs` — 匯報關係與主管歸屬重建
- `MailTemplateService.cs` — `BuildMailContent<T>()`：依 `MailTemplateType` 讀取範本，用反射做 `@{PropName}` 佔位符代換，組出待寄送的 `MailNormalMModel`

### Tool 層（`Tool/`）

- `CommonTool.cs` — 所有檔案匯入型 Job 共用邏輯（SFTP 下載、解壓、解碼、驗證、匯入暫存表、備份、產生 OK 檔），詳見第 2 節
- `DecodeTool.cs` — 自製替換式解密演算法：依密碼衍生的數字和來旋轉/切割替換字母表，逐字元替換解碼，輸出為帶 BOM 的 UTF-16
- `RetryTool.cs`（實體位於本專案 Tool 資料夾，命名空間為 `PIC.Tools`）— 通用重試機制，支援固定間隔或指數退避（`UseExponentialBackoff`），可注入 `LogFunc` 記錄重試過程
- `CheckFile.cs` — 依 `job_config_setting_dev1` 提供的欄位 Schema（`Col{n}.Name/.Len/.Type/.Rule/.Fix/.Allow`）驗證：
  - `ChkBodyResult` — 驗證資料列欄位數、固定值、長度、型別（`num`/`int`/`float`/`date`/自訂規則如 `en`、`PT,FT`、`I,U,D` 等），並組出 insert SQL
  - `ChkHeadEndResult` — 驗證表頭/表尾格式與筆數是否吻合
  - `ChkBodyAllowResult` — 手動匯入專用的允許值白名單過濾

## 5. 設定與外部依賴

### 環境變數（`PIC_JOB_*`）

| 變數 | 用途 |
|---|---|
| `PIC_JOB_MODULE_CODE` | 決定要執行哪個 Job（`switch` 分派依據） |
| `PIC_JOB_MODULE_NAME` | 模組名稱 |
| `PIC_JOB_VERSION_TYPE` | 作業版本類型 |
| `PIC_JOB_JOB_VERSION` | 作業版本年月日 |
| `PIC_JOB_DATABASE_TYPE` | 資料庫類型，交給 `PIC.DB.DBDataAccessFactory` |
| `PIC_JOB_DATABASE_PREFIX` | 資料庫前綴 |
| `PIC_JOB_CREATOR` / `PIC_JOB_CREATOR_IP` | 建立者資訊，寫入稽核欄位 |
| `PIC_JOB_SKIP_EMPTY_FILE` | 容許來源檔為空（部分 Job 使用） |
| `PIC_JOB_RETRY_COUNT` / `PIC_JOB_RETRY_DELAY_SEC` | `RetryTool` 重試次數與間隔 |
| `PIC_JOB_LOOP_COUNT` | `Job_UpdateQuota` 迴圈補跑天數 |

### DB 設定表

- **`job_config_setting_dev1`**（由 `JobConfigSettingDao` 讀取，以模組代碼為 key）：`SFTP_IP/PORT/ID/PWD/Folder`、`FTPFileName`、`LocalPath`、`SFTP_IsNeedDecrypt`、`SFTP_DecodeFileName/DecodeCode`、`IsDecode`、`ManualFilePath/FileName`、`BakPath`、`Table`、`Split`、`HeaderCount`、`IsChkFile`，以及逐欄位的 `Col{n}.Name/.Len/.Type/.Rule/.Fix/.Allow`。
  - ⚠️ 表名帶有 `_dev1` 後綴，建議確認正式環境是否共用同一張表。
- **`system_config_m`**（由 `SystemConfigDao` 讀取，`groupname`/`item`/`value`，含群組快取）：`UpdateOrganization.IsGetFromFTP`、`UpdateOrganization.CheckFlag`/`MaxDiffRate`/`TeamTypeWhiteList`、`UpdateEmployee.CheckFlag`/`MaxDiffRate`/`TeamTypeWhiteList`/`ManagerDefaultFuncList`/`DefaultFuncList`/`IsMailPWD`/`MailPWDShiftDay`、`UpdateQuota.RebuildDate`、`RecoverPwd.MinPwdLength`、`fn_work_exception.official_online_date`。

### 外部相依

- **SFTP**：SAP HR 檔案來源（每個 Job 各自的連線資訊存於 `job_config_setting_dev1`）
- **PostgreSQL**：透過專案外部函式庫 `PIC.DB.DBDataAccessFactory` 存取（依 SQL 語法如 `::date`、`MERGE INTO ... USING`、`TRUNCATE TABLE` 判斷為 PostgreSQL）
- **本機檔案系統**：暫存/手動匯入/備份路徑
- **郵件佇列 `mail_normal_m`**：本專案只負責寫入待寄郵件，實際寄送由本程式碼庫之外的其他程序處理

## 6. 已知技術債／可留意事項

- `Job_UpdateEmployee.cs` 大部分邏輯已被註解，僅剩呼叫 `sp_UpdateWorkflow`，功能已大致被 `Job_UpdateEmployeeVice` 取代，可考慮確認是否還需保留。
- `Job_UpdateQuotaQualify` 在 `UpdateMasterData.cs` 的 `switch` 中已整段註解、目前無法被觸發，但檔案與其呼叫的 SP `sp_UpdateQuotaQualify` 邏輯仍留在專案中。
- `Job_UpdateReporting.cs` 內有一段字串組合的 SQL（`UPDATE {Table} SET REPORTDATE = '{jobVersion}'`），雖然 `Table`/`jobVersion` 來源為 Job 內部控制、注入風險低，但非參數化寫法，建議留意。
- `job_config_setting_dev1` 資料表命名帶有 `_dev1` 字樣，正式環境若使用相同表名建議額外確認。
- `DecodeTool.cs` 是一套自製的替換式解密演算法，並非業界標準加密方式。
- `QuotaService.cs`／`ArrOffDayDao.cs` 內都留有大段已註解停用的舊版 `00901` 補休試算邏輯（`Cal00901ArrOffDayData`、`GetIncrease00901List` 等），目前未啟用，僅供日後參考。
- `UpdateMasterData.cs` 內還留有兩段已註解的一次性工具方法（`EncryptPWD`、`AddViceJobCode`），屬開發期暫時工具，非正式流程的一部分。
- `app.config` 為 .NET Framework 4.5 時期的遺留檔案，實際專案已是 `net8.0`，此檔未被使用。
