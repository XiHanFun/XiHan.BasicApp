// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Organization;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Workflow.Domain.Services;
using XiHan.Framework.Primitives;
using XiHan.Framework.Workflow.Abstractions.Assignees;

namespace XiHan.BasicApp.Workflow.Infrastructure;

/// <summary>
/// 组织类受理人规则的类型名。
/// </summary>
/// <remarks>
/// 与 4.x 同名（设计器预设的规则编辑器按这些名字），框架内置的是 <c>User</c>、<c>Starter</c>、<c>Expression</c>。
/// </remarks>
public static class OrganizationAssigneeRuleTypes
{
    /// <summary>
    /// 角色成员（按角色编码）。
    /// </summary>
    public const string Role = "Role";

    /// <summary>
    /// 部门成员（按部门编码，不含下级）。
    /// </summary>
    public const string Department = "Department";

    /// <summary>
    /// 部门负责人（按部门编码）。
    /// </summary>
    public const string DepartmentLeader = "DepartmentLeader";

    /// <summary>
    /// 岗位任职（按岗位编码）。
    /// </summary>
    public const string Position = "Position";

    /// <summary>
    /// 发起人第 N 级主管。
    /// </summary>
    public const string StarterLeader = "StarterLeader";

    /// <summary>
    /// 发起人第 1 到 N 级主管链。
    /// </summary>
    public const string StarterLeaderChain = "StarterLeaderChain";

    /// <summary>
    /// 主管层级的上限。
    /// </summary>
    public const int MaxLevel = 10;
}

/// <summary>
/// 组织类受理人的公共部分。
/// </summary>
/// <remarks>
/// 照 4.x：一切候选人最后与「实例所在一侧可见的在用账号」求交（平台是平台账号，租户是注册在该租户的账号与普通成员，运维支持成员不算），
/// 停用的账号、离开租户的成员收不到待办；保持候选的顺序、去重。解析在实例所属分区的调用里执行（组织数据按调用的租户读）。
/// </remarks>
/// <param name="users">用户目录。</param>
public abstract class OrganizationAssigneeProvider(IUserDirectory users) : IAssigneeProvider
{
    /// <inheritdoc />
    public abstract string RuleType { get; }

    /// <inheritdoc />
    public abstract Result Validate(AssigneeRule rule);

    /// <inheritdoc />
    public async ValueTask<Result<IReadOnlyList<string>>> ResolveAsync(AssigneeResolution resolution, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        var candidates = await CandidatesAsync(resolution).ConfigureAwait(false);
        if (candidates.Count == 0)
        {
            return Result.Success<IReadOnlyList<string>>([]);
        }

        var visible = await users.ActiveInSideAsync(SideOf(resolution), candidates, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<string>>(candidates.Where(visible.Contains).Distinct().Select(WorkflowIds.ToEngineUser).ToList());
    }

    /// <summary>
    /// 规则给出的候选人（求交之前）。
    /// </summary>
    /// <param name="resolution">解析上下文。</param>
    /// <returns>按规则顺序的候选用户 ID。</returns>
    protected abstract Task<IReadOnlyList<Guid>> CandidatesAsync(AssigneeResolution resolution);

    /// <summary>
    /// 实例所在的一侧。
    /// </summary>
    /// <param name="resolution">解析上下文。</param>
    /// <returns>平台为 <c>platform</c>，否则是租户 ID。</returns>
    protected static string SideOf(AssigneeResolution resolution) => resolution.Instance.TenantId ?? TenantIds.Platform;

    /// <summary>
    /// 编码类规则的编码。
    /// </summary>
    /// <param name="rule">规则。</param>
    /// <returns>去空白的编码。</returns>
    protected static IReadOnlyList<string> CodesOf(AssigneeRule rule) => rule.Values.Where(static v => !string.IsNullOrWhiteSpace(v)).Select(static v => v.Trim()).ToList();

    /// <summary>
    /// 编码类规则至少要一个编码。
    /// </summary>
    /// <param name="rule">规则。</param>
    /// <returns>是否合法。</returns>
    protected static Result RequireCodes(AssigneeRule rule)
        => CodesOf(rule).Count > 0
            ? Result.Success()
            : Error.Validation(WorkflowErrors.AssigneeRuleInvalid, WorkflowMessages.AssigneeCodesRequired, MessageArguments.Of(("type", rule.Type)));
}

/// <summary>
/// 角色成员。
/// </summary>
/// <param name="roles">角色成员目录。</param>
/// <param name="users">用户目录。</param>
public sealed class RoleAssigneeProvider(IRoleMemberDirectory roles, IUserDirectory users) : OrganizationAssigneeProvider(users)
{
    /// <inheritdoc />
    public override string RuleType => OrganizationAssigneeRuleTypes.Role;

    /// <inheritdoc />
    public override Result Validate(AssigneeRule rule) => RequireCodes(rule);

    /// <inheritdoc />
    protected override Task<IReadOnlyList<Guid>> CandidatesAsync(AssigneeResolution resolution) => roles.MembersAsync(resolution.Activity.Invocation, CodesOf(resolution.Rule));
}

/// <summary>
/// 部门成员。
/// </summary>
/// <param name="organization">组织目录。</param>
/// <param name="users">用户目录。</param>
public sealed class DepartmentAssigneeProvider(IOrganizationDirectory organization, IUserDirectory users) : OrganizationAssigneeProvider(users)
{
    /// <inheritdoc />
    public override string RuleType => OrganizationAssigneeRuleTypes.Department;

    /// <inheritdoc />
    public override Result Validate(AssigneeRule rule) => RequireCodes(rule);

