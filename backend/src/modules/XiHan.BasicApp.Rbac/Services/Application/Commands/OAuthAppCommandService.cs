#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthAppCommandService
// Guid:a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Application.Commands;

/// <summary>
/// OAuth应用命令服务（处理OAuth应用的写操作）
/// </summary>
public class OAuthAppCommandService : CrudApplicationServiceBase<SysOAuthApp, RbacDtoBase, long, RbacDtoBase, RbacDtoBase>
{
    private readonly IOAuthAppRepository _oAuthAppRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OAuthAppCommandService(IOAuthAppRepository oAuthAppRepository)
        : base(oAuthAppRepository)
    {
        _oAuthAppRepository = oAuthAppRepository;
    }

    /// <summary>
    /// 创建OAuth应用（重写以生成ClientId和ClientSecret）
    /// </summary>
    public override async Task<RbacDtoBase> CreateAsync(RbacDtoBase input)
    {
        // 1. 检查应用名称是否重复
        var exists = await _oAuthAppRepository.ExistsByNameAsync(input.AppName);
        if (exists)
        {
            throw new InvalidOperationException($"应用名称 '{input.AppName}' 已存在");
        }

        // 2. 映射实体
        var app = input.Adapt<SysOAuthApp>();

        // 3. 生成ClientId和ClientSecret
        app.ClientId = GenerateClientId();
        app.ClientSecret = GenerateClientSecret();

        // 4. 保存
        app = await _oAuthAppRepository.AddAsync(app);

        return await MapToEntityDtoAsync(app);
    }

    /// <summary>
    /// 重置OAuth应用密钥
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <returns>新的密钥</returns>
    public async Task<string> ResetClientSecretAsync(long appId)
    {
        var app = await _oAuthAppRepository.GetByIdAsync(appId);
        if (app == null)
        {
            throw new InvalidOperationException($"应用 {appId} 不存在");
        }

        // 生成新的密钥
        var newSecret = GenerateClientSecret();
        app.ClientSecret = newSecret;

        await _oAuthAppRepository.UpdateAsync(app);

        return newSecret;
    }

    /// <summary>
    /// 启用或禁用OAuth应用
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="enabled">是否启用</param>
    public async Task<bool> SetEnabledAsync(long appId, bool enabled)
    {
        var app = await _oAuthAppRepository.GetByIdAsync(appId);
        if (app == null)
        {
            return false;
        }

        app.IsEnabled = enabled;
        await _oAuthAppRepository.UpdateAsync(app);

        return true;
    }

    /// <summary>
    /// 生成ClientId
    /// </summary>
    private static string GenerateClientId()
    {
        return $"client_{Guid.NewGuid():N}";
    }

    /// <summary>
    /// 生成ClientSecret
    /// </summary>
    private static string GenerateClientSecret()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray() + Guid.NewGuid().ToByteArray());
    }
}
