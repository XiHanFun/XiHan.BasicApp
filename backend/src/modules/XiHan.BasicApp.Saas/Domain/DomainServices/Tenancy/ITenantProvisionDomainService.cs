// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 租户开通领域服务
/// </summary>
/// <remarks>
/// 职责：编排租户开通全流程（创建租户 + 初始化管理员 + 分配默认角色）
/// 不处理事务，由调用方（应用服务）开启 UnitOfWork
/// <para>
/// 平台就是 0 号租户，写只能落在当前作用域：租户注册表与版本是平台数据，在平台作用域写；
/// 管理员账号、成员关系、Owner 角色与授权绑定是该租户的数据，切入该租户写（行的 <c>TenantId</c> 由作用域决定）。
/// 库隔离租户的数据归置尚未完成，开通时显式拒绝，不把租户数据写进平台库。
/// </para>
/// </remarks>
public interface ITenantProvisionDomainService
{
    /// <summary>
    /// 一站式开通租户管理员：创建管理员账号、创建 Owner 角色并按租户绑定的版本白名单授权、绑定角色
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

    /// <summary>
    /// 取待初始化管理员的租户：可开通（库隔离租户的独立库已配置完成）、还没有所有者
    /// </summary>
    /// <param name="tenantId">租户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户实体</returns>
    Task<SysTenant> GetTenantAwaitingAdminAsync(long tenantId, CancellationToken cancellationToken = default);
}
