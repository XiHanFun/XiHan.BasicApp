// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Application.Conditions;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Authorization.Conditions;
using ConditionOperator = XiHan.Framework.Authorization.Conditions.ConditionOperator;

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 访问策略条件的编译与写路径校验
/// </summary>
/// <remarks>
/// 只认目录里登记的属性（含旧名）；数据行属性只用于权限条件，这里按未登记处理。
/// 值类型只决定书写格式：布尔、数字两种值类型只能配同种类的属性。
/// </remarks>
public sealed class AccessPolicyConditionCompiler : IAccessPolicyRuleChecker
{
    private readonly IConditionCompiler _compiler;
    private readonly TimeZoneInfo _timeZone;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AccessPolicyConditionCompiler(IConditionCompiler compiler, IOptions<SaasConditionOptions> options)
    {
        _compiler = compiler;
        _timeZone = options.Value.ResolveTimeZone();
    }

    /// <summary>
    /// 编译已保存的条件，子句以条件主键为标识
    /// </summary>
    public CompiledConditionSet Compile(IEnumerable<SysAccessPolicyCondition> conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        return Compile(conditions.Select(condition => (
            condition.ConditionGroup,
            new ConditionClause(
                condition.AttributeName,
                (ConditionOperator)(int)condition.Operator,
                condition.ConditionValue,
                condition.IsNegated,
                condition.BasicId.ToString(CultureInfo.InvariantCulture)))));
    }

    /// <summary>
    /// 编译条件草稿，子句以从 1 开始的序号为标识
    /// </summary>
    public CompiledConditionSet Compile(IReadOnlyList<AccessPolicyConditionCommand> conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        return Compile(conditions.Select((condition, index) => (
            condition.ConditionGroup,
            new ConditionClause(
                condition.AttributeName.Trim(),
                (ConditionOperator)(int)condition.Operator,
                condition.ConditionValue?.Trim(),
                condition.IsNegated,
                (index + 1).ToString(CultureInfo.InvariantCulture)))));
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">属性未登记、运算符不适用、值解析失败或值类型与属性不符</exception>
    public void Validate(IReadOnlyList<AccessPolicyConditionCommand> conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        var compiled = Compile(conditions);
        if (compiled.Errors.Count > 0)
        {
            var error = compiled.Errors[0];
            throw new InvalidOperationException($"第 {error.ClauseKey} 条条件（{error.AttributeName}）无法求值：{error.Message}");
        }

        var resolutions = compiled.Groups
            .SelectMany(group => group.Clauses)
            .ToDictionary(clause => clause.Source.Key!, clause => clause.Resolution!, StringComparer.Ordinal);
        for (var index = 0; index < conditions.Count; index++)
        {
            var condition = conditions[index];
            var kind = resolutions[(index + 1).ToString(CultureInfo.InvariantCulture)].Descriptor.Kind;
            var mismatched = condition.ValueType switch
            {
                ConfigDataType.Boolean => kind != ConditionValueKind.Boolean,
                ConfigDataType.Number => kind != ConditionValueKind.Number,
                _ => false
            };
            if (mismatched)
            {
                throw new InvalidOperationException($"第 {index + 1} 条条件的值类型 {condition.ValueType} 与属性 {condition.AttributeName.Trim()} 的值种类 {kind} 不符。");
            }
        }
    }

    private CompiledConditionSet Compile(IEnumerable<(int Group, ConditionClause Clause)> clauses)
    {
        var groups = clauses
            .GroupBy(item => item.Group)
            .OrderBy(group => group.Key)
            .Select(group => new ConditionGroup(
                [.. group.Select(item => item.Clause)],
                group.Key.ToString(CultureInfo.InvariantCulture)))
            .ToList();
        return _compiler.Compile(new ConditionSet(groups), new ConditionCompileOptions { TimeZone = _timeZone });
    }
}
