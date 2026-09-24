// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 当前上下文的用户目录
/// </summary>
/// <remarks>
/// 「看得见哪些用户」由上下文决定，而不是账号的注册地：平台里是平台账号；租户里是本租户已接受的成员，
/// 含注册在别处的外部成员。账号是账号域数据（严格隔离、注册地租户戳），成员的账号按成员关系跨租户读取。
/// 身份类操作（资料、密码、锁定、启停、删除）只对当前上下文注册的账号开放，见 <see cref="IsHomeAccount"/>。
/// </remarks>
public interface IUserDirectory
{
    /// <summary>
    /// 当前上下文可见用户的分页
    /// </summary>
    /// <param name="request">分页请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户分页</returns>
    Task<PageResultDtoBase<SysUser>> GetPagedAsync(PageRequestDtoBase request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 当前上下文可见的用户，不可见时返回 null
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户</returns>
    Task<SysUser?> FindAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 当前上下文可见用户里启用账号的主键
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>启用账号主键</returns>
    Task<IReadOnlyList<long>> GetEnabledIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 是否为当前上下文注册的账号（身份类操作只对它们开放；租户里的外部成员返回 false）
    /// </summary>
    /// <param name="user">用户</param>
    /// <returns>是否本上下文注册的账号</returns>
    bool IsHomeAccount(SysUser user);
}
