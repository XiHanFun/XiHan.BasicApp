// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Application.AccessPolicies;
using XiHan.BasicApp.Saas.Application.Conditions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Web.Core.Access;

namespace XiHan.BasicApp.Saas.Infrastructure.Security;

/// <summary>
/// 访问策略认证后一段：接在框架的请求访问闸门上，认证、租户解析与路由之后、授权之前求值
/// </summary>
/// <remarks>
/// 匿名接口、Minimal API 与实时通道的协商请求同样经过。求值本身出错时记错误日志并放行。
/// </remarks>
public sealed class AccessPolicyRequestGate : IRequestAccessGate
{
    /// <summary>
    /// 判定顺序
    /// </summary>
    public const int GateOrder = 100;

    private readonly IOptionsMonitor<AccessPolicyOptions> _options;
    private readonly AccessPolicyAuthenticatedEvaluator _evaluator;
    private readonly ISaasConditionAttributeSourceFactory _sources;
    private readonly IAccessPolicyHitRecorder _hits;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AccessPolicyRequestGate> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AccessPolicyRequestGate(
        IOptionsMonitor<AccessPolicyOptions> options,
        AccessPolicyAuthenticatedEvaluator evaluator,
        ISaasConditionAttributeSourceFactory sources,
        IAccessPolicyHitRecorder hits,
        TimeProvider timeProvider,
        ILogger<AccessPolicyRequestGate> logger)
    {
        _options = options;
        _evaluator = evaluator;
        _sources = sources;
        _hits = hits;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public int Order => GateOrder;

    /// <inheritdoc />
    public async ValueTask<RequestAccessDecision> EvaluateAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!_options.CurrentValue.Enabled)
        {
            return RequestAccessDecision.Allow;
        }

        try
        {
            var now = _timeProvider.GetUtcNow();
            var sourceContext = _sources.CreateCurrentContext(now) with { HttpContext = context };
            var decision = await _evaluator.EvaluateAsync(sourceContext, AccessPolicyRequests.IsPlatformAllowed(context), cancellationToken);
            if (decision is null)
            {
                return RequestAccessDecision.Allow;
            }

            var request = AccessPolicyRequests.Describe(context, null, sourceContext.UserId, sourceContext.UserName, withClient: true);
            AccessPolicyRequests.Record(_hits, decision, AccessPolicyPhase.AfterAuthentication, now, request);
            return decision.IsBlocked
                ? RequestAccessDecision.Deny(StatusCodes.Status403Forbidden, AccessPolicyMiddleware.ForbiddenMessage)
                : RequestAccessDecision.Allow;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "访问策略求值失败，本次请求放行");
            return RequestAccessDecision.Allow;
        }
    }
}
