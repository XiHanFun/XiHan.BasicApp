// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Access.Middleware;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 登录守卫（Access 对 <see cref="ILoginGuard"/> 的实现，4.x 的登录时机访问策略与登录失败自动封禁）。
/// 签发前：按要进入的一侧与账号此刻的角色，求平台层（只记认证后的那些，认证前的本次调用已在中间件里求过）与租户层，拦下即不签发（<c>ACCESS_DENIED_005</c>），命中的时机记为登录时。
/// 认证失败：按调用来源地址计数（参数 <see cref="LoginAutoBanSettings.ConfigKey"/>，缺省关；本次调用命中了平台层的放行策略不计，4.x 同）。
/// 判定或计数本身出错时放行并记警告，与访问策略中间件一致。
/// </summary>
/// <param name="policies">策略缓存。</param>
/// <param name="evaluator">策略求值。</param>
/// <param name="bans">封禁存储。</param>
/// <param name="options">授权包配置。</param>
/// <param name="clock">时间源。</param>
/// <param name="logger">日志。</param>
public sealed partial class LoginDefense(AccessPolicies policies, AccessPolicyEvaluator evaluator, LoginBans bans, AccessOptions options, TimeProvider clock, ILogger<LoginDefense> logger) : ILoginGuard
{
    /// <inheritdoc />
    public async ValueTask<Result> CheckAsync(LoginAttempt attempt, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        ArgumentNullException.ThrowIfNull(context);
        if (!options.AccessPolicyEnabled)
        {
            return Result.Success();
        }

        try
        {
            return await DecideAsync(attempt, context).ConfigureAwait(false)
                ? Error.Denied(AccessErrors.PolicyLoginDenied, AccessMessages.PolicyLoginDenied)
                : Result.Success();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogCheckFailed(logger, exception);
            return Result.Success();
        }
    }

    /// <inheritdoc />
    public async ValueTask RecordFailureAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Items.ContainsKey(AccessPolicyMiddleware.PlatformAllowedKey)
            || !context.GetOrigin().TryGetValue(out var origin)
            || string.IsNullOrWhiteSpace(origin.RemoteAddress))
        {
            return;
        }

        try
        {
            var settings = await SettingsAsync(context).ConfigureAwait(false);
            if (!settings.TryGetValue(out var current))
            {
                LogSettingsInvalid(logger, settings.Error!.Code);
                return;
            }

            if (current.IsEnabled)
            {
                await bans.RecordFailureAsync(origin.RemoteAddress, current, context.CancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogCountFailed(logger, exception);
        }
    }

    /// <summary>
    /// 自动封禁的参数：没装 Settings 或没建参数即缺省值（关）；JSON 写错或超出范围即 <c>ACCESS_VALIDATION_043</c>。
    /// </summary>
    public static async ValueTask<Result<LoginAutoBanSettings>> SettingsAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Services.GetService<IConfigValueReader>() is not { } reader)
        {
            return LoginAutoBanSettings.Default;
        }

        var configured = await reader.GetAsync(LoginAutoBanSettings.ConfigKey, context).ConfigureAwait(false);
        if (!configured.TryGetValue(out var value))
        {
            return configured.Error!;
        }

        if (!value.TryGetValue(out var json))
        {
            return LoginAutoBanSettings.Default;
        }

        LoginAutoBanSettings? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize(json, AccessJsonContext.Default.LoginAutoBanSettings);
        }
        catch (JsonException)
        {
            parsed = null;
        }

        if (parsed is not { IsValid: true })
        {
            return Error.Validation(AccessErrors.LoginAutoBanInvalid, AccessMessages.LoginAutoBanInvalid, MessageArguments.Of(("key", LoginAutoBanSettings.ConfigKey)));
        }

        return parsed;
    }

    // 拦下返回 true；命中全部记下（时机记为登录时）
    private async Task<bool> DecideAsync(LoginAttempt attempt, InvocationContext context)
    {
        var cancellationToken = context.CancellationToken;
        var now = clock.GetUtcNow();
        var request = AccessRequests.ForLogin(context, attempt, now);
        var source = ConditionAttributeSource.Create(request, context.Services, options);
        var platform = await policies.OfAsync(TenantIds.Platform, cancellationToken).ConfigureAwait(false);
        if (platform.Count > 0)
        {
            var layer = await evaluator.EvaluateAsync(platform, source, now, platform: true, cancellationToken).ConfigureAwait(false);
            Record(context, layer, request, TenantIds.Platform, static hit => hit.Phase != ConditionHitPhase.BeforeAuthentication);
            if (layer.BlockedBy is not null)
            {
                return true;
            }
        }

        if (string.Equals(attempt.TenantId, TenantIds.Platform, StringComparison.Ordinal))
        {
            return false;
        }

        var own = await policies.OfAsync(attempt.TenantId, cancellationToken).ConfigureAwait(false);
        if (own.Count == 0)
        {
            return false;
        }

        var tenantLayer = await evaluator.EvaluateAsync(own, source, now, platform: false, cancellationToken).ConfigureAwait(false);
        Record(context, tenantLayer, request, attempt.TenantId, static _ => true);
        return tenantLayer.BlockedBy is not null;
    }

    private static void Record(InvocationContext context, AccessPolicyLayer layer, ConditionRequest request, string tenantId, Func<AccessPolicyHit, bool> include)
    {
        foreach (var hit in layer.Hits.Where(include))
        {
            context.AddAuditLog(new ConditionHitLogEntry(ConditionHitSource.AccessPolicy, hit.Policy.Code)
            {
                TenantId = tenantId,
                PolicyId = hit.Policy.Id,
                PolicyName = hit.Policy.Name,
                Action = AccessPolicyMiddleware.ActionOf(hit.Policy.Action),
                Phase = ConditionHitPhase.Login,
                IsBlocked = layer.BlockedBy is not null && hit.Policy.Action == Domain.Entities.AccessPolicyAction.Deny,
                PermissionCodes = request.PermissionCodes,
            });
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Login-time access policy evaluation failed; the sign-in is let through.")]
    private static partial void LogCheckFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Login failure counting for auto-ban failed.")]
    private static partial void LogCountFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "The login auto-ban settings are invalid ({Code}); failures are not counted.")]
    private static partial void LogSettingsInvalid(ILogger logger, string code);
}
