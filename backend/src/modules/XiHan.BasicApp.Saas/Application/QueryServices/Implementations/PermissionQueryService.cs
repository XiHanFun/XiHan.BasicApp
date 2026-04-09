#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionQueryService
// Guid:5d6e7f80-9102-1234-def0-123456789a02
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 权限查询服务
/// </summary>
public class PermissionQueryService : IPermissionQueryService, ITransientDependency
{
    private readonly IPermissionRepository _permissionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionQueryService(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    /// <inheritdoc />
    [Cacheable(Key = "perm:id:{id}", ExpireSeconds = 300)]
    public async Task<PermissionDto?> GetByIdAsync(long id)
    {
        var entity = await _permissionRepository.GetByIdAsync(id);
        if (entity is null)
        {
            return null;
        }

        var dto = entity.Adapt<PermissionDto>()!;
        dto.GroupName = ResolveGroupName(entity.Tags);
        return dto;
    }

    private static string? ResolveGroupName(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return null;
        }

        return tags
            .Split(new[] { ',', '，', ';', '；', '|', '、' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
    }
}
