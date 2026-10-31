// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Operations.Sessions;
using XiHan.BasicApp.Identity.Operations.Users;
using XiHan.BasicApp.Runtime.Queries;

namespace XiHan.BasicApp.Identity;

/// <summary>
/// 身份包登记的字段安全实体。
/// </summary>
/// <remarks>
/// 见方案 5.9，4.x 同两个实体：用户目录与在线会话。字段由响应类型生成，规则由 Access 的字段安全中间件在读、建、改处统一套用；
/// 用户目录的关键字只匹配没受保护的登录名与显示名（本包收窄），登录、刷新、个人中心等本人自己的资料不套用。
/// </remarks>
public static class IdentityFieldSecurity
{
    /// <summary>
    /// 用户实体码。
    /// </summary>
    public const string Users = "identity.users";

    /// <summary>
    /// 在线会话实体码。
    /// </summary>
    public const string Sessions = "identity.sessions";

    /// <summary>
    /// 登录名。
    /// </summary>
    public const string UserName = "userName";

    /// <summary>
    /// 显示名。
    /// </summary>
    public const string DisplayName = "displayName";

    /// <summary>
    /// 邮箱。
    /// </summary>
    public const string Email = "email";

    /// <summary>
    /// 登记本包的实体与操作。
    /// </summary>
    /// <remarks>
    /// 显示名沿用前端用户页与在线用户页已有的文案键。
    /// </remarks>
    /// <param name="services">服务集合。</param>
    public static void Register(IServiceCollection services)
        => services.AddFieldSecurity("identity", IdentityJsonContext.Default, fields => fields
            .Entity<UserResponse>(Users, "identity.users.title", "identity.users.fields.")
                .Title("phoneNumber", "identity.users.fields.phone")
                .Reads(ListUsers.Id, GetUser.Id)
                .Creates(CreateUser.Id)
                .Updates<UpdateUserRequest>(UpdateUser.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysUser>(context, request.Id, UserResponse.From))
            .Entity<OnlineSessionResponse>(Sessions, "identity.online_users.title", "identity.online_users.col_")
                .Reads(ListSessions.Id)
                .Keyword(UserName, DisplayName));
}
