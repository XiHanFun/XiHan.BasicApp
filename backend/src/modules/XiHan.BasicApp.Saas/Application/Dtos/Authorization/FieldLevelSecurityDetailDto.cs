// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 字段级安全详情 DTO
/// </summary>
public sealed class FieldLevelSecurityDetailDto : FieldLevelSecurityListItemDto
{
    /// <summary>
    /// 创建人ID
    /// </summary>
    public long? CreatedId { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 修改人ID
    /// </summary>
    public long? ModifiedId { get; set; }

    /// <summary>
    /// 修改人
    /// </summary>
    public string? ModifiedBy { get; set; }
}
