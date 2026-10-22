// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authorization.Conditions;
using XiHan.Framework.MultiTenancy.Abstractions;
using ConditionOperator = XiHan.BasicApp.Saas.Domain.Entities.ConditionOperator;

namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 权限条件试算：按模拟的请求上下文算一遍，给出裁决路径
/// </summary>
/// <remarks>
/// 主体属性取被试算用户在目标租户里的真实数据；环境与请求属性取模拟值，没填的取当前请求（IP、User-Agent）或当前时刻。
/// 批量试算按一条授权找出受影响的人（角色成员含继承该角色的角色成员，或直授用户），只在当前上下文里找。
/// </remarks>
public sealed class PermissionConditionTrialService
{
    /// <summary>
    /// 批量试算最多算的人数
    /// </summary>
    public const int BatchLimit = 200;

    private const string WildcardPermission = "*";

    private readonly IAuthorizationSnapshotQueryService _snapshots;
    private readonly ISaasConditionAttributeSourceFactory _sources;
    private readonly PermissionConditionDecider _decider;
    private readonly IPermissionResourceRegistry _resources;
    private readonly IPermissionResourceRowReader _rowReader;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IUserPermissionRepository _userPermissionRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRoleHierarchyDomainService _roleHierarchy;
    private readonly ICurrentTenant _currentTenant;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _timeZone;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionConditionTrialService(
        IAuthorizationSnapshotQueryService snapshots,
        ISaasConditionAttributeSourceFactory sources,
        PermissionConditionDecider decider,
        IPermissionResourceRegistry resources,
        IPermissionResourceRowReader rowReader,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IRolePermissionRepository rolePermissionRepository,
        IUserPermissionRepository userPermissionRepository,
        IPermissionRepository permissionRepository,
        IRoleHierarchyDomainService roleHierarchy,
        ICurrentTenant currentTenant,
        IHttpContextAccessor httpContextAccessor,
        IOptions<SaasConditionOptions> options)
    {
        _snapshots = snapshots;
        _sources = sources;
        _decider = decider;
        _resources = resources;
        _rowReader = rowReader;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _userPermissionRepository = userPermissionRepository;
        _permissionRepository = permissionRepository;
        _roleHierarchy = roleHierarchy;
        _currentTenant = currentTenant;
        _httpContextAccessor = httpContextAccessor;
        _timeZone = options.Value.ResolveTimeZone().Id;
    }

    /// <summary>
    /// 单人试算
    /// </summary>
    /// <exception cref="InvalidOperationException">用户不存在或 IP 写错</exception>
    public async Task<PermissionConditionTrialResultDto> TrialAsync(PermissionConditionTrialInputDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(input.UserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.PermissionCode);

        var tenantId = _currentTenant.Id is > 0 ? _currentTenant.Id.Value : input.TenantId ?? 0;
        using var _ = ChangeTenant(tenantId);
        var user = await _userRepository.GetByIdIgnoreTenantAsync(input.UserId, cancellationToken)
            ?? throw new InvalidOperationException("用户不存在。");
        var environment = ResolveEnvironment(input);
        var code = input.PermissionCode.Trim();
        var (verdict, snapshot, explanation) = await EvaluateAsync(tenantId, user.BasicId, user.UserName, code, input, environment, input.ResourceId, cancellationToken);

        var result = new PermissionConditionTrialResultDto
        {
            Verdict = verdict,
            TenantId = tenantId,
            UserId = user.BasicId,
            UserName = user.UserName,
            PermissionCode = code,
            Ip = environment.Ip?.ToString(),
            Time = environment.Now,
            TimeZone = _timeZone
        };
        if (explanation is null)
        {
            return result;
        }

        var labels = await LoadSourceLabelsAsync(snapshot.Conditions.Grants.GetValueOrDefault(code) ?? [], cancellationToken);
        foreach (var path in snapshot.Conditions.Grants.GetValueOrDefault(code) ?? [])
        {
            var grantSets = path.GrantSetKeys.Select(key => ToSetDto(key, explanation)).ToList();
            var denySets = path.DenySetKeys.Select(key => ToSetDto(key, explanation)).ToList();
            result.Paths.Add(new PermissionConditionTrialPathDto
            {
                Source = path.Source,
                SourceLabel = labels.GetValueOrDefault(path.Source),
                HasUnconditionalGrant = path.HasUnconditionalGrant,
                GrantSets = grantSets,
                DenySets = denySets,
                Outcome = PathOutcome(path.HasUnconditionalGrant, grantSets, denySets)
            });
        }

        result.Denies = [.. (snapshot.Conditions.Denies.GetValueOrDefault(code) ?? []).Select(key => ToSetDto(key, explanation))];
        return result;
    }

