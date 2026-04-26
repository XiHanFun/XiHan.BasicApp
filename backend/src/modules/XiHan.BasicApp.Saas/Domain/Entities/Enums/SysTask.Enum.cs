#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTask.Enum
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 任务触发类型枚举
/// </summary>
public enum TriggerType
{
    /// <summary>
    /// 立即执行
    /// </summary>
    Immediate = 0,

    /// <summary>
    /// 定时执行
    /// </summary>
    Schedule = 1,

    /// <summary>
    /// 循环执行
    /// </summary>
    Recurring = 2,

    /// <summary>
    /// Cron表达式
    /// </summary>
    Cron = 3
}

/// <summary>
/// 任务状态枚举
/// </summary>
public enum RunTaskStatus
{
    /// <summary>
    /// 待执行
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 执行中
    /// </summary>
    Running = 1,

    /// <summary>
    /// 执行成功
    /// </summary>
    Success = 2,

    /// <summary>
    /// 执行失败
    /// </summary>
    Failed = 3,

    /// <summary>
    /// 已停止
    /// </summary>
    Stopped = 4,

    /// <summary>
    /// 已暂停
    /// </summary>
    Paused = 5
}

