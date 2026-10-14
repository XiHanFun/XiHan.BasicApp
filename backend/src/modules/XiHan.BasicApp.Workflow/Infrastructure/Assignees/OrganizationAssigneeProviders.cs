// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.Framework.Workflow.Abstractions.Assignees;

namespace XiHan.BasicApp.Workflow.Infrastructure.Assignees;

/// <summary>
/// 按编码引用组织对象的解析器基类（参数为一个或多个非空编码）
/// </summary>
public abstract class OrganizationCodeAssigneeProviderBase : IWorkflowAssigneeProvider
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="directory">组织目录</param>
    protected OrganizationCodeAssigneeProviderBase(WorkflowOrganizationDirectory directory)
    {
        Directory = directory;
    }

    /// <summary>
    /// 规则类型
    /// </summary>
    public abstract string RuleType { get; }

    /// <summary>
    /// 组织对象名称（校验信息用）
    /// </summary>
    protected abstract string SubjectName { get; }

    /// <summary>
    /// 组织目录
    /// </summary>
    protected WorkflowOrganizationDirectory Directory { get; }

    /// <summary>
    /// 校验规则参数（至少一个非空编码）
    /// </summary>
    /// <param name="rule">规则</param>
    /// <returns>错误信息列表</returns>
    public IReadOnlyList<string> Validate(WorkflowAssigneeRule rule)
    {
        return GetCodes(rule).Count > 0 ? [] : [$"{SubjectName}规则至少需要一个{SubjectName}编码"];
    }

    /// <summary>
    /// 解析受理人
    /// </summary>
    /// <param name="context">解析上下文</param>
    /// <returns>受理人标识列表</returns>
    public async Task<IReadOnlyList<string>> ResolveAsync(WorkflowAssigneeResolveContext context)
    {
        var codes = GetCodes(context.Rule);
        if (codes.Count == 0)
        {
            return [];
        }

        var userIds = await ResolveUserIdsAsync(codes, context.CancellationToken);
        return await Directory.FilterEnabledAsync(userIds, context.CancellationToken);
    }

    /// <summary>
    /// 按编码解析候选用户主键
    /// </summary>
    /// <param name="codes">编码集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>候选用户主键</returns>
    protected abstract Task<IReadOnlyList<long>> ResolveUserIdsAsync(IReadOnlyList<string> codes, CancellationToken cancellationToken);

    private static List<string> GetCodes(WorkflowAssigneeRule rule)
    {
        return [.. rule.Values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)];
    }
}

/// <summary>
/// 角色成员解析器
/// </summary>
public sealed class RoleAssigneeProvider(WorkflowOrganizationDirectory directory) : OrganizationCodeAssigneeProviderBase(directory)
{
    /// <summary>
    /// 规则类型
    /// </summary>
    public override string RuleType => OrganizationAssigneeRuleTypes.Role;

    /// <summary>
    /// 组织对象名称
    /// </summary>
    protected override string SubjectName => "角色";

    /// <summary>
    /// 按角色编码解析成员
    /// </summary>
    protected override Task<IReadOnlyList<long>> ResolveUserIdsAsync(IReadOnlyList<string> codes, CancellationToken cancellationToken)
    {
        return Directory.GetRoleMemberIdsAsync(codes, cancellationToken);
    }
}

/// <summary>
/// 部门成员解析器
/// </summary>
public sealed class DepartmentAssigneeProvider(WorkflowOrganizationDirectory directory) : OrganizationCodeAssigneeProviderBase(directory)
{
    /// <summary>
    /// 规则类型
    /// </summary>
    public override string RuleType => OrganizationAssigneeRuleTypes.Department;

    /// <summary>
    /// 组织对象名称
    /// </summary>
    protected override string SubjectName => "部门";

    /// <summary>
    /// 按部门编码解析成员
    /// </summary>
    protected override async Task<IReadOnlyList<long>> ResolveUserIdsAsync(IReadOnlyList<string> codes, CancellationToken cancellationToken)
    {
        var departments = await Directory.GetDepartmentsAsync(codes, cancellationToken);
        return await Directory.GetDepartmentMemberIdsAsync(departments.Select(department => department.BasicId), cancellationToken);
    }
}

/// <summary>
/// 部门负责人解析器
/// </summary>
public sealed class DepartmentLeaderAssigneeProvider(WorkflowOrganizationDirectory directory) : OrganizationCodeAssigneeProviderBase(directory)
{
    /// <summary>
    /// 规则类型
    /// </summary>
    public override string RuleType => OrganizationAssigneeRuleTypes.DepartmentLeader;

    /// <summary>
    /// 组织对象名称
    /// </summary>
    protected override string SubjectName => "部门";

    /// <summary>
    /// 按部门编码解析负责人（保持编码顺序）
    /// </summary>
    protected override async Task<IReadOnlyList<long>> ResolveUserIdsAsync(IReadOnlyList<string> codes, CancellationToken cancellationToken)
    {
        var departments = await Directory.GetDepartmentsAsync(codes, cancellationToken);
        return [.. codes
            .Select(code => departments.FirstOrDefault(department => department.DepartmentCode == code)?.LeaderId)
            .OfType<long>()];
    }
}

