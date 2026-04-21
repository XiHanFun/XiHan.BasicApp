#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ProfileQueryService
// Guid:e4f5a6b7-c8d9-4012-3456-789abcdef012
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Saas.Constants.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 个人资料查询服务
/// </summary>
public class ProfileQueryService : IProfileQueryService, ITransientDependency
{
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ProfileQueryService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <inheritdoc />
    [Cacheable(Key = QueryCacheKeys.ProfileUserById, ExpireSeconds = 180)]
    public async Task<UserDto?> GetCurrentUserAsync(long userId)
    {
        var entity = await _userRepository.GetByIdAsync(userId);
        return entity?.Adapt<UserDto>();
    }
}
