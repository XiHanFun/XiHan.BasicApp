#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthAppService
// Guid:e1f2g3h4-i5j6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 18:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.OAuthApps;
using XiHan.BasicApp.Rbac.Services.OAuthApps.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.OAuthApps;

/// <summary>
/// 系统OAuth应用服务实现
/// </summary>
public class SysOAuthAppService : CrudApplicationServiceBase<SysOAuthApp, OAuthAppDto, long, CreateOAuthAppDto, UpdateOAuthAppDto>, ISysOAuthAppService
{
    private readonly ISysOAuthAppRepository _oauthAppRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysOAuthAppService(ISysOAuthAppRepository oauthAppRepository) : base(oauthAppRepository)
    {
        _oauthAppRepository = oauthAppRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据客户端ID获取应用
    /// </summary>
    public async Task<OAuthAppDto?> GetByClientIdAsync(string clientId)
    {
        var app = await _oauthAppRepository.GetByClientIdAsync(clientId);
        return app?.Adapt<OAuthAppDto>();
    }

    /// <summary>
    /// 根据应用名称获取应用
    /// </summary>
    public async Task<OAuthAppDto?> GetByAppNameAsync(string appName)
    {
        var app = await _oauthAppRepository.GetByAppNameAsync(appName);
        return app?.Adapt<OAuthAppDto>();
    }

    /// <summary>
    /// 检查客户端ID是否存在
    /// </summary>
    public async Task<bool> ExistsByClientIdAsync(string clientId, long? excludeId = null)
    {
        return await _oauthAppRepository.ExistsByClientIdAsync(clientId, excludeId);
    }

    #endregion 业务特定方法
}
