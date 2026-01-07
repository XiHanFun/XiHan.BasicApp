#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OperationLogQueryService
// Guid:a3b4c5d6-e7f8-4a9b-0c1d-2e3f4a5b6c7d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Application.Queries;

/// <summary>
/// 操作日志查询服务（处理操作日志的读操作）
/// </summary>
public class OperationLogQueryService : ApplicationServiceBase
{
    private readonly IOperationLogRepository _operationLogRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OperationLogQueryService(IOperationLogRepository operationLogRepository)
    {
        _operationLogRepository = operationLogRepository;
    }

    /// <summary>
    /// 根据ID获取操作日志
    /// </summary>
    public async Task<RbacDtoBase?> GetByIdAsync(long id)
    {
        var log = await _operationLogRepository.GetByIdAsync(id);
        return log?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据用户ID获取操作日志列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByUserIdAsync(long userId)
    {
        var logs = await _operationLogRepository.GetByUserIdAsync(userId);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 根据操作类型获取日志列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByOperationTypeAsync(string operationType)
    {
        var logs = await _operationLogRepository.GetByOperationTypeAsync(operationType);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 根据模块名称获取日志列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByModuleNameAsync(string moduleName)
    {
        var logs = await _operationLogRepository.GetByModuleNameAsync(moduleName);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 根据时间范围获取日志列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var logs = await _operationLogRepository.GetByTimeRangeAsync(startTime, endTime);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 根据操作结果获取日志列表（成功/失败）
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByOperationResultAsync(bool isSuccess)
    {
        var logs = await _operationLogRepository.GetByOperationResultAsync(isSuccess);
        return logs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    public async Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input)
    {
        var result = await _operationLogRepository.GetPagedAsync(input);
        var dtos = result.Items.Adapt<List<RbacDtoBase>>();
        return new PageResponse<RbacDtoBase>(dtos, result.PageData);
    }
}
