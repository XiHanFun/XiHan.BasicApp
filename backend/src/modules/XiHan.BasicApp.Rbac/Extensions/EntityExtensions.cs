#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EntityExtensions
// Guid:5b2b3c4d-5e6f-7890-abcd-ef12345678aa
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 5:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Dtos.Departments;
using XiHan.BasicApp.Rbac.Dtos.Menus;
using XiHan.BasicApp.Rbac.Dtos.Permissions;
using XiHan.BasicApp.Rbac.Dtos.Roles;
using XiHan.BasicApp.Rbac.Dtos.Tenants;
using XiHan.BasicApp.Rbac.Dtos.Users;
using XiHan.BasicApp.Rbac.Entities;

namespace XiHan.BasicApp.Rbac.Extensions;

/// <summary>
/// 实体扩展方法
/// </summary>
public static class EntityExtensions
{
    #region 用户扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static UserDto ToDto(this SysUser entity)
    {
        return new UserDto
        {
            BasicId = entity.BasicId,
            TenantId = entity.TenantId,
            UserName = entity.UserName,
            RealName = entity.RealName,
            NickName = entity.NickName,
            Avatar = entity.Avatar,
            Email = entity.Email,
            Phone = entity.Phone,
            Gender = entity.Gender,
            Birthday = entity.Birthday,
            Status = entity.Status,
            LastLoginTime = entity.LastLoginTime,
            LastLoginIp = entity.LastLoginIp,
            TimeZone = entity.TimeZone,
            Language = entity.Language,
            Country = entity.Country,
            Remark = entity.Remark,
            CreatedBy = entity.CreatedBy,
            CreatedTime = entity.CreatedTime,
            ModifiedBy = entity.ModifiedBy,
            ModifiedTime = entity.ModifiedTime,
            IsDeleted = entity.IsDeleted,
            DeletedBy = entity.DeletedBy,
            DeletedTime = entity.DeletedTime
        };
    }

    /// <summary>
    /// 实体列表转DTO列表
    /// </summary>
    /// <param name="entities">实体列表</param>
    /// <returns></returns>
    public static List<UserDto> ToDto(this IEnumerable<SysUser> entities)
    {
        return entities.Select(e => e.ToDto()).ToList();
    }

    #endregion

    #region 角色扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static RoleDto ToDto(this SysRole entity)
    {
        return new RoleDto
        {
            BasicId = entity.BasicId,
            RoleCode = entity.RoleCode,
            RoleName = entity.RoleName,
            RoleDescription = entity.RoleDescription,
            RoleType = entity.RoleType,
            Status = entity.Status,
            Sort = entity.Sort,
            Remark = entity.Remark,
            CreatedBy = entity.CreatedBy,
            CreatedTime = entity.CreatedTime,
            ModifiedBy = entity.ModifiedBy,
            ModifiedTime = entity.ModifiedTime,
            IsDeleted = entity.IsDeleted,
            DeletedBy = entity.DeletedBy,
            DeletedTime = entity.DeletedTime
        };
    }

    /// <summary>
    /// 实体列表转DTO列表
    /// </summary>
    /// <param name="entities">实体列表</param>
    /// <returns></returns>
    public static List<RoleDto> ToDto(this IEnumerable<SysRole> entities)
    {
        return entities.Select(e => e.ToDto()).ToList();
    }

    #endregion

    #region 权限扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static PermissionDto ToDto(this SysPermission entity)
    {
        return new PermissionDto
        {
            BasicId = entity.BasicId,
            PermissionCode = entity.PermissionCode,
            PermissionName = entity.PermissionName,
            PermissionDescription = entity.PermissionDescription,
            PermissionType = entity.PermissionType,
            PermissionValue = entity.PermissionValue,
            Status = entity.Status,
            Sort = entity.Sort,
            Remark = entity.Remark,
            CreatedBy = entity.CreatedBy,
            CreatedTime = entity.CreatedTime,
            ModifiedBy = entity.ModifiedBy,
            ModifiedTime = entity.ModifiedTime,
            IsDeleted = entity.IsDeleted,
            DeletedBy = entity.DeletedBy,
            DeletedTime = entity.DeletedTime
        };
    }

