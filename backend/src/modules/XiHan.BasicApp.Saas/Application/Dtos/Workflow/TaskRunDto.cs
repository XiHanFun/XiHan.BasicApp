// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统任务立即执行 DTO
/// </summary>
public sealed class TaskRunDto
{
    /// <summary>
    /// 任务主键
    /// </summary>
    public long BasicId { get; set; }
}

/// <summary>
/// 系统任务立即执行结果 DTO
/// </summary>
public sealed class TaskRunResultDto
{
    /// <summary>
    /// 本次执行实例标识（可据此关联任务日志批次号）
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;
}
