// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
