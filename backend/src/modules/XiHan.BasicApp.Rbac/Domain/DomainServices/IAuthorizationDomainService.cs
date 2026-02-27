#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAuthorizationDomainService
// Guid:5ddd6c00-a0fd-46c0-a9fc-420169c20dd9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:39:59
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Domain.DomainServices;

/// <summary>
/// 授权领域服务
/// </summary>
public interface IAuthorizationDomainService
{
    /// <summary>
    /// 获取用户权限编码
    /// </summary>
    Task<IReadOnlyCollection<string>> GetUserPermissionCodesAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断用户是否具备某权限
    /// </summary>
    Task<bool> HasPermissionAsync(long userId, string permissionCode, long? tenantId = null, CancellationToken cancellationToken = default);
}
