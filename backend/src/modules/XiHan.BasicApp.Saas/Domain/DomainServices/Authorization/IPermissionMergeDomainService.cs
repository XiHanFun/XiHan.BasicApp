#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPermissionMergeDomainService
// Guid:c3d4e5f6-3333-4444-5555-666677778888
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.ValueObjects;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 权限合并领域服务
/// </summary>
/// <remarks>
/// 职责：合并用户直授 + 角色授权 + 委派授权，生成最终授权快照集合
/// 快照生成后供 IPermissionDecisionDomainService 做裁决
/// </remarks>
public interface IPermissionMergeDomainService
{
    /// <summary>
    /// 加载并合并用户的全部权限授权快照
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="roleIds">用户持有的角色ID集合（已展开继承链）</param>
    /// <param name="now">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>合并后的权限授权快照集合</returns>
    Task<IReadOnlyList<PermissionGrantSnapshot>> MergePermissionGrantsAsync(
        long userId,
        IEnumerable<long> roleIds,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
