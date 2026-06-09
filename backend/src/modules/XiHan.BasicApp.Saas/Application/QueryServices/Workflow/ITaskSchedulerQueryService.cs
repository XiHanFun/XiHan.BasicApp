#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITaskSchedulerQueryService
// Guid:274ac10c-6370-485c-8885-830e170c586d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 任务调度查询服务
/// </summary>
public interface ITaskSchedulerQueryService
{
    /// <summary>
    /// 获取启用的任务
    /// </summary>
    Task<IReadOnlyList<SysTask>> GetEnabledTasksAsync(CancellationToken cancellationToken = default);
}
