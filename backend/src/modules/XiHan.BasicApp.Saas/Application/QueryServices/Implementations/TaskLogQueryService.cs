#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskLogQueryService
// Guid:5a6b7c8d-9e0f-4234-ef01-520000000002
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 任务日志查询服务
/// </summary>
public class TaskLogQueryService : ITaskLogQueryService, ITransientDependency
{
    private readonly ITaskLogRepository _taskLogRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskLogQueryService(ITaskLogRepository taskLogRepository)
    {
        _taskLogRepository = taskLogRepository;
    }

    /// <inheritdoc />
    public async Task<TaskLogDto?> GetByIdAsync(long id)
    {
        var entity = await _taskLogRepository.GetByIdAsync(id);
        return entity?.Adapt<TaskLogDto>();
    }
}