/// <summary>
/// 岗位成员解析器
/// </summary>
public sealed class PositionAssigneeProvider(WorkflowOrganizationDirectory directory) : OrganizationCodeAssigneeProviderBase(directory)
{
    /// <summary>
    /// 规则类型
    /// </summary>
    public override string RuleType => OrganizationAssigneeRuleTypes.Position;

    /// <summary>
    /// 组织对象名称
    /// </summary>
    protected override string SubjectName => "岗位";

    /// <summary>
    /// 按岗位编码解析成员
    /// </summary>
    protected override Task<IReadOnlyList<long>> ResolveUserIdsAsync(IReadOnlyList<string> codes, CancellationToken cancellationToken)
    {
        return Directory.GetPositionMemberIdsAsync(codes, cancellationToken);
    }
}

/// <summary>
/// 发起人主管解析器基类（参数为层级，缺省 1，范围 1-10）
/// </summary>
public abstract class StarterLeaderAssigneeProviderBase : IWorkflowAssigneeProvider
{
    /// <summary>
    /// 最大支持层级
    /// </summary>
    public const int MaxLevel = 10;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="directory">组织目录</param>
    protected StarterLeaderAssigneeProviderBase(WorkflowOrganizationDirectory directory)
    {
        Directory = directory;
    }

    /// <summary>
    /// 规则类型
    /// </summary>
    public abstract string RuleType { get; }

    /// <summary>
    /// 组织目录
    /// </summary>
    protected WorkflowOrganizationDirectory Directory { get; }

    /// <summary>
    /// 校验规则参数（至多一个 1-10 的整数）
    /// </summary>
    /// <param name="rule">规则</param>
    /// <returns>错误信息列表</returns>
    public IReadOnlyList<string> Validate(WorkflowAssigneeRule rule)
    {
        var values = rule.Values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
        if (values.Count > 1)
        {
            return ["主管规则只能配置一个层级"];
        }

        return values.Count == 0 || TryParseLevel(values[0], out _)
            ? []
            : [$"主管层级 {values[0]} 非法（应为 1-{MaxLevel} 的整数）"];
    }

    /// <summary>
    /// 解析受理人（实例未记录发起人或发起人标识不是用户主键时为空）
    /// </summary>
    /// <param name="context">解析上下文</param>
    /// <returns>受理人标识列表</returns>
    public async Task<IReadOnlyList<string>> ResolveAsync(WorkflowAssigneeResolveContext context)
    {
        if (!long.TryParse(context.Instance.StarterId, NumberStyles.None, CultureInfo.InvariantCulture, out var starterUserId))
        {
            return [];
        }

        var levelText = context.Rule.Values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        var level = levelText is not null && TryParseLevel(levelText, out var parsed) ? parsed : 1;

        var chain = await Directory.GetLeaderChainAsync(starterUserId, level, context.CancellationToken);
        return [.. SelectLeaders(chain, level).Select(userId => userId.ToString(CultureInfo.InvariantCulture))];
    }

    /// <summary>
    /// 从主管链中选出受理人
    /// </summary>
    /// <param name="chain">逐级主管链（已过滤停用账号）</param>
    /// <param name="level">层级参数</param>
    /// <returns>受理人主键</returns>
    protected abstract IEnumerable<long> SelectLeaders(IReadOnlyList<long> chain, int level);

    private static bool TryParseLevel(string text, out int level)
    {
        return int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out level) && level is >= 1 and <= MaxLevel;
    }
}

/// <summary>
/// 发起人第 N 级主管解析器
/// </summary>
public sealed class StarterLeaderAssigneeProvider(WorkflowOrganizationDirectory directory) : StarterLeaderAssigneeProviderBase(directory)
{
    /// <summary>
    /// 规则类型
    /// </summary>
    public override string RuleType => OrganizationAssigneeRuleTypes.StarterLeader;

    /// <summary>
    /// 取第 N 级（链不够长时为空）
    /// </summary>
    protected override IEnumerable<long> SelectLeaders(IReadOnlyList<long> chain, int level)
    {
        return chain.Count >= level ? [chain[level - 1]] : [];
    }
}

/// <summary>
/// 发起人逐级主管链解析器
/// </summary>
public sealed class StarterLeaderChainAssigneeProvider(WorkflowOrganizationDirectory directory) : StarterLeaderAssigneeProviderBase(directory)
{
    /// <summary>
    /// 规则类型
    /// </summary>
    public override string RuleType => OrganizationAssigneeRuleTypes.StarterLeaderChain;

    /// <summary>
    /// 取第 1 级到第 N 级（链不够长时取到顶）
    /// </summary>
    protected override IEnumerable<long> SelectLeaders(IReadOnlyList<long> chain, int level)
    {
        return chain;
    }
}
