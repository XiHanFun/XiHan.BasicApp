#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacAbacEvaluator
// Guid:31fdcf98-5f1e-47b5-9f0e-c2d2d44a42f5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.Json;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Authorization.Abac;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Authorization;

/// <summary>
/// 基于系统约束规则的 ABAC 评估器
/// </summary>
public class RbacAbacEvaluator : IAbacEvaluator
{
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly DefaultAbacEvaluator _fallbackEvaluator = new();

    private ISqlSugarClient DbClient => _clientResolver.GetCurrentClient();

    /// <summary>
    /// 构造函数
    /// </summary>
    public RbacAbacEvaluator(ISqlSugarClientResolver clientResolver)
    {
        _clientResolver = clientResolver;
    }

    /// <inheritdoc />
    public async Task<AbacEvaluationResult> EvaluateAsync(AbacEvaluationContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var policyCode = context.PolicyCode?.Trim();
        if (string.IsNullOrWhiteSpace(policyCode))
        {
            return AbacEvaluationResult.Allow("未配置 ABAC 策略");
        }

        var tenantId = ResolveTenantId(context);
        var rule = await LoadRuleAsync(policyCode, tenantId, cancellationToken);
        if (rule is null)
        {
            return await _fallbackEvaluator.EvaluateAsync(context, cancellationToken);
        }

        if (!IsEffective(rule, context.EvaluationTime))
        {
            return AbacEvaluationResult.Allow($"策略 {rule.RuleCode} 不在生效期");
        }

        var result = await EvaluateRuleAsync(rule, context, cancellationToken);
        if (result.IsAllowed)
        {
            return result;
        }

        return rule.ViolationAction switch
        {
            ViolationAction.Warning => AbacEvaluationResult.Allow($"策略 {rule.RuleCode} 命中告警，放行"),
            ViolationAction.Log => AbacEvaluationResult.Allow($"策略 {rule.RuleCode} 仅记录日志，放行"),
            _ => result
        };
    }

    private async Task<SysConstraintRule?> LoadRuleAsync(string policyCode, long? tenantId, CancellationToken cancellationToken)
    {
        var query = DbClient.Queryable<SysConstraintRule>()
            .Where(rule =>
                rule.RuleCode == policyCode
                && rule.IsEnabled
                && rule.Status == YesOrNo.Yes);

        query = tenantId.HasValue
            ? query.Where(rule => rule.TenantId == tenantId.Value || rule.TenantId == 0)
            : query.Where(rule => rule.TenantId == 0);

        var rules = await query
            .OrderBy(rule => rule.Priority)
            .ToListAsync(cancellationToken);

        if (rules.Count == 0)
        {
            return null;
        }

        if (tenantId.HasValue)
        {
            return rules.FirstOrDefault(rule => rule.TenantId == tenantId.Value)
                   ?? rules.FirstOrDefault(rule => rule.TenantId == 0);
        }

        return rules[0];
    }

    private async Task<AbacEvaluationResult> EvaluateRuleAsync(
        SysConstraintRule rule,
        AbacEvaluationContext context,
        CancellationToken cancellationToken)
    {
        if (TryResolveExpression(rule.Parameters, out var expression))
        {
            var expressionContext = new AbacEvaluationContext
            {
                UserId = context.UserId,
                PermissionCode = context.PermissionCode,
                PolicyCode = expression,
                Resource = context.Resource,
                SubjectAttributes = context.SubjectAttributes,
                ResourceAttributes = context.ResourceAttributes,
                EnvironmentAttributes = context.EnvironmentAttributes,
                EvaluationTime = context.EvaluationTime
            };
            var expressionResult = await _fallbackEvaluator.EvaluateAsync(expressionContext, cancellationToken);
            return expressionResult.IsAllowed
                ? AbacEvaluationResult.Allow($"命中约束规则 {rule.RuleCode}")
                : AbacEvaluationResult.Deny($"约束规则 {rule.RuleCode} 校验不通过");
        }

        var fallbackContext = new AbacEvaluationContext
        {
            UserId = context.UserId,
            PermissionCode = context.PermissionCode,
            PolicyCode = rule.RuleCode,
            Resource = context.Resource,
            SubjectAttributes = context.SubjectAttributes,
            ResourceAttributes = context.ResourceAttributes,
            EnvironmentAttributes = context.EnvironmentAttributes,
            EvaluationTime = context.EvaluationTime
        };
        var result = await _fallbackEvaluator.EvaluateAsync(fallbackContext, cancellationToken);
        if (result.IsAllowed || string.IsNullOrWhiteSpace(rule.Parameters))
        {
            return result;
        }

        if (TryResolveRequiredRoles(rule.Parameters, out var requiredRoles))
        {
            var subjectRoles = ResolveSubjectRoles(context);
            return subjectRoles.Intersect(requiredRoles, StringComparer.OrdinalIgnoreCase).Any()
                ? AbacEvaluationResult.Allow($"命中约束规则 {rule.RuleCode}")
                : AbacEvaluationResult.Deny($"约束规则 {rule.RuleCode} 角色不匹配");
        }

        return result;
    }

