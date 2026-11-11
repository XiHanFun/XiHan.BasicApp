// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Operations.Roles;
using XiHan.BasicApp.Contracts.Users;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 导入用户时指派角色
/// </summary>
/// <remarks>
/// 导入行的 <c>roleCodes</c>。导入人要有 <c>access.roles.assign</c>，缺了整批拒绝；有角色码在当前一侧不存在、或新成员会触犯拒绝类或需审批类约束规则（静态职责分离、基数与条件）的行记为失败，
/// 需审批的不在导入里转申请，提示导入后到用户页单独指派。预检只判定不记日志，写入经 <see cref="RoleAssignmentService"/>，与「给角色指派成员」同一套规则、同样记权限变更日志
/// </remarks>
/// <param name="roles">角色域服务</param>
/// <param name="assignments">角色指派</param>
/// <param name="enforcer">约束规则求值</param>
/// <param name="permissions">权限判定</param>
public sealed class RoleUserImport(RoleService roles, RoleAssignmentService assignments, ConstraintEnforcer enforcer, IPermissionChecker permissions) : IUserImportExtension
{
    private const string Field = "roleCodes";

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UserImportIssue>>> ValidateAsync(IReadOnlyList<UserImportLine> lines, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(context);
        var wanted = Wanted(lines);
        if (wanted.Count == 0)
        {
            return Array.Empty<UserImportIssue>();
        }

        var held = await permissions.HasPermissionAsync(context.GetSecurity(), AccessPermissions.RolesAssign, context.CancellationToken).ConfigureAwait(false);
        if (!held.TryGetValue(out var granted))
        {
            return held.Error!;
        }

        if (!granted)
        {
            return Error.Denied(SecurityErrors.PermissionMissing, SecurityMessages.PermissionMissing, MessageArguments.Of(("permission", AccessPermissions.RolesAssign)));
        }

        var owner = RoleService.OwnerOf(context);
        var found = await ResolveAsync(owner, wanted, context.CancellationToken).ConfigureAwait(false);
        var issues = new List<UserImportIssue>();
        var members = new Dictionary<Guid, int>();
        foreach (var line in wanted)
        {
            var missing = line.RoleCodes.Where(c => !found.ContainsKey(c)).ToList();
            if (missing.Count > 0)
            {
                issues.AddRange(missing.Select(c => new UserImportIssue(line.Index, Field, Error.NotFound(AccessErrors.RoleNotFound, AccessMessages.RoleNotFound, MessageArguments.Of(("id", c))))));
                continue;
            }

            // 新用户此前没有角色：变更之后持有的就是这一行要指派的；角色的成员数算上这批里排在前面的
            var roleIds = line.RoleCodes.Select(c => found[c].Id).ToList();
            var violations = new List<ConstraintViolation>(await enforcer.CheckUserRolesAsync(owner, line.UserId, line.UserName, roleIds, roleIds, context).ConfigureAwait(false));
            foreach (var roleId in roleIds)
            {
                if (!members.TryGetValue(roleId, out var count))
                {
                    count = await enforcer.MemberCountAsync(owner, roleId, context.CancellationToken).ConfigureAwait(false);
                }

                violations.AddRange(await enforcer.CheckRoleMembersAsync(owner, roleId, count + 1, context.CancellationToken).ConfigureAwait(false));
                members[roleId] = count;
            }

            // 需审批的规则在导入里不转申请（用户还不存在、一批里逐个发起也不便审），提示导入后单独指派
            if (ConstraintEnforcer.Blocking(violations) is { } blocked)
            {
                issues.Add(new UserImportIssue(line.Index, Field, blocked.Code == AccessErrors.ConstraintApprovalRequired
                    ? Error.Conflict(AccessErrors.ConstraintApprovalRequired, AccessMessages.ConstraintApprovalImport, blocked.Details)
                    : blocked));
                continue;
            }

            foreach (var roleId in roleIds)
            {
                members[roleId] += 1;
            }
        }

        return issues;
    }

    /// <inheritdoc />
    public async Task<Result> ApplyAsync(IReadOnlyList<UserImportLine> lines, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(context);
        var wanted = Wanted(lines);
        if (wanted.Count == 0)
        {
            return Result.Success();
        }

        var found = await ResolveAsync(RoleService.OwnerOf(context), wanted, context.CancellationToken).ConfigureAwait(false);
        var names = wanted.ToDictionary(static l => l.UserId, static l => l.UserName);
        foreach (var code in wanted.SelectMany(static l => l.RoleCodes).Distinct(StringComparer.Ordinal))
        {
            if (!found.TryGetValue(code, out var role))
            {
                return RoleImportFailed(code, wanted.First(l => l.RoleCodes.Contains(code)).UserName, context.Localize(AccessMessages.RoleNotFound, MessageArguments.Of(("id", code))));
            }

            var userIds = wanted.Where(l => l.RoleCodes.Contains(code)).Select(static l => l.UserId).ToList();
            var assigned = await assignments.AssignAsync(new RoleMembersRequest(role.Id, userIds, null, null), context).ConfigureAwait(false);
            if (!assigned.TryGetValue(out var result))
            {
                return assigned.Error!;
            }

            if (result.Batch.Failed.Count > 0)
            {
                var failure = result.Batch.Failed[0];
                return RoleImportFailed(code, names.GetValueOrDefault(failure.Id, failure.Id.ToString()), failure.Message);
            }
        }

        return Result.Success();
    }

    private static List<UserImportLine> Wanted(IReadOnlyList<UserImportLine> lines) => lines.Where(static l => l.RoleCodes.Count > 0).ToList();

    private static Error RoleImportFailed(string role, string userName, string reason)
        => Error.Conflict(AccessErrors.RoleImportFailed, AccessMessages.RoleImportFailed, MessageArguments.Of(("role", role), ("userName", userName), ("reason", reason)));

    private async Task<Dictionary<string, SysRole>> ResolveAsync(string owner, IReadOnlyList<UserImportLine> wanted, CancellationToken cancellationToken)
    {
        var codes = wanted.SelectMany(static l => l.RoleCodes).Distinct(StringComparer.Ordinal).ToList();
        var rows = await roles.FindByCodesAsync(owner, codes, cancellationToken).ConfigureAwait(false);
        return rows.ToDictionary(static r => r.Code, StringComparer.Ordinal);
    }
}
