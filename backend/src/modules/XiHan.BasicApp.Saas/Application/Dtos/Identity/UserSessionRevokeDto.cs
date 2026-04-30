#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSessionRevokeDto
// Guid:7d747b21-4fd6-449e-90f4-9955fd074a3d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户会话撤销 DTO
/// </summary>
public sealed class UserSessionRevokeDto
{
    /// <summary>
    /// 会话主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 撤销原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
