#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PagePreferenceRepository
// Guid:7c3e9014-5d6f-4b0c-9e3a-2f4d8b1c6a73
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/05 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 页面偏好仓储实现
/// </summary>
public sealed class PagePreferenceRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysPagePreference>(clientResolver), IPagePreferenceRepository
{
    /// <inheritdoc />
    public async Task<SysPagePreference?> GetByUserAndPageAsync(long userId, string pageCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(preference => preference.UserId == userId && preference.PageCode == pageCode)
            .FirstAsync(cancellationToken);
    }
}
