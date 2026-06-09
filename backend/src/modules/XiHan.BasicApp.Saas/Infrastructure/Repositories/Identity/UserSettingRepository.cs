#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSettingRepository
// Guid:7c3e9014-5d6f-4b0c-9e3a-2f4d8b1c6a73
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
