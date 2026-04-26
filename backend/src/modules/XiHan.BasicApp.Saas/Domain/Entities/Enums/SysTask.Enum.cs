#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTask.Enum
// Guid:66b68f52-95ef-48a2-8b40-0c18dd6aba20
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 任务状态枚举
/// </summary>
public enum RunTaskStatus
{
    /// <summary>
    /// 待执行
    /// </summary>
    [Description("待执行")]
    Pending = 0,

    /// <summary>
    /// 执行中
    /// </summary>
    [Description("执行中")]
    Running = 1,

    /// <summary>
    /// 执行成功
    /// </summary>
    [Description("执行成功")]
    Success = 2,

    /// <summary>
    /// 执行失败
    /// </summary>
    [Description("执行失败")]
    Failed = 3,

    /// <summary>
    /// 已停止
    /// </summary>
    [Description("已停止")]
    Stopped = 4,

    /// <summary>
    /// 已暂停
    /// </summary>
    [Description("已暂停")]
    Paused = 5
}

/// <summary>
/// 任务触发类型枚举
/// </summary>
public enum TriggerType
{
    /// <summary>
    /// 立即执行
    /// </summary>
    [Description("立即执行")]
    Immediate = 0,

    /// <summary>
    /// 定时执行
    /// </summary>
    [Description("定时执行")]
    Schedule = 1,

    /// <summary>
    /// 循环执行
    /// </summary>
    [Description("循环执行")]
    Recurring = 2,

    /// <summary>
    /// Cron表达式
    /// </summary>
    [Description("Cron表达式")]
    Cron = 3
}
