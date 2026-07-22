// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统审查详情 DTO
/// </summary>
public sealed class ReviewDetailDto : ReviewListItemDto
{
    /// <summary>
    /// 审查内容
    /// </summary>
    public string? ReviewContent { get; set; }

    /// <summary>
    /// 审查描述
    /// </summary>
    public string? ReviewDescription { get; set; }

    /// <summary>
    /// 业务数据
    /// </summary>
    public string? BusinessData { get; set; }

    /// <summary>
    /// 审查人 ID 列表
    /// </summary>
    public string? ReviewUserIds { get; set; }

    /// <summary>
    /// 附件信息
    /// </summary>
    public string? Attachments { get; set; }

    /// <summary>
    /// 扩展数据
    /// </summary>
    public string? ExtendData { get; set; }

    /// <summary>
    /// 创建者主键
    /// </summary>
    public long? CreatedId { get; set; }

    /// <summary>
    /// 创建者
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 修改者主键
    /// </summary>
    public long? ModifiedId { get; set; }

    /// <summary>
    /// 修改者
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
