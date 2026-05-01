#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOperationLogQueryService
// Guid:ac36f4f5-f488-4e7e-99e3-cfb40c9b959e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 操作日志查询应用服务接口
/// </summary>
public interface IOperationLogQueryService : IApplicationService
{
    /// <summary>
    /// 获取操作日志分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作日志分页列表</returns>
    Task<PageResultDtoBase<OperationLogListItemDto>> GetOperationLogPageAsync(OperationLogPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取操作日志详情
    /// </summary>
    /// <param name="id">操作日志主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作日志详情</returns>
    Task<OperationLogDetailDto?> GetOperationLogDetailAsync(long id, CancellationToken cancellationToken = default);
}
