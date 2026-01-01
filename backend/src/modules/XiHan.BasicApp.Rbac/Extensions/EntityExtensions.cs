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

using Mapster;
using System.Linq;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Services.AccessLogs.Dtos;
using XiHan.BasicApp.Rbac.Services.ApiLogs.Dtos;
using XiHan.BasicApp.Rbac.Services.AuditLogs.Dtos;
using XiHan.BasicApp.Rbac.Services.Audits.Dtos;
using XiHan.BasicApp.Rbac.Services.Configs.Dtos;
using XiHan.BasicApp.Rbac.Services.Departments.Dtos;
using XiHan.BasicApp.Rbac.Services.DictItems.Dtos;
using XiHan.BasicApp.Rbac.Services.Dicts.Dtos;
using XiHan.BasicApp.Rbac.Services.Emails.Dtos;
using XiHan.BasicApp.Rbac.Services.Files.Dtos;
using XiHan.BasicApp.Rbac.Services.LoginLogs.Dtos;
using XiHan.BasicApp.Rbac.Services.Menus.Dtos;
using XiHan.BasicApp.Rbac.Services.Notifications.Dtos;
using XiHan.BasicApp.Rbac.Services.OAuthApps.Dtos;
using XiHan.BasicApp.Rbac.Services.OAuthCodes.Dtos;
using XiHan.BasicApp.Rbac.Services.OAuthTokens.Dtos;
using XiHan.BasicApp.Rbac.Services.OperationLogs.Dtos;
using XiHan.BasicApp.Rbac.Services.Permissions.Dtos;
using XiHan.BasicApp.Rbac.Services.Roles.Dtos;
using XiHan.BasicApp.Rbac.Services.Sms.Dtos;
using XiHan.BasicApp.Rbac.Services.TaskLogs.Dtos;
using XiHan.BasicApp.Rbac.Services.Tasks.Dtos;
using XiHan.BasicApp.Rbac.Services.Tenants.Dtos;
using XiHan.BasicApp.Rbac.Services.UserPermissions.Dtos;
using XiHan.BasicApp.Rbac.Services.Users.Dtos;
using XiHan.BasicApp.Rbac.Services.UserSecurities.Dtos;
using XiHan.BasicApp.Rbac.Services.UserSessions.Dtos;

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
            ParentRoleId = entity.ParentRoleId,
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

    #region 访问日志扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static AccessLogDto ToDto(this SysAccessLog entity)
    {
        return new AccessLogDto
        {
            BasicId = entity.BasicId,
            TenantId = entity.TenantId,
            UserId = entity.UserId,
            UserName = entity.UserName,
            SessionId = entity.SessionId,
            ResourcePath = entity.ResourcePath,
            ResourceName = entity.ResourceName,
            ResourceType = entity.ResourceType,
            Method = entity.Method,
            AccessResult = entity.AccessResult,
            StatusCode = entity.StatusCode,
            AccessIp = entity.AccessIp,
            AccessLocation = entity.AccessLocation,
            UserAgent = entity.UserAgent,
            Browser = entity.Browser,
            Os = entity.Os,
            Device = entity.Device,
            Referer = entity.Referer,
            ResponseTime = entity.ResponseTime,
            ResponseSize = entity.ResponseSize,
            AccessTime = entity.AccessTime,
            LeaveTime = entity.LeaveTime,
            StayTime = entity.StayTime,
            ErrorMessage = entity.ErrorMessage,
            ExtendData = entity.ExtendData,
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
    public static List<AccessLogDto> ToDto(this IEnumerable<SysAccessLog> entities)
    {
        return entities.Adapt<List<AccessLogDto>>();
    }

    #endregion

    #region API日志扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static ApiLogDto ToDto(this SysApiLog entity)
    {
        return new ApiLogDto
        {
            BasicId = entity.BasicId,
            TenantId = entity.TenantId,
            UserId = entity.UserId,
            UserName = entity.UserName,
            RequestId = entity.RequestId,
            SessionId = entity.SessionId,
            ApiPath = entity.ApiPath,
            ApiName = entity.ApiName,
            ApiDescription = entity.ApiDescription,
            Method = entity.Method,
            ControllerName = entity.ControllerName,
            ActionName = entity.ActionName,
            RequestParams = entity.RequestParams,
            RequestBody = entity.RequestBody,
            ResponseBody = entity.ResponseBody,
            StatusCode = entity.StatusCode,
            RequestHeaders = entity.RequestHeaders,
            ResponseHeaders = entity.ResponseHeaders,
            RequestIp = entity.RequestIp,
            RequestLocation = entity.RequestLocation,
            UserAgent = entity.UserAgent,
            Browser = entity.Browser,
            Os = entity.Os,
            Referer = entity.Referer,
            RequestTime = entity.RequestTime,
            ResponseTime = entity.ResponseTime,
            ExecutionTime = entity.ExecutionTime,
            RequestSize = entity.RequestSize,
            ResponseSize = entity.ResponseSize,
            IsSuccess = entity.IsSuccess,
            ErrorMessage = entity.ErrorMessage,
            ExceptionStackTrace = entity.ExceptionStackTrace,
            ApiVersion = entity.ApiVersion,
            BusinessType = entity.BusinessType,
            ExtendData = entity.ExtendData,
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
    public static List<ApiLogDto> ToDto(this IEnumerable<SysApiLog> entities)
    {
        return entities.Select(e => e.ToDto()).ToList();
    }

    #endregion

    #region 审核日志扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static AuditLogDto ToDto(this SysAuditLog entity)
    {
        return new AuditLogDto
        {
            BasicId = entity.BasicId,
            AuditId = entity.AuditId,
            AuditorId = entity.AuditorId,
            AuditLevel = entity.AuditLevel,
            AuditResult = entity.AuditResult,
            AuditOpinion = entity.AuditOpinion,
            AuditTime = entity.AuditTime,
            BeforeStatus = entity.BeforeStatus,
            AfterStatus = entity.AfterStatus,
            AuditIp = entity.AuditIp,
            AuditLocation = entity.AuditLocation,
            Browser = entity.Browser,
            Os = entity.Os,
            Attachments = entity.Attachments,
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
    public static List<AuditLogDto> ToDto(this IEnumerable<SysAuditLog> entities)
    {
        return entities.Select(e => e.ToDto()).ToList();
    }

    #endregion

    #region 操作日志扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static OperationLogDto ToDto(this SysOperationLog entity)
    {
        return new OperationLogDto
        {
            BasicId = entity.BasicId,
            TenantId = entity.TenantId,
            UserId = entity.UserId,
            UserName = entity.UserName,
            OperationType = entity.OperationType,
            Module = entity.Module,
            Function = entity.Function,
            Title = entity.Title,
            Description = entity.Description,
            Method = entity.Method,
            RequestUrl = entity.RequestUrl,
            RequestParams = entity.RequestParams,
            ResponseResult = entity.ResponseResult,
            ExecutionTime = entity.ExecutionTime,
            OperationIp = entity.OperationIp,
            OperationLocation = entity.OperationLocation,
            Browser = entity.Browser,
            Os = entity.Os,
            Status = entity.Status,
            ErrorMessage = entity.ErrorMessage,
            OperationTime = entity.OperationTime,
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
    public static List<OperationLogDto> ToDto(this IEnumerable<SysOperationLog> entities)
    {
        return entities.Select(e => e.ToDto()).ToList();
    }

    #endregion

    #region 登录日志扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static LoginLogDto ToDto(this SysLoginLog entity)
    {
        return new LoginLogDto
        {
            BasicId = entity.BasicId,
            UserId = entity.UserId,
            UserName = entity.UserName,
            LoginIp = entity.LoginIp,
            LoginLocation = entity.LoginLocation,
            Browser = entity.Browser,
            Os = entity.Os,
            Status = entity.Status,
            Message = entity.Message,
            LoginTime = entity.LoginTime,
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
    public static List<LoginLogDto> ToDto(this IEnumerable<SysLoginLog> entities)
    {
        return entities.Adapt<List<LoginLogDto>>();
    }

    #endregion

    #region 任务日志扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static TaskLogDto ToDto(this SysTaskLog entity)
    {
        return new TaskLogDto
        {
            BasicId = entity.BasicId,
            TaskId = entity.TaskId,
            TaskCode = entity.TaskCode,
            TaskName = entity.TaskName,
            BatchNumber = entity.BatchNumber,
            ServerName = entity.ServerName,
            ProcessId = entity.ProcessId,
            ThreadId = entity.ThreadId,
            TaskStatus = entity.TaskStatus,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            ExecutionTime = entity.ExecutionTime,
            ExecutionResult = entity.ExecutionResult,
            ExceptionMessage = entity.ExceptionMessage,
            ExceptionStackTrace = entity.ExceptionStackTrace,
            OutputLog = entity.OutputLog,
            MemoryUsage = entity.MemoryUsage,
            CpuUsage = entity.CpuUsage,
            RetryCount = entity.RetryCount,
            TriggerMode = entity.TriggerMode,
            ExtendData = entity.ExtendData,
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
    public static List<TaskLogDto> ToDto(this IEnumerable<SysTaskLog> entities)
    {
        return entities.Adapt<List<TaskLogDto>>();
    }

    #endregion

    #region 任务扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static TaskDto ToDto(this SysTask entity)
    {
        return new TaskDto
        {
            BasicId = entity.BasicId,
            TenantId = entity.TenantId,
            TaskCode = entity.TaskCode,
            TaskName = entity.TaskName,
            TaskDescription = entity.TaskDescription,
            TaskGroup = entity.TaskGroup,
            TaskClass = entity.TaskClass,
            TaskMethod = entity.TaskMethod,
            TaskParams = entity.TaskParams,
            TriggerType = entity.TriggerType,
            CronExpression = entity.CronExpression,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            NextRunTime = entity.NextRunTime,
            LastRunTime = entity.LastRunTime,
            IntervalSeconds = entity.IntervalSeconds,
            RepeatCount = entity.RepeatCount,
            ExecutedCount = entity.ExecutedCount,
            TimeoutSeconds = entity.TimeoutSeconds,
            TaskStatus = entity.TaskStatus,
            Priority = entity.Priority,
            AllowConcurrent = entity.AllowConcurrent,
            RetryCount = entity.RetryCount,
            MaxRetryCount = entity.MaxRetryCount,
            Status = entity.Status,
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
    public static List<TaskDto> ToDto(this IEnumerable<SysTask> entities)
    {
        return entities.Adapt<List<TaskDto>>();
    }

    #endregion

    #region 审核扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static AuditDto ToDto(this SysAudit entity)
    {
        return new AuditDto
        {
            BasicId = entity.BasicId,
            TenantId = entity.TenantId,
            Title = entity.Title,
            Content = entity.Content,
            BusinessType = entity.BusinessType,
            BusinessId = entity.BusinessId,
            BusinessData = entity.BusinessData,
            SubmitterId = entity.SubmitterId,
            AuditorId = entity.AuditorId,
            AuditStatus = entity.AuditStatus,
            AuditResult = entity.AuditResult,
            AuditOpinion = entity.AuditOpinion,
            SubmitTime = entity.SubmitTime,
            AuditTime = entity.AuditTime,
            Priority = entity.Priority,
            IsMultiLevel = entity.IsMultiLevel,
            CurrentLevel = entity.CurrentLevel,
            TotalLevel = entity.TotalLevel,
            Deadline = entity.Deadline,
            Attachments = entity.Attachments,
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
    public static List<AuditDto> ToDto(this IEnumerable<SysAudit> entities)
    {
        return entities.Adapt<List<AuditDto>>();
    }

    #endregion

    #region 字典扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static DictDto ToDto(this SysDict entity)
    {
        return new DictDto
        {
            BasicId = entity.BasicId,
            DictCode = entity.DictCode,
            DictName = entity.DictName,
            DictType = entity.DictType,
            DictDescription = entity.DictDescription,
            IsBuiltIn = entity.IsBuiltIn,
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
    public static List<DictDto> ToDto(this IEnumerable<SysDict> entities)
    {
        return entities.Adapt<List<DictDto>>();
    }

    #endregion

    #region 字典项扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static DictItemDto ToDto(this SysDictItem entity)
    {
        return new DictItemDto
        {
            BasicId = entity.BasicId,
            DictId = entity.DictId,
            DictCode = entity.DictCode,
            ParentId = entity.ParentId,
            ItemCode = entity.ItemCode,
            ItemName = entity.ItemName,
            ItemValue = entity.ItemValue,
            ItemDescription = entity.ItemDescription,
            ExtendField1 = entity.ExtendField1,
            ExtendField2 = entity.ExtendField2,
            ExtendField3 = entity.ExtendField3,
            IsDefault = entity.IsDefault,
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
    public static List<DictItemDto> ToDto(this IEnumerable<SysDictItem> entities)
    {
        return entities.Adapt<List<DictItemDto>>();
    }

    #endregion

    #region 配置扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static ConfigDto ToDto(this SysConfig entity)
    {
        return new ConfigDto
        {
            BasicId = entity.BasicId,
            TenantId = entity.TenantId,
            ConfigKey = entity.ConfigKey,
            ConfigName = entity.ConfigName,
            ConfigValue = entity.ConfigValue,
            DefaultValue = entity.DefaultValue,
            ConfigType = entity.ConfigType,
            DataType = entity.DataType,
            ConfigDescription = entity.ConfigDescription,
            IsBuiltIn = entity.IsBuiltIn,
            IsEncrypted = entity.IsEncrypted,
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
    public static List<ConfigDto> ToDto(this IEnumerable<SysConfig> entities)
    {
        return entities.Adapt<List<ConfigDto>>();
    }

    #endregion

    #region 文件扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static FileDto ToDto(this SysFile entity)
    {
        return new FileDto
        {
            BasicId = entity.BasicId,
            TenantId = entity.TenantId,
            FileName = entity.FileName,
            OriginalName = entity.OriginalName,
            FileExtension = entity.FileExtension,
            FileType = entity.FileType,
            MimeType = entity.MimeType,
            FileSize = entity.FileSize,
            FileHash = entity.FileHash,
            StoragePath = entity.StoragePath,
            AccessUrl = entity.AccessUrl,
            StorageType = entity.StorageType,
            BucketName = entity.BucketName,
            UploaderId = entity.UploaderId,
            UploadIp = entity.UploadIp,
            BusinessType = entity.BusinessType,
            BusinessId = entity.BusinessId,
            DownloadCount = entity.DownloadCount,
            LastDownloadTime = entity.LastDownloadTime,
            Status = entity.Status,
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
    public static List<FileDto> ToDto(this IEnumerable<SysFile> entities)
    {
        return entities.Adapt<List<FileDto>>();
    }

    #endregion

    #region 邮件扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static EmailDto ToDto(this SysEmail entity)
    {
        return new EmailDto
        {
            BasicId = entity.BasicId,
            TenantId = entity.TenantId,
            SenderId = entity.SenderId,
            ReceiverId = entity.ReceiverId,
            EmailType = entity.EmailType,
            FromEmail = entity.FromEmail,
            FromName = entity.FromName,
            ToEmail = entity.ToEmail,
            CcEmail = entity.CcEmail,
            BccEmail = entity.BccEmail,
            Subject = entity.Subject,
            Content = entity.Content,
            IsHtml = entity.IsHtml,
            Attachments = entity.Attachments,
            TemplateId = entity.TemplateId,
            TemplateParams = entity.TemplateParams,
            EmailStatus = entity.EmailStatus,
            ScheduledTime = entity.ScheduledTime,
            SendTime = entity.SendTime,
            RetryCount = entity.RetryCount,
            MaxRetryCount = entity.MaxRetryCount,
            ErrorMessage = entity.ErrorMessage,
            BusinessType = entity.BusinessType,
            BusinessId = entity.BusinessId,
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
    public static List<EmailDto> ToDto(this IEnumerable<SysEmail> entities)
    {
        return entities.Adapt<List<EmailDto>>();
    }

    #endregion

    #region 短信扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static SmsDto ToDto(this SysSms entity)
    {
        return new SmsDto
        {
            BasicId = entity.BasicId,
            TenantId = entity.TenantId,
            SenderId = entity.SenderId,
            ReceiverId = entity.ReceiverId,
            SmsType = entity.SmsType,
            ToPhone = entity.ToPhone,
            Content = entity.Content,
            TemplateId = entity.TemplateId,
            TemplateParams = entity.TemplateParams,
            Provider = entity.Provider,
            SmsStatus = entity.SmsStatus,
            ScheduledTime = entity.ScheduledTime,
            SendTime = entity.SendTime,
            ProviderMessageId = entity.ProviderMessageId,
            RetryCount = entity.RetryCount,
            MaxRetryCount = entity.MaxRetryCount,
            ErrorMessage = entity.ErrorMessage,
            Cost = entity.Cost,
            BusinessType = entity.BusinessType,
            BusinessId = entity.BusinessId,
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
    public static List<SmsDto> ToDto(this IEnumerable<SysSms> entities)
    {
        return entities.Adapt<List<SmsDto>>();
    }

    #endregion

    #region 通知扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static NotificationDto ToDto(this SysNotification entity)
    {
        return new NotificationDto
        {
            BasicId = entity.BasicId,
            TenantId = entity.TenantId,
            UserId = entity.UserId,
            SenderId = entity.SenderId,
            NotificationType = entity.NotificationType,
            Title = entity.Title,
            Content = entity.Content,
            Icon = entity.Icon,
            Link = entity.Link,
            BusinessType = entity.BusinessType,
            BusinessId = entity.BusinessId,
            NotificationStatus = entity.NotificationStatus,
            ReadTime = entity.ReadTime,
            SendTime = entity.SendTime,
            ExpireTime = entity.ExpireTime,
            IsGlobal = entity.IsGlobal,
            NeedConfirm = entity.NeedConfirm,
            Status = entity.Status,
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
    public static List<NotificationDto> ToDto(this IEnumerable<SysNotification> entities)
    {
        return entities.Adapt<List<NotificationDto>>();
    }

    #endregion

    #region OAuth应用扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static OAuthAppDto ToDto(this SysOAuthApp entity)
    {
        return new OAuthAppDto
        {
            BasicId = entity.BasicId,
            AppName = entity.AppName,
            AppDescription = entity.AppDescription,
            ClientId = entity.ClientId,
            ClientSecret = entity.ClientSecret,
            AppType = entity.AppType,
            GrantTypes = entity.GrantTypes,
            RedirectUris = entity.RedirectUris,
            Scopes = entity.Scopes,
            AccessTokenLifetime = entity.AccessTokenLifetime,
            RefreshTokenLifetime = entity.RefreshTokenLifetime,
            AuthorizationCodeLifetime = entity.AuthorizationCodeLifetime,
            Logo = entity.Logo,
            Homepage = entity.Homepage,
            SkipConsent = entity.SkipConsent,
            Status = entity.Status,
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
    public static List<OAuthAppDto> ToDto(this IEnumerable<SysOAuthApp> entities)
    {
        return entities.Adapt<List<OAuthAppDto>>();
    }

    #endregion

    #region OAuth授权码扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static OAuthCodeDto ToDto(this SysOAuthCode entity)
    {
        return new OAuthCodeDto
        {
            BasicId = entity.BasicId,
            Code = entity.Code,
            ClientId = entity.ClientId,
            UserId = entity.UserId,
            RedirectUri = entity.RedirectUri,
            Scopes = entity.Scopes,
            State = entity.State,
            CodeChallenge = entity.CodeChallenge,
            CodeChallengeMethod = entity.CodeChallengeMethod,
            ExpiresAt = entity.ExpiresAt,
            IsUsed = entity.IsUsed,
            UsedAt = entity.UsedAt,
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
    public static List<OAuthCodeDto> ToDto(this IEnumerable<SysOAuthCode> entities)
    {
        return entities.Adapt<List<OAuthCodeDto>>();
    }

    #endregion

    #region OAuth令牌扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static OAuthTokenDto ToDto(this SysOAuthToken entity)
    {
        return new OAuthTokenDto
        {
            BasicId = entity.BasicId,
            AccessToken = entity.AccessToken,
            RefreshToken = entity.RefreshToken,
            TokenType = entity.TokenType,
            ClientId = entity.ClientId,
            UserId = entity.UserId,
            GrantType = entity.GrantType,
            Scopes = entity.Scopes,
            AccessTokenExpiresAt = entity.AccessTokenExpiresAt,
            RefreshTokenExpiresAt = entity.RefreshTokenExpiresAt,
            IsRevoked = entity.IsRevoked,
            RevokedAt = entity.RevokedAt,
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
    public static List<OAuthTokenDto> ToDto(this IEnumerable<SysOAuthToken> entities)
    {
        return entities.Adapt<List<OAuthTokenDto>>();
    }

    #endregion

    #region 用户权限扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static UserPermissionDto ToDto(this SysUserPermission entity)
    {
        return new UserPermissionDto
        {
            BasicId = entity.BasicId,
            UserId = entity.UserId,
            PermissionId = entity.PermissionId,
            PermissionAction = entity.PermissionAction,
            EffectiveTime = entity.EffectiveTime,
            ExpirationTime = entity.ExpirationTime,
            Status = entity.Status,
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
    public static List<UserPermissionDto> ToDto(this IEnumerable<SysUserPermission> entities)
    {
        return entities.Adapt<List<UserPermissionDto>>();
    }

    #endregion

    #region 用户安全扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static UserSecurityDto ToDto(this SysUserSecurity entity)
    {
        return new UserSecurityDto
        {
            BasicId = entity.BasicId,
            UserId = entity.UserId,
            LastPasswordChangeTime = entity.LastPasswordChangeTime,
            PasswordExpiryTime = entity.PasswordExpiryTime,
            FailedLoginAttempts = entity.FailedLoginAttempts,
            LastFailedLoginTime = entity.LastFailedLoginTime,
            IsLocked = entity.IsLocked,
            LockoutTime = entity.LockoutTime,
            LockoutEndTime = entity.LockoutEndTime,
            TwoFactorEnabled = entity.TwoFactorEnabled,
            TwoFactorSecret = entity.TwoFactorSecret,
            SecurityStamp = entity.SecurityStamp,
            EmailVerified = entity.EmailVerified,
            PhoneVerified = entity.PhoneVerified,
            AllowMultiLogin = entity.AllowMultiLogin,
            MaxLoginDevices = entity.MaxLoginDevices,
            LastSecurityCheckTime = entity.LastSecurityCheckTime,
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
    public static List<UserSecurityDto> ToDto(this IEnumerable<SysUserSecurity> entities)
    {
        return entities.Adapt<List<UserSecurityDto>>();
    }

    #endregion

    #region 用户会话扩展

    /// <summary>
    /// 实体转DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    public static UserSessionDto ToDto(this SysUserSession entity)
    {
        return new UserSessionDto
        {
            BasicId = entity.BasicId,
            UserId = entity.UserId,
            Token = entity.Token,
            RefreshToken = entity.RefreshToken,
            SessionId = entity.SessionId,
            DeviceType = entity.DeviceType,
            DeviceName = entity.DeviceName,
            DeviceId = entity.DeviceId,
            OperatingSystem = entity.OperatingSystem,
            Browser = entity.Browser,
            IpAddress = entity.IpAddress,
            Location = entity.Location,
            LoginTime = entity.LoginTime,
            LastActivityTime = entity.LastActivityTime,
            TokenExpiresAt = entity.TokenExpiresAt,
            RefreshTokenExpiresAt = entity.RefreshTokenExpiresAt,
            IsOnline = entity.IsOnline,
            IsRevoked = entity.IsRevoked,
            RevokedAt = entity.RevokedAt,
            RevokedReason = entity.RevokedReason,
            LogoutTime = entity.LogoutTime,
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
    public static List<UserSessionDto> ToDto(this IEnumerable<SysUserSession> entities)
    {
        return entities.Adapt<List<UserSessionDto>>();
    }

    #endregion
}
