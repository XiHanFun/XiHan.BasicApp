#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITenantProvisionDomainService
// Guid:a1b2c3d4-5e6f-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 租户开通领域服务
/// </summary>
/// <remarks>
/// 职责：编排租户开通全流程（创建租户 + 初始化管理员 + 分配默认角色）
/// 不处理事务，由调用方（应用服务）开启 UnitOfWork
/// </remarks>
public interface ITenantProvisionDomainService
{
    /// <summary>
    /// 初始化租户管理员账号
    /// </summary>
    /// <param name="tenant">已创建的租户实体</param>
    /// <param name="adminUserName">管理员用户名</param>
    /// <param name="passwordHash">管理员密码哈希</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的管理员用户</returns>
    Task<SysUser> InitializeTenantAdminAsync(SysTenant tenant, string adminUserName, string passwordHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// 为租户管理员分配默认角色
    /// </summary>
    /// <param name="tenant">租户实体</param>
    /// <param name="adminUserId">管理员用户ID</param>
    /// <param name="ownerRoleId">Owner 角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AssignAdminRoleAsync(SysTenant tenant, long adminUserId, long ownerRoleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 为租户分配默认版本
    /// </summary>
    /// <param name="tenant">租户实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分配的默认版本ID（null 表示无默认版本）</returns>
    Task<long?> AssignDefaultEditionAsync(SysTenant tenant, CancellationToken cancellationToken = default);
}
