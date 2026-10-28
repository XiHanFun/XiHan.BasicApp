// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Tenancy.Operations.Editions;

/// <summary>
/// <c>editions.list</c> 的请求：没有输入，套餐数量有限，一次全列。
/// </summary>
public sealed record ListEditionsRequest;

/// <summary>
/// <c>editions.get</c> 的请求：标识由路由值 <c>{id}</c> 绑定。
/// </summary>
/// <param name="Id">套餐主键。</param>
public sealed record GetEditionRequest(Guid Id);

/// <summary>
/// <c>editions.create</c> 的请求：新建套餐并给出初始白名单。
/// </summary>
/// <param name="Code">编码：小写字母开头，只含小写字母、数字与 <c>-</c>，2 到 32 个字符；创建后不可改。</param>
/// <param name="Name">名称：1 到 64 个字符。</param>
/// <param name="Description">说明，可空。</param>
/// <param name="IsDefault">是否设为新租户的缺省套餐（其余套餐的缺省标记随之清掉）。</param>
/// <param name="Order">排序号。</param>
/// <param name="Permissions">白名单：租户能生效的权限码（作用侧 Tenant / Both），至多 500 个；缺省为空（什么都不放行）。</param>
public sealed record CreateEditionRequest(
    [Required, StringLength(32, MinimumLength = 2), RegularExpression("^[a-z][a-z0-9-]{1,31}$")] string Code,
    [Required, StringLength(64, MinimumLength = 1)] string Name,
    [StringLength(512)] string? Description = null,
    bool IsDefault = false,
    [Range(0, 100_000)] int Order = 0,
    [MaxLength(500)] IReadOnlyList<string>? Permissions = null);

/// <summary>
/// <c>editions.update</c> 的请求：改名称、说明、缺省标记与排序（编码不可改，白名单走 <c>editions.set-permissions</c>）。标识由路由值 <c>{id}</c> 绑定。
/// </summary>
/// <param name="Id">套餐主键。</param>
/// <param name="Name">名称。</param>
/// <param name="Description">说明，可空。</param>
/// <param name="IsDefault">是否新租户的缺省套餐；取消后可以没有缺省套餐（开通租户时就必须指定）。</param>
/// <param name="Order">排序号。</param>
public sealed record UpdateEditionRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string Name,
    [StringLength(512)] string? Description = null,
    bool IsDefault = false,
    [Range(0, 100_000)] int Order = 0);

/// <summary>
/// <c>editions.set-permissions</c> 的请求：整体替换白名单。标识由路由值 <c>{id}</c> 绑定。
/// </summary>
/// <param name="Id">套餐主键。</param>
/// <param name="Permissions">白名单：租户能生效的权限码，0 到 500 个；空即什么都不放行。</param>
public sealed record SetEditionPermissionsRequest(Guid Id, [Required, MaxLength(500)] IReadOnlyList<string> Permissions);

/// <summary>
/// <c>editions.delete</c> 的请求：集合入参。
/// </summary>
/// <param name="Ids">套餐主键，1 到 100 个。</param>
public sealed record EditionIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