    /// <summary>
    /// 批量试算：按一条授权找出受影响的人，用各自的主体属性加同一个模拟上下文各算一遍
    /// </summary>
    /// <exception cref="InvalidOperationException">授权不存在，或角色权限与用户直授没有恰好给一个</exception>
    public async Task<PermissionConditionBatchTrialResultDto> BatchTrialAsync(PermissionConditionBatchTrialInputDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.RolePermissionId.HasValue == input.UserPermissionId.HasValue)
        {
            throw new InvalidOperationException("角色权限与用户直授必须且只能给一个。");
        }

        var tenantId = _currentTenant.Id ?? 0;
        var environment = ResolveEnvironment(input);
        long permissionId;
        List<long> userIds;
        if (input.RolePermissionId is { } rolePermissionId)
        {
            var rolePermission = await _rolePermissionRepository.GetByIdAsync(rolePermissionId, cancellationToken)
                ?? throw new InvalidOperationException("角色权限不存在。");
            permissionId = rolePermission.PermissionId;
            var graph = await _roleHierarchy.GetGraphAsync(cancellationToken);
            var roleIds = graph.DescendantsOf(rolePermission.RoleId).Keys.Append(rolePermission.RoleId).Distinct().ToList();
            userIds = [.. (await _userRoleRepository.GetValidByRoleIdsAsync(roleIds, environment.Now, cancellationToken))
                .Where(binding => binding.TenantId == tenantId)
                .Select(binding => binding.UserId)
                .Distinct()];
        }
        else
        {
            var userPermission = await _userPermissionRepository.GetByIdAsync(input.UserPermissionId!.Value, cancellationToken)
                ?? throw new InvalidOperationException("用户直授不存在。");
            permissionId = userPermission.PermissionId;
            userIds = [userPermission.UserId];
        }

        var code = (await _permissionRepository.GetByIdAsync(permissionId, cancellationToken))?.PermissionCode
            ?? throw new InvalidOperationException("权限不存在。");
        var users = userIds.Count == 0 ? [] : await _userRepository.GetListByIdsIgnoreTenantAsync([.. userIds.Take(BatchLimit)], cancellationToken);
        var result = new PermissionConditionBatchTrialResultDto
        {
            PermissionCode = code,
            TotalUsers = userIds.Count,
            Truncated = userIds.Count > BatchLimit
        };
        foreach (var user in users.OrderBy(user => user.UserName, StringComparer.OrdinalIgnoreCase))
        {
            var (verdict, _, _) = await EvaluateAsync(tenantId, user.BasicId, user.UserName, code, input, environment, null, cancellationToken);
            result.Items.Add(new PermissionConditionBatchTrialItemDto { UserId = user.BasicId, UserName = user.UserName, Verdict = verdict });
        }

