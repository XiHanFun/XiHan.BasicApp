#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IChatConversationRepository
// Guid:3e7f6a9b-1a8d-4fae-c2db-6d7e8f9a0b1c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
