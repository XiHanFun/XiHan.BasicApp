#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleQueryService
// Guid:c9f5a3b2-4d7e-5f6a-0b2c-3d4e5f6a7b82
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
/// 角色查询服务
/// </summary>
public class RoleQueryService : IRoleQueryService, ITransientDependency
{
    private readonly IRoleRepository _roleRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleQueryService(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    /// <inheritdoc />
    [Cacheable(Key = "role:id:{id}", ExpireSeconds = 300)]
    public async Task<RoleDto?> GetByIdAsync(long id)
    {
        var entity = await _roleRepository.GetByIdAsync(id);
        return entity?.Adapt<RoleDto>();
    }
}