    private static bool IsEffective(SysConstraintRule rule, DateTimeOffset evaluationTime)
    {
        if (rule.EffectiveFrom.HasValue && evaluationTime < rule.EffectiveFrom.Value)
        {
            return false;
        }

        if (rule.EffectiveTo.HasValue && evaluationTime > rule.EffectiveTo.Value)
        {
            return false;
        }

        return true;
    }

    private static long? ResolveTenantId(AbacEvaluationContext context)
    {
        if (TryConvertToLong(context.SubjectAttributes, "tenant_id", out var tenantId))
        {
            return tenantId;
        }

        if (TryConvertToLong(context.ResourceAttributes, "route.tenant_id", out tenantId))
        {
            return tenantId;
        }

        if (TryConvertToLong(context.ResourceAttributes, "query.tenant_id", out tenantId))
        {
            return tenantId;
        }

        return null;
    }

    private static bool TryConvertToLong(IReadOnlyDictionary<string, object?> source, string key, out long value)
    {
        value = 0;
        if (!source.TryGetValue(key, out var raw) || raw is null)
        {
            return false;
        }

        if (raw is long longValue)
        {
            value = longValue;
            return true;
        }

        return long.TryParse(raw.ToString(), out value);
    }

    private static bool TryResolveExpression(string parameters, out string expression)
    {
        expression = string.Empty;
        if (string.IsNullOrWhiteSpace(parameters))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(parameters);
            if (!doc.RootElement.TryGetProperty("expression", out var expressionElement))
            {
                return false;
            }

            expression = expressionElement.GetString()?.Trim() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(expression);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryResolveRequiredRoles(string parameters, out string[] requiredRoles)
    {
        requiredRoles = [];
        if (string.IsNullOrWhiteSpace(parameters))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(parameters);
            if (!doc.RootElement.TryGetProperty("requiredRoles", out var requiredRolesElement)
                && !doc.RootElement.TryGetProperty("required_roles", out requiredRolesElement))
            {
                return false;
            }

            if (requiredRolesElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            requiredRoles =
            [
                .. requiredRolesElement
                    .EnumerateArray()
                    .Select(item => item.GetString()?.Trim())
                    .Where(static role => !string.IsNullOrWhiteSpace(role))
                    .Cast<string>()
                    .Distinct(StringComparer.OrdinalIgnoreCase)
            ];
            return requiredRoles.Length > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static IReadOnlyCollection<string> ResolveSubjectRoles(AbacEvaluationContext context)
    {
        if (!context.SubjectAttributes.TryGetValue("roles", out var rolesValue) || rolesValue is null)
        {
            return [];
        }

        if (rolesValue is IEnumerable<string> roles)
        {
            return [.. roles.Where(static role => !string.IsNullOrWhiteSpace(role))];
        }

        if (rolesValue is IEnumerable<object> objectRoles)
        {
            return
            [
                .. objectRoles
                    .Select(static role => role?.ToString())
                    .Where(static role => !string.IsNullOrWhiteSpace(role))
                    .Cast<string>()
            ];
        }

        var raw = rolesValue.ToString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return [raw.Trim()];
    }
}
