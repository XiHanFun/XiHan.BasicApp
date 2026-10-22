// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Application.Conditions;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authorization.Conditions;
using XiHan.Framework.Web.Core.Clients;
using ConditionOperator = XiHan.BasicApp.Saas.Domain.Entities.ConditionOperator;

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 访问策略的试运行与防锁死检查
/// </summary>
/// <remarks>
/// 试运行只求这一条策略，不看其他策略，也不看生效期；不填的上下文取当前请求与当前用户。
/// 防锁死检查用当前请求本身求一条启用中的拒绝策略，命中就不让保存。
/// </remarks>
public sealed class AccessPolicyTrialService
{
    private readonly AccessPolicyConditionCompiler _compiler;
    private readonly IConditionEvaluator _evaluator;
    private readonly ISaasConditionAttributeSourceFactory _sources;
    private readonly IAccessPolicyRepository _policyRepository;
    private readonly IAccessPolicyConditionRepository _conditionRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IIpRegionResolver _regions;
    private readonly TimeProvider _timeProvider;
    private readonly string _timeZone;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AccessPolicyTrialService(
        AccessPolicyConditionCompiler compiler,
        IConditionEvaluator evaluator,
        ISaasConditionAttributeSourceFactory sources,
        IAccessPolicyRepository policyRepository,
        IAccessPolicyConditionRepository conditionRepository,
        IHttpContextAccessor httpContextAccessor,
        IIpRegionResolver regions,
        TimeProvider timeProvider,
        IOptions<SaasConditionOptions> options)
    {
        _compiler = compiler;
        _evaluator = evaluator;
        _sources = sources;
        _policyRepository = policyRepository;
        _conditionRepository = conditionRepository;
        _httpContextAccessor = httpContextAccessor;
        _regions = regions;
        _timeProvider = timeProvider;
        _timeZone = options.Value.TimeZone;
    }

    /// <summary>
    /// 试运行一条策略
    /// </summary>
    /// <exception cref="InvalidOperationException">策略不存在、没给条件，或 IP 写错</exception>
    public async Task<AccessPolicyTrialResultDto> TrialAsync(AccessPolicyTrialInputDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        CompiledConditionSet compiled;
        AccessPolicyAction action;
        if (input.PolicyId is > 0)
        {
            var policy = await _policyRepository.GetByIdAsync(input.PolicyId.Value, cancellationToken)
                ?? throw new InvalidOperationException("访问策略不存在。");
            compiled = _compiler.Compile(await _conditionRepository.GetByPolicyIdAsync(policy.BasicId, cancellationToken));
            action = input.Action ?? policy.Action;
        }
        else
        {
            if (input.Conditions is not { Count: > 0 })
            {
                throw new InvalidOperationException("请给出要试运行的策略或条件。");
            }

            compiled = _compiler.Compile([.. input.Conditions.Select(ToCommand)]);
            action = input.Action ?? AccessPolicyAction.LogOnly;
        }

        var context = input.Context ?? new AccessPolicyTrialContextDto();
        var ip = ResolveIp(context.Ip);
        var now = context.Time ?? _timeProvider.GetUtcNow();
        var current = _sources.CreateCurrentContext(now);
        var userId = context.UserId is > 0 ? context.UserId : current.UserId;
        var httpContext = BuildRequest(context, ip);
        var result = new AccessPolicyTrialResultDto
        {
            Action = action,
            Phase = AccessPolicyPhases.IsBeforeAuthentication(compiled.ReferencedAttributes) ? AccessPolicyPhase.BeforeAuthentication : AccessPolicyPhase.AfterAuthentication,
            Errors = [.. compiled.Errors.Select(error => $"{error.AttributeName}：{error.Message}")],
            Ip = AccessPolicyRequests.FormatIp(ip),
            Location = ip is null ? null : _regions.Resolve(ip)?.Location,
            Time = now,
            TimeZone = _timeZone,
            UserAgent = httpContext.Request.Headers.UserAgent.ToString() is { Length: > 0 } userAgent ? userAgent : null,
            UserId = userId
        };
        if (compiled.Errors.Count > 0 || compiled.IsEmpty)
        {
            return result;
        }

        var source = _sources.Create(current with
        {
            UserId = userId,
            UserName = userId == current.UserId ? current.UserName : null,
            IsImpersonating = userId == current.UserId && current.IsImpersonating,
            HttpContext = httpContext,
            RemoteIpAddress = ip,
            PermissionCodes = context.PermissionCodes is null ? [] : [.. context.PermissionCodes.Where(code => !string.IsNullOrWhiteSpace(code)).Select(code => code.Trim())]
        });
        var evaluation = await _evaluator.EvaluateAsync(compiled, source, explain: true, cancellationToken);
        result.Outcome = evaluation.Outcome;
        result.WouldBlock = evaluation.IsMatched && action == AccessPolicyAction.Deny;
        result.Groups = [.. evaluation.Groups.Select(group => new PermissionConditionTrialGroupDto
        {
            Group = group.Key,
            Outcome = group.Outcome,
            Clauses = [.. group.Clauses.Select(clause => new PermissionConditionTrialClauseDto
            {
                ConditionId = clause.Key,
                AttributeName = clause.AttributeName,
                Operator = (ConditionOperator)(int)clause.Operator,
                IsNegated = clause.IsNegated,
                Value = clause.Value,
                Outcome = clause.Outcome,
                AttributeValues = [.. clause.AttributeValues],
                Message = clause.Message
            })]
        })];
        return result;
    }

