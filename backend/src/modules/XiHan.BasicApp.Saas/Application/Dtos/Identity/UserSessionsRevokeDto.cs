// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户全部会话撤销 DTO
/// </summary>
public sealed class UserSessionsRevokeDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 撤销原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
