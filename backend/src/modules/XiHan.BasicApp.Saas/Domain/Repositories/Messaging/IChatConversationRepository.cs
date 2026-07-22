// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 聊天会话仓储接口
/// </summary>
public interface IChatConversationRepository : ISaasRepository<SysChatConversation>
{
    /// <summary>
    /// 按单聊配对键查询（租户内唯一）
    /// </summary>
    Task<SysChatConversation?> GetByPairKeyAsync(string pairKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按部门查询部门群（租户内同一部门至多一个）
    /// </summary>
    Task<SysChatConversation?> GetByDepartmentIdAsync(long departmentId, CancellationToken cancellationToken = default);
}
