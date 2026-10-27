// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Organization.Domain.Entities;

/// <summary>
/// 部门 / 岗位的启停状态。
/// </summary>
public enum OrganizationStatus
{
    /// <summary>
    /// 启用。
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用：仍可查看与归属，但新建归属时前端不再推荐。
    /// </summary>
    Disabled = 1,
}
