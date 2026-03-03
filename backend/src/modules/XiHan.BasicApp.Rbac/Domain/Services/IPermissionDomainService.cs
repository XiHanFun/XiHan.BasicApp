#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPermissionDomainService
// Guid:ee8304f6-cb96-4ca3-a9f3-fc89173aa70d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 15:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Domain.Services;

/// <summary>
/// 权限规则领域服务
/// </summary>
public interface IPermissionDomainService
{
    /// <summary>
    /// 计算用户最终权限编码
    /// </summary>
    Task<IReadOnlyCollection<string>> GetUserPermissionCodesAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断用户是否具备权限
    /// </summary>
    Task<bool> HasPermissionAsync(long userId, string permissionCode, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 计算用户数据范围部门ID
    /// </summary>
    Task<IReadOnlyCollection<long>> GetUserDataScopeDepartmentIdsAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);
}
