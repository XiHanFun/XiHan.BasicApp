#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictItemPageQueryDto
// Guid:896798d1-d6f7-4a41-9848-553a667c4429
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统字典项分页查询 DTO
/// </summary>
public sealed class DictItemPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 字典主键
    /// </summary>
    public long? DictId { get; set; }

    /// <summary>
    /// 父级字典项主键
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 字典项编码
    /// </summary>
    public string? ItemCode { get; set; }

    /// <summary>
    /// 是否默认
    /// </summary>
    public bool? IsDefault { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus? Status { get; set; }
}