    /// <summary>
    /// 实体列表转DTO列表
    /// </summary>
    /// <param name="entities">实体列表</param>
    /// <returns></returns>
    public static List<PermissionDto> ToDto(this IEnumerable<SysPermission> entities)
    {
        return entities.Select(e => e.ToDto()).ToList();
    }

    #endregion

    #region 菜单扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static MenuDto ToDto(this SysMenu entity)
    {
        return new MenuDto
        {
            BasicId = entity.BasicId,
            ParentId = entity.ParentId,
            MenuName = entity.MenuName,
            MenuCode = entity.MenuCode,
            MenuType = entity.MenuType,
            Path = entity.Path,
            Component = entity.Component,
            Icon = entity.Icon,
            Permission = entity.Permission,
            IsExternal = entity.IsExternal,
            IsCache = entity.IsCache,
            IsVisible = entity.IsVisible,
            Status = entity.Status,
            Sort = entity.Sort,
            Remark = entity.Remark,
            CreatedBy = entity.CreatedBy,
            CreatedTime = entity.CreatedTime,
            ModifiedBy = entity.ModifiedBy,
            ModifiedTime = entity.ModifiedTime,
            IsDeleted = entity.IsDeleted,
            DeletedBy = entity.DeletedBy,
            DeletedTime = entity.DeletedTime
        };
    }

    /// <summary>
    /// 实体列表转DTO列表
    /// </summary>
    /// <param name="entities">实体列表</param>
    /// <returns></returns>
    public static List<MenuDto> ToDto(this IEnumerable<SysMenu> entities)
    {
        return entities.Select(e => e.ToDto()).ToList();
    }

    /// <summary>
    /// 构建菜单树
    /// </summary>
    /// <param name="menus">菜单列表</param>
    /// <param name="parentId">父级ID</param>
    /// <returns></returns>
    public static List<MenuTreeDto> BuildTree(this IEnumerable<MenuDto> menus, XiHanBasicAppIdType? parentId = null)
    {
        var menuList = menus.ToList();
        var treeList = new List<MenuTreeDto>();

        var rootMenus = menuList.Where(m => m.ParentId == parentId).OrderBy(m => m.Sort);
        foreach (var menu in rootMenus)
        {
            var treeDto = new MenuTreeDto
            {
                BasicId = menu.BasicId,
                ParentId = menu.ParentId,
                MenuName = menu.MenuName,
                MenuCode = menu.MenuCode,
                MenuType = menu.MenuType,
                Path = menu.Path,
                Component = menu.Component,
                Icon = menu.Icon,
                Permission = menu.Permission,
                IsExternal = menu.IsExternal,
                IsCache = menu.IsCache,
                IsVisible = menu.IsVisible,
                Status = menu.Status,
                Sort = menu.Sort,
                Remark = menu.Remark,
                CreatedBy = menu.CreatedBy,
                CreatedTime = menu.CreatedTime,
                ModifiedBy = menu.ModifiedBy,
                ModifiedTime = menu.ModifiedTime,
                IsDeleted = menu.IsDeleted,
                DeletedBy = menu.DeletedBy,
                DeletedTime = menu.DeletedTime,
                Children = BuildTree(menuList, menu.BasicId)
            };
            treeList.Add(treeDto);
        }

        return treeList;
    }

    #endregion

