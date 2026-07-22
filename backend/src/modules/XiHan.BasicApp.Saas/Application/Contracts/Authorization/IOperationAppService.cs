// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 操作定义命令应用服务接口
/// </summary>
public interface IOperationAppService : IApplicationService
{
    /// <summary>
    /// 创建操作定义
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作详情</returns>
    Task<OperationDetailDto> CreateOperationAsync(OperationCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新操作定义
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作详情</returns>
    Task<OperationDetailDto> UpdateOperationAsync(OperationUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新操作定义状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作详情</returns>
    Task<OperationDetailDto> UpdateOperationStatusAsync(OperationStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除操作定义
    /// </summary>
    /// <param name="id">操作主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteOperationAsync(long id, CancellationToken cancellationToken = default);
}
