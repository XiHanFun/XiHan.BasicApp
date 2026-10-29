// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// <c>tenants.initialize-admin</c> 的请求：给租户建第一个管理员。
/// </summary>
/// <remarks>
/// 标识由路由值 <c>{id}</c> 绑定。
/// </remarks>
/// <param name="Id">租户主键。</param>
/// <param name="UserName">管理员登录名：3 到 64 个字符，小写字母开头，只含小写字母、数字、<c>.</c>、<c>_</c>、<c>-</c>。</param>
/// <param name="Password">初始密码（策略：最短 8 位、含字母与数字）；管理员首次登录必须改密。</param>
/// <param name="DisplayName">显示名；不填用登录名。</param>
/// <param name="Email">邮箱，可空。</param>
public sealed record InitializeAdminRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 3), RegularExpression("^[a-z][a-z0-9._-]*$")] string UserName,
    [Required, StringLength(128, MinimumLength = 8)] string Password,
    [StringLength(64)] string? DisplayName = null,
    [StringLength(256)] string? Email = null);
