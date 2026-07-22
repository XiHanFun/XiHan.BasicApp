// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    /// 检查邮箱是否已被占用（全平台范围，邮箱为登录身份标识须全局唯一）
    /// </summary>
    /// <param name="email">邮箱（调用方已 Trim）</param>
    /// <param name="excludeUserId">排除的用户主键（更新自身时传入）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<bool> ExistsEmailGloballyAsync(string email, long? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 忽略租户过滤，按主键获取用户（平台运维 / 跨租户切换场景使用，需上层做权限校验）
    /// </summary>
    /// <remarks>
    /// 多租户成员切换时，用户当前 token 的活动租户可能与 SysUser.TenantId（归属租户）不一致，
    /// 经全局租户过滤会查不到用户，故此处显式忽略租户过滤按主键定位。
    /// </remarks>
    Task<SysUser?> GetByIdIgnoreTenantAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按主键批量获取用户（忽略租户过滤）
    /// </summary>
    /// <remarks>
    /// 用于跨租户场景批量解析用户身份：跨租户成员（外部协作者/顾问）的 <see cref="SysUser"/> 属于来源租户，
    /// 而成员关系行属于目标租户，带租户过滤会解析不出他们的名字。
    /// </remarks>
    /// <param name="userIds">用户主键集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户列表（集合为空时返回空列表）</returns>
    Task<List<SysUser>> GetListByIdsIgnoreTenantAsync(IReadOnlyCollection<long> userIds, CancellationToken cancellationToken = default);
}
