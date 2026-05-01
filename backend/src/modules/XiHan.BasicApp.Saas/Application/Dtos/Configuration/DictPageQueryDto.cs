#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictPageQueryDto
// Guid:d912c39c-f2c0-493d-807a-fdc17da6189e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统字典分页查询 DTO
/// </summary>
public sealed class DictPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 字典编码
    /// </summary>
    public string? DictCode { get; set; }

    /// <summary>
    /// 字典类型
    /// </summary>
    public string? DictType { get; set; }

    /// <summary>
    /// 是否内置
    /// </summary>
    public bool? IsBuiltIn { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus? Status { get; set; }
}
