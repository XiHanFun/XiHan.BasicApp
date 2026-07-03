#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IChatAuditQueryService
// Guid:6c9e8d1f-4a5b-4c2d-e0a3-8f9a0b1c2d3e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 聊天审计查询应用服务接口（管理侧跨会话合规查询）
/// </summary>
public interface IChatAuditQueryService : IApplicationService
{
    /// <summary>
    /// 聊天消息审计分页
    /// </summary>
    Task<PageResultDtoBase<ChatAuditListItemDto>> GetChatMessagePageAsync(ChatAuditPageQueryDto input, CancellationToken cancellationToken = default);
}
