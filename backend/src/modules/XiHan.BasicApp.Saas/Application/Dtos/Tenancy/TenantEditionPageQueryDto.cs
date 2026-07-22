// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户版本分页查询 DTO
/// </summary>
public sealed class TenantEditionPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（版本编码、名称、描述）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus? Status { get; set; }

    /// <summary>
    /// 是否免费版本
    /// </summary>
    public bool? IsFree { get; set; }

    /// <summary>
    /// 是否默认版本
    /// </summary>
    public bool? IsDefault { get; set; }
}
