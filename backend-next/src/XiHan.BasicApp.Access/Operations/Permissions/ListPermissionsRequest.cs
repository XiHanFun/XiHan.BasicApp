// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Access.Operations.Permissions;

/// <summary>
/// <c>permissions.list</c> 的请求：目录缺省按调用方的作用侧返回。
/// </summary>
/// <param name="Side">要看的作用侧：<c>platform</c> 或 <c>tenant</c>；不填即调用方所在的一侧。平台主体可以看租户侧（编辑套餐白名单用），租户主体只能看租户侧。</param>
public sealed record ListPermissionsRequest([RegularExpression("^(platform|tenant)$")] string? Side = null);
