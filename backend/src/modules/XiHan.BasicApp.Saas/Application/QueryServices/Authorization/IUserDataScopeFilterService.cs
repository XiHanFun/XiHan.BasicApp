// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 当前用户在“用户”资源上的数据范围过滤结果
/// </summary>
/// <param name="Unrestricted">是否不限制（超管 / 全部数据范围 / 无登录上下文）</param>
/// <param name="UserIds">可见用户主键集合（Unrestricted=false 时生效，始终包含本人）</param>
public sealed record UserDataScopeFilter(bool Unrestricted, IReadOnlyCollection<long> UserIds)
{
    /// <summary>
    /// 不限制数据范围
    /// </summary>
    public static UserDataScopeFilter Unlimited { get; } = new(true, []);
}

/// <summary>
/// 用户数据范围过滤服务：依据当前用户的角色数据范围（RoleDataScope）、用户级数据范围（UserDataScope）
/// 与部门归属，经 <c>DataScopeDecisionDomainService</c> 裁决出可见的用户主键集合，供列表查询施加数据范围过滤。
/// </summary>
public interface IUserDataScopeFilterService
{
    /// <summary>
    /// 解析当前用户可见的用户主键集合。
    /// </summary>
    Task<UserDataScopeFilter> ResolveAccessibleUsersAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
}
