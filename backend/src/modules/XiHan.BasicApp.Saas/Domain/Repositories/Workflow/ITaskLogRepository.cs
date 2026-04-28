#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITaskLogRepository
// Guid:0cda5769-d0e6-44b3-a723-7c3eb3d933ec
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 任务日志仓储接口
/// </summary>
public interface ITaskLogRepository : ISaasSplitRepository<SysTaskLog>
{
}
