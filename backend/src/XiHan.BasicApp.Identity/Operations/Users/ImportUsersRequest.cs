// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Identity.Domain.Entities;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// <c>users.import</c> 的请求
/// </summary>
/// <remarks>
/// 行的字段不在请求 Schema 上约束：每一行按 <c>users.create</c> 的请求契约单独校验，不合格的行记为失败，其余行照常导入
/// </remarks>
/// <param name="Rows">要导入的用户，一次至多 200 行</param>
/// <param name="Password">这批用户共用的初始密码，账号标记为须改密</param>
public sealed record ImportUsersRequest(
    [Required, MinLength(1), MaxLength(200)] IReadOnlyList<ImportUserRow> Rows,
    [Required, StringLength(128, MinimumLength = 8)] string Password);

/// <summary>
/// 导入的一行
/// </summary>
/// <remarks>
/// 前九个字段与 <c>users.create</c> 的同名字段同一套规则
/// </remarks>
/// <param name="UserName">登录名</param>
/// <param name="DisplayName">显示名</param>
/// <param name="Email">邮箱，可空</param>
/// <param name="PhoneNumber">手机号，可空</param>
/// <param name="RealName">真实姓名，可空</param>
/// <param name="Gender">性别；不填即未设置</param>
/// <param name="Birthday">生日，可空</param>
/// <param name="Country">国家或地区，可空</param>
/// <param name="Remark">备注，可空</param>
/// <param name="DepartmentCode">要加入的部门编码，成为主部门；部门只在租户侧，导入人要有加入部门的权限</param>
/// <param name="RoleCodes">要指派的角色码；导入人要有指派角色的权限，新成员照常过约束规则</param>
public sealed record ImportUserRow(
    string? UserName = null,
    string? DisplayName = null,
    string? Email = null,
    string? PhoneNumber = null,
    string? RealName = null,
    UserGender? Gender = null,
    DateTimeOffset? Birthday = null,
    string? Country = null,
    string? Remark = null,
    string? DepartmentCode = null,
    IReadOnlyList<string>? RoleCodes = null);

/// <summary>
/// <c>users.import</c> 的响应
/// </summary>
/// <param name="Succeeded">导入成功的行</param>
/// <param name="Failed">不能导入的行与原因，按行序；同一行可能有多条</param>
public sealed record ImportUsersResponse(IReadOnlyList<ImportedUser> Succeeded, IReadOnlyList<ImportUserFailure> Failed);

/// <summary>
/// 导入成功的一行
/// </summary>
/// <param name="Index">行在请求里的序号（从 0 开始）</param>
/// <param name="Id">新用户的 ID</param>
public sealed record ImportedUser(int Index, Guid Id);

/// <summary>
/// 不能导入的一行
/// </summary>
/// <param name="Index">行在请求里的序号（从 0 开始）</param>
/// <param name="Field">出问题的字段（行里的字段名）；整行的问题为空</param>
/// <param name="Code">错误码</param>
/// <param name="Message">按请求语言本地化的说明</param>
public sealed record ImportUserFailure(int Index, string? Field, string Code, string Message);
