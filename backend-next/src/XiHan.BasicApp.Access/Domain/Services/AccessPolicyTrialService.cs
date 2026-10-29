// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Access.Operations.PermissionConditions;
using XiHan.BasicApp.Access.Operations.Policies;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Conditions;
using XiHan.Framework.Security.Abstractions.Network;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 访问策略的试运行（4.x 的 AccessPolicyTrialService）：已有策略或草稿条件，按给定的请求情形只求这一条、不看有效期，逐组逐条件说明；
/// 给了用户即按他此刻的有效指派取角色（不看令牌），没给即按未登录。编译不过的条件写进 <c>errors</c>，不求值。
/// </summary>
/// <param name="service">策略服务。</param>
/// <param name="grants">授予全貌（用户此刻的角色）。</param>
/// <param name="users">用户仓储。</param>
/// <param name="policyEvaluator">策略求值（编译与时机）。</param>
/// <param name="conditionEvaluator">条件求值器（说明模式）。</param>
/// <param name="options">授权包配置。</param>
/// <param name="services">服务提供器（属性来源查库、归属地）。</param>
/// <param name="clock">时间源。</param>
public sealed class AccessPolicyTrialService(
    AccessPolicyService service,
    GrantEvaluator grants,
    IRepository<SysUser, Guid> users,
    AccessPolicyEvaluator policyEvaluator,
    IConditionEvaluator conditionEvaluator,
    AccessOptions options,
    IServiceProvider services,
    TimeProvider clock)
{
    /// <summary>
    /// 试运行。
    /// </summary>
    public async Task<Result<AccessPolicyTrialResponse>> TrialAsync(string owner, TrialAccessPolicyRequest request, Func<Error, string> localize, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(localize);
        var ip = string.IsNullOrWhiteSpace(request.Ip) ? null : request.Ip.Trim();
        IPAddress? address = null;
        if (ip is not null && !IPAddress.TryParse(ip, out address))
        {
            return Error.Validation(AccessErrors.TrialIpInvalid, AccessMessages.TrialIpInvalid, MessageArguments.Of(("ip", ip)));
        }

        IReadOnlyList<PolicyConditionDefinition> definitions;
        AccessPolicyAction action;
        if (request.Conditions is { Count: > 0 } draft)
        {
            definitions = [.. draft.Select(static c => c.ToDefinition())];
            action = request.Action ?? AccessPolicyAction.LogOnly;
        }
        else if (request.PolicyId is { } policyId)
        {
            if (!(await service.FindAsync(policyId, owner, cancellationToken).ConfigureAwait(false)).TryGetValue(out var policy))
            {
                return Error.NotFound(AccessErrors.PolicyNotFound, AccessMessages.PolicyNotFound, MessageArguments.Of(("id", policyId)));
            }

            definitions = [.. (await service.ConditionsOfAsync(owner, [policy.Id], cancellationToken).ConfigureAwait(false))[policy.Id]
                .Select(static c => new PolicyConditionDefinition(c.GroupNo, c.AttributeName, c.Operator, c.IsNegated, c.Value, c.Description))];
            action = request.Action ?? policy.Action;
        }
        else
        {
            return Error.Validation(AccessErrors.PolicyConditionsRequired, AccessMessages.PolicyConditionsRequired);
        }

        var time = request.Time ?? clock.GetUtcNow();
        Guid? userId = null;
        string? userName = null;
        IReadOnlyList<Guid> roleIds = [];
        if (request.UserId is { } id)
        {
            if (!(await users.FindAsync(id, cancellationToken).ConfigureAwait(false)).TryGetValue(out var user) || !string.Equals(user.HomeTenantId, owner, StringComparison.Ordinal))
            {
                return Error.NotFound(AccessErrors.MemberNotFound, AccessMessages.MemberNotFound, MessageArguments.Of(("id", id)));
            }

            userId = user.Id;
            userName = user.UserName;
            roleIds = [.. (await grants.ViewOfAsync(owner, user.Id, time, cancellationToken).ConfigureAwait(false)).Roles.Select(static r => r.RoleId)];
        }

        var userAgent = string.IsNullOrWhiteSpace(request.UserAgent) ? null : request.UserAgent.Trim();
        var conditionRequest = new ConditionRequest(
            owner,
            userId,
            userName,
            false,
            roleIds,
            [.. (request.PermissionCodes ?? []).Where(static p => !string.IsNullOrWhiteSpace(p)).Select(static p => p.Trim())],
            string.IsNullOrWhiteSpace(request.Path) ? null : request.Path.Trim(),
            string.IsNullOrWhiteSpace(request.Method) ? null : request.Method.Trim().ToUpperInvariant(),
            null,
            ip,
            userAgent,
            userId is null ? "anonymous" : InvocationProtocols.Http,
            time);
        var compiled = policyEvaluator.Compile(AccessPolicyService.ViewOf(Guid.Empty, string.Empty, string.Empty, action, 0, null, null, definitions));
        var evaluation = compiled.Errors.Count == 0
            ? await conditionEvaluator.EvaluateAsync(compiled, ConditionAttributeSource.Create(conditionRequest, services, options), explain: true, cancellationToken).ConfigureAwait(false)
            : null;
        return new AccessPolicyTrialResponse(
            evaluation?.Outcome,
            action,
            evaluation?.IsMatched == true && action == AccessPolicyAction.Deny,
            AccessPolicyEvaluator.PhaseOf(compiled, string.Equals(owner, TenantIds.Platform, StringComparison.Ordinal)),
            [.. compiled.Errors.Select(e => localize(e.Error))],
            ip,
            LocationOf(address),
            time,
            options.ConditionTimeZone.Id,
            userAgent,
            userId,
            userName,
            [.. (evaluation?.Groups ?? []).Select(g => new ConditionTrialGroupResponse(
                g.Key,
                g.Outcome,
                [.. g.Clauses.Select(c => new ConditionTrialClauseResponse(c.Key, c.AttributeName, c.Operator, c.IsNegated, c.Value, c.Outcome, c.AttributeValues, c.Reason is { } reason ? localize(reason) : null))]))]);
    }

    private string? LocationOf(IPAddress? address)
    {
        if (address is null || services.GetService<IIpRegionResolver>() is not { } regions || !regions.Resolve(address).TryGetValue(out var region))
        {
            return null;
        }

        var parts = new[] { region.Country, region.Province, region.City, region.Isp }.Where(static p => !string.IsNullOrWhiteSpace(p)).ToList();
        return parts.Count == 0 ? null : string.Join(' ', parts);
    }
}
