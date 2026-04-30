#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOperationAppService
// Guid:a5d9a858-cb3c-4421-aa09-9752f5b0765c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
