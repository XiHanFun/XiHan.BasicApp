#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RunTaskStatus
// Guid:ed28152c-d6e9-4396-addb-b479254bad34
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 4:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

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
