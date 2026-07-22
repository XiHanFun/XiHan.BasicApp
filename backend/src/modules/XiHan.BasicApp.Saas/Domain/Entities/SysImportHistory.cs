// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 导入历史实体
/// 记录 Schema 页面批量导入的执行留痕（页面/文件/行数/成败/错误摘要），日志型只写不改。
/// </summary>
/// <remarks>
/// 职责边界：
/// - 仅作导入行为留痕，不承载导入数据本身；逐行创建经各资源自身端点完成（权限/校验在各端点落地）
/// - ErrorSummary 为前端上报的错误摘要（JSON，服务端做长度截断保护），不解释语义
///
/// 写入：
/// - 导入执行完毕（无论成败）追加一条；硬删（无软删除），可按保留策略清理
///
/// 查询：
/// - 导入对话框「最近导入」：IX_TeId_UsId_PaCo + WHERE UserId=? AND PageCode=? ORDER BY CreatedTime DESC LIMIT N
/// </remarks>
[SugarTable(TableName = "Sys_Import_History", TableDescription = "导入历史表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_UsId_PaCo", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(PageCode), OrderByType.Asc)]
public partial class SysImportHistory : BasicAppCreationEntity
{
    /// <summary>
    /// 用户ID（执行导入的用户）
    /// </summary>
    [SugarColumn(ColumnName = "User_Id", ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 页面码（Schema 页面唯一码，如 platform.config）
    /// </summary>
    [SugarColumn(ColumnName = "Page_Code", ColumnDescription = "页面码", Length = 100, IsNullable = false)]
    public virtual string PageCode { get; set; } = string.Empty;

    /// <summary>
    /// 资源码（对应后端资源，可空）
    /// </summary>
    [SugarColumn(ColumnName = "Resource_Code", ColumnDescription = "资源码", Length = 100, IsNullable = true)]
    public virtual string? ResourceCode { get; set; }

    /// <summary>
    /// 导入文件名（原始上传文件名）
    /// </summary>
    [SugarColumn(ColumnName = "File_Name", ColumnDescription = "导入文件名", Length = 256, IsNullable = false)]
    public virtual string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 数据行总数
    /// </summary>
    [SugarColumn(ColumnName = "Total_Count", ColumnDescription = "数据行总数", IsNullable = false)]
    public virtual int TotalCount { get; set; }

    /// <summary>
    /// 成功行数
    /// </summary>
    [SugarColumn(ColumnName = "Success_Count", ColumnDescription = "成功行数", IsNullable = false)]
    public virtual int SuccessCount { get; set; }

    /// <summary>
    /// 失败行数（校验失败 + 创建失败）
    /// </summary>
    [SugarColumn(ColumnName = "Fail_Count", ColumnDescription = "失败行数", IsNullable = false)]
    public virtual int FailCount { get; set; }

    /// <summary>
    /// 错误摘要（JSON，前端上报；服务端截断保护，后端不解释语义）
    /// </summary>
    [SugarColumn(ColumnName = "Error_Summary", ColumnDescription = "错误摘要(JSON)", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ErrorSummary { get; set; }
}
