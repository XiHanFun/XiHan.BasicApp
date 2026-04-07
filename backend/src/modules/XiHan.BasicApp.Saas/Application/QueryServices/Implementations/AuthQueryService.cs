#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthQueryService
// Guid:c2d3e4f5-a6b7-4890-1234-56789abcdef0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;

namespace XiHan.BasicApp.Saas.Application.QueryServices.Implementations;

/// <summary>
/// 认证权限查询服务
/// </summary>
public class AuthQueryService : IAuthQueryService, ITransientDependency
{
    private readonly IPermissionRepository _permissionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthQueryService(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    /// <inheritdoc />
    [Cacheable(Key = "auth:perms:{userId}:{tenantId}", ExpireSeconds = 120)]
    public async Task<IReadOnlyList<string>> GetUserPermissionCodesAsync(long userId, long? tenantId = null)
    {
        var permissions = await _permissionRepository.GetUserPermissionsAsync(userId, tenantId);
        return permissions
            .Where(static p => !string.IsNullOrWhiteSpace(p.PermissionCode))
            .Select(static p => p.PermissionCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static code => code, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
