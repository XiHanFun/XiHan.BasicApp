// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
