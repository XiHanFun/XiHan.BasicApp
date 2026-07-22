// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Extensions;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 聊天审计查询应用服务（管理侧跨会话合规查询，权限 saas:chat:audit）
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "聊天审计")]
public sealed class ChatAuditQueryService
    : SaasApplicationService, IChatAuditQueryService
{
    private readonly ISqlSugarClientResolver _clientResolver;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ChatAuditQueryService(ISqlSugarClientResolver clientResolver)
    {
        _clientResolver = clientResolver;
    }

    private ISqlSugarClient DbClient => _clientResolver.GetCurrentClient();

    /// <inheritdoc />
    [HttpPost]
    [PermissionAuthorize(SaasPermissionCodes.Chat.Audit)]
    public async Task<PageResultDtoBase<ChatAuditListItemDto>> GetChatMessagePageAsync(ChatAuditPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.CreatedTimeStart.HasValue && input.CreatedTimeEnd.HasValue && input.CreatedTimeStart > input.CreatedTimeEnd)
        {
            throw new ArgumentOutOfRangeException(nameof(input.CreatedTimeStart), "发送开始时间不能晚于结束时间。");
        }

        var keyword = input.Keyword?.Trim();
        var query = DbClient.Queryable<SysChatMessage>()
            .WhereIF(input.ConversationId is > 0, message => message.ConversationId == input.ConversationId!.Value)
            .WhereIF(input.SenderUserId is > 0, message => message.SenderUserId == input.SenderUserId!.Value)
            .WhereIF(input.CreatedTimeStart.HasValue, message => message.CreatedTime >= input.CreatedTimeStart!.Value)
            .WhereIF(input.CreatedTimeEnd.HasValue, message => message.CreatedTime <= input.CreatedTimeEnd!.Value)
            .WhereIF(!input.IncludeRecalled, message => !message.IsRecalled)
            .WhereIF(!string.IsNullOrEmpty(keyword), message =>
                message.Content!.Contains(keyword!)
                || message.SenderUserName!.Contains(keyword!)
                || message.Attachments!.Contains(keyword!));

        // 前端选择的排序/区间过滤原样带入，无有效排序回退按发送时间倒序
        query = query.ApplyFilters(input.Conditions.Filters);
        query = input.Conditions.Sorts.Count > 0
            ? query.ApplySorts(input.Conditions.Sorts)
            : query.OrderByDescending(message => message.CreatedTime);

        RefAsync<int> totalCount = 0;
        var entities = await query
            .ToPageListAsync(input.Page.PageIndex, input.Page.PageSize, totalCount, cancellationToken);

        var page = new PageResultMetadata(input.Page.PageIndex, input.Page.PageSize, totalCount);
        if (entities.Count == 0)
        {
            return new PageResultDtoBase<ChatAuditListItemDto>([], page);
        }

        // 批量带出会话名称/类型（免逐行 JOIN）
        var conversationIds = entities.Select(message => message.ConversationId).Distinct().ToList();
        var conversations = await DbClient.Queryable<SysChatConversation>()
            .Where(conversation => conversationIds.Contains(conversation.BasicId))
            .ToListAsync(cancellationToken);
        var conversationMap = conversations.ToDictionary(conversation => conversation.BasicId);

        var items = entities.Select(message =>
        {
            var conversation = conversationMap.GetValueOrDefault(message.ConversationId);
            return new ChatAuditListItemDto
            {
                BasicId = message.BasicId,
                ConversationId = message.ConversationId,
                ConversationName = conversation?.ConversationName,
                ConversationType = conversation?.ConversationType ?? ChatConversationType.Single,
                SenderUserId = message.SenderUserId,
                SenderUserName = message.SenderUserName,
                MessageType = message.MessageType,
                Content = message.Content,
                FileName = BuildAttachmentSummary(message.Attachments),
                IsRecalled = message.IsRecalled,
                EditedTime = message.EditedTime,
                CreatedTime = message.CreatedTime
            };
        }).ToList();
        return new PageResultDtoBase<ChatAuditListItemDto>(items, page);
    }

    /// <summary>
    /// 附件列表 JSON → 审计展示用文件名摘要（多附件以逗号连接，无附件返回 null）
    /// </summary>
    private static string? BuildAttachmentSummary(string? attachmentsJson)
    {
        var names = ChatMessageAttachments.Deserialize(attachmentsJson)
            .Select(attachment => attachment.FileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();
        return names.Count > 0 ? string.Join("，", names) : null;
    }
}
