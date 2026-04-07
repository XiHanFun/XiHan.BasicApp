#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthAppDomainService
// Guid:6a7b8c9d-0e1f-4345-f012-640000000004
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Domain.DomainServices.Implementations;

/// <summary>
/// OAuth 应用领域服务
/// </summary>
public class OAuthAppDomainService : IOAuthAppDomainService, ITransientDependency
{
    private readonly IOAuthAppRepository _oauthAppRepository;
    private readonly ILocalEventBus _localEventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OAuthAppDomainService(IOAuthAppRepository oauthAppRepository, ILocalEventBus localEventBus)
    {
        _oauthAppRepository = oauthAppRepository;
        _localEventBus = localEventBus;
    }

    /// <inheritdoc />
    public async Task<SysOAuthApp> CreateAsync(SysOAuthApp oauthApp)
    {
        var created = await _oauthAppRepository.AddAsync(oauthApp);
        await _localEventBus.PublishAsync(new OAuthChangedDomainEvent(created.BasicId));
        return created;
    }

    /// <inheritdoc />
    public async Task<SysOAuthApp> UpdateAsync(SysOAuthApp oauthApp)
    {
        var updated = await _oauthAppRepository.UpdateAsync(oauthApp);
        await _localEventBus.PublishAsync(new OAuthChangedDomainEvent(updated.BasicId));
        return updated;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(long id)
    {
        var oauthApp = await _oauthAppRepository.GetByIdAsync(id);
        if (oauthApp == null)
        {
            return false;
        }

        var result = await _oauthAppRepository.DeleteAsync(oauthApp);
        if (result)
        {
            await _localEventBus.PublishAsync(new OAuthChangedDomainEvent(id));
        }
        return result;
    }
}
