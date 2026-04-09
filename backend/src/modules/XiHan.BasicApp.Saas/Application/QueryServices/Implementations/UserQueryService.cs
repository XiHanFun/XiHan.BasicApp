#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserQueryService
// Guid:3a4b5c6d-7e8f-4012-cdef-320000000002
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
/// 用户查询服务
/// </summary>
public class UserQueryService : IUserQueryService, ITransientDependency
{
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserQueryService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <inheritdoc />
    [Cacheable(Key = "user:id:{id}", ExpireSeconds = 300)]
    public async Task<UserDto?> GetByIdAsync(long id)
    {
        var entity = await _userRepository.GetByIdAsync(id);
        if (entity is null)
        {
            return null;
        }

        var dto = entity.Adapt<UserDto>()!;
        var relations = await _userRepository.GetUserRolesAsync(entity.BasicId, entity.TenantId);
        dto.RoleIds = relations
            .Select(relation => relation.RoleId)
            .Distinct()
            .ToArray();
        return dto;
    }
}
