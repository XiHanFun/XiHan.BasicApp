#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskRunDto
// Guid:d8e4b6f2-1a73-4c95-8b0d-5f2e9c7a3d61
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
