#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserTwoFactorResetDto
// Guid:6f2b8c1d-4e7a-4b9c-8d3e-1a5f7c9b2e4d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户双因素认证重置 DTO（清除 OTP 绑定）
/// </summary>
public sealed class UserTwoFactorResetDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
