// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 字段级安全查询应用服务接口
/// </summary>
public interface IFieldLevelSecurityQueryService : IApplicationService
{
    /// <summary>
    /// 获取字段级安全分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字段级安全分页列表</returns>
    Task<PageResultDtoBase<FieldLevelSecurityListItemDto>> GetFieldLevelSecurityPageAsync(FieldLevelSecurityPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取字段级安全详情
    /// </summary>
    /// <param name="id">字段级安全主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字段级安全详情</returns>
    Task<FieldLevelSecurityDetailDto?> GetFieldLevelSecurityDetailAsync(long id, CancellationToken cancellationToken = default);
}
