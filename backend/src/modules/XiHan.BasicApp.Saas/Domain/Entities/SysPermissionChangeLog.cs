#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermissionChangeLog
// Guid:c3d4e5f6-a7b8-9012-def0-345678901234
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 权限变更日志实体
/// 结构化记录"谁在什么时候给谁授予/撤销了什么权限"，是 RBAC 合规审计的核心证据
/// </summary>
/// <remarks>
/// 职责边界：
/// - 与 SysAuditLog（数据库实体变更审计）职责分离：SysAuditLog 记录通用实体字段变更，本表专注权限授予/撤销的业务语义
/// - 与 SysOperationLog（用户操作日志）职责分离：本表只记录权限变更事实，不记录操作上下文
///
/// 分表策略：
/// - 按月分表；查询必带时间范围
///
/// 写入：
/// - 由权限变更服务在授予/撤销角色、权限时同步写入
/// - 只追加，禁止更新和删除
///
/// 查询：
/// - 权限回溯：IX_TaUsId + WHERE TargetUserId=? ORDER BY ChangeTime DESC
/// - 操作人审计：IX_OpUsId
/// - 按变更类型统计：IX_ChTy
/// </remarks>
[SugarTable("SysPermissionChangeLog_{year}{month}{day}", "权限变更日志表"), SplitTable(SplitType.Month)]
[SugarIndex("IX_{split_table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{split_table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_OpUsId", nameof(OperatorUserId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_TaUsId", nameof(TargetUserId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_TaRoId", nameof(TargetRoleId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_PeId", nameof(PermissionId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_ChTy", nameof(ChangeType), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_TeId_ChTi", nameof(TenantId), OrderByType.Asc, nameof(ChangeTime), OrderByType.Desc)]
[SugarIndex("IX_{split_table}_TrId", nameof(TraceId), OrderByType.Asc)]
public partial class SysPermissionChangeLog : BasicAppCreationEntity, ISplitTableEntity, ITraceableEntity
{
    /// <summary>
    /// 操作人ID（执行授予/撤销动作的用户）
    /// </summary>
    [SugarColumn(ColumnDescription = "操作人ID", IsNullable = true)]
    public virtual long? OperatorUserId { get; set; }

    /// <summary>
    /// 目标用户ID（被授予/撤销权限的用户，用户级变更时填写）
    /// </summary>
    [SugarColumn(ColumnDescription = "目标用户ID", IsNullable = true)]
    public virtual long? TargetUserId { get; set; }

    /// <summary>
    /// 目标角色ID（被授予/撤销权限的角色，角色级变更时填写）
    /// </summary>
    [SugarColumn(ColumnDescription = "目标角色ID", IsNullable = true)]
    public virtual long? TargetRoleId { get; set; }

    /// <summary>
    /// 权限ID（被授予/撤销的权限，权限级变更时填写）
    /// </summary>
    [SugarColumn(ColumnDescription = "权限ID", IsNullable = true)]
    public virtual long? PermissionId { get; set; }

    /// <summary>
    /// 变更类型
    /// </summary>
    [SugarColumn(ColumnDescription = "变更类型")]
    public virtual PermissionChangeType ChangeType { get; set; }

    /// <summary>
    /// 变更原因（关联审批单号、工单号等）
    /// </summary>
    [SugarColumn(ColumnDescription = "变更原因", Length = 500, IsNullable = true)]
    public virtual string? ChangeReason { get; set; }

    /// <summary>
    /// 变更描述（人类可读摘要）
    /// </summary>
    [SugarColumn(ColumnDescription = "变更描述", Length = 500, IsNullable = true)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// 操作IP
    /// </summary>
    [SugarColumn(ColumnDescription = "操作IP", Length = 50, IsNullable = true)]
    public virtual string? OperationIp { get; set; }

    /// <summary>
    /// 链路追踪ID
    /// </summary>
    [SugarColumn(ColumnDescription = "链路追踪ID", Length = 64, IsNullable = true)]
    public virtual string? TraceId { get; set; }

    /// <summary>
    /// 变更时间
    /// </summary>
    [SugarColumn(ColumnDescription = "变更时间")]
    public virtual DateTimeOffset ChangeTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false, ColumnDescription = "创建时间")]
    [SplitField]
    public override DateTimeOffset CreatedTime { get; set; }
}
