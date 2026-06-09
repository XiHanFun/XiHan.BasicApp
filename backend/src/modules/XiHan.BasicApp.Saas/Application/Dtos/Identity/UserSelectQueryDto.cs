#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSelectQueryDto
// Guid:ca9ee910-0520-4e20-9168-ea2b8d27c55a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户选择器查询 DTO
/// </summary>
public sealed class UserSelectQueryDto
{
    /// <summary>
    /// 关键字（用户名、真实姓名、昵称）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    public UserGender? Gender { get; set; }

    /// <summary>
    /// 是否系统内置账号
    /// </summary>
    public bool? IsSystemAccount { get; set; }

    /// <summary>
    /// 返回数量上限
    /// </summary>
    public int Limit { get; set; } = 100;
}
