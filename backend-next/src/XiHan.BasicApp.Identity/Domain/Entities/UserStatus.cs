// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 用户状态。JSON 上是字符串枚举。
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// 正常。
    /// </summary>
    Active = 0,

    /// <summary>
    /// 已禁用，不能登录。
    /// </summary>
    Disabled = 1,

    /// <summary>
    /// 已锁定（安全策略触发），待解锁。
    /// </summary>
    Locked = 2,
}
