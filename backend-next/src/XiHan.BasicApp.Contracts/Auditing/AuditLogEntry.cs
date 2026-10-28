// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Auditing;

/// <summary>
/// 操作在调用里登记的一条业务日志（登录、权限变更）。Auditing 在 Observe 阶段等调用结束、看到最终结果后才落库：
/// 登录日志无论成败都记，成败取调用结果；权限变更只在调用成功时记（失败的调用已回滚，什么也没变）。
/// </summary>
public abstract class AuditLogEntry
{
    /// <summary>
    /// 日志归属的租户；<see langword="null"/> 即当前调用的租户（没有租户即平台）。
    /// </summary>
    public string? TenantId { get; set; }
}

/// <summary>
/// 一条登录日志：身份事件与当时的账号。调用中途能确定多少就填多少，失败返回之前已经填上的照样记下。
/// </summary>
public sealed class LoginLogEntry : AuditLogEntry
{
    /// <summary>
    /// 创建。
    /// </summary>
    /// <param name="loginEvent">事件。</param>
    public LoginLogEntry(LoginEvent loginEvent)
    {
        Event = loginEvent;
    }

    /// <summary>
    /// 事件；处理过程中可以改判（密码通过但要第二步时，登录改记为 <see cref="LoginEvent.TwoFactorChallenge"/>）。
    /// </summary>
    public LoginEvent Event { get; set; }

    /// <summary>
    /// 账号所在的租户（登录时填的租户）；平台账号为 <see cref="TenantIds.Platform"/>。目录里不存在或停用的租户，日志落在平台一侧，这里留着原值供排查。
    /// </summary>
    public string? AccountTenantId { get; set; }

    /// <summary>
    /// 用户；还没认出账号时为 <see langword="null"/>。
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// 登录名（失败时是调用方填的原样）。
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 会话（登录成功即新会话，刷新与退出是当时的会话）。
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// 模仿事件的发起人。
    /// </summary>
    public Guid? ImpersonatorUserId { get; set; }

    /// <summary>
    /// 补充说明（如切换到的租户）。
    /// </summary>
    public string? Detail { get; set; }
}

/// <summary>
/// 一条权限变更日志：谁（当前主体）对哪个角色、用户或套餐做了什么授权变更。
/// </summary>
public sealed class PermissionChangeLogEntry : AuditLogEntry
{
    /// <summary>
    /// 创建。
    /// </summary>
    /// <param name="changeType">变更类型。</param>
    public PermissionChangeLogEntry(PermissionChangeType changeType)
    {
        ChangeType = changeType;
    }

    /// <summary>
    /// 变更类型。
    /// </summary>
    public PermissionChangeType ChangeType { get; }

    /// <summary>
    /// 角色。
    /// </summary>
    public Guid? RoleId { get; set; }

    /// <summary>
    /// 角色名称（写入时的快照）。
    /// </summary>
    public string? RoleName { get; set; }

    /// <summary>
    /// 目标用户（加入或移出角色的用户）。
    /// </summary>
    public Guid? TargetUserId { get; set; }

    /// <summary>
    /// 目标用户的登录名（写入时的快照）。
    /// </summary>
    public string? TargetUserName { get; set; }

    /// <summary>
    /// 套餐。
    /// </summary>
    public Guid? EditionId { get; set; }

    /// <summary>
    /// 套餐编码（写入时的快照）。
    /// </summary>
    public string? EditionCode { get; set; }

    /// <summary>
    /// 换套餐的租户。
    /// </summary>
    public string? TargetTenantId { get; set; }

    /// <summary>
    /// 涉及的权限码。
    /// </summary>
    public IReadOnlyList<string> PermissionCodes { get; set; } = [];

    /// <summary>
    /// 补充说明（如数据范围的新值）。
    /// </summary>
    public string? Detail { get; set; }
}
