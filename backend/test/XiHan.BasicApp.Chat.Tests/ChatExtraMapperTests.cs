// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using XiHan.BasicApp.Chat.Application.Dtos;
using XiHan.BasicApp.Chat.Application.Mappers;
using XiHan.BasicApp.Chat.Domain.DomainServices;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Hubs;

namespace XiHan.BasicApp.Chat.Tests;

/// <summary>
/// 聊天应用层纯函数测试：附件 JSON 编解码、@ 名单解析、实体到 DTO 的字段守恒与实时方法名常量。
/// </summary>
/// <remarks>
/// 这些函数没有副作用却横跨前后端契约：附件列存的是 camelCase JSON，@ 串存的是逗号分隔文本，
/// 二者一旦编解码不对称，历史消息会整页丢附件或丢 @ 人；实时方法名是前端 <c>signalR.on</c> 的字面量，
/// 改一个字符就断链且不会有任何编译期信号。本文件把这些约定钉成会红的断言。
/// </remarks>
public sealed class ChatExtraMapperTests
{
    /// <summary>
    /// 空附件与 null 附件都必须落成 null，绝不落空数组。
    /// </summary>
    /// <remarks>落 "[]" 会让「有没有附件」的判断在 SQL LIKE 与前端两侧都要多一套特例。</remarks>
    [Fact]
    public void Serialize_NullOrEmptyShouldReturnNull()
    {
        Assert.Null(ChatMessageAttachments.Serialize(null));
        Assert.Null(ChatMessageAttachments.Serialize([]));
    }

    /// <summary>
    /// 附件列表序列化后再反序列化必须逐字段守恒，包括可空的大小与语音时长。
    /// </summary>
    [Fact]
    public void SerializeThenDeserialize_ShouldPreserveEveryField()
    {
        IReadOnlyList<ChatMessageAttachment> source =
        [
            new(FileId: 11, FileName: "报表.xlsx", FileSize: 2048, DurationSeconds: null),
            new(FileId: 12, FileName: "voice.m4a", FileSize: null, DurationSeconds: 37)
        ];

        var json = ChatMessageAttachments.Serialize(source);
        Assert.NotNull(json);

        var restored = ChatMessageAttachments.Deserialize(json);

        Assert.Equal(2, restored.Count);
        Assert.Equal(11, restored[0].FileId);
        Assert.Equal("报表.xlsx", restored[0].FileName);
        Assert.Equal(2048L, restored[0].FileSize);
        Assert.Null(restored[0].DurationSeconds);
        Assert.Equal(12, restored[1].FileId);
        Assert.Equal("voice.m4a", restored[1].FileName);
        Assert.Null(restored[1].FileSize);
        Assert.Equal(37, restored[1].DurationSeconds);
    }

