#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPermissionDomainService
// Guid:5d6e7f80-9102-1234-def0-123456789a03
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 权限领域服务接口
/// </summary>
public interface IPermissionDomainService
{
    /// <summary>
    /// 创建权限
    /// </summary>
    Task<SysPermission> CreateAsync(SysPermission permission);

    /// <summary>
    /// 更新权限
    /// </summary>
    Task<SysPermission> UpdateAsync(SysPermission permission);

    /// <summary>
    /// 删除权限
    /// </summary>
    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// 获取用户权限代码
    /// </summary>
    Task<IReadOnlyCollection<string>> GetUserPermissionCodesAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断用户是否有权限
    /// </summary>
    Task<bool> HasPermissionAsync(long userId, string permissionCode, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户数据范围部门 ID
    /// </summary>
    Task<IReadOnlyCollection<long>> GetUserDataScopeDepartmentIdsAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);
}
