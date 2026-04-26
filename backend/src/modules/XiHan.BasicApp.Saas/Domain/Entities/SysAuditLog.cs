#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAuditLog
// Guid:c528152c-d6e9-4396-addb-b479254bad62
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/08 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统审计日志实体
/// 数据库实体变更审计：自动记录实体的增删改操作及字段级变更快照，用于数据溯源和合规审计
/// </summary>
/// <remarks>
/// 职责边界：
/// - 本表仅关注"数据库实体变更"（谁在什么时候改了哪张表哪条记录的哪些字段）
/// - 与 SysAccessLog（HTTP 访问日志）、SysApiLog（API 调用日志）职责分离，通过 TraceId 串联
/// - 与 SysReviewLog（业务审批动作日志）职责分离：本表记录数据变更事实，SysReviewLog 记录审批决策过程
///
/// 写入触发：
/// - 由 ORM 拦截器自动捕获实体 Insert/Update/Delete 操作
/// - BeforeData/AfterData 记录变更前后的完整 JSON 快照
/// - ChangedFields 记录本次变更涉及的字段列表
/// - 写入前应对敏感字段（密码/Token/身份证等）做脱敏处理
///
/// 分表策略：
/// - 按月分表，查询必带时间范围
///
/// 查询：
/// - 实体变更史：IX_EnTy + IX_EnId + WHERE EntityType=? AND EntityId=?
/// - 用户操作轨迹：IX_UsId + 时间范围
/// - 租户审计报告：IX_TeId_AuTi
///
/// 删除：
/// - 合规要求下禁止删除；仅允许按保留期归档（通常 ≥ 6 个月）
/// </remarks>
[SugarTable("SysAuditLog_{year}{month}{day}", "系统审计日志表"), SplitTable(SplitType.Month)]
[SugarIndex("IX_{split_table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{split_table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_UsId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_AuTy", nameof(AuditType), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_EnTy", nameof(EntityType), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_OpTy", nameof(OperationType), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_TeId_AuTi", nameof(TenantId), OrderByType.Asc, nameof(AuditTime), OrderByType.Desc)]
[SugarIndex("IX_{split_table}_RiLe", nameof(RiskLevel), OrderByType.Desc)]
[SugarIndex("IX_{split_table}_EnId", nameof(EntityId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_TrId", nameof(TraceId), OrderByType.Asc)]
public partial class SysAuditLog : BasicAppCreationEntity, ISplitTableEntity, ITraceableEntity
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = true)]
    public virtual long? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [SugarColumn(ColumnDescription = "用户名", Length = 50, IsNullable = true)]
    public virtual string? UserName { get; set; }

    /// <summary>
    /// 会话ID（关联 SysUserSession，用于串联同一会话内的操作）
    /// </summary>
    [SugarColumn(ColumnDescription = "会话ID", Length = 100, IsNullable = true)]
    public virtual string? SessionId { get; set; }

    /// <summary>
    /// 请求ID（关联 SysApiLog，用于定位触发本次变更的 API 请求）
    /// </summary>
    [SugarColumn(ColumnDescription = "请求ID", Length = 100, IsNullable = true)]
    public virtual string? RequestId { get; set; }

    /// <summary>
    /// 链路追踪ID（串联 SysAccessLog/SysApiLog/SysAuditLog 的完整请求生命周期）
    /// </summary>
    [SugarColumn(ColumnDescription = "链路追踪ID", Length = 64, IsNullable = true)]
    public virtual string? TraceId { get; set; }

    /// <summary>
    /// 审计类型
    /// </summary>
    [SugarColumn(ColumnDescription = "审计类型", Length = 50, IsNullable = false)]
    public virtual string AuditType { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型
    /// </summary>
    [SugarColumn(ColumnDescription = "操作类型")]
    public virtual OperationType OperationType { get; set; }

    /// <summary>
    /// 实体类型
    /// </summary>
    [SugarColumn(ColumnDescription = "实体类型", Length = 100, IsNullable = true)]
    public virtual string? EntityType { get; set; }

    /// <summary>
    /// 实体ID
    /// </summary>
    [SugarColumn(ColumnDescription = "实体ID", Length = 100, IsNullable = true)]
    public virtual string? EntityId { get; set; }

    /// <summary>
    /// 实体名称
    /// </summary>
    [SugarColumn(ColumnDescription = "实体名称", Length = 200, IsNullable = true)]
    public virtual string? EntityName { get; set; }

    /// <summary>
    /// 表名称
    /// </summary>
    [SugarColumn(ColumnDescription = "表名称", Length = 100, IsNullable = true)]
    public virtual string? TableName { get; set; }

    /// <summary>
    /// 主键字段
    /// </summary>
    [SugarColumn(ColumnDescription = "主键字段", Length = 50, IsNullable = true)]
    public virtual string? PrimaryKey { get; set; }

    /// <summary>
    /// 主键值
    /// </summary>
    [SugarColumn(ColumnDescription = "主键值", Length = 100, IsNullable = true)]
    public virtual string? PrimaryKeyValue { get; set; }

    /// <summary>
    /// 操作描述
    /// </summary>
    [SugarColumn(ColumnDescription = "操作描述", Length = 500, IsNullable = true)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// 变更前数据（JSON格式，实体变更前的完整快照或差异字段旧值）
    /// </summary>
    [SugarColumn(ColumnDescription = "变更前数据", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? BeforeData { get; set; }

    /// <summary>
    /// 变更后数据（JSON格式，实体变更后的完整快照或差异字段新值）
    /// </summary>
    [SugarColumn(ColumnDescription = "变更后数据", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? AfterData { get; set; }

    /// <summary>
    /// 变更字段列表（JSON数组，记录本次变更涉及的字段名）
    /// </summary>
    [SugarColumn(ColumnDescription = "变更字段", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ChangedFields { get; set; }

    /// <summary>
    /// 变更摘要（人类可读的变更描述，如"将状态从启用改为停用"）
    /// </summary>
    [SugarColumn(ColumnDescription = "变更摘要", Length = 1000, IsNullable = true)]
    public virtual string? ChangeDescription { get; set; }

    /// <summary>
    /// 执行耗时（毫秒）
    /// </summary>
    [SugarColumn(ColumnDescription = "执行耗时（毫秒）")]
    public virtual long ExecutionTime { get; set; } = 0;

    /// <summary>
    /// 操作IP
    /// </summary>
    [SugarColumn(ColumnDescription = "操作IP", Length = 50, IsNullable = true)]
    public virtual string? OperationIp { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    [SugarColumn(ColumnDescription = "是否成功")]
    public virtual bool IsSuccess { get; set; } = true;

    /// <summary>
    /// 异常信息
    /// </summary>
    [SugarColumn(ColumnDescription = "异常信息", Length = 2000, IsNullable = true)]
    public virtual string? ExceptionMessage { get; set; }

    /// <summary>
    /// 异常堆栈
    /// </summary>
    [SugarColumn(ColumnDescription = "异常堆栈", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ExceptionStackTrace { get; set; }

    /// <summary>
    /// 风险等级
    /// </summary>
    [SugarColumn(ColumnDescription = "风险等级")]
    public virtual AuditRiskLevel RiskLevel { get; set; } = AuditRiskLevel.Low;

    /// <summary>
    /// 审计时间
    /// </summary>
    [SugarColumn(ColumnDescription = "审计时间")]
    public virtual DateTimeOffset AuditTime { get; set; }

    /// <summary>
    /// 扩展数据（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "扩展数据", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ExtendData { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false, ColumnDescription = "创建时间")]
    [SplitField]
    public override DateTimeOffset CreatedTime { get; set; }
}
