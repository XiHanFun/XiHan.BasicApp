// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
