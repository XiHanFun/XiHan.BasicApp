// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS1591

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 岗位创建 DTO
/// </summary>
public sealed class PositionCreateDto
{
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public int Sort { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// 岗位更新 DTO
/// </summary>
public sealed class PositionUpdateDto : BasicAppUDto
{
    public string PositionName { get; set; } = string.Empty;
    public int Sort { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// 岗位状态更新 DTO
/// </summary>
public sealed class PositionStatusUpdateDto : BasicAppDto
{
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

/// <summary>
/// 岗位列表项 DTO
/// </summary>
public class PositionListItemDto : BasicAppDto
{
    /// <summary>
    /// 岗位编码
    /// </summary>
    public string PositionCode { get; set; } = string.Empty;

    /// <summary>
    /// 岗位名称
    /// </summary>
    public string PositionName { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}

/// <summary>
/// 岗位详情 DTO
/// </summary>
public sealed class PositionDetailDto : PositionListItemDto
{
    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

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
}

/// <summary>
/// 岗位分页查询 DTO
/// </summary>
public sealed class PositionPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（岗位编码、名称、备注）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus? Status { get; set; }
}
