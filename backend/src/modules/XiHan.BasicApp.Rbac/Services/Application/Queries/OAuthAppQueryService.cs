#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthAppQueryService
// Guid:b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e
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
/// OAuth应用查询服务（处理OAuth应用的读操作 - CQRS）
/// </summary>
public class OAuthAppQueryService : ApplicationServiceBase
{
    private readonly IOAuthAppRepository _oAuthAppRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OAuthAppQueryService(IOAuthAppRepository oAuthAppRepository)
    {
        _oAuthAppRepository = oAuthAppRepository;
    }

    /// <summary>
    /// 根据ID获取OAuth应用
    /// </summary>
    public async Task<RbacDtoBase?> GetByIdAsync(long id)
    {
        var app = await _oAuthAppRepository.GetByIdAsync(id);
        return app?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据ClientId获取OAuth应用
    /// </summary>
    public async Task<RbacDtoBase?> GetByClientIdAsync(string clientId)
    {
        var app = await _oAuthAppRepository.GetByClientIdAsync(clientId);
        return app?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 获取所有启用的OAuth应用
    /// </summary>
    public async Task<List<RbacDtoBase>> GetEnabledAppsAsync()
    {
        var apps = await _oAuthAppRepository.GetEnabledAppsAsync();
        return apps.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 验证ClientId和ClientSecret
    /// </summary>
    public async Task<bool> ValidateClientCredentialsAsync(string clientId, string clientSecret)
    {
        return await _oAuthAppRepository.ValidateClientCredentialsAsync(clientId, clientSecret);
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    public async Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input)
    {
        var result = await _oAuthAppRepository.GetPagedAsync(input);
        var dtos = result.Items.Adapt<List<RbacDtoBase>>();
        return new PageResponse<RbacDtoBase>(dtos, result.PageData);
    }
}
