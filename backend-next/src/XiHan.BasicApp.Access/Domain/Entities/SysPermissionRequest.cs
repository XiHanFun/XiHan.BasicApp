// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 授权申请（租户库实体）。
/// </summary>
/// <remarks>
/// 照 4.x 的 <c>SysPermissionRequest</c>：用户为自己申请一个角色或一个权限码（也可以都申请），提交即发起审批业务 <c>permission-request</c> 的流程，
/// 审批通过即按期望的有效期指派或直授，驳回或中止即完结。平台一侧的申请租户为 <c>platform</c>。权限按码申请（6.0 没有权限实体），不加唯一约束，同一对象的待审批申请在服务里查重。
/// </remarks>
[SugarIndex("ix_{table}_tenant_id_created_at", nameof(TenantId), OrderByType.Asc, nameof(CreatedAt), OrderByType.Desc)]
[SugarIndex("ix_{table}_tenant_id_request_user_id", nameof(TenantId), OrderByType.Asc, nameof(RequestUserId), OrderByType.Asc)]
[SugarIndex("ix_{table}_tenant_id_status", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public sealed class SysPermissionRequest : TenantScopedAuditedEntity
{
    /// <summary>
    /// 申请人。
    /// </summary>
    public Guid RequestUserId { get; set; }

    /// <summary>
    /// 申请的角色（与权限码至少给一个）。
    /// </summary>
    public Guid? RoleId { get; set; }

    /// <summary>
    /// 申请的权限码（与角色至少给一个）。
    /// </summary>
    [StringLength(128)]
    public string? PermissionCode { get; set; }

    /// <summary>
    /// 申请原因。
    /// </summary>
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 期望生效时间（为空即审批通过立即生效）。
    /// </summary>
    public DateTimeOffset? ExpectedEffectiveAt { get; set; }

    /// <summary>
    /// 期望失效时间（为空即长期有效）。
    /// </summary>
    public DateTimeOffset? ExpectedExpiresAt { get; set; }

    /// <summary>
    /// 当前的审批流程实例（提交与重新提交时回填）。
    /// </summary>
    public Guid? WorkflowInstanceId { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public PermissionRequestStatus Status { get; set; } = PermissionRequestStatus.Pending;

    /// <summary>
    /// 备注（手工处理与流程中止时写原因）。
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}

/// <summary>
/// 授权申请的状态。
/// </summary>
/// <remarks>
/// 4.x 另有「已过期」，从来没有写入过（列表按期望失效时间算出标记），6.0 去掉这个值（设计稿 D6）。
/// </remarks>
public enum PermissionRequestStatus
{
    /// <summary>
    /// 待审批。
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 已批准（已自动授权）。
    /// </summary>
    Approved = 2,

    /// <summary>
    /// 已驳回。
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// 已撤回。
    /// </summary>
    Withdrawn = 4,
}
