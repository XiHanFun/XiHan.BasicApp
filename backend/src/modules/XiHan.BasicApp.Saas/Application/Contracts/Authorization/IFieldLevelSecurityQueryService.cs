#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IFieldLevelSecurityQueryService
// Guid:3a4b750f-bb82-4e6d-9d2b-e8e62e21b6df
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
