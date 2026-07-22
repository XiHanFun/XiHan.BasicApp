// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统任务详情 DTO
/// </summary>
public sealed class TaskDetailDto : TaskListItemDto
{
    /// <summary>
    /// 任务类名
    /// </summary>
    public string TaskClass { get; set; } = string.Empty;

    /// <summary>
    /// 任务方法
    /// </summary>
    public string? TaskMethod { get; set; }

    /// <summary>
    /// 任务参数
    /// </summary>
    public string? TaskParams { get; set; }

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