    /// <inheritdoc />
    protected override Task<IReadOnlyList<Guid>> CandidatesAsync(AssigneeResolution resolution) => organization.DepartmentMembersAsync(resolution.Activity.Invocation, CodesOf(resolution.Rule));
}

/// <summary>
/// 部门负责人。
/// </summary>
/// <param name="organization">组织目录。</param>
/// <param name="users">用户目录。</param>
public sealed class DepartmentLeaderAssigneeProvider(IOrganizationDirectory organization, IUserDirectory users) : OrganizationAssigneeProvider(users)
{
    /// <inheritdoc />
    public override string RuleType => OrganizationAssigneeRuleTypes.DepartmentLeader;

    /// <inheritdoc />
    public override Result Validate(AssigneeRule rule) => RequireCodes(rule);

    /// <inheritdoc />
    protected override Task<IReadOnlyList<Guid>> CandidatesAsync(AssigneeResolution resolution) => organization.DepartmentLeadersAsync(resolution.Activity.Invocation, CodesOf(resolution.Rule));
}

/// <summary>
/// 岗位任职。
/// </summary>
/// <param name="organization">组织目录。</param>
/// <param name="users">用户目录。</param>
public sealed class PositionAssigneeProvider(IOrganizationDirectory organization, IUserDirectory users) : OrganizationAssigneeProvider(users)
{
    /// <inheritdoc />
    public override string RuleType => OrganizationAssigneeRuleTypes.Position;

    /// <inheritdoc />
    public override Result Validate(AssigneeRule rule) => RequireCodes(rule);

    /// <inheritdoc />
    protected override Task<IReadOnlyList<Guid>> CandidatesAsync(AssigneeResolution resolution) => organization.PositionHoldersAsync(resolution.Activity.Invocation, CodesOf(resolution.Rule));
}

/// <summary>
/// 发起人主管类规则的公共部分。
/// </summary>
/// <remarks>
/// 照 4.x 的主管链：从发起人的主部门起逐级上溯，跳过停用的部门、没设负责人的、发起人自己负责的、已经数过的负责人与当前一侧不可见的账号，最多数到要的层级。
/// </remarks>
/// <param name="organization">组织目录。</param>
/// <param name="users">用户目录。</param>
public abstract class StarterLeaderAssigneeProviderBase(IOrganizationDirectory organization, IUserDirectory users) : OrganizationAssigneeProvider(users)
{
    private readonly IUserDirectory _users = users;

    /// <inheritdoc />
    public override Result Validate(AssigneeRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var values = CodesOf(rule);
        return values.Count == 0 || (values.Count == 1 && TryParseLevel(values[0], out _))
            ? Result.Success()
            : Error.Validation(WorkflowErrors.AssigneeRuleInvalid, WorkflowMessages.AssigneeLevelInvalid, MessageArguments.Of(("type", rule.Type), ("value", string.Join(",", values)), ("max", OrganizationAssigneeRuleTypes.MaxLevel)));
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<Guid>> CandidatesAsync(AssigneeResolution resolution)
    {
        if (WorkflowIds.UserOf(resolution.Instance.StarterId) is not { } starter)
        {
            return [];
        }

        var values = CodesOf(resolution.Rule);
        var level = values.Count == 1 && TryParseLevel(values[0], out var parsed) ? parsed : 1;
        var context = resolution.Activity.Invocation;
        var path = await organization.LeaderPathAsync(context, starter).ConfigureAwait(false);
        var leaders = path.Where(static s => s.IsActive && s.LeaderUserId.HasValue).Select(static s => s.LeaderUserId!.Value).ToList();
        var visible = leaders.Count == 0 ? new HashSet<Guid>() : await _users.ActiveInSideAsync(SideOf(resolution), leaders, context.CancellationToken).ConfigureAwait(false);
        var chain = new List<Guid>();
        foreach (var leader in leaders)
        {
            if (chain.Count >= level)
            {
                break;
            }

            if (leader != starter && !chain.Contains(leader) && visible.Contains(leader))
            {
                chain.Add(leader);
            }
        }

        return Pick(chain, level);
    }

    /// <summary>
    /// 从主管链里取受理人。
    /// </summary>
    /// <param name="chain">由近到远的主管链（至多 <paramref name="level"/> 级）。</param>
    /// <param name="level">层级。</param>
    /// <returns>受理人。</returns>
    protected abstract IReadOnlyList<Guid> Pick(IReadOnlyList<Guid> chain, int level);

    private static bool TryParseLevel(string text, out int level)
        => int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out level) && level is >= 1 and <= OrganizationAssigneeRuleTypes.MaxLevel;
}

/// <summary>
/// 发起人第 N 级主管。
/// </summary>
/// <param name="organization">组织目录。</param>
/// <param name="users">用户目录。</param>
public sealed class StarterLeaderAssigneeProvider(IOrganizationDirectory organization, IUserDirectory users) : StarterLeaderAssigneeProviderBase(organization, users)
{
    /// <inheritdoc />
    public override string RuleType => OrganizationAssigneeRuleTypes.StarterLeader;

    /// <inheritdoc />
    protected override IReadOnlyList<Guid> Pick(IReadOnlyList<Guid> chain, int level) => chain.Count >= level ? [chain[level - 1]] : [];
}

/// <summary>
/// 发起人第 1 到 N 级主管链。
/// </summary>
/// <param name="organization">组织目录。</param>
/// <param name="users">用户目录。</param>
public sealed class StarterLeaderChainAssigneeProvider(IOrganizationDirectory organization, IUserDirectory users) : StarterLeaderAssigneeProviderBase(organization, users)
{
    /// <inheritdoc />
    public override string RuleType => OrganizationAssigneeRuleTypes.StarterLeaderChain;

    /// <inheritdoc />
    protected override IReadOnlyList<Guid> Pick(IReadOnlyList<Guid> chain, int level) => chain;
}
