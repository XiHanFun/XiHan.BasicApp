#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskQueryService
// Guid:4a5b6c7d-8e9f-4123-def0-420000000002
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Saas.Constants.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 任务查询服务
/// </summary>
public class TaskQueryService : ITaskQueryService, ITransientDependency
{
    private readonly ITaskRepository _taskRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskQueryService(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    /// <inheritdoc />
    [Cacheable(Key = QueryCacheKeys.TaskById, ExpireSeconds = 300)]
    public async Task<TaskDto?> GetByIdAsync(long id)
    {
        var entity = await _taskRepository.GetByIdAsync(id);
        return entity?.Adapt<TaskDto>();
    }
}
