#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserPreferenceRepository
// Guid:d8e3b4f5-0c9a-4b2d-a7f6-4a1b9e3d2f58
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 用户偏好仓储实现
/// </summary>
public sealed class UserPreferenceRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysUserPreference>(clientResolver), IUserPreferenceRepository
{
    /// <inheritdoc />
    public async Task<SysUserPreference?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(preference => preference.UserId == userId)
            .FirstAsync(cancellationToken);
    }
}
