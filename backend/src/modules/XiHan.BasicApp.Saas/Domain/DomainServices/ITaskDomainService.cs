#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITaskDomainService
// Guid:4a5b6c7d-8e9f-4123-def0-430000000003
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 任务领域服务接口
/// </summary>
public interface ITaskDomainService
{
    /// <summary>
    /// 创建任务
    /// </summary>
    Task<SysTask> CreateAsync(SysTask task);

    /// <summary>
    /// 更新任务
    /// </summary>
    Task<SysTask> UpdateAsync(SysTask task);

    /// <summary>
    /// 删除任务
    /// </summary>
    Task<bool> DeleteAsync(long id);
}
