#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserPageQueryDto
// Guid:a9a35b7f-19a6-4698-bf1c-2dbbd9584b9f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户分页查询 DTO
/// </summary>
public sealed class UserPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（用户名、真实姓名、昵称、国家/地区、备注）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    public UserGender? Gender { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus? Status { get; set; }

    /// <summary>
    /// 是否系统内置账号
    /// </summary>
    public bool? IsSystemAccount { get; set; }

    /// <summary>
    /// 语言
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// 国家/地区
    /// </summary>
    public string? Country { get; set; }
}
