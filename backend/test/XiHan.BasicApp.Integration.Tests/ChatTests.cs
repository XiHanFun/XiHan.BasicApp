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
/// 单聊按两人唯一，群聊的治理（加人、移出、退群、改信息与公告、转让群主、设管理员、禁言、解散），部门群只有本部门的人能进、随部门归属事件同步成员；
/// 消息的发送、撤回、编辑、回应、置顶、已读与搜索（会话内与跨会话），提及所有人只给群主与管理员；会话里图片与文件的汇总，聊天里看一个人的资料；附件以会话为访问范围、服务端转发（一条或多条，逐条或合并成一条聊天记录）、聊天审计的详情与成员（方案 H1、H2、H4）；
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
    public async Task Dissolve_IsForTheOwnerOfAGroup_AndKeepsTheHistoryForAudit()
    {
        var (server, users) = await StartAsync();
        await using var running = server;
        var (alice, bob, carol, dave) = (users["alice"], users["bob"], users["carol"], users["dave"]);
        var single = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-single", new { peerUserId = bob }, alice)).Body.GetProperty("conversationId").GetString()!;
        var group = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:create-group", new { conversationName = "Team", memberUserIds = new[] { bob, carol, dave } }, alice, "zh-CN")).Body.GetProperty("conversationId").GetString()!;
        var said = (await SendTextAsync(server, bob, group, "before the end")).GetProperty("messageId").GetString()!;
        // dave 先退群：解散时他不在里面
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:leave", new { conversationId = group }, dave, "zh-CN")).Status);

        async Task<(HttpStatusCode Status, JsonElement Body)> DissolveAsync(string conversationId, string caller)
            => await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:dissolve", new { conversationId }, caller, "zh-CN");
        async Task<string?> CodeOfAsync(HttpMethod method, string path, object? body, string caller)
            => (await CallAsync(server, method, path, body, caller)).Body.GetProperty("code").GetString();

        // 只在群聊、只有群主
        Assert.Equal("CHAT_VALIDATION_003", (await DissolveAsync(single, alice)).Body.GetProperty("code").GetString());
        Assert.Equal("CHAT_DENIED_003", (await DissolveAsync(group, bob)).Body.GetProperty("code").GetString());
        Assert.Equal("CHAT_NOTFOUND_001", (await DissolveAsync(Guid.CreateVersion7().ToString(), alice)).Body.GetProperty("code").GetString());
        var (status, dissolved) = await DissolveAsync(group, alice);
        Assert.True(status == HttpStatusCode.OK, dissolved.ToString());
        Assert.Equal(group, dissolved.GetProperty("conversationId").GetString());

        // 谁的列表里都没有了，在里面做什么都当它不在；单聊不受影响
        foreach (var member in new[] { alice, bob, carol })
        {
            Assert.DoesNotContain(await MineAsync(server, member), c => c.GetProperty("conversationId").GetString() == group);
        }

        Assert.Contains(await MineAsync(server, alice), c => c.GetProperty("conversationId").GetString() == single);
        Assert.Equal("CHAT_DENIED_001", await CodeOfAsync(HttpMethod.Get, $"/api/chat-messages:history?conversationId={group}", null, bob));
        Assert.Equal("CHAT_NOTFOUND_001", await CodeOfAsync(HttpMethod.Post, "/api/chat-messages:send", new { conversationId = group, messageType = "Text", content = "anyone?" }, alice));
        Assert.Equal("CHAT_NOTFOUND_001", (await DissolveAsync(group, alice)).Body.GetProperty("code").GetString());
        Assert.Equal("CHAT_NOTFOUND_001", await CodeOfAsync(HttpMethod.Post, "/api/chat-conversations:add-members", new { conversationId = group, userIds = new[] { dave } }, alice));
        Assert.Equal("CHAT_NOTFOUND_001", await CodeOfAsync(HttpMethod.Post, "/api/chat-conversations:transfer-owner", new { conversationId = group, newOwnerUserId = bob }, alice));
        Assert.Equal("CHAT_NOTFOUND_001", await CodeOfAsync(HttpMethod.Post, "/api/chat-conversations:update-info", new { conversationId = group, conversationName = "Back" }, alice));
        Assert.Equal(0, (await CallAsync(server, HttpMethod.Get, "/api/chat-messages:search-all?keyword=before", null, alice)).Body.GetProperty("items").GetArrayLength());
        Assert.Equal(0, (await CallAsync(server, HttpMethod.Get, $"/api/chat-conversations:user-profile?userId={bob}", null, alice)).Body.GetProperty("commonGroups").GetArrayLength());

        // 聊天审计照样看得到：消息（末尾是解散提示）、会话的名字与解散时间、解散时还在里面的成员
        var auditor = TestTokens.Issue("auditor", "acme", Chatter);
        var (_, page) = await CallAsync(server, HttpMethod.Post, "/api/chat-audit:list", new { page = new { index = 1, size = 20 }, filters = new[] { new { field = "conversationId", @operator = "eq", value = group } }, sorts = new[] { new { field = "createdAt", descending = false } } }, auditor);
        Assert.Equal(["alice 创建了群聊", "before the end", "dave 退出群聊", "alice 解散了群聊"], page.GetProperty("items").EnumerateArray().Select(static m => m.GetProperty("content").GetString()));
        Assert.All(page.GetProperty("items").EnumerateArray(), static m => Assert.Equal(("Team", "Group"), (m.GetProperty("conversationName").GetString(), m.GetProperty("conversationType").GetString())));
        var (found, detail) = await CallAsync(server, HttpMethod.Get, $"/api/chat-audit/{said}", null, auditor);
        Assert.True(found == HttpStatusCode.OK, detail.ToString());
        Assert.Equal(("before the end", "Team", JsonValueKind.String), (detail.GetProperty("message").GetProperty("content").GetString(), detail.GetProperty("conversation").GetProperty("conversationName").GetString(), detail.GetProperty("conversation").GetProperty("dissolvedAt").ValueKind));
        var (_, members) = await CallAsync(server, HttpMethod.Get, $"/api/chat-audit:members?conversationId={group}", null, auditor);
        Assert.Equal(new[] { alice, bob, carol }.Order(), members.GetProperty("items").EnumerateArray().Select(static m => m.GetProperty("userId").GetString()).Order());
        Assert.Equal("Owner", members.GetProperty("items").EnumerateArray().Single(m => m.GetProperty("userId").GetString() == alice).GetProperty("memberRole").GetString());
        Assert.Equal(JsonValueKind.String, members.GetProperty("conversation").GetProperty("dissolvedAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, (await CallAsync(server, HttpMethod.Get, $"/api/chat-audit:members?conversationId={single}", null, auditor)).Body.GetProperty("conversation").GetProperty("dissolvedAt").ValueKind);
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
    public async Task UserOptions_PageByDisplayName_AndFilterByDepartmentSubtree()
    {
        var (server, users) = await StartAsync();
        await using var running = server;
        var organizer = TestTokens.Issue("root", "acme", Organizer);
        var (_, parent) = await CallAsync(server, HttpMethod.Post, "/api/departments", new { code = "rnd", name = "R&D" }, organizer);
        var parentId = parent.GetProperty("id").GetString()!;
        var (_, child) = await CallAsync(server, HttpMethod.Post, "/api/departments", new { code = "web", name = "Web", parentId }, organizer);
        var childId = child.GetProperty("id").GetString()!;
        var (_, other) = await CallAsync(server, HttpMethod.Post, "/api/departments", new { code = "ops", name = "Ops" }, organizer);
        var otherId = other.GetProperty("id").GetString()!;

        // alice 在上级部门，bob 在下级部门（主部门）又兼在别的部门，carol 只在别的部门，dave 没有任职
        foreach (var (user, departmentId) in new[] { (users["alice"], parentId), (users["bob"], childId), (users["bob"], otherId), (users["carol"], otherId) })
        {
            Assert.Equal(HttpStatusCode.Created, (await CallAsync(server, HttpMethod.Post, "/api/memberships", new { userId = user, departmentId }, organizer)).Status);
        }

        // 按显示名排序逐页取，多取一条判断后面还有没有；带主部门名称，没有任职的为空
        async Task<(List<JsonElement> Items, bool HasMore)> OptionsAsync(string query)
        {
            var (status, body) = await CallAsync(server, HttpMethod.Get, $"/api/chat-conversations:user-options?{query}", null, users["alice"]);
            Assert.True(status == HttpStatusCode.OK, body.ToString());
            return ([.. body.GetProperty("items").EnumerateArray()], body.GetProperty("hasMore").GetBoolean());
        }

        static string?[] NamesOf(List<JsonElement> items) => [.. items.Select(static u => u.GetProperty("userName").GetString())];
        var (everyone, _) = await OptionsAsync("limit=500");
        var chatters = everyone.Where(u => users.ContainsKey(u.GetProperty("userName").GetString()!)).ToList();
        Assert.Equal(["alice", "bob", "carol", "dave"], NamesOf(chatters));
        Assert.Equal(["R&D", "Web", "Ops", null], chatters.Select(static u => u.GetProperty("departmentName").GetString()));
        Assert.DoesNotContain("erin", NamesOf(everyone));
        var (firstPage, firstMore) = await OptionsAsync("limit=2");
        var (secondPage, _) = await OptionsAsync("skip=2&limit=2");
        Assert.True(firstMore);
        Assert.Equal(NamesOf([.. everyone.Take(4)]), NamesOf([.. firstPage, .. secondPage]));
        var (last, lastMore) = await OptionsAsync($"skip={everyone.Count - 1}&limit=2");
        Assert.Equal((1, false), (last.Count, lastMore));

        // 部门筛选取部门自身与全部下级；可与关键字叠加；没人的部门与别的租户的部门都是空
        Assert.Equal(["alice", "bob"], NamesOf((await OptionsAsync($"departmentId={parentId}")).Items));
        Assert.Equal(["bob"], NamesOf((await OptionsAsync($"departmentId={childId}")).Items));
        Assert.Equal(["bob", "carol"], NamesOf((await OptionsAsync($"departmentId={otherId}")).Items));
        Assert.Equal(["carol"], NamesOf((await OptionsAsync($"departmentId={otherId}&keyword=CAR")).Items));
        Assert.Empty((await OptionsAsync($"departmentId={Guid.NewGuid()}")).Items);
        var (_, foreign) = await CallAsync(server, HttpMethod.Get, $"/api/chat-conversations:user-options?departmentId={parentId}", null, TestTokens.Issue(users["erin"], "globex", Chatter));
        Assert.Equal(0, foreign.GetProperty("items").GetArrayLength());

        // 成员条目带头像文件
        var (_, group) = await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:create-group", new { conversationName = "Team", memberUserIds = new[] { users["bob"] } }, users["alice"]);
        var (_, members) = await CallAsync(server, HttpMethod.Get, $"/api/chat-conversations:members?conversationId={group.GetProperty("conversationId").GetString()}", null, users["alice"]);
        Assert.All(members.GetProperty("items").EnumerateArray(), static m => Assert.Equal(JsonValueKind.Null, m.GetProperty("avatarFileId").ValueKind));
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
        var (_, forwarded) = await CallAsync(server, HttpMethod.Post, "/api/chat-messages:forward", new { messageIds = new[] { sent.GetProperty("messageId").GetString() }, conversationIds = new[] { group, unrelated } }, alice);
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
        Assert.Equal("CHAT_VALIDATION_022", (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:forward", new { messageIds = new[] { copy.GetProperty("messageId").GetString() }, conversationIds = new[] { single } }, alice)).Body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Forward_TakesSeveralMessages_OneByOneOrMergedIntoARecord()
    {
        var (server, users) = await StartAsync();
        await using var running = server;
        var (alice, bob, carol, dave) = (users["alice"], users["bob"], users["carol"], users["dave"]);
        var single = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-single", new { peerUserId = bob }, alice)).Body.GetProperty("conversationId").GetString()!;
        var docs = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:create-group", new { conversationName = "Docs", memberUserIds = new[] { carol } }, alice)).Body.GetProperty("conversationId").GetString()!;
        var team = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:create-group", new { conversationName = "Team", memberUserIds = new[] { dave } }, alice)).Body.GetProperty("conversationId").GetString()!;

        async Task<(HttpStatusCode Status, JsonElement Body)> ForwardAsync(string user, string[] messageIds, string conversationId, bool merge = false, string? locale = null)
            => await CallAsync(server, HttpMethod.Post, "/api/chat-messages:forward", new { messageIds, conversationIds = new[] { conversationId }, merge }, user, locale);
        static (string? Type, string? Content) BodyOf(JsonElement message) => (message.GetProperty("messageType").GetString(), message.GetProperty("content").GetString());

        // 原会话里三条：文本、带附件的文件、对方的文本
        var upload = BasicAppTestServer.Request(HttpMethod.Post, "/api/chat-messages/attachments", token: Token(alice));
        var bytes = new ByteArrayContent("%PDF-1.4 forwarded"u8.ToArray());
        bytes.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        upload.Content = new MultipartFormDataContent { { bytes, "file", "spec.pdf" } };
        var fileId = (await server.SendAsync(upload)).Body.GetProperty("attachment").GetProperty("fileId").GetString()!;
        var one = (await SendTextAsync(server, alice, single, "第一条")).GetProperty("messageId").GetString()!;
        var two = (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:send", new { conversationId = single, messageType = "File", content = "see spec", attachments = new[] { new { fileId, fileName = "spec.pdf" } } }, alice)).Body.GetProperty("messageId").GetString()!;
        var three = (await SendTextAsync(server, bob, single, "three")).GetProperty("messageId").GetString()!;
        string[] picked = [three, one, two];

        // 逐条转发：不管给的次序，按原来的先后各成一条，发送人是转发的人
        var (_, copied) = await ForwardAsync(alice, picked, docs);
        Assert.Equal(docs, Assert.Single(copied.GetProperty("succeeded").EnumerateArray()).GetString());
        var copies = (await HistoryAsync(server, carol, docs))[^3..];
        Assert.Equal([("Text", "第一条"), ("File", "see spec"), ("Text", "three")], copies.Select(BodyOf));
        Assert.All(copies, copy => Assert.Equal(alice, copy.GetProperty("senderUserId").GetString()));

        // 合并转发：收成一条聊天记录，正文记着来自哪个会话与里面的每条消息（原文不转义），附件是里面所有消息的附件
        Assert.Equal(HttpStatusCode.OK, (await ForwardAsync(alice, picked, team, merge: true, locale: "zh-CN")).Status);
        var record = (await HistoryAsync(server, dave, team))[^1];
        var recordId = record.GetProperty("messageId").GetString()!;
        Assert.Equal(("Record", alice), (record.GetProperty("messageType").GetString(), record.GetProperty("senderUserId").GetString()));
        Assert.Equal(fileId, Assert.Single(record.GetProperty("attachments").EnumerateArray()).GetProperty("fileId").GetString());
        Assert.Contains("第一条", record.GetProperty("content").GetString(), StringComparison.Ordinal);
        using (var content = JsonDocument.Parse(record.GetProperty("content").GetString()!))
        {
            var root = content.RootElement;
            Assert.Equal(("Single", JsonValueKind.Null), (root.GetProperty("sourceType").GetString(), root.GetProperty("sourceName").ValueKind));
            Assert.Equal(["alice", "bob"], root.GetProperty("participants").EnumerateArray().Select(static name => name.GetString()));
            var items = root.GetProperty("items").EnumerateArray().ToList();
            Assert.Equal([("alice", "Text", "第一条"), ("alice", "File", "see spec"), ("bob", "Text", "three")], items.Select(static item => (item.GetProperty("senderUserName").GetString(), item.GetProperty("messageType").GetString(), item.GetProperty("content").GetString())));
            Assert.Equal("spec.pdf", Assert.Single(items[1].GetProperty("attachments").EnumerateArray()).GetProperty("fileName").GetString());
        }

        // 会话预览写「[聊天记录]」；目标会话的成员凭这条聊天记录换得到附件；它不进搜索，回复它时快照用预览文案
        Assert.Equal("[聊天记录]", (await ConversationAsync(server, dave, team, "zh-CN")).GetProperty("lastMessagePreview").GetString());
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:presign?conversationId={team}&fileId={fileId}", null, dave)).Status);
        Assert.Equal(0, (await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:search?conversationId={team}&keyword=three", null, dave)).Body.GetProperty("items").GetArrayLength());
        var (_, reply) = await CallAsync(server, HttpMethod.Post, "/api/chat-messages:send", new { conversationId = team, messageType = "Text", content = "got it", replyToMessageId = recordId }, dave, "zh-CN");
        Assert.Equal("alice: [聊天记录]", reply.GetProperty("replyPreview").GetString());

        // 聊天记录能再合并：套在里面，附件跟着走；来自群的记着群名
        var extra = (await SendTextAsync(server, dave, team, "extra")).GetProperty("messageId").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await ForwardAsync(alice, [extra, recordId], docs, merge: true)).Status);
        var nested = (await HistoryAsync(server, carol, docs))[^1];
        Assert.Equal(fileId, Assert.Single(nested.GetProperty("attachments").EnumerateArray()).GetProperty("fileId").GetString());
        using (var content = JsonDocument.Parse(nested.GetProperty("content").GetString()!))
        {
            var root = content.RootElement;
            Assert.Equal(("Group", "Team", 0), (root.GetProperty("sourceType").GetString(), root.GetProperty("sourceName").GetString(), root.GetProperty("participants").GetArrayLength()));
            var items = root.GetProperty("items").EnumerateArray().ToList();
            Assert.Equal(["Record", "Text"], items.Select(static item => item.GetProperty("messageType").GetString()));
            using var inner = JsonDocument.Parse(items[0].GetProperty("content").GetString()!);
            Assert.Equal(3, inner.RootElement.GetProperty("items").GetArrayLength());
        }

        // 聊天记录也能逐条转发：原样一条
        Assert.Equal(HttpStatusCode.OK, (await ForwardAsync(alice, [recordId], single)).Status);
        var again = (await HistoryAsync(server, bob, single))[^1];
        Assert.Equal(("Record", record.GetProperty("content").GetString()), BodyOf(again));

        // 不同会话的消息不能一起转；不存在的、已撤回的不能转
        Assert.Equal("CHAT_VALIDATION_034", (await ForwardAsync(alice, [one, extra], docs)).Body.GetProperty("code").GetString());
        Assert.Equal("CHAT_NOTFOUND_002", (await ForwardAsync(alice, [one, Guid.CreateVersion7().ToString()], docs)).Body.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:recall", new { messageId = extra }, dave)).Status);
        Assert.Equal("CHAT_VALIDATION_022", (await ForwardAsync(dave, [recordId, extra], team, merge: true)).Body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task MentionAll_IsForOwnersAndAdminsOfAGroup()
    {
        var (server, users) = await StartAsync();
        await using var running = server;
        var (alice, bob, carol) = (users["alice"], users["bob"], users["carol"]);
        var single = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-single", new { peerUserId = bob }, alice)).Body.GetProperty("conversationId").GetString()!;
        var group = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:create-group", new { conversationName = "Team", memberUserIds = new[] { bob, carol } }, alice)).Body.GetProperty("conversationId").GetString()!;

        async Task<(HttpStatusCode Status, JsonElement Body)> SendToAllAsync(string user, string conversationId)
            => await CallAsync(server, HttpMethod.Post, "/api/chat-messages:send", new { conversationId, messageType = "Text", content = "@all please read", mentionAll = true }, user);

        // 单聊不能提及所有人；群里只有群主与管理员能用
        Assert.Equal("CHAT_VALIDATION_033", (await SendToAllAsync(alice, single)).Body.GetProperty("code").GetString());
        Assert.Equal("CHAT_DENIED_002", (await SendToAllAsync(carol, group)).Body.GetProperty("code").GetString());
        var (status, sent) = await SendToAllAsync(alice, group);
        Assert.True(status == HttpStatusCode.OK, sent.ToString());
        Assert.Equal((true, 0), (sent.GetProperty("mentionAll").GetBoolean(), sent.GetProperty("mentionedUserIds").GetArrayLength()));
        Assert.True((await HistoryAsync(server, carol, group))[^1].GetProperty("mentionAll").GetBoolean());

        // 设成管理员之后也能用；平常的消息不带这个标记
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:set-member-role", new { conversationId = group, userId = bob, memberRole = "Admin" }, alice)).Status);
        Assert.Equal(HttpStatusCode.OK, (await SendToAllAsync(bob, group)).Status);
        Assert.False((await SendTextAsync(server, carol, group, "plain")).GetProperty("mentionAll").GetBoolean());

        // 部门群没有群主与管理员，用不了
        var organizer = TestTokens.Issue("root", "acme", Organizer);
        var departmentId = (await CallAsync(server, HttpMethod.Post, "/api/departments", new { code = "rnd", name = "R&D" }, organizer)).Body.GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.Created, (await CallAsync(server, HttpMethod.Post, "/api/memberships", new { userId = alice, departmentId }, organizer)).Status);
        var department = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-department", new { departmentId }, alice)).Body.GetProperty("conversationId").GetString()!;
        Assert.Equal("CHAT_VALIDATION_033", (await SendToAllAsync(alice, department)).Body.GetProperty("code").GetString());

        // 审计详情看得到
        var (_, detail) = await CallAsync(server, HttpMethod.Get, $"/api/chat-audit/{sent.GetProperty("messageId").GetString()}", null, TestTokens.Issue("auditor", "acme", Chatter));
        Assert.True(detail.GetProperty("mentionAll").GetBoolean());
    }

    [Fact]
    public async Task Search_SpansConversations_MediaListsAttachments_AndProfilesShowCommonGroups()
    {
        var (server, users) = await StartAsync();
        await using var running = server;
        var (alice, bob, carol, dave) = (users["alice"], users["bob"], users["carol"], users["dave"]);
        var single = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-single", new { peerUserId = bob }, alice)).Body.GetProperty("conversationId").GetString()!;
        var docs = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:create-group", new { conversationName = "Docs", memberUserIds = new[] { carol } }, alice)).Body.GetProperty("conversationId").GetString()!;
        var team = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:create-group", new { conversationName = "Team", memberUserIds = new[] { bob, carol } }, alice)).Body.GetProperty("conversationId").GetString()!;
        var elsewhere = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-single", new { peerUserId = dave }, carol)).Body.GetProperty("conversationId").GetString()!;

        async Task<string> UploadAsync(string name, string contentType)
        {
            var request = BasicAppTestServer.Request(HttpMethod.Post, "/api/chat-messages/attachments", token: Token(alice));
            var content = new ByteArrayContent(Encoding.UTF8.GetBytes($"content of {name}"));
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            request.Content = new MultipartFormDataContent { { content, "file", name } };
            return (await server.SendAsync(request)).Body.GetProperty("attachment").GetProperty("fileId").GetString()!;
        }

        async Task<string> SendFilesAsync(string conversationId, string messageType, params string[] fileIds)
        {
            var (status, body) = await CallAsync(server, HttpMethod.Post, "/api/chat-messages:send", new { conversationId, messageType, attachments = fileIds.Select(static fileId => new { fileId, fileName = "x" }) }, alice);
            Assert.True(status == HttpStatusCode.OK, body.ToString());
            return body.GetProperty("messageId").GetString()!;
        }

        // 跨会话搜索：只在自己所在的会话里找，最近的在前，按偏移翻页；已撤回的不算
        var draft = (await SendTextAsync(server, alice, single, "quarterly report draft")).GetProperty("messageId").GetString()!;
        await SendTextAsync(server, bob, team, "nothing to see");
        var inDocs = (await SendTextAsync(server, carol, docs, "report for docs")).GetProperty("messageId").GetString()!;
        await SendTextAsync(server, carol, elsewhere, "secret report");
        var (_, found) = await CallAsync(server, HttpMethod.Get, "/api/chat-messages:search-all?keyword=report", null, alice);
        Assert.Equal([(inDocs, docs), (draft, single)], found.GetProperty("items").EnumerateArray().Select(static hit => (hit.GetProperty("messageId").GetString()!, hit.GetProperty("conversationId").GetString()!)));
        Assert.False(found.GetProperty("hasMore").GetBoolean());
        var (_, first) = await CallAsync(server, HttpMethod.Get, "/api/chat-messages:search-all?keyword=report&take=1", null, alice);
        Assert.Equal((inDocs, true), (Assert.Single(first.GetProperty("items").EnumerateArray()).GetProperty("messageId").GetString(), first.GetProperty("hasMore").GetBoolean()));
        var (_, second) = await CallAsync(server, HttpMethod.Get, "/api/chat-messages:search-all?keyword=report&take=1&skip=1", null, alice);
        Assert.Equal((draft, false), (Assert.Single(second.GetProperty("items").EnumerateArray()).GetProperty("messageId").GetString(), second.GetProperty("hasMore").GetBoolean()));
        Assert.Equal(0, (await CallAsync(server, HttpMethod.Get, "/api/chat-messages:search-all?keyword=%20", null, alice)).Body.GetProperty("items").GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:recall", new { messageId = draft }, alice)).Status);
        Assert.Equal(inDocs, Assert.Single((await CallAsync(server, HttpMethod.Get, "/api/chat-messages:search-all?keyword=report", null, alice)).Body.GetProperty("items").EnumerateArray()).GetProperty("messageId").GetString());

        // 图片与文件汇总：各取各的，最近的在前、往前翻页；已撤回的不算，不是成员的看不了
        var (photo, chart, spec) = (await UploadAsync("photo.png", "image/png"), await UploadAsync("chart.png", "image/png"), await UploadAsync("spec.pdf", "application/pdf"));
        var album = await SendFilesAsync(docs, "Image", photo);
        var paper = await SendFilesAsync(docs, "File", spec);
        var later = await SendFilesAsync(docs, "Image", chart);
        var (_, images) = await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:media?conversationId={docs}&kind=Image", null, carol);
        Assert.Equal([later, album], images.GetProperty("items").EnumerateArray().Select(static m => m.GetProperty("messageId").GetString()));
        var (_, newest) = await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:media?conversationId={docs}&kind=Image&take=1", null, carol);
        Assert.Equal((later, true), (Assert.Single(newest.GetProperty("items").EnumerateArray()).GetProperty("messageId").GetString(), newest.GetProperty("hasMore").GetBoolean()));
        var (_, older) = await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:media?conversationId={docs}&kind=Image&take=1&beforeMessageId={later}", null, carol);
        Assert.Equal((album, false), (Assert.Single(older.GetProperty("items").EnumerateArray()).GetProperty("messageId").GetString(), older.GetProperty("hasMore").GetBoolean()));
        var (_, files) = await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:media?conversationId={docs}&kind=File", null, carol);
        Assert.Equal("spec.pdf", Assert.Single(Assert.Single(files.GetProperty("items").EnumerateArray()).GetProperty("attachments").EnumerateArray()).GetProperty("fileName").GetString());
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:recall", new { messageId = paper }, alice)).Status);
        Assert.Equal(0, (await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:media?conversationId={docs}&kind=File", null, carol)).Body.GetProperty("items").GetArrayLength());
        Assert.Equal("CHAT_DENIED_001", (await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:media?conversationId={docs}&kind=Image", null, bob)).Body.GetProperty("code").GetString());

        // 资料：聊天里看得到的那些，另带两个人都在的群（按群名排，单聊不算）；看自己没有共同的群
        var (status, profile) = await CallAsync(server, HttpMethod.Get, $"/api/chat-conversations:user-profile?userId={carol}", null, alice);
        Assert.True(status == HttpStatusCode.OK, profile.ToString());
        Assert.Equal((carol, "carol", "CAROL", false), (profile.GetProperty("userId").GetString(), profile.GetProperty("userName").GetString(), profile.GetProperty("displayName").GetString(), profile.GetProperty("isOnline").GetBoolean()));
        Assert.Equal([(docs, "Docs", 2), (team, "Team", 3)], profile.GetProperty("commonGroups").EnumerateArray().Select(static g => (g.GetProperty("conversationId").GetString()!, g.GetProperty("conversationName").GetString()!, g.GetProperty("memberCount").GetInt32())));
        Assert.Equal(team, Assert.Single((await CallAsync(server, HttpMethod.Get, $"/api/chat-conversations:user-profile?userId={bob}", null, alice)).Body.GetProperty("commonGroups").EnumerateArray()).GetProperty("conversationId").GetString());
        Assert.Equal(0, (await CallAsync(server, HttpMethod.Get, $"/api/chat-conversations:user-profile?userId={alice}", null, alice)).Body.GetProperty("commonGroups").GetArrayLength());

        // 别的租户的人、不存在的人看不了
        Assert.Equal("CHAT_VALIDATION_021", (await CallAsync(server, HttpMethod.Get, $"/api/chat-conversations:user-profile?userId={users["erin"]}", null, alice)).Body.GetProperty("code").GetString());
        Assert.Equal("CHAT_NOTFOUND_003", (await CallAsync(server, HttpMethod.Get, $"/api/chat-conversations:user-profile?userId={Guid.CreateVersion7()}", null, alice)).Body.GetProperty("code").GetString());
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
        var (config, _) = await SettingsTestHost.SetBuiltInConfigAsync(server, platform, "chat.policy", """{"retentionDays":30,"sensitiveWords":["Forbidden"]}""");
        Assert.Equal(HttpStatusCode.OK, config);
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
    public async Task Presence_RidesOnReads_AndIsPushedToPeersAndOpenGroups()
    {
        // 离线宽限调到 1 秒：够验证「宽限内回来不推离线」，又不用干等缺省的 8 秒
        var (server, users) = await StartAsync([new("BasicApp:Realtime:Presence:OfflineGrace", "00:00:01")]);
        await using var running = server;
        var (alice, bob, carol) = (users["alice"], users["bob"], users["carol"]);
        var single = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-single", new { peerUserId = bob }, alice)).Body.GetProperty("conversationId").GetString()!;
        var group = (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:create-group", new { conversationName = "Team", memberUserIds = new[] { bob, carol } }, alice)).Body.GetProperty("conversationId").GetString()!;

        async Task<string> LoginAsync(string userName) => (await AuthFlowTests.LoginAsync(server, userName, Password, "acme")).GetProperty("accessToken").GetString()!;
        async Task<bool> PeerOnlineAsync() => (await ConversationAsync(server, bob, single)).GetProperty("peerOnline").GetBoolean();
        async Task<Dictionary<string, bool>> MembersOnlineAsync()
        {
            var (_, members) = await CallAsync(server, HttpMethod.Get, $"/api/chat-conversations:members?conversationId={group}", null, bob);
            return members.GetProperty("items").EnumerateArray().ToDictionary(static m => m.GetProperty("userName").GetString()!, static m => m.GetProperty("isOnline").GetBoolean());
        }

        // bob 是 alice 的单聊对端；carol 只和她同群、正开着这个群；dave 和她没有任何会话
        await using var bobHub = Connect(server, await LoginAsync("bob"));
        await using var carolHub = Connect(server, await LoginAsync("carol"));
        await using var daveHub = Connect(server, await LoginAsync("dave"));
        var toBob = ExpectPresence(bobHub, alice);
        var toCarol = ExpectPresence(carolHub, alice);
        var toDave = ExpectPresence(daveHub, alice);
        await bobHub.StartAsync();
        await carolHub.StartAsync();
        await daveHub.StartAsync();
        await carolHub.InvokeAsync(ChatRealtimeMethods.JoinConversation, group);

        // 读的时候带此刻的状态：alice 还没连上
        Assert.False(await PeerOnlineAsync());
        Assert.Equal((false, true, true), ((await MembersOnlineAsync())["alice"], (await MembersOnlineAsync())["bob"], (await MembersOnlineAsync())["carol"]));

        // 上线：推给单聊对端与正开着群的人，不相干的人收不到
        var aliceLogin = await LoginAsync("alice");
        var aliceHub = Connect(server, aliceLogin);
        await aliceHub.StartAsync();
        Assert.True(await toBob.Task.WaitAsync(_wait));
        Assert.True(await toCarol.Task.WaitAsync(_wait));
        Assert.True(await PeerOnlineAsync());
        Assert.True((await MembersOnlineAsync())["alice"]);
        var (_, options) = await CallAsync(server, HttpMethod.Get, "/api/chat-conversations:user-options?keyword=ALICE", null, bob);
        Assert.True(Assert.Single(options.GetProperty("items").EnumerateArray()).GetProperty("isOnline").GetBoolean());
        Assert.False(toDave.Task.IsCompleted);

        // 宽限内断了又连上（刷新页面）：不推离线，回来时补一条在线（空档里读到离线的人靠它纠正）
        var back = ExpectPresence(bobHub, alice);
        await aliceHub.DisposeAsync();
        aliceHub = Connect(server, aliceLogin);
        await aliceHub.StartAsync();
        Assert.True(await back.Task.WaitAsync(_wait));
        var flap = ExpectPresence(bobHub, alice);
        await Task.Delay(TimeSpan.FromSeconds(2));
        Assert.False(flap.Task.IsCompleted);
        Assert.True(await PeerOnlineAsync());

        // 真的走了：宽限过后推离线
        await aliceHub.DisposeAsync();
        Assert.False(await flap.Task.WaitAsync(_wait));
        Assert.False(await PeerOnlineAsync());
        Assert.False(toDave.Task.IsCompleted);
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
    /// 租户 acme 与 globex，角色 chatter 在平台与每个租户持聊天的全部权限，organizer 在每个租户持组织的部门与归属；acme 建 alice、bob、carol、dave，globex 建 erin，真实用户按用户授予聊天权限
    /// </remarks>
    private async Task<(BasicAppTestServer Server, Dictionary<string, string> Users)> StartAsync(IEnumerable<KeyValuePair<string, string?>>? settings = null)
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: settings);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        await server.SeedAsync([], [
            .. ChatPermissions.All.SelectMany(static p => TestGrants.BothSides(GrantSubject.ForRole(Chatter), p.Code)),
            .. TestGrants.BothSides(GrantSubject.ForRole(Chatter), "identity.users.create"),
            TestGrants.AnyTenant(GrantSubject.ForRole(Organizer), OrganizationPermissions.DepartmentsCreate),
            TestGrants.AnyTenant(GrantSubject.ForRole(Organizer), OrganizationPermissions.MembershipsCreate),
            TestGrants.AnyTenant(GrantSubject.ForRole(Organizer), OrganizationPermissions.MembershipsDelete),
        ]);
        var users = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, tenant) in new[] { ("alice", "acme"), ("bob", "acme"), ("carol", "acme"), ("dave", "acme"), ("erin", "globex") })
        {
            var (created, user) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName = name, displayName = name.ToUpperInvariant(), password = Password }), TestTokens.Issue("root", tenant, "admin")));
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            users[name] = user.GetProperty("id").GetString()!;
        }

        // 真实用户按用户在所属租户授予（令牌里的角色不替真实用户授权）
        await server.SeedAsync([], [.. users.Values.SelectMany(static id => ChatPermissions.All.Select(p => TestGrants.AnyTenant(GrantSubject.ForUser(id), p.Code)))]);
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

    /// <summary>
    /// 等某个用户的在线状态推送
    /// </summary>
    /// <remarks>
    /// 结果是推来的在线与否；别人的上下线不算
    /// </remarks>
    private static TaskCompletionSource<bool> ExpectPresence(HubConnection connection, string userId)
    {
        var received = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<JsonElement>(ChatRealtimeMethods.ChatPresenceChanged, payload =>
        {
            if (payload.GetProperty("userId").GetString() == userId)
            {
                received.TrySetResult(payload.GetProperty("isOnline").GetBoolean());
            }
        });
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