        return result;
    }

    private async Task<(PermissionConditionTrialVerdict Verdict, AuthorizationSnapshot Snapshot, PermissionConditionExplanation? Explanation)> EvaluateAsync(
        long tenantId,
        long userId,
        string? userName,
        string code,
        PermissionConditionTrialContextDto input,
        TrialEnvironment environment,
        long? resourceId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _snapshots.BuildAsync(userId, environment.Now, cancellationToken);
        var granted = !snapshot.ContextDeniedCodes.Contains(code)
            && (snapshot.Permissions.Contains(WildcardPermission, StringComparer.OrdinalIgnoreCase)
                || snapshot.Permissions.Contains(code, StringComparer.OrdinalIgnoreCase));
        if (!granted)
        {
            return (PermissionConditionTrialVerdict.NotGranted, snapshot, null);
        }

        if (!snapshot.Conditions.Requires(code))
        {
            return (PermissionConditionTrialVerdict.Unconditional, snapshot, null);
        }

        var source = _sources.Create(new SaasConditionContext
        {
            TenantId = tenantId,
            Now = environment.Now,
            UserId = userId,
            UserName = userName,
            IsImpersonating = input.IsImpersonating,
            HttpContext = BuildRequest(input, environment),
            RemoteIpAddress = environment.Ip,
            PermissionCodes = [code]
        });
        IConditionAttributeSource rowSource;
        if (_resources.TryGet(code, out var registration) && resourceId is > 0)
        {
            var row = (await _rowReader.ReadAsync(registration, [resourceId.Value], cancellationToken)).FirstOrDefault();
            rowSource = PermissionRowAttributeSource.WithRow(source, registration, row);
        }
        else
        {
            rowSource = PermissionRowAttributeSource.BeforeBinding(source, registration);
        }

        var explanation = await _decider.ExplainAsync(snapshot.Conditions, code, rowSource, cancellationToken);
        var verdict = explanation.Outcome switch
        {
            PermissionConditionOutcome.Granted => PermissionConditionTrialVerdict.Granted,
            PermissionConditionOutcome.Pending => PermissionConditionTrialVerdict.Pending,
            _ => PermissionConditionTrialVerdict.Denied
        };
        return (verdict, snapshot, explanation);
    }

    /// <summary>
    /// 路径结果：授予侧不命中或禁止命中为不成立；授予命中且禁止确定不命中为成立；其余为待定
    /// </summary>
    private static ConditionOutcome PathOutcome(bool hasUnconditionalGrant, List<PermissionConditionTrialSetDto> grantSets, List<PermissionConditionTrialSetDto> denySets)
    {
        var grant = hasUnconditionalGrant || grantSets.Any(set => set.Outcome == ConditionOutcome.Matched)
            ? ConditionOutcome.Matched
            : grantSets.Any(set => set.Outcome == ConditionOutcome.Pending) ? ConditionOutcome.Pending : ConditionOutcome.NotMatched;
        if (grant == ConditionOutcome.NotMatched || denySets.Any(set => set.Outcome is null or ConditionOutcome.Matched))
        {
            return ConditionOutcome.NotMatched;
        }

        return grant == ConditionOutcome.Matched && denySets.All(set => set.Outcome != ConditionOutcome.Pending)
            ? ConditionOutcome.Matched
            : ConditionOutcome.Pending;
    }

    private static PermissionConditionTrialSetDto ToSetDto(string key, PermissionConditionExplanation explanation)
    {
        var evaluation = explanation.Sets.GetValueOrDefault(key);
        return new PermissionConditionTrialSetDto
        {
            Key = key,
            Outcome = evaluation?.Outcome,
            Groups = evaluation is null
                ? []
                : [.. evaluation.Groups.Select(group => new PermissionConditionTrialGroupDto
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
                })]
        };
    }

    private async Task<Dictionary<string, string>> LoadSourceLabelsAsync(IEnumerable<ConditionalGrantPath> paths, CancellationToken cancellationToken)
    {
        var labels = new Dictionary<string, string>(StringComparer.Ordinal);
        var roleSources = paths
            .Select(path => path.Source)
            .Where(source => source.StartsWith("role:", StringComparison.Ordinal) || source.StartsWith("delegation-role:", StringComparison.Ordinal))
            .Select(source => (Source: source, Id: long.TryParse(source[(source.IndexOf(':') + 1)..], out var id) ? id : 0))
            .Where(item => item.Id > 0)
            .ToList();
        var roles = roleSources.Count == 0
            ? new Dictionary<long, string>()
            : (await _roleRepository.GetByIdsAsync([.. roleSources.Select(item => item.Id).Distinct()], cancellationToken))
                .ToDictionary(role => role.BasicId, role => role.RoleName);
        foreach (var (source, id) in roleSources)
        {
            if (roles.TryGetValue(id, out var name))
            {
                labels[source] = name;
            }
        }

        return labels;
    }

    private TrialEnvironment ResolveEnvironment(PermissionConditionTrialContextDto input)
    {
        IPAddress? ip;
        if (string.IsNullOrWhiteSpace(input.Ip))
        {
            ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;
        }
        else if (!IPAddress.TryParse(input.Ip.Trim(), out ip))
        {
            throw new InvalidOperationException($"IP 地址格式不正确：{input.Ip}");
        }

        return new TrialEnvironment(
            input.Time ?? DateTimeOffset.UtcNow,
            ip,
            string.IsNullOrWhiteSpace(input.UserAgent) ? _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() : input.UserAgent.Trim());
    }

    private static DefaultHttpContext BuildRequest(PermissionConditionTrialContextDto input, TrialEnvironment environment)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = environment.Ip;
        if (!string.IsNullOrWhiteSpace(environment.UserAgent))
        {
            httpContext.Request.Headers.UserAgent = environment.UserAgent;
        }

        if (!string.IsNullOrWhiteSpace(input.Method))
        {
            httpContext.Request.Method = input.Method.Trim().ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(input.Path))
        {
            var path = input.Path.Trim();
            httpContext.Request.Path = path.StartsWith('/') ? path : "/" + path;
        }

        if (input.QueryValues is { Count: > 0 })
        {
            httpContext.Request.QueryString = QueryString.Create(input.QueryValues.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value)));
        }

        foreach (var (name, value) in input.RouteValues ?? [])
        {
            httpContext.Request.RouteValues[name] = value;
        }

        return httpContext;
    }

    private IDisposable? ChangeTenant(long tenantId)
    {
        if (_currentTenant.Id is > 0)
        {
            return null;
        }

        var target = tenantId > 0 ? tenantId : (long?)null;
        return _currentTenant.Change(target, target?.ToString());
    }

    private sealed record TrialEnvironment(DateTimeOffset Now, IPAddress? Ip, string? UserAgent);
}
