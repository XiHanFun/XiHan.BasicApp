// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.Framework.Authorization.Conditions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Web.Api.Constants;

namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 权限条件的请求期执行：快照里已授予的权限码，按当前请求求条件
/// </summary>
/// <remarks>
/// 请求作用域：同一请求里属性只取一次。
/// <list type="bullet">
/// <item>条件约束的是用户本人发起的请求。没有请求（后台续做，如导出执行）或查的不是当前用户时不求值，按有条件授予放行，以发起时那次请求的判定为准</item>
/// <item>还差数据行属性时，端点是 MVC 动作、权限码又登记了条件资源、补算还没发生，就记成待定，模型绑定之后由 <see cref="PermissionConditionRowFilter"/> 补算；否则（含动作里再查权限）按不满足处理</item>
/// <item><see cref="PermissionConditionMode.LogOnly"/> 下不满足只记录、照常放行；<see cref="PermissionConditionMode.Disabled"/> 下不求值</item>
/// </list>
/// </remarks>
public sealed class PermissionConditionEnforcer
{
    private readonly PermissionConditionDecider _decider;
    private readonly ISaasConditionAttributeSourceFactory _sourceFactory;
    private readonly IPermissionResourceRegistry _resources;
    private readonly IOptionsMonitor<SaasPermissionConditionOptions> _options;
    private readonly IPermissionConditionHitRecorder _hitRecorder;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _currentTenant;

    private HttpContext? _sourceContext;
    private IConditionAttributeSource? _requestSource;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionConditionEnforcer(
        PermissionConditionDecider decider,
        ISaasConditionAttributeSourceFactory sourceFactory,
        IPermissionResourceRegistry resources,
        IOptionsMonitor<SaasPermissionConditionOptions> options,
        IPermissionConditionHitRecorder hitRecorder,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant)
    {
        _decider = decider;
        _sourceFactory = sourceFactory;
        _resources = resources;
        _options = options;
        _hitRecorder = hitRecorder;
        _currentUser = currentUser;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 快照里已授予的权限码在这次请求下是否放行
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="snapshot">该用户的授权快照</param>
    /// <param name="permissionCode">权限码</param>
    /// <param name="httpContext">当前请求；没有请求为空</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否放行</returns>
    public async Task<bool> AllowAsync(long userId, AuthorizationSnapshot snapshot, string permissionCode, HttpContext? httpContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        var code = permissionCode.Trim();
        if (!snapshot.Conditions.Requires(code))
        {
            return true;
        }

        var mode = _options.CurrentValue.Mode;
        if (mode == PermissionConditionMode.Disabled)
        {
            return PermissionConditionDecider.DecideIgnoringConditions(snapshot.Conditions, code) == PermissionConditionOutcome.Granted;
        }

        if (httpContext is null || _currentUser.UserId != userId)
        {
            return true;
        }

        _resources.TryGet(code, out var registration);
        var requestSource = GetRequestSource(httpContext);
        var outcome = await _decider.DecideAsync(
            snapshot.Conditions,
            code,
            PermissionRowAttributeSource.BeforeBinding(requestSource, registration),
            cancellationToken);
        if (outcome == PermissionConditionOutcome.Granted)
        {
            return true;
        }

        var tenantId = _currentTenant.Id ?? 0;
        if (outcome == PermissionConditionOutcome.Pending && registration is not null && IsMvcAction(httpContext) && PendingPermissionConditions.CanDefer(httpContext))
        {
            PendingPermissionConditions.Add(httpContext, new PendingPermissionCondition(code, userId, tenantId, snapshot.Conditions, requestSource, mode));
            return true;
        }

        await _hitRecorder.RecordAsync(CreateHit(userId, tenantId, code, mode, httpContext), cancellationToken);
        return mode == PermissionConditionMode.LogOnly;
    }

    /// <summary>
    /// 拒绝记录
    /// </summary>
    public static PermissionConditionHit CreateHit(long userId, long tenantId, string permissionCode, PermissionConditionMode mode, HttpContext httpContext)
    {
        return new PermissionConditionHit(
            userId,
            tenantId,
            permissionCode,
            mode,
            httpContext.Request.Path.Value,
            httpContext.Items[XiHanWebApiConstants.TraceIdItemKey]?.ToString() ?? httpContext.TraceIdentifier);
    }

    private static bool IsMvcAction(HttpContext httpContext)
    {
        return httpContext.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>() is not null;
    }

    private IConditionAttributeSource GetRequestSource(HttpContext httpContext)
    {
        if (_requestSource is null || !ReferenceEquals(_sourceContext, httpContext))
        {
            _requestSource = _sourceFactory.Create(_sourceFactory.CreateCurrentContext(DateTimeOffset.UtcNow) with { HttpContext = httpContext });
            _sourceContext = httpContext;
        }

        return _requestSource;
    }
}

/// <summary>
/// 鉴权时还差数据行属性、等模型绑定之后补算的一项
/// </summary>
public sealed record PendingPermissionCondition(
    string PermissionCode,
    long UserId,
    long TenantId,
    AuthorizationConditions Conditions,
    IConditionAttributeSource RequestSource,
    PermissionConditionMode Mode);

/// <summary>
/// 待补算项存在请求的 <c>Items</c> 里
/// </summary>
public static class PendingPermissionConditions
{
    private const string ItemKey = "__XiHanBasicApp.PendingPermissionConditions";
    private const string ResolvedKey = "__XiHanBasicApp.PendingPermissionConditions.Resolved";

    /// <summary>
    /// 记一项
    /// </summary>
    public static void Add(HttpContext httpContext, PendingPermissionCondition pending)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        if (httpContext.Items[ItemKey] is not List<PendingPermissionCondition> list)
        {
            httpContext.Items[ItemKey] = list = [];
        }

        list.Add(pending);
    }

    /// <summary>
    /// 这次请求还能不能记待定：补算一旦发生（进了动作），之后的检查等不到补算
    /// </summary>
    public static bool CanDefer(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return httpContext.Items[ResolvedKey] is not true;
    }

    /// <summary>
    /// 取出全部待补算项并清空，此后这次请求不再接受待定
    /// </summary>
    public static IReadOnlyList<PendingPermissionCondition> Take(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        httpContext.Items[ResolvedKey] = true;
        if (httpContext.Items[ItemKey] is not List<PendingPermissionCondition> list)
        {
            return [];
        }

        httpContext.Items.Remove(ItemKey);
        return list;
    }
}
