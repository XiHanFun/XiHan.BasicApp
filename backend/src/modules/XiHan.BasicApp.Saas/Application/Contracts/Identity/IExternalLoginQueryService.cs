#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IExternalLoginQueryService
// Guid:d0fe38a2-79fd-4930-b66a-e7be542afa2a
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
/// 第三方登录绑定查询应用服务接口
/// </summary>
public interface IExternalLoginQueryService : IApplicationService
{
    /// <summary>
    /// 获取第三方登录绑定分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>第三方登录绑定分页列表</returns>
    Task<PageResultDtoBase<ExternalLoginListItemDto>> GetExternalLoginPageAsync(ExternalLoginPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取第三方登录绑定详情
    /// </summary>
    /// <param name="id">第三方登录绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>第三方登录绑定详情</returns>
    Task<ExternalLoginDetailDto?> GetExternalLoginDetailAsync(long id, CancellationToken cancellationToken = default);
}
