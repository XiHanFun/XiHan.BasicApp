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
    /// 一站式开通租户：确保版本、创建管理员账号、创建 Owner 角色并按版本白名单授权、绑定角色
    /// </summary>
    /// <param name="tenant">已创建的租户实体</param>
    /// <param name="adminUserName">管理员用户名</param>
    /// <param name="adminEmail">管理员邮箱（登录身份标识，全平台唯一）</param>
    /// <param name="passwordHash">管理员密码哈希</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的管理员用户</returns>
    Task<SysUser> ProvisionTenantAdminAsync(SysTenant tenant, string adminUserName, string adminEmail, string passwordHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// 初始化租户管理员账号
    /// </summary>
    /// <param name="tenant">已创建的租户实体</param>
    /// <param name="adminUserName">管理员用户名</param>
    /// <param name="adminEmail">管理员邮箱（登录身份标识，全平台唯一）</param>
    /// <param name="passwordHash">管理员密码哈希</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的管理员用户</returns>
    Task<SysUser> InitializeTenantAdminAsync(SysTenant tenant, string adminUserName, string adminEmail, string passwordHash, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// 套餐变更（含降级）后回收越界授权：将该租户超出其当前版本权限白名单的
    /// 角色权限/用户直授权限行置为失效（保留行以供审计追溯）
    /// </summary>
    /// <remarks>
    /// 与运行时门控语义一致：版本未绑定或白名单为空（门控未启用）时不做任何回收，避免误清。
    /// 运行时门控已保证越界权限不生效，本方法负责数据层面的存量清理（REQ-5.3）。
    /// </remarks>
    /// <param name="tenant">租户实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>回收（置失效）的授权行数</returns>
    Task<int> ReconcileTenantAuthorizationWithEditionAsync(SysTenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// 版本权限白名单收窄（撤销/停用映射）后，对绑定该版本的所有租户回收越界授权
    /// </summary>
    /// <param name="editionId">版本ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>回收（置失效）的授权行数合计</returns>
    Task<int> ReconcileEditionTenantsAuthorizationAsync(long editionId, CancellationToken cancellationToken = default);
}
