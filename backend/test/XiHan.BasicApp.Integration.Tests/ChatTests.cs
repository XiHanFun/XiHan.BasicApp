// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Chat;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Jobs;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Organization;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedulers;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证在线聊天
/// </summary>
/// <remarks>
/// 单聊按两人唯一，群聊的治理（加人、移出、退群、改信息与公告、转让群主、设管理员、禁言），部门群只有本部门的人能进、随部门归属事件同步成员；
/// 消息的发送、撤回、编辑、回应、置顶、已读与搜索；附件以会话为访问范围、服务端转发、聊天审计的详情与成员（方案 H1、H2、H4）；
/// 敏感词与保留清理读 <c>chat.policy</c>；推送经聊天 Hub 在提交之后发出；平台一侧同样能聊天；模块可以关掉
/// </remarks>
public sealed class ChatTests : IDisposable
{
    private const string Chatter = "chatter";
    private const string Organizer = "organizer";
    private const string Password = "Chat@123456";
    private static readonly TimeSpan _wait = TimeSpan.FromSeconds(10);
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task SingleChat_SendsRecallsEditsReactsPinsAndReads()
    {
        var (server, users) = await StartAsync();
        await using var running = server;
        var (alice, bob) = (users["alice"], users["bob"]);

        // 同一对用户只有一个单聊；和自己单聊、对端不在这一侧都被拒
        var (_, opened) = await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-single", new { peerUserId = bob }, alice);
        Assert.True(opened.GetProperty("created").GetBoolean());
        var conversation = opened.GetProperty("conversationId").GetString()!;
        var (_, again) = await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-single", new { peerUserId = alice }, bob);
        Assert.Equal((conversation, false), (again.GetProperty("conversationId").GetString(), again.GetProperty("created").GetBoolean()));
        Assert.Equal("CHAT_VALIDATION_001", (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-single", new { peerUserId = alice }, alice)).Body.GetProperty("code").GetString());
        Assert.Equal("CHAT_VALIDATION_021", (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-single", new { peerUserId = users["erin"] }, alice)).Body.GetProperty("code").GetString());

        // 发消息：序号递增，对端未读加一；文本为空、提及非成员、回复别处的消息都被拒
        var first = await SendTextAsync(server, alice, conversation, "hello bob");
        var second = await SendTextAsync(server, bob, conversation, "hi alice", replyTo: first.GetProperty("messageId").GetString());
        Assert.Equal((1L, 2L), (first.GetProperty("seq").GetInt64(), second.GetProperty("seq").GetInt64()));
        Assert.Equal("alice: hello bob", second.GetProperty("replyPreview").GetString());
        Assert.Equal("CHAT_VALIDATION_007", (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:send", new { conversationId = conversation, messageType = "Text", content = "   " }, alice)).Body.GetProperty("code").GetString());
        Assert.Equal("CHAT_VALIDATION_012", (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:send", new { conversationId = conversation, messageType = "Text", content = "@carol", mentionedUserIds = new[] { users["carol"] } }, alice)).Body.GetProperty("code").GetString());
        Assert.Equal("CHAT_VALIDATION_010", (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:send", new { conversationId = conversation, messageType = "System", content = "x" }, alice)).Body.GetProperty("code").GetString());
        Assert.Equal("CHAT_DENIED_001", (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:send", new { conversationId = conversation, messageType = "Text", content = "intruder" }, users["carol"])).Body.GetProperty("code").GetString());
        var mine = await ConversationAsync(server, alice, conversation);
        Assert.Equal((1, "hi alice", 2L), (mine.GetProperty("unreadCount").GetInt32(), mine.GetProperty("lastMessagePreview").GetString(), mine.GetProperty("lastSeq").GetInt64()));
        Assert.Equal("bob", mine.GetProperty("displayName").GetString());

        // 已读：清零、已读位前移，对端能看到
        var (_, read) = await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:mark-read", new { conversationId = conversation }, alice);
        Assert.Equal(2L, read.GetProperty("lastReadSeq").GetInt64());
        Assert.Equal(0, (await ConversationAsync(server, alice, conversation)).GetProperty("unreadCount").GetInt32());
        var (_, positions) = await CallAsync(server, HttpMethod.Get, $"/api/chat-conversations:read-positions?conversationId={conversation}", null, bob);
        Assert.Equal(2L, positions.GetProperty("items").EnumerateArray().Single(p => p.GetProperty("userId").GetString() == alice).GetProperty("lastReadSeq").GetInt64());

        // 编辑只限本人的文本；回应再点即取消；单聊双方都能置顶消息
        var firstId = first.GetProperty("messageId").GetString()!;
        Assert.Equal("CHAT_DENIED_005", (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:edit", new { messageId = firstId, content = "hacked" }, bob)).Body.GetProperty("code").GetString());
        var (_, edited) = await CallAsync(server, HttpMethod.Post, "/api/chat-messages:edit", new { messageId = firstId, content = "hello, bob" }, alice);
        Assert.Equal("hello, bob", edited.GetProperty("content").GetString());
        Assert.True(edited.TryGetProperty("editedAt", out var editedAt) && editedAt.ValueKind == JsonValueKind.String);
        Assert.True((await CallAsync(server, HttpMethod.Post, "/api/chat-messages:react", new { messageId = firstId, emoji = "👍" }, bob)).Body.GetProperty("added").GetBoolean());
        Assert.True((await CallAsync(server, HttpMethod.Post, "/api/chat-messages:react", new { messageId = firstId, emoji = "🎉" }, alice)).Body.GetProperty("added").GetBoolean());
        Assert.False((await CallAsync(server, HttpMethod.Post, "/api/chat-messages:react", new { messageId = firstId, emoji = "🎉" }, alice)).Body.GetProperty("added").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:pin", new { messageId = firstId }, bob)).Status);
        var (_, pinned) = await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:pinned?conversationId={conversation}", null, alice);
        Assert.Equal(firstId, Assert.Single(pinned.GetProperty("items").EnumerateArray()).GetProperty("messageId").GetString());

        // 历史：按序号正序、带回应；以某条为中心定位；搜索不含撤回的
        var (_, history) = await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:history?conversationId={conversation}&take=10", null, bob);
        var items = history.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(["hello, bob", "hi alice"], items.Select(static m => m.GetProperty("content").GetString()));
        Assert.Equal("👍", Assert.Single(items[0].GetProperty("reactions").EnumerateArray()).GetProperty("emoji").GetString());
        Assert.False(history.GetProperty("hasMore").GetBoolean());
        var (_, around) = await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:history?conversationId={conversation}&aroundMessageId={second.GetProperty("messageId").GetString()}&take=2", null, bob);
        Assert.Equal(2, around.GetProperty("items").GetArrayLength());
        var (_, found) = await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:search?conversationId={conversation}&keyword=alice", null, alice);
        Assert.Equal("hi alice", Assert.Single(found.GetProperty("items").EnumerateArray()).GetProperty("content").GetString());

        // 撤回：本人、限时内，清空正文，最后一条时刷新会话预览（预览按撤回人那次调用的区域写进库里）
        Assert.Equal("CHAT_DENIED_005", (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:recall", new { messageId = second.GetProperty("messageId").GetString() }, alice)).Body.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:recall", new { messageId = second.GetProperty("messageId").GetString() }, bob, "zh-CN")).Status);
        Assert.Equal("CHAT_VALIDATION_017", (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:recall", new { messageId = second.GetProperty("messageId").GetString() }, bob)).Body.GetProperty("code").GetString());
        Assert.Equal("[消息已撤回]", (await ConversationAsync(server, alice, conversation, "zh-CN")).GetProperty("lastMessagePreview").GetString());
        var (_, afterRecall) = await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:search?conversationId={conversation}&keyword=alice", null, alice);
        Assert.Equal(0, afterRecall.GetProperty("items").GetArrayLength());

        // 个人设置只改自己的
        Assert.True((await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:pin", new { conversationId = conversation, isPinned = true }, alice)).Body.GetProperty("isOn").GetBoolean());
        Assert.True((await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:mute", new { conversationId = conversation, isMuted = true }, alice)).Body.GetProperty("isOn").GetBoolean());
        var aliceView = await ConversationAsync(server, alice, conversation);
        Assert.Equal((true, true), (aliceView.GetProperty("isPinned").GetBoolean(), aliceView.GetProperty("isMuted").GetBoolean()));
        Assert.False((await ConversationAsync(server, bob, conversation)).GetProperty("isPinned").GetBoolean());
    }

    [Fact]
    public async Task GroupChat_GovernanceFollows4x()
    {
        var (server, users) = await StartAsync();
        await using var running = server;
        var (alice, bob, carol, dave) = (users["alice"], users["bob"], users["carol"], users["dave"]);
        Assert.Equal("CHAT_VALIDATION_002", (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:create-group", new { conversationName = "solo", memberUserIds = Array.Empty<string>() }, alice)).Body.GetProperty("code").GetString());
        var (created, group) = await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:create-group", new { conversationName = "Team", memberUserIds = new[] { bob } }, alice, "zh-CN");
        Assert.Equal(HttpStatusCode.OK, created);
        var conversation = group.GetProperty("conversationId").GetString()!;
        Assert.Equal("alice 创建了群聊", (await HistoryAsync(server, bob, conversation)).Last().GetProperty("content").GetString());

        // 加人要群主或管理员；普通成员加不了
        Assert.Equal("CHAT_DENIED_002", (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:add-members", new { conversationId = conversation, userIds = new[] { carol } }, bob)).Body.GetProperty("code").GetString());
        var (_, added) = await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:add-members", new { conversationId = conversation, userIds = new[] { carol, dave } }, alice, "zh-CN");
        Assert.Equal(2, added.GetProperty("succeeded").GetArrayLength());
        Assert.Equal("alice 邀请 carol、dave 加入群聊", (await HistoryAsync(server, bob, conversation)).Last().GetProperty("content").GetString());
        Assert.Equal(4, (await ConversationAsync(server, alice, conversation)).GetProperty("memberCount").GetInt32());

        // 设管理员只有群主；管理员与群主不能被禁言；禁言的成员不能发
        Assert.Equal("CHAT_DENIED_003", (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:set-member-role", new { conversationId = conversation, userId = carol, memberRole = "Admin" }, bob)).Body.GetProperty("code").GetString());
        var (_, role) = await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:set-member-role", new { conversationId = conversation, userId = bob, memberRole = "Admin" }, alice);
        Assert.Equal("Admin", role.GetProperty("memberRole").GetString());
        var (_, silenced) = await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:silence-members", new { conversationId = conversation, userIds = new[] { carol, alice }, isSilenced = true }, bob);
        Assert.Equal(carol, Assert.Single(silenced.GetProperty("succeeded").EnumerateArray()).GetString());
        Assert.Equal("CHAT_DENIED_007", Assert.Single(silenced.GetProperty("failed").EnumerateArray()).GetProperty("code").GetString());
        Assert.Equal("CHAT_DENIED_004", (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:send", new { conversationId = conversation, messageType = "Text", content = "let me talk" }, carol)).Body.GetProperty("code").GetString());

        // 公告变了以操作人的身份进消息流；群主不能退群、不能被移出
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:update-info", new { conversationId = conversation, conversationName = "Core team", announcement = "Ship on Friday" }, bob)).Status);
        var announcement = (await HistoryAsync(server, carol, conversation)).Last();
        Assert.Equal(("System", "Ship on Friday", bob), (announcement.GetProperty("messageType").GetString(), announcement.GetProperty("content").GetString(), announcement.GetProperty("senderUserId").GetString()));
        Assert.Equal("Core team", (await ConversationAsync(server, carol, conversation)).GetProperty("displayName").GetString());
        Assert.Equal("CHAT_VALIDATION_005", (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:leave", new { conversationId = conversation }, alice)).Body.GetProperty("code").GetString());
        var (_, removed) = await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:remove-members", new { conversationId = conversation, userIds = new[] { alice, dave, users["erin"] } }, bob);
        Assert.Equal(dave, Assert.Single(removed.GetProperty("succeeded").EnumerateArray()).GetString());
        Assert.Equal(["CHAT_DENIED_001", "CHAT_VALIDATION_005"], removed.GetProperty("failed").EnumerateArray().Select(static f => f.GetProperty("code").GetString()).Order());
        Assert.Equal("CHAT_DENIED_001", (await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:history?conversationId={conversation}", null, dave)).Body.GetProperty("code").GetString());

        // 转让群主：旧群主降为普通成员，之后可以退群
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:transfer-owner", new { conversationId = conversation, newOwnerUserId = bob }, alice)).Status);
        var (_, members) = await CallAsync(server, HttpMethod.Get, $"/api/chat-conversations:members?conversationId={conversation}", null, alice);
        Assert.Equal(["Owner", "Member", "Member"], members.GetProperty("items").EnumerateArray().Select(static m => m.GetProperty("memberRole").GetString()));
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:leave", new { conversationId = conversation }, alice)).Status);
        Assert.DoesNotContain(await MineAsync(server, alice), c => c.GetProperty("conversationId").GetString() == conversation);
        Assert.Equal(2, (await ConversationAsync(server, bob, conversation)).GetProperty("memberCount").GetInt32());
    }

    [Fact]
    public async Task DepartmentChat_OnlyForTheDepartment_AndFollowsMembershipEvents()
    {
        var (server, users) = await StartAsync();
        await using var running = server;
        var organizer = TestTokens.Issue("root", "acme", Organizer);
        var (_, department) = await CallAsync(server, HttpMethod.Post, "/api/departments", new { code = "rnd", name = "R&D" }, organizer);
        var departmentId = department.GetProperty("id").GetString()!;
        foreach (var user in new[] { users["alice"], users["bob"] })
        {
            Assert.Equal(HttpStatusCode.Created, (await CallAsync(server, HttpMethod.Post, "/api/memberships", new { userId = user, departmentId }, organizer)).Status);
        }

        Assert.Equal("CHAT_DENIED_006", (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-department", new { departmentId }, users["carol"])).Body.GetProperty("code").GetString());
        var (_, opened) = await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-department", new { departmentId }, users["alice"]);
        Assert.Equal(("Department", "R&D", true), (opened.GetProperty("conversationType").GetString(), opened.GetProperty("conversationName").GetString(), opened.GetProperty("created").GetBoolean()));
        var conversation = opened.GetProperty("conversationId").GetString()!;
        Assert.Equal(2, (await ConversationAsync(server, users["bob"], conversation)).GetProperty("memberCount").GetInt32());

        // 部门群没有群主与管理员：改名、改公告都不行
        Assert.Equal("CHAT_DENIED_002", (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:update-info", new { conversationId = conversation, announcement = "x" }, users["alice"])).Body.GetProperty("code").GetString());

        // 进了部门即入群、移出部门即出群（经发件箱与订阅，按处理时的归属核对）
        var (_, carolJoined) = await CallAsync(server, HttpMethod.Post, "/api/memberships", new { userId = users["carol"], departmentId }, organizer);
        await EventuallyAsync(async () => (await MineAsync(server, users["carol"])).Any(c => c.GetProperty("conversationId").GetString() == conversation));
        Assert.Equal(3, (await ConversationAsync(server, users["alice"], conversation)).GetProperty("memberCount").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Delete, $"/api/memberships?ids={carolJoined.GetProperty("id").GetString()}", null, organizer)).Status);
        await EventuallyAsync(async () => (await MineAsync(server, users["carol"])).All(c => c.GetProperty("conversationId").GetString() != conversation));
        Assert.Equal(2, (await ConversationAsync(server, users["alice"], conversation)).GetProperty("memberCount").GetInt32());
    }

    [Fact]
    public async Task Presign_ComparesAttachmentIds_NotTheirFileNames()
    {
        var (server, users) = await StartAsync();
        await using var running = server;
        var (alice, bob, carol, dave) = (users["alice"], users["bob"], users["carol"], users["dave"]);
        var private1 = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-single", new { peerUserId = bob }, alice)).Body.GetProperty("conversationId").GetString()!;
        var carols = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-single", new { peerUserId = dave }, carol)).Body.GetProperty("conversationId").GetString()!;

        async Task<string> SendFileAsync(string user, string conversation, string name)
        {
            var request = BasicAppTestServer.Request(HttpMethod.Post, "/api/chat-messages/attachments", token: Token(user));
            var content = new ByteArrayContent("%PDF-1.4"u8.ToArray());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            request.Content = new MultipartFormDataContent { { content, "file", name } };
            var (_, uploaded) = await server.SendAsync(request);
            var fileId = uploaded.GetProperty("attachment").GetProperty("fileId").GetString()!;
            var (status, sent) = await CallAsync(server, HttpMethod.Post, "/api/chat-messages:send", new { conversationId = conversation, messageType = "File", attachments = new[] { new { fileId, fileName = name, size = 8L } } }, user);
            Assert.True(status == HttpStatusCode.OK, sent.ToString());
            return fileId;
        }

        // 别人会话里的文件：把它的 ID 写进自己附件的文件名，发到自己的会话，也换不到它的地址
        var secret = await SendFileAsync(alice, private1, "secret.pdf");
        var mine = await SendFileAsync(carol, carols, $"{secret}.pdf");
        var (denied, deniedBody) = await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:presign?conversationId={carols}&fileId={secret}&download=true", null, carol);
        Assert.Equal(HttpStatusCode.NotFound, denied);
        Assert.Equal("CHAT_NOTFOUND_005", deniedBody.GetProperty("code").GetString());

        // 自己的附件照常
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:presign?conversationId={carols}&fileId={mine}", null, dave)).Status);
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:presign?conversationId={private1}&fileId={secret}", null, bob)).Status);
    }

    [Fact]
    public async Task Attachments_AreScopedToTheConversation_AndMessagesForwardServerSide()
    {
        var (server, users) = await StartAsync();
        await using var running = server;
        var (alice, bob, carol, dave) = (users["alice"], users["bob"], users["carol"], users["dave"]);
        var single = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-single", new { peerUserId = bob }, alice)).Body.GetProperty("conversationId").GetString()!;
        var group = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:create-group", new { conversationName = "Docs", memberUserIds = new[] { carol } }, alice)).Body.GetProperty("conversationId").GetString()!;
        var unrelated = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-single", new { peerUserId = dave }, carol)).Body.GetProperty("conversationId").GetString()!;

        // 上传只要聊天的发送权限；文件名与大小以文件记录为准
        var upload = BasicAppTestServer.Request(HttpMethod.Post, "/api/chat-messages/attachments", token: Token(alice));
        var form = new MultipartFormDataContent();
        var bytes = new ByteArrayContent("%PDF-1.4 chat attachment"u8.ToArray());
        bytes.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(bytes, "file", "spec.pdf");
        upload.Content = form;
        var (uploaded, file) = await server.SendAsync(upload);
        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        var fileId = file.GetProperty("attachment").GetProperty("fileId").GetString()!;
        Assert.Equal("spec.pdf", file.GetProperty("attachment").GetProperty("fileName").GetString());

        // 发送时核对附件是自己在聊天里上传的
        Assert.Equal("CHAT_DENIED_009", (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:send", new { conversationId = single, messageType = "File", attachments = new[] { new { fileId, fileName = "x.pdf" } } }, bob)).Body.GetProperty("code").GetString());
        var (_, sent) = await CallAsync(server, HttpMethod.Post, "/api/chat-messages:send", new { conversationId = single, messageType = "File", content = "see spec", attachments = new[] { new { fileId, fileName = "renamed.pdf", fileSize = 1 } } }, alice, "zh-CN");
        var attachment = Assert.Single(sent.GetProperty("attachments").EnumerateArray());
        Assert.Equal("spec.pdf", attachment.GetProperty("fileName").GetString());
        Assert.Equal("[文件] spec.pdf", (await ConversationAsync(server, bob, single, "zh-CN")).GetProperty("lastMessagePreview").GetString());

        // 会话成员凭会话换签名地址；不是成员、别的会话都换不到
        var (signed, url) = await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:presign?conversationId={single}&fileId={fileId}", null, bob);
        Assert.Equal(HttpStatusCode.OK, signed);
        Assert.False(string.IsNullOrEmpty(url.GetProperty("url").GetString()));
        Assert.Equal("CHAT_DENIED_001", (await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:presign?conversationId={single}&fileId={fileId}", null, carol)).Body.GetProperty("code").GetString());
        Assert.Equal("CHAT_NOTFOUND_005", (await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:presign?conversationId={unrelated}&fileId={fileId}", null, carol)).Body.GetProperty("code").GetString());

        // 服务端转发：原样复制类型、正文与附件，不是成员的会话逐项失败；转发之后群成员也能凭群换地址
        var (_, forwarded) = await CallAsync(server, HttpMethod.Post, "/api/chat-messages:forward", new { messageId = sent.GetProperty("messageId").GetString(), conversationIds = new[] { group, unrelated } }, alice);
        Assert.Equal(group, Assert.Single(forwarded.GetProperty("succeeded").EnumerateArray()).GetString());
        Assert.Equal(("CHAT_DENIED_001", unrelated), (Assert.Single(forwarded.GetProperty("failed").EnumerateArray()).GetProperty("code").GetString(), forwarded.GetProperty("failed")[0].GetProperty("id").GetString()));
        var copy = (await HistoryAsync(server, carol, group)).Last();
        Assert.Equal(("File", "see spec", alice), (copy.GetProperty("messageType").GetString(), copy.GetProperty("content").GetString(), copy.GetProperty("senderUserId").GetString()));
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:presign?conversationId={group}&fileId={fileId}", null, carol)).Status);

        // 群头像：文件主键须是自己在聊天里上传的；设上之后成员凭会话换得到它的地址
        async Task<string> UploadAsync(string user, string name)
        {
            var request = BasicAppTestServer.Request(HttpMethod.Post, "/api/chat-messages/attachments", token: Token(user));
            var content = new ByteArrayContent(Encoding.UTF8.GetBytes($"avatar of {name}"));
            content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            request.Content = new MultipartFormDataContent { { content, "file", name } };
            var (_, body) = await server.SendAsync(request);
            return body.GetProperty("attachment").GetProperty("fileId").GetString()!;
        }

        var logo = await UploadAsync(alice, "logo.png");
        var carols = await UploadAsync(carol, "carol.png");
        Assert.Equal("CHAT_NOTFOUND_005", (await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:presign?conversationId={group}&fileId={logo}", null, carol)).Body.GetProperty("code").GetString());
        Assert.Equal("CHAT_DENIED_009", (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:update-info", new { conversationId = group, avatar = carols }, alice)).Body.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:update-info", new { conversationId = group, avatar = logo.ToUpperInvariant() }, alice)).Status);
        Assert.Equal(logo, (await ConversationAsync(server, carol, group, "zh-CN")).GetProperty("avatar").GetString());
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:presign?conversationId={group}&fileId={logo}", null, carol)).Status);

        // 单聊的头像是对端用户的头像文件
        var aliceId = Guid.Parse(alice);
        var aliceAvatar = Guid.CreateVersion7();
        await SettingsTestHost.InvokeAsync(server, null, async ctx =>
        {
            var unitOfWork = ctx.Services.GetRequiredService<SqlSugarUnitOfWork>();
            await unitOfWork.EnsureTransactionAsync(ctx.CancellationToken);
            return await unitOfWork.ConnectionFor(typeof(SysUser)).Updateable<SysUser>().SetColumns(u => new SysUser { AvatarFileId = aliceAvatar }).Where(u => u.Id == aliceId).ExecuteCommandAsync(ctx.CancellationToken);
        });
        Assert.Equal(aliceAvatar.ToString(), (await ConversationAsync(server, bob, single, "zh-CN")).GetProperty("avatar").GetString());

        // 已撤回的不能转发
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:recall", new { messageId = copy.GetProperty("messageId").GetString() }, alice)).Status);
        Assert.Equal("CHAT_VALIDATION_022", (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:forward", new { messageId = copy.GetProperty("messageId").GetString(), conversationIds = new[] { single } }, alice)).Body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Audit_SeesEveryMessageOnTheSide_WithDetailsAndMembers()
    {
        var (server, users) = await StartAsync();
        await using var running = server;
        var (alice, bob) = (users["alice"], users["bob"]);
        var conversation = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-single", new { peerUserId = bob }, alice)).Body.GetProperty("conversationId").GetString()!;
        var kept = await SendTextAsync(server, alice, conversation, "audit me");
        var recalled = await SendTextAsync(server, alice, conversation, "oops");
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:recall", new { messageId = recalled.GetProperty("messageId").GetString() }, alice)).Status);
        var auditor = TestTokens.Issue("auditor", "acme", Chatter);

        // 不要求是成员；缺省不含已撤回的，includeRecalled 即都看
        var (_, page) = await CallAsync(server, HttpMethod.Post, "/api/chat-audit:list", new { page = new { index = 1, size = 20 } }, auditor);
        Assert.Equal("audit me", Assert.Single(page.GetProperty("items").EnumerateArray()).GetProperty("content").GetString());
        var (_, all) = await CallAsync(server, HttpMethod.Post, "/api/chat-audit:list", new { page = new { index = 1, size = 20 }, filters = new[] { new { field = "includeRecalled", @operator = "eq", value = "true" } } }, auditor);
        Assert.Equal(2, all.GetProperty("total").GetInt32());
        var (_, filtered) = await CallAsync(server, HttpMethod.Post, "/api/chat-audit:list", new { page = new { index = 1, size = 20 }, keyword = "audit", filters = new[] { new { field = "senderUserId", @operator = "eq", value = alice } } }, auditor);
        Assert.Equal(1, filtered.GetProperty("total").GetInt32());
        Assert.Equal("CHAT_VALIDATION_025", (await CallAsync(server, HttpMethod.Post, "/api/chat-audit:list", new { page = new { index = 1, size = 20 }, filters = new[] { new { field = "content", @operator = "eq", value = "x" } } }, auditor)).Body.GetProperty("code").GetString());

        var (_, detail) = await CallAsync(server, HttpMethod.Get, $"/api/chat-audit/{kept.GetProperty("messageId").GetString()}", null, auditor);
        Assert.Equal(("audit me", "Single", 1L), (detail.GetProperty("message").GetProperty("content").GetString(), detail.GetProperty("conversation").GetProperty("conversationType").GetString(), detail.GetProperty("seq").GetInt64()));
        var (_, members) = await CallAsync(server, HttpMethod.Get, $"/api/chat-audit:members?conversationId={conversation}", null, auditor);
        Assert.Equal([alice, bob], members.GetProperty("items").EnumerateArray().Select(static m => m.GetProperty("userId").GetString()).Order());

        // 别的租户看不到
        var (_, other) = await CallAsync(server, HttpMethod.Post, "/api/chat-audit:list", new { page = new { index = 1, size = 20 }, filters = new[] { new { field = "includeRecalled", @operator = "eq", value = "true" } } }, TestTokens.Issue("auditor", "globex", Chatter));
        Assert.Equal(0, other.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Policy_BlocksSensitiveWords_AndRetentionPurgesOldMessages()
    {
        var (server, users) = await StartAsync();
        await using var running = server;
        var (alice, bob) = (users["alice"], users["bob"]);
        var platform = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var conversation = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-single", new { peerUserId = bob }, alice)).Body.GetProperty("conversationId").GetString()!;
        var old = await SendTextAsync(server, alice, conversation, "an old message");
        Assert.True((await CallAsync(server, HttpMethod.Post, "/api/chat-messages:react", new { messageId = old.GetProperty("messageId").GetString(), emoji = "👍" }, bob)).Body.GetProperty("added").GetBoolean());
        await SendTextAsync(server, bob, conversation, "a recent message");

        // 平台参数：敏感词对租户同样生效（租户没有覆盖时取平台值）
        var (config, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs", JsonSerializer.Serialize(new { key = "chat.policy", name = "Chat policy", dataType = "Json", value = """{"retentionDays":30,"sensitiveWords":["Forbidden"]}""" }), platform));
        Assert.Equal(HttpStatusCode.Created, config.StatusCode);
        Assert.Equal("CHAT_VALIDATION_023", (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:send", new { conversationId = conversation, messageType = "Text", content = "this is forbidden" }, alice)).Body.GetProperty("code").GetString());

        // 保留清理：把第一条消息拨到 31 天前，作业删它与它的回应，近期的留着
        var oldId = Guid.Parse(old.GetProperty("messageId").GetString()!);
        await SettingsTestHost.InvokeAsync(server, "acme", async ctx =>
        {
            var unitOfWork = ctx.Services.GetRequiredService<SqlSugarUnitOfWork>();
            await unitOfWork.EnsureTransactionAsync(ctx.CancellationToken);
            var past = DateTimeOffset.UtcNow.AddDays(-31);
            return await unitOfWork.ConnectionFor(typeof(SysChatMessage)).Updateable<SysChatMessage>().SetColumns(m => new SysChatMessage { CreatedAt = past }).Where(m => m.Id == oldId).ExecuteCommandAsync(ctx.CancellationToken);
        });
        var ran = await SettingsTestHost.InvokeAsync(server, null, async ctx => await ctx.Services.GetRequiredService<ChatRetentionJob>().RunAsync(JobRun.Create(new JobName(ChatRetentionJob.Name), DateTimeOffset.UtcNow, JobTrigger.Manual), ctx));
        Assert.True(ran.IsSuccess, ran.Error?.Code);
        var remaining = await HistoryAsync(server, alice, conversation);
        Assert.Equal("a recent message", Assert.Single(remaining).GetProperty("content").GetString());
        var reactions = await SettingsTestHost.InvokeAsync(server, "acme", async ctx => await ctx.Services.GetRequiredService<SqlSugarUnitOfWork>().ConnectionFor(typeof(SysChatMessageReaction)).Queryable<SysChatMessageReaction>().Where(r => r.MessageId == oldId).CountAsync(ctx.CancellationToken));
        Assert.Equal(0, reactions);
    }

    [Fact]
    public async Task Hub_PushesMessagesAfterCommit_AndTypingToOtherMembers()
    {
        var (server, users) = await StartAsync();
        await using var running = server;
        var (alice, bob) = (users["alice"], users["bob"]);
        var conversation = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-single", new { peerUserId = bob }, alice)).Body.GetProperty("conversationId").GetString()!;
        var bobLogin = (await AuthFlowTests.LoginAsync(server, "bob", Password, "acme")).GetProperty("accessToken").GetString()!;
        var aliceLogin = (await AuthFlowTests.LoginAsync(server, "alice", Password, "acme")).GetProperty("accessToken").GetString()!;
        await using var bobHub = Connect(server, bobLogin);
        await using var aliceHub = Connect(server, aliceLogin);
        var received = Expect(bobHub, ChatRealtimeMethods.ReceiveChatMessage);
        var typing = Expect(bobHub, ChatRealtimeMethods.ChatTyping);
        var recalledPush = Expect(bobHub, ChatRealtimeMethods.ChatMessageRecalled);
        await bobHub.StartAsync();
        await aliceHub.StartAsync();
        await bobHub.InvokeAsync(ChatRealtimeMethods.JoinConversation, conversation);
        await aliceHub.InvokeAsync(ChatRealtimeMethods.JoinConversation, conversation);

        var sent = await SendTextAsync(server, alice, conversation, "pushed");
        var payload = await received.Task.WaitAsync(_wait);
        Assert.Equal(("pushed", conversation), (payload.GetProperty("message").GetProperty("content").GetString(), payload.GetProperty("conversation").GetProperty("conversationId").GetString()));

        await aliceHub.InvokeAsync(ChatRealtimeMethods.Typing, conversation);
        var typed = await typing.Task.WaitAsync(_wait);
        Assert.Equal((alice, "alice"), (typed.GetProperty("userId").GetString(), typed.GetProperty("userName").GetString()));

        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:recall", new { messageId = sent.GetProperty("messageId").GetString() }, alice)).Status);
        Assert.Equal(sent.GetProperty("messageId").GetString(), (await recalledPush.Task.WaitAsync(_wait)).GetProperty("messageId").GetString());

        // 失败的调用不推送：单聊不能加人
        var stray = Expect(bobHub, ChatRealtimeMethods.ChatConversationChanged);
        Assert.Equal("CHAT_VALIDATION_003", (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:add-members", new { conversationId = conversation, userIds = new[] { users["carol"] } }, alice)).Body.GetProperty("code").GetString());
        await Task.Delay(500);
        Assert.False(stray.Task.IsCompleted);
    }

    [Fact]
    public async Task PlatformSide_ChatsAmongPlatformAccountsOnly()
    {
        var (server, users) = await StartAsync();
        await using var running = server;
        var platform = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var (createdOps, operatorUser) = await CallAsync(server, HttpMethod.Post, "/api/users", new { userName = "ops", displayName = "Ops", password = Password }, platform);
        Assert.True(createdOps == HttpStatusCode.Created, operatorUser.ToString());
        var opsId = operatorUser.GetProperty("id").GetString()!;
        await server.SeedAsync([], [.. ChatPermissions.All.Select(p => new PermissionGrant(GrantSubject.ForUser(opsId), p.Code))]);
        var superadmin = (await CallAsync(server, HttpMethod.Get, "/api/auth:me", null, platform)).Body.GetProperty("user").GetProperty("id").GetString()!;
        var root = TestTokens.Issue(superadmin, null, Chatter);
        var (_, group) = await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:create-group", new { conversationName = "Platform", memberUserIds = new[] { opsId } }, root);
        var conversation = group.GetProperty("conversationId").GetString()!;
        await SendTextAsync(server, superadmin, conversation, "platform hello", tenant: null);
        var (_, history) = await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:history?conversationId={conversation}", null, TestTokens.Issue(opsId, null, Chatter));
        Assert.Equal("platform hello", history.GetProperty("items").EnumerateArray().Last().GetProperty("content").GetString());

        // 租户用户不能拉进平台会话；租户一侧看不到平台的会话
        Assert.Equal("CHAT_VALIDATION_021", (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:add-members", new { conversationId = conversation, userIds = new[] { users["alice"] } }, root)).Body.GetProperty("code").GetString());
        Assert.Equal("CHAT_NOTFOUND_001", (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:send", new { conversationId = conversation, messageType = "Text", content = "x" }, Token(users["alice"]))).Body.GetProperty("code").GetString());
        Assert.Equal(0, (await CallAsync(server, HttpMethod.Get, "/api/chat-conversations:department-tree", null, root)).Body.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task ChatModule_IsOnByDefault_AndCanBeSwitchedOff()
    {
        await using (var server = await BasicAppTestServer.StartAsync(_database.ConnectionString))
        {
            await server.RunSeedersAsync();
            var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
            var (_, permissions) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: superadmin));
            var pages = permissions.GetProperty("pages").EnumerateArray().Select(static p => p.GetString()).ToList();
            Assert.Contains("chat.conversations", pages);
            Assert.Contains("chat.audit", pages);
            Assert.Contains(permissions.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()), static p => p == ChatPermissions.ChatMessagesSend);
            using var anonymous = new HttpClient(server.Server.CreateHandler()) { BaseAddress = new Uri("http://localhost") };
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync(new Uri("/hubs/chat/negotiate?negotiateVersion=1", UriKind.Relative), null)).StatusCode);
        }

        await using (var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Modules:Chat:Enabled", "false")]))
        {
            await server.RunSeedersAsync();
            var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
            var (_, permissions) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: superadmin));
            Assert.DoesNotContain("chat.conversations", permissions.GetProperty("pages").EnumerateArray().Select(static p => p.GetString()));
            Assert.DoesNotContain(permissions.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()), static p => p!.StartsWith("chat.", StringComparison.Ordinal));
            Assert.Equal(HttpStatusCode.NotFound, (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/chat-conversations:mine", token: superadmin))).Response.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/chat-messages/attachments", token: superadmin))).Response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/openapi.json"))).Response.StatusCode);
        }

        await Assert.ThrowsAsync<ArgumentException>(() => BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Modules:Chat:Enabled", "maybe")]));
    }

    private static string Token(string userId) => TestTokens.Issue(userId, "acme", Chatter);

    /// <summary>
    /// 起宿主并建好用户
    /// </summary>
    /// <remarks>
    /// 租户 acme 与 globex，角色 chatter 持聊天的全部权限，organizer 持组织的部门与归属；acme 建 alice、bob、carol、dave，globex 建 erin，真实用户按用户授予聊天权限
    /// </remarks>
    private async Task<(BasicAppTestServer Server, Dictionary<string, string> Users)> StartAsync()
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        await server.SeedAsync([], [
            .. ChatPermissions.All.Select(static p => new PermissionGrant(GrantSubject.ForRole(Chatter), p.Code)),
            new PermissionGrant(GrantSubject.ForRole(Chatter), "identity.users.create"),
            new PermissionGrant(GrantSubject.ForRole(Organizer), OrganizationPermissions.DepartmentsCreate),
            new PermissionGrant(GrantSubject.ForRole(Organizer), OrganizationPermissions.MembershipsCreate),
            new PermissionGrant(GrantSubject.ForRole(Organizer), OrganizationPermissions.MembershipsDelete),
        ]);
        var users = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, tenant) in new[] { ("alice", "acme"), ("bob", "acme"), ("carol", "acme"), ("dave", "acme"), ("erin", "globex") })
        {
            var (created, user) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName = name, displayName = name.ToUpperInvariant(), password = Password }), TestTokens.Issue("root", tenant, "admin")));
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            users[name] = user.GetProperty("id").GetString()!;
        }

        // 真实用户按用户授予（令牌里的角色不替真实用户授权）
        await server.SeedAsync([], [.. users.Values.SelectMany(static id => ChatPermissions.All.Select(p => new PermissionGrant(GrantSubject.ForUser(id), p.Code)))]);
        return (server, users);
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> CallAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string tokenOrUser, string? locale = null)
    {
        var token = Guid.TryParse(tokenOrUser, out _) ? Token(tokenOrUser) : tokenOrUser;
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body is null ? null : JsonSerializer.Serialize(body), token, locale));
        return (response.StatusCode, json);
    }

    private static async Task<JsonElement> SendTextAsync(BasicAppTestServer server, string userId, string conversationId, string content, string? replyTo = null, string? tenant = "acme")
    {
        var (status, body) = await CallAsync(server, HttpMethod.Post, "/api/chat-messages:send", new { conversationId, messageType = "Text", content, replyToMessageId = replyTo }, TestTokens.Issue(userId, tenant, Chatter));
        Assert.True(status == HttpStatusCode.OK, body.ToString());
        return body;
    }

    private static async Task<List<JsonElement>> MineAsync(BasicAppTestServer server, string userId, string? locale = null)
        => [.. (await CallAsync(server, HttpMethod.Get, "/api/chat-conversations:mine", null, userId, locale)).Body.GetProperty("items").EnumerateArray()];

    private static async Task<JsonElement> ConversationAsync(BasicAppTestServer server, string userId, string conversationId, string? locale = null)
        => (await MineAsync(server, userId, locale)).Single(c => c.GetProperty("conversationId").GetString() == conversationId);

    private static async Task<List<JsonElement>> HistoryAsync(BasicAppTestServer server, string userId, string conversationId)
    {
        var (status, body) = await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:history?conversationId={conversationId}&take=50", null, userId);
        Assert.True(status == HttpStatusCode.OK, body.ToString());
        return [.. body.GetProperty("items").EnumerateArray()];
    }

    private static HubConnection Connect(BasicAppTestServer server, string token) => new HubConnectionBuilder()
        .WithUrl("http://localhost" + RealtimeHubs.Chat, options =>
        {
            options.HttpMessageHandlerFactory = _ => server.Server.CreateHandler();
            options.Transports = HttpTransportType.LongPolling;
            options.AccessTokenProvider = () => Task.FromResult<string?>(token);
        })
        .Build();

    private static TaskCompletionSource<JsonElement> Expect(HubConnection connection, string method)
    {
        var received = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<JsonElement>(method, payload => received.TrySetResult(payload));
        return received;
    }

    private static async Task EventuallyAsync(Func<Task<bool>> condition)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(100);
        }

        Assert.Fail("The condition was not met in time.");
    }
}
