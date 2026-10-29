// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 会话激活的角色（平台库实体，4.x 的 SysSessionRole）：会话 × 角色一行，随会话所在的一侧。令牌只带激活中的角色；
/// 激活前校验持有、激活时过动态职责分离（DSD）。切换租户、重新激活时旧行记停用，不再持有的记到期，留作查询。
/// </summary>
public sealed class SysSessionRole : AppRecord, IOwnedByTenant
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 会话 ID（访问令牌的 <c>sid</c>）。
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// 用户 ID。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 角色 ID。
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public SessionRoleStatus Status { get; set; } = SessionRoleStatus.Active;

    /// <summary>
    /// 激活时间。
    /// </summary>
    public DateTimeOffset ActivatedAt { get; set; }

    /// <summary>
    /// 停用或到期的时间。
    /// </summary>
    public DateTimeOffset? DeactivatedAt { get; set; }

    /// <summary>
    /// 原因：激活时为 <c>login</c>、<c>switch-tenant</c>、<c>impersonation</c>、<c>refresh</c> 或 <c>manual</c>；停用时为 <c>manual</c>、<c>switch-tenant</c>、<c>dsd:&lt;规则编码&gt;</c>，到期为 <c>not-held</c>。
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// 会话角色的状态（4.x 同）。
/// </summary>
public enum SessionRoleStatus
{
    /// <summary>
    /// 激活中。
    /// </summary>
    Active = 0,

    /// <summary>
    /// 已停用（本人换了激活集、切换租户或续签时按 DSD 停掉）。
    /// </summary>
    Inactive = 1,

    /// <summary>
    /// 已到期（不再持有这个角色）。
    /// </summary>
    Expired = 2,
}
