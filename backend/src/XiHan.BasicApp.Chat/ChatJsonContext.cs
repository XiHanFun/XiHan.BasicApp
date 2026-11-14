// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Chat.Domain.Services;
using XiHan.BasicApp.Chat.Operations.ChatAudit;
using XiHan.BasicApp.Chat.Operations.ChatCalls;
using XiHan.BasicApp.Chat.Operations.ChatConversations;
using XiHan.BasicApp.Chat.Operations.ChatMessages;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Organization;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Contracts.Realtime;

namespace XiHan.BasicApp.Chat;

/// <summary>
/// 聊天模块的源生成序列化上下文
/// </summary>
/// <remarks>
/// 操作的请求与响应、推送载荷、附件、通话记录、聊天记录与策略参数的 JSON，以及订阅的部门归属与在线状态事件；枚举按名称
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(ChatEmptyRequest))]
[JsonSerializable(typeof(ChatConversationRequest))]
[JsonSerializable(typeof(OpenSingleConversationRequest))]
[JsonSerializable(typeof(CreateGroupConversationRequest))]
[JsonSerializable(typeof(OpenDepartmentConversationRequest))]
[JsonSerializable(typeof(ChatConversationUsersRequest))]
[JsonSerializable(typeof(UpdateConversationInfoRequest))]
[JsonSerializable(typeof(TransferOwnerRequest))]
[JsonSerializable(typeof(SilenceMembersRequest))]
[JsonSerializable(typeof(SetMemberRoleRequest))]
[JsonSerializable(typeof(PinConversationRequest))]
[JsonSerializable(typeof(MuteConversationRequest))]
[JsonSerializable(typeof(MarkReadRequest))]
[JsonSerializable(typeof(ChatUserOptionsRequest))]
[JsonSerializable(typeof(ChatConversationOpenResponse))]
[JsonSerializable(typeof(ChatConversationsResponse))]
[JsonSerializable(typeof(ChatMembersResponse))]
[JsonSerializable(typeof(ChatMemberItem))]
[JsonSerializable(typeof(ChatReadPositionsResponse))]
[JsonSerializable(typeof(ChatReadPosition))]
[JsonSerializable(typeof(ChatDepartmentTreeResponse))]
[JsonSerializable(typeof(ChatUserOptionsResponse))]
[JsonSerializable(typeof(ChatToggleResponse))]
[JsonSerializable(typeof(ChatConversationIdResponse))]
[JsonSerializable(typeof(ChatMessageHistoryRequest))]
[JsonSerializable(typeof(ChatMessageSearchRequest))]
[JsonSerializable(typeof(SendChatMessageRequest))]
[JsonSerializable(typeof(ChatMessageRequest))]
[JsonSerializable(typeof(EditChatMessageRequest))]
[JsonSerializable(typeof(ReactChatMessageRequest))]
[JsonSerializable(typeof(ForwardChatMessageRequest))]
[JsonSerializable(typeof(FastUploadChatAttachmentRequest))]
[JsonSerializable(typeof(PresignChatAttachmentRequest))]
[JsonSerializable(typeof(ChatMessageItem))]
[JsonSerializable(typeof(ChatMessagePageResponse))]
[JsonSerializable(typeof(ChatMessagesResponse))]
[JsonSerializable(typeof(ChatMessageIdResponse))]
[JsonSerializable(typeof(ChatReactionResponse))]
[JsonSerializable(typeof(ChatAttachmentResponse))]
[JsonSerializable(typeof(ChatAttachmentUploadResponse))]
[JsonSerializable(typeof(ChatAttachmentUrlResponse))]
[JsonSerializable(typeof(IReadOnlyList<ChatMessageAttachment>))]
[JsonSerializable(typeof(ChatRecord))]
[JsonSerializable(typeof(PageRequest))]
[JsonSerializable(typeof(PageResult<ChatAuditMessageResponse>))]
[JsonSerializable(typeof(GetChatAuditMessageRequest))]
[JsonSerializable(typeof(ChatAuditMessageDetailResponse))]
[JsonSerializable(typeof(ChatAuditMembersResponse))]
[JsonSerializable(typeof(BatchResult))]
[JsonSerializable(typeof(ChatMessagePushPayload))]
[JsonSerializable(typeof(ChatMessageRecalledPayload))]
[JsonSerializable(typeof(ChatConversationChangedPayload))]
[JsonSerializable(typeof(ChatTypingPayload))]
[JsonSerializable(typeof(ChatMessageEditedPayload))]
[JsonSerializable(typeof(ChatReactionChangedPayload))]
[JsonSerializable(typeof(ChatReadPositionChangedPayload))]
[JsonSerializable(typeof(ChatPresenceChangedPayload))]
[JsonSerializable(typeof(ChatAssistantDeltaPayload))]
[JsonSerializable(typeof(ChatAssistantCompletedPayload))]
[JsonSerializable(typeof(ChatAssistantsRequest))]
[JsonSerializable(typeof(ChatAssistantsResponse))]
[JsonSerializable(typeof(OpenAssistantConversationRequest))]
[JsonSerializable(typeof(ChatAssistantOpenResponse))]
[JsonSerializable(typeof(ReplyAssistantRequest))]
[JsonSerializable(typeof(ReplyAssistantResponse))]
[JsonSerializable(typeof(StartChatCallRequest))]
[JsonSerializable(typeof(ChatCallRequest))]
[JsonSerializable(typeof(ChatCallEmptyRequest))]
[JsonSerializable(typeof(ChatCallItem))]
[JsonSerializable(typeof(ChatActiveCallResponse))]
[JsonSerializable(typeof(ChatIceServersResponse))]
[JsonSerializable(typeof(ChatCallRecord))]
[JsonSerializable(typeof(ChatCallSettingsResponse))]
[JsonSerializable(typeof(ChatCallSignalPayload))]
[JsonSerializable(typeof(ChatPolicySettings))]
[JsonSerializable(typeof(MembershipChanged))]
[JsonSerializable(typeof(PresenceChanged))]
public sealed partial class ChatJsonContext : JsonSerializerContext;
