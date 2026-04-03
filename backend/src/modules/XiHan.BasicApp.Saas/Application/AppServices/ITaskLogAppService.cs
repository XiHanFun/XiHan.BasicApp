#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITaskLogAppService
// Guid:a1b2c3d4-0002-0003-0001-000000000001
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/04 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 任务调度日志应用服务接口
/// </summary>
public interface ITaskLogAppService : IApplicationService
{
    /// <summary>
    /// 分页查询
    /// </summary>
    Task<PageResultDtoBase<TaskLogDto>> PageAsync(BasicAppPRDto input);

    /// <summary>
    /// 清空日志
    /// </summary>
    Task<bool> ClearAsync();
}
