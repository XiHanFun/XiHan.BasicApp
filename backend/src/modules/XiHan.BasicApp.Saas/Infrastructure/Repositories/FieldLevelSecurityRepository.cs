#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FieldLevelSecurityRepository
// Guid:89abcdef-0123-4567-89ab-cdef01234567
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/22 18:32:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 字段级安全仓储实现
/// </summary>
public class FieldLevelSecurityRepository : SqlSugarAuditedRepository<SysFieldLevelSecurity, long>, IFieldLevelSecurityRepository
{
    public FieldLevelSecurityRepository(ISqlSugarClientResolver clientResolver)
        : base(clientResolver)
    {
    }

    public async Task<IReadOnlyCollection<SysFieldLevelSecurity>> GetEffectiveRulesAsync(
        long userId,
        long? tenantId,
        long resourceId,
        IReadOnlyCollection<long> roleIds,
        IReadOnlyCollection<long> permissionIds,
        IReadOnlyCollection<string>? fieldNames = null,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || resourceId <= 0)
        {
            return [];
        }

        var distinctRoleIds = roleIds.Where(static id => id > 0).Distinct().ToArray();
        var distinctPermissionIds = permissionIds.Where(static id => id > 0).Distinct().ToArray();
        var normalizedFields = fieldNames?
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Select(static name => name.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var query = CreateQueryable()
            .Where(rule =>
                rule.ResourceId == resourceId
                && rule.Status == YesOrNo.Yes
                && (rule.TenantId == 0 || (tenantId.HasValue && rule.TenantId == tenantId.Value)))
            .WhereIF(
                normalizedFields is { Length: > 0 },
                rule => normalizedFields!.Contains(rule.FieldName))
            .Where(rule =>
                (rule.TargetType == FieldSecurityTargetType.User && rule.TargetId == userId)
                || (rule.TargetType == FieldSecurityTargetType.Role && distinctRoleIds.Contains(rule.TargetId))
                || (rule.TargetType == FieldSecurityTargetType.Permission && distinctPermissionIds.Contains(rule.TargetId)));

        return await query
            .OrderByDescending(rule => rule.Priority)
            .OrderByDescending(rule => rule.TenantId)
            .ToListAsync(cancellationToken);
    }
}
