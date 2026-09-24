// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 成员在本租户的数据范围设置（与设置 DTO 同形，供编辑回显）
/// </summary>
public sealed class UserDataScopeSettingDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 覆盖档位（null 表示跟随角色）
    /// </summary>
    public DataPermissionScope? DataScope { get; set; }

    /// <summary>
    /// 当前生效的自定义部门
    /// </summary>
    public List<UserDataScopeListItemDto> Departments { get; set; } = [];
}
