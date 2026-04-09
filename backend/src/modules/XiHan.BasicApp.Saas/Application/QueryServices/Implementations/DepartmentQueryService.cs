#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentQueryService
// Guid:3b4c5d6e-7f80-9012-bcde-f01234567802
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;

namespace XiHan.BasicApp.Saas.Application.QueryServices.Implementations;

/// <summary>
/// 部门查询服务
/// </summary>
public class DepartmentQueryService : IDepartmentQueryService, ITransientDependency
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DepartmentQueryService(IDepartmentRepository departmentRepository, IUserRepository userRepository)
    {
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
    }

    /// <inheritdoc />
    [Cacheable(Key = "dept:id:{id}", ExpireSeconds = 300)]
    public async Task<DepartmentDto?> GetByIdAsync(long id)
    {
        var entity = await _departmentRepository.GetByIdAsync(id);
        if (entity is null)
        {
            return null;
        }

        var dto = entity.Adapt<DepartmentDto>()!;
        if (entity.LeaderId.HasValue && entity.LeaderId.Value > 0)
        {
            var user = await _userRepository.GetByIdAsync(entity.LeaderId.Value);
            dto.LeaderName = ResolveUserDisplayName(user);
        }
        return dto;
    }

    private static string? ResolveUserDisplayName(SysUser? user)
    {
        if (user is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(user.RealName))
        {
            return user.RealName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(user.NickName))
        {
            return user.NickName.Trim();
        }

        return user.UserName;
    }
}
