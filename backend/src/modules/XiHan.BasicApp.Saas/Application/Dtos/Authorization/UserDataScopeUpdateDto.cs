// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户数据范围更新 DTO
/// </summary>
public sealed class UserDataScopeUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 是否包含子部门（部门由绑定记录决定，更新时不变更部门）
    /// </summary>
    public bool IncludeChildren { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
