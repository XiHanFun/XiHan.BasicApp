// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 授权申请（租户库实体）
/// </summary>
/// <remarks>
/// 用户为自己申请一个角色或一个权限码（二选一），提交即在同一个事务里发起审批业务 <c>permission-request</c> 的流程：发起失败即整体回滚，不会留下没有流程的待审批申请
/// 审批通过即按期望的有效期指派或直授，经的是与管理员指派、直授同一套服务（同侧、约束规则、权限变更日志）：本表只记申请、进度与结论，授权本身不另走一条路
/// 落租户库（<c>TenantScoped*</c>），平台一侧的申请租户为 <c>platform</c>：申请与它的审批流程都属于提出它的那一侧
/// 唯一性只针对待审批的申请（同一申请人同一对象一条），随状态变化，所以不加唯一约束，在服务里查重；权限按码申请，没有权限实体
/// 约束规则要求审批时也替用户提交一条，原因写明触犯的规则、流程变量带规则编码，审批定义可以据此把例外转给专门的审批人
/// 结论由审批业务处理器回写，通过时重核对象仍可用、已批准时幂等；撤回与驳回都同时撤回进行中的流程。Workflow 模块关掉时列表照常、提交报审批未启用
/// 没有删除：撤回、驳回即完结，记录保留
/// </remarks>
[SugarTable(TableName = "sys_permission_request", TableDescription = "授权申请（租户库实体）")]
[SugarIndex("ix_sys_permission_request_tenant_id_created_at", nameof(TenantId), OrderByType.Asc, nameof(CreatedAt), OrderByType.Desc)]
[SugarIndex("ix_sys_permission_request_tenant_id_request_user_id", nameof(TenantId), OrderByType.Asc, nameof(RequestUserId), OrderByType.Asc)]
[SugarIndex("ix_sys_permission_request_tenant_id_status", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public sealed class SysPermissionRequest : TenantScopedAuditedEntity
{
    /// <summary>
    /// 申请人
    /// </summary>
    [SugarColumn(ColumnName = "request_user_id", IsNullable = false, ColumnDescription = "申请人")]
    public Guid RequestUserId { get; set; }

    /// <summary>
    /// 申请的角色（与权限码至少给一个）
    /// </summary>
    [SugarColumn(ColumnName = "role_id", IsNullable = true, ColumnDescription = "申请的角色（与权限码至少给一个）")]
    public Guid? RoleId { get; set; }

    /// <summary>
    /// 申请的权限码（与角色至少给一个）
    /// </summary>
    [SugarColumn(ColumnName = "permission_code", Length = 128, IsNullable = true, ColumnDescription = "申请的权限码（与角色至少给一个）")]
    public string? PermissionCode { get; set; }

    /// <summary>
    /// 申请原因
    /// </summary>
    [SugarColumn(ColumnName = "reason", Length = 1000, IsNullable = false, ColumnDescription = "申请原因")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 期望生效时间（为空即审批通过立即生效）
    /// </summary>
    [SugarColumn(ColumnName = "expected_effective_at", IsNullable = true, ColumnDescription = "期望生效时间（为空即审批通过立即生效）")]
    public DateTimeOffset? ExpectedEffectiveAt { get; set; }

    /// <summary>
    /// 期望失效时间（为空即长期有效）
    /// </summary>
    [SugarColumn(ColumnName = "expected_expires_at", IsNullable = true, ColumnDescription = "期望失效时间（为空即长期有效）")]
    public DateTimeOffset? ExpectedExpiresAt { get; set; }

    /// <summary>
    /// 当前的审批流程实例（提交与重新提交时回填）
    /// </summary>
    [SugarColumn(ColumnName = "workflow_instance_id", IsNullable = true, ColumnDescription = "当前的审批流程实例（提交与重新提交时回填）")]
    public Guid? WorkflowInstanceId { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public PermissionRequestStatus Status { get; set; } = PermissionRequestStatus.Pending;

    /// <summary>
    /// 备注（手工处理与流程中止时写原因）
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "备注（手工处理与流程中止时写原因）")]
    public string? Remark { get; set; }
}

/// <summary>
/// 授权申请的状态
/// </summary>
/// <remarks>
/// 没有「已过期」：列表按期望失效时间算出标记（设计稿 D6）
/// </remarks>
public enum PermissionRequestStatus
{
    /// <summary>
    /// 待审批
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 已批准（已自动授权）
    /// </summary>
    Approved = 2,

    /// <summary>
    /// 已驳回
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// 已撤回
    /// </summary>
    Withdrawn = 4,
}
