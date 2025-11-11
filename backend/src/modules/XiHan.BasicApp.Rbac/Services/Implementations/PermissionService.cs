#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionService
// Guid:5c2b3c4d-5e6f-7890-abcd-ef12345678ba
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 7:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Constants;
using XiHan.BasicApp.Rbac.Dtos.Permissions;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Managers;
using XiHan.BasicApp.Rbac.Repositories.Abstractions;
using XiHan.BasicApp.Rbac.Services.Abstractions;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Implementations;

/// <summary>
/// 权限服务实现
/// </summary>
public class PermissionService : ApplicationServiceBase, IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly PermissionManager _permissionManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionService(
        IPermissionRepository permissionRepository,
        PermissionManager permissionManager)
    {
        _permissionRepository = permissionRepository;
        _permissionManager = permissionManager;
    }

    /// <summary>
    /// 根据ID获取权限
    /// </summary>
    public async Task<PermissionDto?> GetByIdAsync(long id)
    {
        var permission = await _permissionRepository.GetByIdAsync(id);
        return permission?.ToDto();
    }

    /// <summary>
    /// 根据权限编码获取权限
    /// </summary>
    public async Task<PermissionDto?> GetByPermissionCodeAsync(string permissionCode)
    {
        var permission = await _permissionRepository.GetByPermissionCodeAsync(permissionCode);
        return permission?.ToDto();
    }

    /// <summary>
    /// 创建权限
    /// </summary>
    public async Task<PermissionDto> CreateAsync(CreatePermissionDto input)
    {
        // 验证权限编码唯一性
        if (!await _permissionManager.IsPermissionCodeUniqueAsync(input.PermissionCode))
        {
            throw new InvalidOperationException(ErrorMessageConstants.PermissionCodeExists);
        }

        var permission = new SysPermission
        {
            PermissionCode = input.PermissionCode,
            PermissionName = input.PermissionName,
            PermissionDescription = input.PermissionDescription,
            PermissionType = input.PermissionType,
            PermissionValue = input.PermissionValue,
            Sort = input.Sort,
            Remark = input.Remark
        };

        await _permissionRepository.InsertAsync(permission);

        return permission.ToDto();
    }

    /// <summary>
    /// 更新权限
    /// </summary>
    public async Task<PermissionDto> UpdateAsync(UpdatePermissionDto input)
    {
        var permission = await _permissionRepository.GetByIdAsync(input.BasicId);
        if (permission == null)
        {
            throw new InvalidOperationException(ErrorMessageConstants.PermissionNotFound);
        }

        // 更新权限信息
        if (input.PermissionName != null)
        {
            permission.PermissionName = input.PermissionName;
        }

        if (input.PermissionDescription != null)
        {
            permission.PermissionDescription = input.PermissionDescription;
        }

        if (input.PermissionType.HasValue)
        {
            permission.PermissionType = input.PermissionType.Value;
        }

        if (input.PermissionValue != null)
        {
            permission.PermissionValue = input.PermissionValue;
        }

        if (input.Status.HasValue)
        {
            permission.Status = input.Status.Value;
        }

        if (input.Sort.HasValue)
        {
            permission.Sort = input.Sort.Value;
        }

        if (input.Remark != null)
        {
            permission.Remark = input.Remark;
        }

        await _permissionRepository.UpdateAsync(permission);

        return permission.ToDto();
    }

    /// <summary>
    /// 删除权限
    /// </summary>
    public async Task<bool> DeleteAsync(long id)
    {
        var permission = await _permissionRepository.GetByIdAsync(id);
        if (permission == null)
        {
            throw new InvalidOperationException(ErrorMessageConstants.PermissionNotFound);
        }

        return await _permissionRepository.DeleteAsync(permission);
    }

    /// <summary>
    /// 获取角色的权限列表
    /// </summary>
    public async Task<List<PermissionDto>> GetByRoleIdAsync(long roleId)
    {
        var permissions = await _permissionRepository.GetByRoleIdAsync(roleId);
        return permissions.ToDto();
    }

    /// <summary>
    /// 获取用户的权限列表
    /// </summary>
    public async Task<List<PermissionDto>> GetByUserIdAsync(long userId)
    {
        var permissions = await _permissionRepository.GetByUserIdAsync(userId);
        return permissions.ToDto();
    }

    /// <summary>
    /// 分页查询权限
    /// </summary>
    public async Task<PageResponse<PermissionDto>> GetPagedListAsync(PageQuery query)
    {
        var queryable = _permissionRepository.Queryable();

        // 应用筛选条件
        if (query.Conditions != null && query.Conditions.Any())
        {
            foreach (var condition in query.Conditions)
            {
                if (condition.Field == "PermissionName" && !string.IsNullOrEmpty(condition.Value?.ToString()))
                {
                    queryable = queryable.Where(p => p.PermissionName.Contains(condition.Value.ToString()!));
                }
            }
        }

        // 应用排序
        if (query.Sorts != null && query.Sorts.Any())
        {
            foreach (var sort in query.Sorts)
            {
                queryable = sort.Direction == Paging.Enums.SortDirection.Ascending
                    ? queryable.OrderBy($"{sort.Field} ASC")
                    : queryable.OrderBy($"{sort.Field} DESC");
            }
        }
        else
        {
            queryable = queryable.OrderBy(p => p.Sort);
        }

        // 分页
        var total = await queryable.CountAsync();
        var items = await queryable
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PageResponse<PermissionDto>
        {
            Items = items.ToDto(),
            PageInfo = new PageInfo
            {
                PageIndex = query.PageIndex,
                PageSize = query.PageSize,
                Total = total
            }
        };
    }
}
