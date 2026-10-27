// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// <c>roles.create</c> 的请求：在当前作用侧（租户主体的租户，平台主体的平台）创建角色。
/// </summary>
/// <param name="Code">角色码：2 到 64 个字符，小写字母开头，只含小写字母、数字、<c>-</c>；创建后不可改。</param>
/// <param name="Name">显示名：1 到 64 个字符。</param>
/// <param name="Description">说明，可空。</param>
/// <param name="Order">排序号。</param>
public sealed record CreateRoleRequest(
    [Required, StringLength(64, MinimumLength = 2), RegularExpression("^[a-z][a-z0-9-]*$")] string Code,
    [Required, StringLength(64, MinimumLength = 1)] string Name,
    [StringLength(256)] string? Description = null,
    [Range(0, 100_000)] int Order = 0);