    #region 部门扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static DepartmentDto ToDto(this SysDepartment entity)
    {
        return new DepartmentDto
        {
            BasicId = entity.BasicId,
            ParentId = entity.ParentId,
            DepartmentName = entity.DepartmentName,
            DepartmentCode = entity.DepartmentCode,
            DepartmentType = entity.DepartmentType,
            LeaderId = entity.LeaderId,
            Phone = entity.Phone,
            Email = entity.Email,
            Address = entity.Address,
            Status = entity.Status,
            Sort = entity.Sort,
            Remark = entity.Remark,
            CreatedBy = entity.CreatedBy,
            CreatedTime = entity.CreatedTime,
            ModifiedBy = entity.ModifiedBy,
            ModifiedTime = entity.ModifiedTime,
            IsDeleted = entity.IsDeleted,
            DeletedBy = entity.DeletedBy,
            DeletedTime = entity.DeletedTime
        };
    }

    /// <summary>
    /// 实体列表转DTO列表
    /// </summary>
    /// <param name="entities">实体列表</param>
    /// <returns></returns>
    public static List<DepartmentDto> ToDto(this IEnumerable<SysDepartment> entities)
    {
        return entities.Select(e => e.ToDto()).ToList();
    }

    /// <summary>
    /// 构建部门树
    /// </summary>
    /// <param name="departments">部门列表</param>
    /// <param name="parentId">父级ID</param>
    /// <returns></returns>
    public static List<DepartmentTreeDto> BuildTree(this IEnumerable<DepartmentDto> departments, XiHanBasicAppIdType? parentId = null)
    {
        var departmentList = departments.ToList();
        var treeList = new List<DepartmentTreeDto>();

        var rootDepartments = departmentList.Where(d => d.ParentId == parentId).OrderBy(d => d.Sort);
        foreach (var department in rootDepartments)
        {
            var treeDto = new DepartmentTreeDto
            {
                BasicId = department.BasicId,
                ParentId = department.ParentId,
                DepartmentName = department.DepartmentName,
                DepartmentCode = department.DepartmentCode,
                DepartmentType = department.DepartmentType,
                LeaderId = department.LeaderId,
                LeaderName = department.LeaderName,
                Phone = department.Phone,
                Email = department.Email,
                Address = department.Address,
                Status = department.Status,
                Sort = department.Sort,
                Remark = department.Remark,
                CreatedBy = department.CreatedBy,
                CreatedTime = department.CreatedTime,
                ModifiedBy = department.ModifiedBy,
                ModifiedTime = department.ModifiedTime,
                IsDeleted = department.IsDeleted,
                DeletedBy = department.DeletedBy,
                DeletedTime = department.DeletedTime,
                Children = BuildTree(departmentList, department.BasicId)
            };
            treeList.Add(treeDto);
        }

        return treeList;
    }

    #endregion

    #region 租户扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static TenantDto ToDto(this SysTenant entity)
    {
        return new TenantDto
        {
            BasicId = entity.BasicId,
            TenantCode = entity.TenantCode,
            TenantName = entity.TenantName,
            TenantShortName = entity.TenantShortName,
            ContactPerson = entity.ContactPerson,
            ContactPhone = entity.ContactPhone,
            ContactEmail = entity.ContactEmail,
            Address = entity.Address,
            Logo = entity.Logo,
            Domain = entity.Domain,
            IsolationMode = entity.IsolationMode,
            ConfigStatus = entity.ConfigStatus,
            ExpireTime = entity.ExpireTime,
            UserLimit = entity.UserLimit,
            StorageLimit = entity.StorageLimit,
            TenantStatus = entity.TenantStatus,
            Status = entity.Status,
            Sort = entity.Sort,
            Remark = entity.Remark,
            CreatedBy = entity.CreatedBy,
            CreatedTime = entity.CreatedTime,
            ModifiedBy = entity.ModifiedBy,
            ModifiedTime = entity.ModifiedTime,
            IsDeleted = entity.IsDeleted,
            DeletedBy = entity.DeletedBy,
            DeletedTime = entity.DeletedTime
        };
    }

    /// <summary>
    /// 实体列表转DTO列表
    /// </summary>
    /// <param name="entities">实体列表</param>
    /// <returns></returns>
    public static List<TenantDto> ToDto(this IEnumerable<SysTenant> entities)
    {
        return entities.Select(e => e.ToDto()).ToList();
    }

    #endregion
}
