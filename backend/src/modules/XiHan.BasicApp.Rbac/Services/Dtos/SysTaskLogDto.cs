#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTaskLogDto
// Guid:1628152c-d6e9-4396-addb-b479254bad97
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;

namespace XiHan.BasicApp.Rbac.Services.Dtos;

/// <summary>
/// 系统任务日志查询 DTO
/// </summary>
public class SysTaskLogGetDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public long TaskId { get; set; }

    /// <summary>
    /// 任务名称
    /// </summary>
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// 任务分组
    /// </summary>
    public string? TaskGroup { get; set; }

    /// <summary>
    /// 执行状态
    /// </summary>
    public string? ExecutionStatus { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// 执行时长（毫秒）
    /// </summary>
    public long ExecutionTime { get; set; } = 0;

    /// <summary>
    /// 执行结果
    /// </summary>
    public string? ExecutionResult { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 异常堆栈
    /// </summary>
    public string? ExceptionStackTrace { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