    /// <summary>
    /// 附件 JSON 必须以 camelCase 键名存储，前端按同名字段直读。
    /// </summary>
    [Fact]
    public void Serialize_ShouldUseCamelCasePropertyNames()
    {
        var json = ChatMessageAttachments.Serialize([new ChatMessageAttachment(1, "a.png", 10, 5)])!;

        Assert.Contains("\"fileId\"", json, StringComparison.Ordinal);
        Assert.Contains("\"fileName\"", json, StringComparison.Ordinal);
        Assert.Contains("\"fileSize\"", json, StringComparison.Ordinal);
        Assert.Contains("\"durationSeconds\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"FileId\"", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// 单条脏数据不得拖垮整页历史：非法 JSON 一律降级为空附件列表而不是抛异常。
    /// </summary>
    /// <param name="json">附件列原始值。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-json")]
    [InlineData("{}")]
    [InlineData("[{\"fileId\":\"不是数字\"}]")]
    [InlineData("null")]
    public void Deserialize_DirtyValueShouldFallBackToEmpty(string? json)
    {
        Assert.Empty(ChatMessageAttachments.Deserialize(json));
    }

    /// <summary>
    /// 附件载荷的语音时长是可选参数，默认必须为空（非语音消息不写时长）。
    /// </summary>
    [Fact]
    public void ChatMessageAttachment_DurationSecondsShouldDefaultToNull()
    {
        var attachment = new ChatMessageAttachment(FileId: 1, FileName: "a.png", FileSize: 10);

        Assert.Null(attachment.DurationSeconds);
    }

    /// <summary>
    /// @ 串解析必须容忍脏值：空白项、非数字、非正数一律丢弃，只保留有效用户主键。
    /// </summary>
    /// <param name="raw">数据库中的 @ 用户ID 串。</param>
    /// <param name="expected">期望解析出的用户主键序列。</param>
    [Theory]
    [InlineData(null, new long[0])]
    [InlineData("", new long[0])]
    [InlineData("   ", new long[0])]
    [InlineData("1,2,3", new long[] { 1, 2, 3 })]
    [InlineData(" 1 , 2 ", new long[] { 1, 2 })]
    [InlineData("1,,2", new long[] { 1, 2 })]
    [InlineData("0,-5,abc,7", new long[] { 7 })]
    [InlineData("abc", new long[0])]
    public void ParseMentionedUserIds_ShouldTolerateDirtyValues(string? raw, long[] expected)
    {
        Assert.Equal(expected, ChatApplicationMapper.ParseMentionedUserIds(raw));
    }

    /// <summary>
    /// 发送 DTO 转领域命令必须逐字段带过去，附件与 @ 名单不得丢失。
    /// </summary>
    [Fact]
    public void ToSendCommand_ShouldCarryEveryFieldIncludingAttachmentsAndMentions()
    {
        var input = new ChatMessageSendDto
        {
            ConversationId = 100,
            MessageType = ChatMessageType.Image,
            Content = "看图",
            Attachments = [new ChatMessageAttachmentDto { FileId = 9, FileName = "a.png", FileSize = 66, DurationSeconds = 3 }],
            ClientMessageId = "client-1",
            ReplyToMessageId = 55,
            MentionedUserIds = [7, 8]
        };

        var command = ChatApplicationMapper.ToSendCommand(input, senderUserId: 42);

        Assert.Equal(100, command.ConversationId);
        Assert.Equal(42, command.SenderUserId);
        Assert.Equal(ChatMessageType.Image, command.MessageType);
        Assert.Equal("看图", command.Content);
        Assert.Equal("client-1", command.ClientMessageId);
        Assert.Equal(55L, command.ReplyToMessageId);
        Assert.Equal([7L, 8L], command.MentionedUserIds);
        Assert.NotNull(command.Attachments);
        var attachment = Assert.Single(command.Attachments);
        Assert.Equal(9, attachment.FileId);
        Assert.Equal("a.png", attachment.FileName);
        Assert.Equal(66L, attachment.FileSize);
        Assert.Equal(3, attachment.DurationSeconds);
    }

    /// <summary>
    /// 发送 DTO 无附件时命令的附件必须是 null 而不是空列表，交给领域层按消息类型判空。
    /// </summary>
    [Fact]
    public void ToSendCommand_WithoutAttachmentsShouldKeepNull()
    {
        var command = ChatApplicationMapper.ToSendCommand(
            new ChatMessageSendDto { ConversationId = 1, MessageType = ChatMessageType.Text, Content = "hi" },
            senderUserId: 1);

        Assert.Null(command.Attachments);
        Assert.Null(command.MentionedUserIds);
        Assert.Null(command.ReplyToMessageId);
    }

    /// <summary>
    /// 全部映射入口对 null 入参必须抛参数空异常，而不是产出半截 DTO。
    /// </summary>
    [Fact]
    public void Mappers_NullArgumentShouldThrow()
    {
        _ = Assert.ThrowsAny<ArgumentNullException>(() => ChatApplicationMapper.ToSendCommand(null!, 1));
        _ = Assert.ThrowsAny<ArgumentNullException>(() => ChatApplicationMapper.ToConversationDto(null!, created: true));
        _ = Assert.ThrowsAny<ArgumentNullException>(() => ChatApplicationMapper.ToMessageItemDto(null!));
        _ = Assert.ThrowsAny<ArgumentNullException>(() => ChatApplicationMapper.ToReactionItemDto(null!));
        _ = Assert.ThrowsAny<ArgumentNullException>(() => ChatApplicationMapper.ToMemberItemDto(null!, "张三"));
    }

    /// <summary>
    /// 会话摘要 DTO 的会话主键取实体主键，Created 由调用方显式传入而不是从实体推断。
    /// </summary>
    [Fact]
    public void ToConversationDto_ShouldProjectEntityIdAndExplicitCreatedFlag()
    {
        var conversation = new SysChatConversation
        {
            ConversationType = ChatConversationType.Group,
            ConversationName = "项目群"
        };
        SetEntityId(conversation, 321);

        var created = ChatApplicationMapper.ToConversationDto(conversation, created: true);
        var reused = ChatApplicationMapper.ToConversationDto(conversation, created: false);

        Assert.Equal(321, created.ConversationId);
        Assert.Equal(ChatConversationType.Group, created.ConversationType);
        Assert.Equal("项目群", created.ConversationName);
        Assert.True(created.Created);
        Assert.False(reused.Created);
    }

    /// <summary>
    /// 消息实体转 DTO 必须带出附件、@ 名单、回复快照与 Pin 标记；未传回应时回应列表为空集合而非 null。
    /// </summary>
    [Fact]
    public void ToMessageItemDto_ShouldProjectFullMessageShape()
    {
        var createdTime = DateTimeOffset.UtcNow.AddMinutes(-3);
        var editedTime = DateTimeOffset.UtcNow.AddMinutes(-1);
        var message = new SysChatMessage
        {
            ConversationId = 100,
            SenderUserId = 5,
            SenderUserName = "张三",
            MessageType = ChatMessageType.File,
            Content = "见附件",
            Attachments = ChatMessageAttachments.Serialize([new ChatMessageAttachment(9, "a.pdf", 100)]),
            ClientMessageId = "client-9",
            CreatedTime = createdTime,
            ReplyToMessageId = 90,
            ReplyPreview = "李四: 原文",
            EditedTime = editedTime,
            MentionedUserIds = "7,8",
            IsPinned = true
        };
        SetEntityId(message, 200);

        var dto = ChatApplicationMapper.ToMessageItemDto(message);

        Assert.Equal(200, dto.MessageId);
        Assert.Equal(100, dto.ConversationId);
        Assert.Equal(5, dto.SenderUserId);
        Assert.Equal("张三", dto.SenderUserName);
        Assert.Equal(ChatMessageType.File, dto.MessageType);
        Assert.Equal("见附件", dto.Content);
        Assert.False(dto.IsRecalled);
        Assert.Equal("client-9", dto.ClientMessageId);
        Assert.Equal(createdTime, dto.CreatedTime);
        Assert.Equal(90L, dto.ReplyToMessageId);
        Assert.Equal("李四: 原文", dto.ReplyPreview);
        Assert.Equal(editedTime, dto.EditedTime);
        Assert.Equal([7L, 8L], dto.MentionedUserIds);
        Assert.True(dto.IsPinned);
        Assert.Empty(dto.Reactions);
        var attachment = Assert.Single(dto.Attachments);
        Assert.Equal(9, attachment.FileId);
        Assert.Equal("a.pdf", attachment.FileName);
        Assert.Equal(100L, attachment.FileSize);
    }

    /// <summary>
    /// 带回应聚合时，回应必须按表情、用户ID、用户名快照原样投影到消息 DTO。
    /// </summary>
    [Fact]
    public void ToMessageItemDto_WithReactionsShouldProjectReactionSnapshots()
    {
        var message = new SysChatMessage { ConversationId = 100, SenderUserId = 5 };
        SetEntityId(message, 201);
        var reaction = new SysChatMessageReaction
        {
            ConversationId = 100,
            MessageId = 201,
            UserId = 6,
            UserName = "李四",
            Emoji = "👍"
        };

        var dto = ChatApplicationMapper.ToMessageItemDto(message, [reaction]);

        var item = Assert.Single(dto.Reactions);
        Assert.Equal("👍", item.Emoji);
        Assert.Equal(6, item.UserId);
        Assert.Equal("李四", item.UserName);
    }

    /// <summary>
    /// 成员 DTO 的用户名来自外部批量解析结果，成员行本身不存用户名；解析不到时保留 null。
    /// </summary>
    [Fact]
    public void ToMemberItemDto_ShouldTakeUserNameFromCaller()
    {
        var joinTime = DateTimeOffset.UtcNow.AddDays(-2);
        var member = new SysChatConversationMember
        {
            ConversationId = 100,
            UserId = 6,
            MemberRole = ChatMemberRole.Admin,
            IsSilenced = true,
            JoinTime = joinTime
        };

        var named = ChatApplicationMapper.ToMemberItemDto(member, "李四");
        var unresolved = ChatApplicationMapper.ToMemberItemDto(member, null);

        Assert.Equal(6, named.UserId);
        Assert.Equal("李四", named.UserName);
        Assert.Equal(ChatMemberRole.Admin, named.MemberRole);
        Assert.True(named.IsSilenced);
        Assert.Equal(joinTime, named.JoinTime);
        Assert.Null(unresolved.UserName);
    }

    /// <summary>
    /// 会话组名必须是 <c>chat:conv:{会话ID}</c>，Hub 进出组与 typing 组播都依赖这一格式。
    /// </summary>
    [Fact]
    public void ConversationGroup_ShouldUseStablePrefixedFormat()
    {
        Assert.Equal("chat:conv:1", ChatRealtimeMethods.ConversationGroup(1));
        Assert.Equal("chat:conv:9007199254740993", ChatRealtimeMethods.ConversationGroup(9007199254740993L));
    }

    /// <summary>
    /// 实时方法名是前端订阅的字面量契约，值必须与常量名逐字一致，改名即断链。
    /// </summary>
    [Fact]
    public void ChatRealtimeMethods_ClientMethodNamesShouldMatchTheirConstantNames()
    {
        var mismatched = typeof(ChatRealtimeMethods)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (field.Name, Value: (string)field.GetRawConstantValue()!))
            .Where(item => !string.Equals(item.Name, item.Value, StringComparison.Ordinal))
            .Select(item => $"{item.Name} = \"{item.Value}\"")
            .ToList();

        Assert.True(mismatched.Count == 0,
            $"下列 {mismatched.Count} 个实时方法名常量的值与常量名不一致，前端按常量名订阅会静默收不到消息：" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, mismatched)}");
    }

    /// <summary>
    /// 模拟 ORM 回填受保护的实体主键。
    /// </summary>
    /// <param name="entity">实体实例。</param>
    /// <param name="id">主键值。</param>
    private static void SetEntityId(object entity, long id)
    {
        var property = entity.GetType().GetProperty(
            "BasicId",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("未找到实体主键属性。");
        property.SetValue(entity, id);
    }
}
