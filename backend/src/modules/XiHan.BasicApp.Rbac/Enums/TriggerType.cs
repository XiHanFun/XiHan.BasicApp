#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TriggerType
// Guid:ed28152c-d6e9-4396-addb-b479254bad34
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 04:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

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
