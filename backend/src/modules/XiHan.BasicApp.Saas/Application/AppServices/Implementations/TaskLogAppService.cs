#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskLogAppService
// Guid:dcc694a8-5a62-4d38-a054-29cb97182a29
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/04 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 任务调度日志应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
public class TaskLogAppService : ApplicationServiceBase, ITaskLogAppService
{
    private readonly ITaskLogRepository _taskLogRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskLogAppService(ITaskLogRepository taskLogRepository)
    {
        _taskLogRepository = taskLogRepository;
    }

    /// <summary>
    /// 分页查询
    /// </summary>
    [HttpPost]
    public async Task<PageResultDtoBase<TaskLogDto>> PageAsync(BasicAppPRDto input)
    {
        var pageData = await _taskLogRepository.GetPagedAsync(input);

        var dtos = pageData.Adapt<PageResultDtoBase<TaskLogDto>>();
        return dtos;
    }

    /// <summary>
    /// 清空日志
    /// </summary>
    [HttpDelete]
    public async Task<bool> ClearAsync()
    {
        return await _taskLogRepository.ClearAsync();
    }
}
