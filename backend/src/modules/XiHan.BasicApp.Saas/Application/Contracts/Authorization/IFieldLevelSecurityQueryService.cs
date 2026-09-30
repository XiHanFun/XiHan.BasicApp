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
    Task<PageResultDtoBase<FieldLevelSecurityListItemDto>> GetFieldLevelSecurityPageAsync(FieldLevelSecurityPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取字段级安全详情
    /// </summary>
    Task<FieldLevelSecurityDetailDto?> GetFieldLevelSecurityDetailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取可配置字段安全的实体与字段
    /// </summary>
    Task<IReadOnlyList<FieldSecurityEntityDto>> GetFieldSecurityEntitiesAsync(CancellationToken cancellationToken = default);
}
