#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IFieldLevelSecurityRepository
// Guid:01234567-89ab-cdef-0123-456789abcdef
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/22 18:31:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 字段级安全仓储接口
/// </summary>
public interface IFieldLevelSecurityRepository : IRepositoryBase<SysFieldLevelSecurity, long>
{
    Task<IReadOnlyCollection<SysFieldLevelSecurity>> GetEffectiveRulesAsync(
        long userId,
        long? tenantId,
        long resourceId,
        IReadOnlyCollection<long> roleIds,
        IReadOnlyCollection<long> permissionIds,
        IReadOnlyCollection<string>? fieldNames = null,
        CancellationToken cancellationToken = default);
}
