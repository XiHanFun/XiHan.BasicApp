#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IFieldSecurityCacheService
// Guid:b29c8672-2d1c-4d57-bb60-2ddab6c6b24f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/22 22:11:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Caching;

public interface IFieldSecurityCacheService
{
    Task<FieldSecurityDecisionCacheItem?> GetDecisionAsync(
        long userId,
        long? tenantId,
        string resourceCode,
        IReadOnlyCollection<string> fieldNames,
        Func<CancellationToken, Task<FieldSecurityDecisionCacheItem>> factory,
        CancellationToken cancellationToken = default);

    Task InvalidateAsync(long? tenantId, CancellationToken cancellationToken = default);
}
