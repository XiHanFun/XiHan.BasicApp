#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserRepository
// Guid:66af7d1e-c9c4-4d8d-913f-2bb82edab1bf
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 用户仓储接口
/// </summary>
public interface IUserRepository : ISaasAggregateRepository<SysUser>
{
    /// <summary>
    /// 根据当前租户和用户名获取用户
    /// </summary>
    /// <remarks>
    /// 经 CreateQueryable 的全局租户过滤（AOP）按当前租户上下文隔离，与唯一索引 UX_TeId_UsNa 语义一致。
    /// 注：登录路径的用户定位实际走框架 IUserStore（SaasUserStore，显式 WHERE TenantId + UserName）；本方法当前无调用方，仅为仓储能力预留。
    /// </remarks>
    Task<SysUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据当前租户和邮箱获取用户
    /// </summary>
    /// <remarks>
    /// 经 CreateQueryable 的全局租户过滤（AOP）按当前租户上下文隔离。邮箱列为非唯一索引（IX_Em），存在重复时取首条匹配。
    /// </remarks>
    Task<SysUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查当前租户下用户名是否存在
    /// </summary>
    Task<bool> ExistsUserNameAsync(string userName, long? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 忽略租户过滤，按主键获取用户（平台运维 / 跨租户切换场景使用，需上层做权限校验）
    /// </summary>
    /// <remarks>
    /// 多租户成员切换时，用户当前 token 的活动租户可能与 SysUser.TenantId（归属租户）不一致，
    /// 经全局租户过滤会查不到用户，故此处显式忽略租户过滤按主键定位。
    /// </remarks>
    Task<SysUser?> GetByIdIgnoreTenantAsync(long userId, CancellationToken cancellationToken = default);
}
