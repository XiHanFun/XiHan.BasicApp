// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 用户设置仓储实现
/// </summary>
public sealed class UserSettingRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysUserSetting>(clientResolver), IUserSettingRepository
{
    /// <inheritdoc />
    public async Task<SysUserSetting?> GetByUserSettingAsync(long userId, UserSettingScene scene, string settingKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(setting => setting.UserId == userId && setting.Scene == scene && setting.SettingKey == settingKey)
            .FirstAsync(cancellationToken);
    }
}
