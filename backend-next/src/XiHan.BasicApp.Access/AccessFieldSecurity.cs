// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Operations.ConstraintRules;
using XiHan.BasicApp.Access.Operations.Permissions;
using XiHan.BasicApp.Access.Operations.Policies;
using XiHan.BasicApp.Access.Operations.Roles;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Runtime.Queries;

namespace XiHan.BasicApp.Access;

/// <summary>
/// 授权包登记的字段安全实体。
/// </summary>
/// <remarks>
/// 4.x 同四个实体：角色、权限目录、约束规则与访问策略。字段由响应类型生成，规则由本包的字段安全中间件在读、建、改处统一套用。
/// </remarks>
public static class AccessFieldSecurity
{
    /// <summary>
    /// 登记本包的实体与操作。
    /// </summary>
    /// <param name="services">服务集合。</param>
    public static void Register(IServiceCollection services)
        => services.AddFieldSecurity("access", AccessJsonContext.Default, fields => fields
            .Entity<RoleResponse>("access.roles", "access.roles.title", "access.roles.fields.")
                .Reads(ListRoles.Id)
                .ReadsAt(GetRole.Id, "role")
                .Creates(CreateRole.Id)
                .Updates<UpdateRoleRequest>(UpdateRole.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysRole>(context, request.Id, RoleResponse.From))
                .Keyword("code", "name")
            .Entity<PermissionResponse>("access.permissions", "access.permissions.title", "access.permissions.fields.")
                .ReadsAt(ListPermissions.Id, "items")
            .Entity<ConstraintRuleResponse>("access.constraint-rules", "access.constraint_rules.title", "access.constraint_rules.fields.")
                .Reads(ListConstraintRules.Id, GetConstraintRule.Id)
                .Creates(CreateConstraintRule.Id)
                .Updates<UpdateConstraintRuleRequest>(UpdateConstraintRule.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysConstraintRule>(context, request.Id, static async (rule, ctx) => await ctx.Services.GetRequiredService<ConstraintRuleProjection>().ToAsync(rule, ctx.CancellationToken).ConfigureAwait(false)))
                .Keyword("code", "name", "description", "remark")
            .Entity<AccessPolicyResponse>("access.policies", "access.policies.title", "access.policies.fields.")
                .Reads(ListAccessPolicies.Id, GetAccessPolicy.Id)
                .Creates(CreateAccessPolicy.Id)
                .Updates<UpdateAccessPolicyRequest>(UpdateAccessPolicy.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysAccessPolicy>(context, request.Id, static async (policy, ctx) => await ctx.Services.GetRequiredService<AccessPolicyProjection>().ToAsync(policy, ctx.CancellationToken).ConfigureAwait(false)))
                .Keyword("code", "name", "description"));
}