    /// <summary>
    /// 防锁死：一条启用中的拒绝策略要是会拦下当前这个请求，就不让保存
    /// </summary>
    /// <exception cref="InvalidOperationException">会拦下当前请求</exception>
    public async Task EnsureNotBlockingCurrentRequestAsync(long policyId, CancellationToken cancellationToken = default)
    {
        var policy = await _policyRepository.GetByIdAsync(policyId, cancellationToken);
        if (policy is not { Action: AccessPolicyAction.Deny, Status: EnableStatus.Enabled })
        {
            return;
        }

        var compiled = _compiler.Compile(await _conditionRepository.GetByPolicyIdAsync(policy.BasicId, cancellationToken));
        if (compiled.IsEmpty)
        {
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        var source = _sources.Create(_sources.CreateCurrentContext(_timeProvider.GetUtcNow()) with { HttpContext = httpContext });
        var evaluation = await _evaluator.EvaluateAsync(compiled, source, explain: false, cancellationToken);
        if (evaluation.IsMatched)
        {
            throw new InvalidOperationException(
                $"这条拒绝策略会拦下你当前的请求（IP {AccessPolicyRequests.FormatIp(httpContext?.Connection.RemoteIpAddress) ?? "未知"}），不能保存。请先调整条件，或保持「只记录」，换到不受影响的网络或账号后再启用拦截。");
        }
    }

    private static AccessPolicyConditionCommand ToCommand(AccessPolicyConditionDto condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        return new AccessPolicyConditionCommand(
            condition.ConditionGroup,
            condition.AttributeName ?? string.Empty,
            condition.Operator,
            condition.IsNegated,
            condition.ValueType,
            condition.ConditionValue ?? string.Empty,
            condition.Description);
    }

    private IPAddress? ResolveIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;
        }

        return IPAddress.TryParse(ip.Trim(), out var address)
            ? address
            : throw new InvalidOperationException($"IP 地址格式不正确：{ip}");
    }

    private DefaultHttpContext BuildRequest(AccessPolicyTrialContextDto context, IPAddress? ip)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = ip;
        var userAgent = string.IsNullOrWhiteSpace(context.UserAgent)
            ? _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString()
            : context.UserAgent.Trim();
        if (!string.IsNullOrWhiteSpace(userAgent))
        {
            httpContext.Request.Headers.UserAgent = userAgent;
        }

        if (!string.IsNullOrWhiteSpace(context.Method))
        {
            httpContext.Request.Method = context.Method.Trim().ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(context.Path))
        {
            var path = context.Path.Trim();
            httpContext.Request.Path = path.StartsWith('/') ? path : "/" + path;
        }

        return httpContext;
    }
}
