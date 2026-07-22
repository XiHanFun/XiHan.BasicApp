// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 字段级安全命令应用服务接口
/// </summary>
public interface IFieldLevelSecurityAppService : IApplicationService
{
    /// <summary>
    /// 创建字段级安全策略
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字段级安全详情</returns>
    Task<FieldLevelSecurityDetailDto> CreateFieldLevelSecurityAsync(FieldLevelSecurityCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新字段级安全策略
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字段级安全详情</returns>
    Task<FieldLevelSecurityDetailDto> UpdateFieldLevelSecurityAsync(FieldLevelSecurityUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新字段级安全策略状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字段级安全详情</returns>
    Task<FieldLevelSecurityDetailDto> UpdateFieldLevelSecurityStatusAsync(FieldLevelSecurityStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除字段级安全策略
    /// </summary>
    /// <param name="id">字段级安全主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteFieldLevelSecurityAsync(long id, CancellationToken cancellationToken = default);
}
