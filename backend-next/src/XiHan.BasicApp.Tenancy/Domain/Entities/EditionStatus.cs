// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Tenancy.Domain.Entities;

/// <summary>
/// 套餐的启停状态
/// </summary>
public enum EditionStatus
{
    /// <summary>
    /// 启用
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用：不能再分配给租户，已绑定的租户不受影响
    /// </summary>
    Disabled = 1,
}
