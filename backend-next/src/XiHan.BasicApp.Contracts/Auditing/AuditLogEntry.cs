// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Auditing;

/// <summary>
/// 操作在调用里登记的一条业务日志（登录、权限变更）。
/// </summary>
/// <remarks>
/// Auditing 在 Observe 阶段等调用结束、看到最终结果后才落库：
/// 登录日志无论成败都记，成败取调用结果；权限变更只在调用成功时记（失败的调用已回滚，什么也没变）。
/// </remarks>
public abstract class AuditLogEntry
{
    /// <summary>
    /// 日志归属的租户。
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> 即当前调用的租户（没有租户即平台）。
    /// </remarks>
    public string? TenantId { get; set; }
}

/// <summary>
/// 一条登录日志：身份事件与当时的账号。
/// </summary>
/// <remarks>
/// 调用中途能确定多少就填多少，失败返回之前已经填上的照样记下。
/// </remarks>
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
    /// 身份事件。
    /// </summary>
    /// <remarks>
    /// 处理过程中可以改判（密码通过但要第二步时，登录改记为 <see cref="LoginEvent.TwoFactorChallenge"/>）。
    /// </remarks>
    public LoginEvent Event { get; set; }

    /// <summary>
    /// 账号所在的租户（登录时填的租户）。
    /// </summary>
    /// <remarks>
    /// 平台账号为 <see cref="TenantIds.Platform"/>。目录里不存在或停用的租户，日志落在平台一侧，这里留着原值供排查。
    /// </remarks>
    public string? AccountTenantId { get; set; }

    /// <summary>
    /// 用户。
    /// </summary>
    /// <remarks>
    /// 还没认出账号时为 <see langword="null"/>。
    /// </remarks>
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
    /// 换套餐或转移所有权的租户。
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

/// <summary>
/// 一条条件命中：访问策略或权限条件在一次调用上的判定结果。
/// </summary>
/// <remarks>
/// 方案 5.12。由 Auditing 落进按月分表的条件命中日志，同一批落库里键相同、同一分钟的合并计数。
/// </remarks>
public sealed class ConditionHitLogEntry : AuditLogEntry
{
    /// <summary>
    /// 创建。
    /// </summary>
    /// <param name="source">命中来源。</param>
    /// <param name="policyCode">策略码（权限条件为权限码）。</param>
    public ConditionHitLogEntry(ConditionHitSource source, string policyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyCode);
        Source = source;
        PolicyCode = policyCode;
    }

    /// <summary>
    /// 命中来源。
    /// </summary>
    public ConditionHitSource Source { get; }

    /// <summary>
    /// 策略码（权限条件为权限码）。
    /// </summary>
    public string PolicyCode { get; }

    /// <summary>
    /// 策略（权限条件没有）。
    /// </summary>
    public Guid? PolicyId { get; set; }

    /// <summary>
    /// 策略名称（写入时的快照）。
    /// </summary>
    public string? PolicyName { get; set; }

    /// <summary>
    /// 命中的动作。
    /// </summary>
    public ConditionHitAction Action { get; set; }

    /// <summary>
    /// 判定的时机。
    /// </summary>
    public ConditionHitPhase Phase { get; set; }

    /// <summary>
    /// 是否拦下了这次调用。
    /// </summary>
    public bool IsBlocked { get; set; }

    /// <summary>
    /// 涉及的权限码。
    /// </summary>
    public IReadOnlyList<string> PermissionCodes { get; set; } = [];
}

/// <summary>
/// 一条开放接口日志：签名开放接口一次调用的接入方与签名结果。
/// </summary>
/// <remarks>
/// 方案 5.12，OpenPlatform 写入。成败、状态码、错误与耗时由 Auditing 取调用结果，
/// 无论成败都记（验签失败的调用本身就是失败）。请求与响应正文是解密后的明文摘要，由写入方截断。
/// </remarks>
public sealed class OpenApiLogEntry : AuditLogEntry
{
    /// <summary>
    /// 请求头里的 AppKey（原值，验签失败也记）。
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// 凭证名称（找到凭证时的快照）。
    /// </summary>
    public string? AppName { get; set; }

    /// <summary>
    /// 凭证归属人。
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// 凭证归属人的登录名。
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 实际用于验签的算法。
    /// </summary>
    public string? SignatureAlgorithm { get; set; }

    /// <summary>
    /// 签名是否有效。
    /// </summary>
    public bool SignatureValid { get; set; }

    /// <summary>
    /// 请求正文大小（字节，传输的原文）。
    /// </summary>
    public long RequestSize { get; set; }

    /// <summary>
    /// 响应正文大小（字节，传输的原文）。
    /// </summary>
    public long ResponseSize { get; set; }

    /// <summary>
    /// 请求正文摘要（明文，截断）。
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// 响应正文摘要（明文，截断）。
    /// </summary>
    public string? ResponseBody { get; set; }
}
