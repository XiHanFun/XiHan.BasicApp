// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Chat;
using XiHan.BasicApp.Chat.Jobs;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedulers;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证聊天里的语音与视频通话
/// </summary>
/// <remarks>
/// 通话只记状态：发起、接听、拒绝、加入、挂断与没人接，同一会话里排成先后，一个人同一时刻只在一通电话里；
/// 单聊一对一，群里点名邀请、不超过人数上限，没被邀请的成员能在通话结束前加入，只剩一个人时结束；
/// 信令经聊天 Hub 只在接通了的通话里转发、可以指给一个人；结束时往会话里追加通话记录；离线与收尾作业结束没人管的通话，群解散时里面的通话一并结束；
/// 穿透服务器读宿主配置、TURN 凭据按用户现签，响铃秒数与人数上限读 <c>chat.policy</c>
/// 响铃到点用拨钟验证：钟只影响进程内取的时刻，令牌照常按真实时间校验
/// </remarks>
public sealed class ChatCallTests : IDisposable
{
    private const string Chatter = "chatter";
    private const string Password = "Chat@123456";
    private const string CallsSection = "BasicApp:Chat:Calls";
    private static readonly TimeSpan _wait = TimeSpan.FromSeconds(10);
    private readonly SqliteDatabase _database = new();
    private readonly ManualClock _clock = new(DateTimeOffset.UtcNow);

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Call_RingsConnectsRelaysSignals_AndLeavesARecord()
    {
        var (server, users) = await StartAsync();
        await using var running = server;
        var (alice, bob) = (users["alice"], users["bob"]);
        var conversation = await OpenSingleAsync(server, alice, bob);
        await using var aliceHub = await ConnectAsync(server, "alice");
        await using var bobHub = await ConnectAsync(server, "bob");
        await using var carolHub = await ConnectAsync(server, "carol");
        var (aliceCalls, bobCalls) = (Watch(aliceHub, ChatRealtimeMethods.ChatCallChanged), Watch(bobHub, ChatRealtimeMethods.ChatCallChanged));
        var (aliceSignals, bobSignals) = (Watch(aliceHub, ChatRealtimeMethods.ChatCallSignal), Watch(bobHub, ChatRealtimeMethods.ChatCallSignal));

        // 发起：开始响铃，发起人已加入、对方受邀，通话推给两个人
        var (status, started) = await CallAsync(server, HttpMethod.Post, "/api/chat-calls:start", new { conversationId = conversation, media = "Voice" }, alice);
        Assert.True(status == HttpStatusCode.OK, started.ToString());
        var call = started.GetProperty("callId").GetString()!;
        Assert.Equal(("Ringing", "Voice", alice), (started.GetProperty("status").GetString(), started.GetProperty("media").GetString(), started.GetProperty("initiatorUserId").GetString()));
        Assert.Equal([(alice, "Joined"), (bob, "Invited")], ParticipantsOf(started));
        Assert.Equal(("Single", JsonValueKind.Null), (started.GetProperty("conversationType").GetString(), started.GetProperty("conversationName").ValueKind));
        Assert.Equal(TimeSpan.FromSeconds(60), started.GetProperty("ringExpiresAt").GetDateTimeOffset() - started.GetProperty("startedAt").GetDateTimeOffset());
        Assert.Equal((call, "Ringing"), StateOf(await NextAsync(bobCalls)));
        Assert.Equal((call, "Ringing"), StateOf(await NextAsync(aliceCalls)));
        Assert.Equal(call, await ActiveAsync(server, bob));
        Assert.Equal(_clock.GetUtcNow(), (await CallAsync(server, HttpMethod.Get, "/api/chat-calls:active", null, bob)).Body.GetProperty("now").GetDateTimeOffset());

        // 响铃时不转发信令；接听只有受邀的人、只成功一次
        await aliceHub.InvokeAsync(ChatRealtimeMethods.CallSignal, call, null, "early");
        Assert.Equal("CHAT_CONFLICT_005", (await CallAsync(server, HttpMethod.Post, "/api/chat-calls:accept", new { callId = call }, alice)).Body.GetProperty("code").GetString());
        var (_, accepted) = await CallAsync(server, HttpMethod.Post, "/api/chat-calls:accept", new { callId = call }, bob);
        Assert.Equal("Ongoing", accepted.GetProperty("status").GetString());
        Assert.Equal([(alice, "Joined"), (bob, "Joined")], ParticipantsOf(accepted));
        Assert.Equal("CHAT_CONFLICT_005", (await CallAsync(server, HttpMethod.Post, "/api/chat-calls:accept", new { callId = call }, bob)).Body.GetProperty("code").GetString());
        Assert.Equal((call, "Ongoing"), StateOf(await NextAsync(aliceCalls)));
        Assert.Equal((call, "Ongoing"), StateOf(await NextAsync(bobCalls)));

        // 信令原样转给通话里的另一个人；不在通话里的人、过长的信令都不转
        const string offer = """{"type":"offer","sdp":"v=0"}""";
        await aliceHub.InvokeAsync(ChatRealtimeMethods.CallSignal, call, null, offer);
        var relayed = await NextAsync(bobSignals);
        Assert.Equal((call, alice, offer), (relayed.GetProperty("callId").GetString(), relayed.GetProperty("fromUserId").GetString(), relayed.GetProperty("data").GetString()));
        await bobHub.InvokeAsync(ChatRealtimeMethods.CallSignal, call, alice, """{"type":"answer"}""");
        Assert.Equal(bob, (await NextAsync(aliceSignals)).GetProperty("fromUserId").GetString());
        await carolHub.InvokeAsync(ChatRealtimeMethods.CallSignal, call, null, "intruder");
        await aliceHub.InvokeAsync(ChatRealtimeMethods.CallSignal, call, null, new string('x', 20001));
        await Task.Delay(300);
        Assert.False(bobSignals.TryRead(out _));
        Assert.False(aliceSignals.TryRead(out _));

        // 挂断：记时长，往会话里追加通话记录（预览按挂断那次调用的区域），接通过的不记未读
        _clock.Set(_clock.GetUtcNow().AddSeconds(135));
        var (_, ended) = await CallAsync(server, HttpMethod.Post, "/api/chat-calls:hangup", new { callId = call }, bob, "zh-CN");
        Assert.Equal(("Ended", "Completed"), (ended.GetProperty("status").GetString(), ended.GetProperty("endReason").GetString()));
        Assert.Equal([(alice, "Left"), (bob, "Left")], ParticipantsOf(ended));
        Assert.Equal((call, "Ended"), StateOf(await NextAsync(aliceCalls)));
        var record = (await HistoryAsync(server, alice, conversation))[^1];
        Assert.Equal(("Call", alice, "alice"), (record.GetProperty("messageType").GetString(), record.GetProperty("senderUserId").GetString(), record.GetProperty("senderUserName").GetString()));
        Assert.Equal((call, "Voice", "Completed", 135), RecordOf(record));
        Assert.Empty(MissedOf(record));
        var mine = await ConversationAsync(server, bob, conversation);
        Assert.Equal(("[语音通话]", 0), (mine.GetProperty("lastMessagePreview").GetString(), mine.GetProperty("unreadCount").GetInt32()));
        Assert.Null(await ActiveAsync(server, alice));

        // 已经结束的照样能挂（两边可能同时挂）；通话记录不能撤回、不能转发、不进搜索，回复它时快照用预览文案
        Assert.Equal("Ended", (await CallAsync(server, HttpMethod.Post, "/api/chat-calls:hangup", new { callId = call }, alice)).Body.GetProperty("status").GetString());
        var recordId = record.GetProperty("messageId").GetString()!;
        Assert.Equal("CHAT_VALIDATION_029", (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:recall", new { messageId = recordId }, alice)).Body.GetProperty("code").GetString());
        Assert.Equal("CHAT_VALIDATION_022", (await CallAsync(server, HttpMethod.Post, "/api/chat-messages:forward", new { messageIds = new[] { recordId }, conversationIds = new[] { conversation } }, alice)).Body.GetProperty("code").GetString());
        Assert.Equal(0, (await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:search?conversationId={conversation}&keyword=Voice", null, alice)).Body.GetProperty("items").GetArrayLength());
        var (_, reply) = await CallAsync(server, HttpMethod.Post, "/api/chat-messages:send", new { conversationId = conversation, messageType = "Text", content = "回头再聊", replyToMessageId = recordId }, bob, "zh-CN");
        Assert.Equal("alice: [语音通话]", reply.GetProperty("replyPreview").GetString());
    }

    [Fact]
    public async Task Call_IsRefusedWhenItCannotRing_AndEndsByRejectCancelOrTimeout()
    {
        var (server, users) = await StartAsync();
        await using var running = server;
        var (alice, bob, carol) = (users["alice"], users["bob"], users["carol"]);
        var conversation = await OpenSingleAsync(server, alice, bob);
        var withCarol = await OpenSingleAsync(server, alice, carol);
        var bobAndCarol = await OpenSingleAsync(server, bob, carol);
        var group = await CreateGroupAsync(server, alice, "Team", bob, carol);

        async Task<(HttpStatusCode Status, JsonElement Body)> StartCallAsync(string caller, string conversationId, string media = "Voice")
            => await CallAsync(server, HttpMethod.Post, "/api/chat-calls:start", new { conversationId, media }, caller);
        async Task<string?> CodeOfStartAsync(string caller, string conversationId)
            => (await StartCallAsync(caller, conversationId)).Body.GetProperty("code").GetString();
        async Task<JsonElement> EndAsync(string verb, string caller, string callId, string? locale = null)
        {
            var (status, body) = await CallAsync(server, HttpMethod.Post, $"/api/chat-calls:{verb}", new { callId }, caller, locale);
            Assert.True(status == HttpStatusCode.OK, body.ToString());
            return body;
        }

        // 群里得点名；不是成员、没有权限的不能打；对方不在线打不通
        Assert.Equal("CHAT_VALIDATION_030", await CodeOfStartAsync(alice, group));
        Assert.Equal("CHAT_DENIED_001", await CodeOfStartAsync(carol, conversation));
        Assert.Equal("CHAT_CONFLICT_004", await CodeOfStartAsync(alice, conversation));
        Assert.Equal(HttpStatusCode.Forbidden, (await CallAsync(server, HttpMethod.Get, "/api/chat-calls:active", null, TestTokens.Issue(Guid.CreateVersion7().ToString(), "acme", "nobody"))).Status);
        await using var aliceHub = await ConnectAsync(server, "alice");
        await using var bobHub = await ConnectAsync(server, "bob");
        await using var carolHub = await ConnectAsync(server, "carol");

        // 忙线：受邀还没接也算占着，自己占着不能再打，对方占着打不进来
        var (_, first) = await StartCallAsync(alice, conversation);
        var ringing = first.GetProperty("callId").GetString()!;
        Assert.Equal("CHAT_CONFLICT_003", await CodeOfStartAsync(carol, bobAndCarol));
        Assert.Equal("CHAT_CONFLICT_002", await CodeOfStartAsync(alice, withCarol));
        Assert.Equal("CHAT_CONFLICT_002", await CodeOfStartAsync(bob, conversation));
        Assert.Equal("CHAT_DENIED_010", (await CallAsync(server, HttpMethod.Post, "/api/chat-calls:accept", new { callId = ringing }, carol)).Body.GetProperty("code").GetString());
        Assert.Equal("CHAT_NOTFOUND_006", (await CallAsync(server, HttpMethod.Post, "/api/chat-calls:hangup", new { callId = Guid.CreateVersion7() }, alice)).Body.GetProperty("code").GetString());

        // 拒绝：通话结束，对方是自己拒的、不记未读；之后两个人都空了
        var rejected = await EndAsync("reject", bob, ringing);
        Assert.Equal(("Ended", "Rejected"), (rejected.GetProperty("status").GetString(), rejected.GetProperty("endReason").GetString()));
        Assert.Equal([(alice, "Left"), (bob, "Rejected")], ParticipantsOf(rejected));
        Assert.Equal((ringing, "Voice", "Rejected", 0), RecordOf((await HistoryAsync(server, alice, conversation))[^1]));
        Assert.Equal(0, (await ConversationAsync(server, bob, conversation)).GetProperty("unreadCount").GetInt32());
        Assert.Equal("CHAT_CONFLICT_005", (await CallAsync(server, HttpMethod.Post, "/api/chat-calls:reject", new { callId = ringing }, bob)).Body.GetProperty("code").GetString());

        // 取消：发起人在接通前挂断，对方没接到、记一条未读
        var (_, video) = await StartCallAsync(carol, bobAndCarol, "Video");
        var cancelled = await EndAsync("hangup", carol, video.GetProperty("callId").GetString()!, "zh-CN");
        Assert.Equal("Cancelled", cancelled.GetProperty("endReason").GetString());
        Assert.Equal([(carol, "Left"), (bob, "Missed")], ParticipantsOf(cancelled));
        Assert.Equal([bob], MissedOf((await HistoryAsync(server, carol, bobAndCarol))[^1]));
        var missedByBob = await ConversationAsync(server, bob, bobAndCarol);
        Assert.Equal(("[视频通话]", 1), (missedByBob.GetProperty("lastMessagePreview").GetString(), missedByBob.GetProperty("unreadCount").GetInt32()));

        // 受邀的人在响铃时挂断即拒绝
        var (_, declined) = await StartCallAsync(alice, conversation);
        Assert.Equal("Rejected", (await EndAsync("hangup", bob, declined.GetProperty("callId").GetString()!)).GetProperty("endReason").GetString());

        // 响铃到点：哪一端的计时器来挂都算没人接
        var (_, unanswered) = await StartCallAsync(alice, conversation);
        _clock.Set(_clock.GetUtcNow().AddSeconds(58));
        Assert.Equal("Missed", (await EndAsync("hangup", alice, unanswered.GetProperty("callId").GetString()!)).GetProperty("endReason").GetString());
        var (_, expired) = await StartCallAsync(alice, conversation);
        _clock.Set(_clock.GetUtcNow().AddSeconds(61));
        Assert.Equal("Missed", (await EndAsync("hangup", bob, expired.GetProperty("callId").GetString()!)).GetProperty("endReason").GetString());

        // 收尾作业：两端都没来挂的，过点之后记未接听；还在响的不动
        var (_, abandoned) = await StartCallAsync(alice, conversation);
        var abandonedId = abandoned.GetProperty("callId").GetString()!;
        await SweepAsync(server);
        Assert.Equal(abandonedId, await ActiveAsync(server, alice));
        _clock.Set(_clock.GetUtcNow().AddSeconds(80));
        await SweepAsync(server);
        Assert.Null(await ActiveAsync(server, alice));
        Assert.Null(await ActiveAsync(server, bob));
        Assert.Equal((abandonedId, "Voice", "Missed", 0), RecordOf((await HistoryAsync(server, alice, conversation))[^1]));
        Assert.Equal(3, (await ConversationAsync(server, bob, conversation)).GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public async Task GroupCall_RingsTheInvited_LetsOtherMembersJoin_AndEndsWhenOneIsLeft()
    {
        var (server, users) = await StartWithAsync(["alice", "bob", "carol", "dave", "erin"]);
        await using var running = server;
        var (alice, bob, carol, dave, erin) = (users["alice"], users["bob"], users["carol"], users["dave"], users["erin"]);
        var group = await CreateGroupAsync(server, alice, "Team", bob, carol, dave);
        var platform = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await SettingsTestHost.SetBuiltInConfigAsync(server, platform, "chat.policy", """{"retentionDays":365,"sensitiveWords":[],"callRingSeconds":60,"callMaxVoiceParticipants":3,"callMaxVideoParticipants":2}""")).Status);
        await using var aliceHub = await ConnectAsync(server, "alice");
        await using var bobHub = await ConnectAsync(server, "bob");
        await using var carolHub = await ConnectAsync(server, "carol");
        await using var daveHub = await ConnectAsync(server, "dave");
        await daveHub.InvokeAsync(ChatRealtimeMethods.JoinConversation, group);
        var (aliceCalls, bobCalls, carolCalls, daveCalls) = (Watch(aliceHub, ChatRealtimeMethods.ChatCallChanged), Watch(bobHub, ChatRealtimeMethods.ChatCallChanged), Watch(carolHub, ChatRealtimeMethods.ChatCallChanged), Watch(daveHub, ChatRealtimeMethods.ChatCallChanged));
        var (aliceSignals, bobSignals, carolSignals, daveSignals) = (Watch(aliceHub, ChatRealtimeMethods.ChatCallSignal), Watch(bobHub, ChatRealtimeMethods.ChatCallSignal), Watch(carolHub, ChatRealtimeMethods.ChatCallSignal), Watch(daveHub, ChatRealtimeMethods.ChatCallSignal));

        async Task<(HttpStatusCode Status, JsonElement Body)> StartCallAsync(string caller, string media, params string[] inviteeUserIds)
            => await CallAsync(server, HttpMethod.Post, "/api/chat-calls:start", new { conversationId = group, media, inviteeUserIds }, caller);
        async Task<string?> CodeOfAsync(string verb, string caller, string callId)
            => (await CallAsync(server, HttpMethod.Post, $"/api/chat-calls:{verb}", new { callId }, caller)).Body.GetProperty("code").GetString();
        async Task<JsonElement> DoAsync(string verb, string caller, string callId, string? locale = null)
        {
            var (status, body) = await CallAsync(server, HttpMethod.Post, $"/api/chat-calls:{verb}", new { callId }, caller, locale);
            Assert.True(status == HttpStatusCode.OK, body.ToString());
            return body;
        }

        // 点名：不能为空（只点自己也算空）、都得是会话成员、连发起人在内不超过这种媒体的人数上限
        Assert.Equal("CHAT_VALIDATION_030", (await StartCallAsync(alice, "Voice")).Body.GetProperty("code").GetString());
        Assert.Equal("CHAT_VALIDATION_030", (await StartCallAsync(alice, "Voice", alice)).Body.GetProperty("code").GetString());
        Assert.Equal("CHAT_VALIDATION_031", (await StartCallAsync(alice, "Voice", bob, erin)).Body.GetProperty("code").GetString());
        Assert.Equal("CHAT_VALIDATION_032", (await StartCallAsync(alice, "Voice", bob, carol, dave)).Body.GetProperty("code").GetString());
        Assert.Equal("CHAT_VALIDATION_032", (await StartCallAsync(alice, "Video", bob, carol)).Body.GetProperty("code").GetString());
        var (_, limits) = await CallAsync(server, HttpMethod.Get, "/api/chat-calls:settings", null, alice);
        Assert.Equal((60, 3, 2), (limits.GetProperty("ringSeconds").GetInt32(), limits.GetProperty("maxVoiceParticipants").GetInt32(), limits.GetProperty("maxVideoParticipants").GetInt32()));

        // 发起：点到的人响铃；通话按人推给参与者，开着这个会话的成员经会话组也收到
        var (status, started) = await StartCallAsync(alice, "Voice", bob, carol);
        Assert.True(status == HttpStatusCode.OK, started.ToString());
        var call = started.GetProperty("callId").GetString()!;
        Assert.Equal(("Ringing", "Group", "Team"), (started.GetProperty("status").GetString(), started.GetProperty("conversationType").GetString(), started.GetProperty("conversationName").GetString()));
        Assert.Equal(Statuses((alice, "Joined"), (bob, "Invited"), (carol, "Invited")), StatusesOf(started));
        Assert.Equal((call, "Ringing"), StateOf(await NextAsync(aliceCalls)));
        Assert.Equal((call, "Ringing"), StateOf(await NextAsync(bobCalls)));
        Assert.Equal((call, "Ringing"), StateOf(await NextAsync(carolCalls)));
        Assert.Equal((call, "Ringing"), StateOf(await NextAsync(daveCalls)));

        // 一个群同一时刻只有一通；成员看得到会话里的这一通，不是成员的看不到
        Assert.Equal("CHAT_CONFLICT_006", (await StartCallAsync(dave, "Voice", bob)).Body.GetProperty("code").GetString());
        Assert.Equal(call, await OngoingAsync(server, dave, group));
        Assert.Equal("CHAT_DENIED_001", (await CallAsync(server, HttpMethod.Get, $"/api/chat-calls:ongoing?conversationId={group}", null, erin)).Body.GetProperty("code").GetString());
        Assert.Null(await ActiveAsync(server, dave));

        // 有人接了就接通，别人照常响
        var accepted = await DoAsync("accept", bob, call);
        Assert.Equal("Ongoing", accepted.GetProperty("status").GetString());
        Assert.Equal(Statuses((alice, "Joined"), (bob, "Joined"), (carol, "Invited")), StatusesOf(accepted));

        // 信令：指了对象只转给他；没指转给已经加入的其他人；还在响的、不在通话里的、自己、不成样子的对象都不转
        const string offer = """{"type":"offer","sdp":"v=0"}""";
        await aliceHub.InvokeAsync(ChatRealtimeMethods.CallSignal, call, bob, offer);
        var relayed = await NextAsync(bobSignals);
        Assert.Equal((call, alice, offer), (relayed.GetProperty("callId").GetString(), relayed.GetProperty("fromUserId").GetString(), relayed.GetProperty("data").GetString()));
        await aliceHub.InvokeAsync(ChatRealtimeMethods.CallSignal, call, null, "state");
        Assert.Equal("state", (await NextAsync(bobSignals)).GetProperty("data").GetString());
        await aliceHub.InvokeAsync(ChatRealtimeMethods.CallSignal, call, carol, "to-the-ringing");
        await aliceHub.InvokeAsync(ChatRealtimeMethods.CallSignal, call, alice, "to-myself");
        await aliceHub.InvokeAsync(ChatRealtimeMethods.CallSignal, call, "nobody", "to-nobody");
        await daveHub.InvokeAsync(ChatRealtimeMethods.CallSignal, call, alice, "intruder");
        await Task.Delay(300);
        Assert.False(aliceSignals.TryRead(out _));
        Assert.False(bobSignals.TryRead(out _));
        Assert.False(carolSignals.TryRead(out _));

        // 拒绝的人不来，通话照常；没被邀请的成员直接加入，再调一次原样返回
        var declined = await DoAsync("reject", carol, call);
        Assert.Equal(("Ongoing", "Rejected"), (declined.GetProperty("status").GetString(), StatusesOf(declined).Single(p => p.UserId == carol).Status));
        var joined = await DoAsync("join", dave, call);
        Assert.Equal(Statuses((alice, "Joined"), (bob, "Joined"), (carol, "Rejected"), (dave, "Joined")), StatusesOf(joined));
        Assert.Equal(StatusesOf(joined), StatusesOf(await DoAsync("join", dave, call)));
        Assert.Equal(call, await ActiveAsync(server, dave));

        // 三个人里两两之间各自协商：指给谁就只到谁
        await daveHub.InvokeAsync(ChatRealtimeMethods.CallSignal, call, bob, "dave-to-bob");
        Assert.Equal((dave, "dave-to-bob"), SignalOf(await NextAsync(bobSignals)));
        await daveHub.InvokeAsync(ChatRealtimeMethods.CallSignal, call, null, "dave-to-all");
        Assert.Equal((dave, "dave-to-all"), SignalOf(await NextAsync(aliceSignals)));
        Assert.Equal((dave, "dave-to-all"), SignalOf(await NextAsync(bobSignals)));
        Assert.False(daveSignals.TryRead(out _));

        // 人数满了进不来；不是成员的不能加入；有人离开腾出位置，拒绝过的也能再来
        Assert.Equal("CHAT_CONFLICT_008", await CodeOfAsync("join", carol, call));
        Assert.Equal("CHAT_DENIED_001", await CodeOfAsync("join", erin, call));
        _clock.Set(_clock.GetUtcNow().AddSeconds(30));
        var left = await DoAsync("hangup", bob, call);
        Assert.Equal(("Ongoing", "Left"), (left.GetProperty("status").GetString(), StatusesOf(left).Single(p => p.UserId == bob).Status));
        Assert.Null(await ActiveAsync(server, bob));
        Assert.Equal(Statuses((alice, "Joined"), (bob, "Left"), (carol, "Joined"), (dave, "Joined")), StatusesOf(await DoAsync("join", carol, call)));

        // 一个个离开，只剩一个人时通话结束；时长从第一个人接听算起，记录记在发起人名下，没有人没接到
        _clock.Set(_clock.GetUtcNow().AddSeconds(90));
        Assert.Equal("Ongoing", (await DoAsync("hangup", alice, call)).GetProperty("status").GetString());
        var ended = await DoAsync("hangup", dave, call, "zh-CN");
        Assert.Equal(("Ended", "Completed"), (ended.GetProperty("status").GetString(), ended.GetProperty("endReason").GetString()));
        Assert.Equal(Statuses((alice, "Left"), (bob, "Left"), (carol, "Left"), (dave, "Left")), StatusesOf(ended));
        var record = (await HistoryAsync(server, bob, group))[^1];
        Assert.Equal(("Call", alice), (record.GetProperty("messageType").GetString(), record.GetProperty("senderUserId").GetString()));
        Assert.Equal((call, "Voice", "Completed", 120), RecordOf(record));
        Assert.Empty(MissedOf(record));
        var mine = await ConversationAsync(server, bob, group);
        Assert.Equal(("[语音通话]", 0), (mine.GetProperty("lastMessagePreview").GetString(), mine.GetProperty("unreadCount").GetInt32()));

        // 结束了：会话里没有通话了，也不能再加入
        Assert.Null(await OngoingAsync(server, dave, group));
        Assert.Equal("CHAT_CONFLICT_009", await CodeOfAsync("join", bob, call));
        Assert.Equal("CHAT_NOTFOUND_006", await CodeOfAsync("join", bob, Guid.CreateVersion7().ToString()));
    }

    [Fact]
    public async Task GroupCall_SkipsThoseWhoCannotRing_AndCountsUnreadForTheMissed()
    {
        var (server, users) = await StartWithAsync(["alice", "bob", "carol", "dave"]);
        await using var running = server;
        var (alice, bob, carol, dave) = (users["alice"], users["bob"], users["carol"], users["dave"]);
        var group = await CreateGroupAsync(server, alice, "Team", bob, carol, dave);
        var bobAndCarol = await OpenSingleAsync(server, bob, carol);
        await using var aliceHub = await ConnectAsync(server, "alice");
        await using var carolHub = await ConnectAsync(server, "carol");
        var bobHub = await ConnectAsync(server, "bob");
        var aliceCalls = Watch(aliceHub, ChatRealtimeMethods.ChatCallChanged);

        async Task<(HttpStatusCode Status, JsonElement Body)> StartCallAsync(params string[] inviteeUserIds)
            => await CallAsync(server, HttpMethod.Post, "/api/chat-calls:start", new { conversationId = group, media = "Voice", inviteeUserIds }, alice);
        async Task<string> RingAsync(params string[] inviteeUserIds)
        {
            var (status, body) = await StartCallAsync(inviteeUserIds);
            Assert.True(status == HttpStatusCode.OK, body.ToString());
            return body.GetProperty("callId").GetString()!;
        }

        async Task<JsonElement> DoAsync(string verb, string caller, string callId)
        {
            var (status, body) = await CallAsync(server, HttpMethod.Post, $"/api/chat-calls:{verb}", new { callId }, caller);
            Assert.True(status == HttpStatusCode.OK, body.ToString());
            return body;
        }

        async Task<int> UnreadAsync(string userId) => (await ConversationAsync(server, userId, group)).GetProperty("unreadCount").GetInt32();

        // 占线的、不在线的不响铃：一个能响的都没有就打不出去
        var (_, single) = await CallAsync(server, HttpMethod.Post, "/api/chat-calls:start", new { conversationId = bobAndCarol, media = "Voice" }, carol);
        Assert.Equal("CHAT_CONFLICT_007", (await StartCallAsync(bob, carol, dave)).Body.GetProperty("code").GetString());
        await DoAsync("hangup", carol, single.GetProperty("callId").GetString()!);

        // 不在线的记成没接到，别人照常响；有人拒绝、还有人在响，通话还在
        var first = await RingAsync(bob, carol, dave);
        Assert.Equal(Statuses((alice, "Joined"), (bob, "Invited"), (carol, "Invited"), (dave, "Missed")), StatusesOf((await CallAsync(server, HttpMethod.Get, "/api/chat-calls:active", null, alice)).Body.GetProperty("call")));
        Assert.Equal("Ringing", (await DoAsync("reject", bob, first)).GetProperty("status").GetString());

        // 受邀的都不来了即结束：有人没接到记未接听，只给没接到的人记未读
        var unanswered = await DoAsync("reject", carol, first);
        Assert.Equal(("Ended", "Missed"), (unanswered.GetProperty("status").GetString(), unanswered.GetProperty("endReason").GetString()));
        var record = (await HistoryAsync(server, alice, group))[^1];
        Assert.Equal((first, "Voice", "Missed", 0), RecordOf(record));
        Assert.Equal([dave], MissedOf(record));
        Assert.Equal((0, 0, 1), (await UnreadAsync(bob), await UnreadAsync(carol), await UnreadAsync(dave)));

        // 都是自己拒绝的：记被拒绝，谁也不记未读
        await using var daveHub = await ConnectAsync(server, "dave");
        var second = await RingAsync(bob, carol);
        await DoAsync("reject", bob, second);
        Assert.Equal("Rejected", (await DoAsync("hangup", carol, second)).GetProperty("endReason").GetString());
        Assert.Empty(MissedOf((await HistoryAsync(server, alice, group))[^1]));

        // 没人接时发起人挂断：取消，受邀的都没接到
        var third = await RingAsync(bob, carol, dave);
        var cancelled = await DoAsync("hangup", alice, third);
        Assert.Equal("Cancelled", cancelled.GetProperty("endReason").GetString());
        Assert.Equal(Statuses((alice, "Left"), (bob, "Missed"), (carol, "Missed"), (dave, "Missed")), StatusesOf(cancelled));
        Assert.Equal(new[] { bob, carol, dave }.Order(StringComparer.Ordinal), MissedOf((await HistoryAsync(server, alice, group))[^1]).Order(StringComparer.Ordinal));
        Assert.Equal((1, 1, 2), (await UnreadAsync(bob), await UnreadAsync(carol), await UnreadAsync(dave)));

        // 通话中还挂着的邀请：响铃到点由收尾作业记成没接到，通话照常
        var fourth = await RingAsync(bob, carol);
        await DoAsync("accept", bob, fourth);
        await DoAsync("join", dave, fourth);
        _clock.Set(_clock.GetUtcNow().AddSeconds(80));
        await SweepAsync(server);
        var swept = (await CallAsync(server, HttpMethod.Get, "/api/chat-calls:active", null, alice)).Body.GetProperty("call");
        Assert.Equal("Ongoing", swept.GetProperty("status").GetString());
        Assert.Equal(Statuses((alice, "Joined"), (bob, "Joined"), (carol, "Missed"), (dave, "Joined")), StatusesOf(swept));
        Assert.Null(await ActiveAsync(server, carol));
        while (aliceCalls.TryRead(out _))
        {
        }

        // 通话中有人离线（宽限过后）记离开，还剩两个人的继续；再走一个，只剩一个人即以中断结束
        await bobHub.DisposeAsync();
        var afterBob = await UntilAsync(aliceCalls, c => StatusesOf(c).Any(p => p.UserId == bob && p.Status == "Left"));
        Assert.Equal((fourth, "Ongoing"), StateOf(afterBob));
        await daveHub.DisposeAsync();
        var interrupted = await UntilAsync(aliceCalls, static c => c.GetProperty("status").GetString() == "Ended");
        Assert.Equal((fourth, "Interrupted"), (interrupted.GetProperty("callId").GetString(), interrupted.GetProperty("endReason").GetString()));
        Assert.Equal([carol], MissedOf((await HistoryAsync(server, alice, group))[^1]));
        Assert.Equal(2, await UnreadAsync(carol));
    }

    [Fact]
    public async Task DissolvingAGroup_EndsItsCall_AndTellsTheMembers()
    {
        var (server, users) = await StartWithAsync(["alice", "bob", "carol"]);
        await using var running = server;
        var (alice, bob, carol) = (users["alice"], users["bob"], users["carol"]);
        var group = await CreateGroupAsync(server, alice, "Team", bob, carol);
        await using var aliceHub = await ConnectAsync(server, "alice");
        await using var bobHub = await ConnectAsync(server, "bob");
        await using var carolHub = await ConnectAsync(server, "carol");
        var bobCalls = Watch(bobHub, ChatRealtimeMethods.ChatCallChanged);
        var carolChanges = Watch(carolHub, ChatRealtimeMethods.ChatConversationChanged);
        var (status, started) = await CallAsync(server, HttpMethod.Post, "/api/chat-calls:start", new { conversationId = group, media = "Voice", inviteeUserIds = new[] { bob, carol } }, alice);
        Assert.True(status == HttpStatusCode.OK, started.ToString());
        var call = started.GetProperty("callId").GetString()!;
        Assert.Equal("Ongoing", (await CallAsync(server, HttpMethod.Post, "/api/chat-calls:accept", new { callId = call }, bob)).Body.GetProperty("status").GetString());

        // 解散：通话以中断结束，还在响铃的记没接到；成员收到解散
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:dissolve", new { conversationId = group }, alice)).Status);
        var ended = await UntilAsync(bobCalls, static c => c.GetProperty("status").GetString() == "Ended");
        Assert.Equal((call, "Interrupted"), (ended.GetProperty("callId").GetString(), ended.GetProperty("endReason").GetString()));
        Assert.Equal(Statuses((alice, "Left"), (bob, "Left"), (carol, "Missed")), StatusesOf(ended));
        var dissolved = await UntilAsync(carolChanges, static c => c.GetProperty("changeType").GetString() == "dissolved");
        Assert.Equal(group, dissolved.GetProperty("conversationId").GetString());
        Assert.Null(await ActiveAsync(server, alice));
        Assert.Null(await ActiveAsync(server, bob));

        // 那通电话照样读得到（解散了的群还找得到）；群里不能再发起
        Assert.Equal("Ended", (await CallAsync(server, HttpMethod.Post, "/api/chat-calls:hangup", new { callId = call }, alice)).Body.GetProperty("status").GetString());
        Assert.Equal("CHAT_DENIED_001", (await CallAsync(server, HttpMethod.Post, "/api/chat-calls:start", new { conversationId = group, media = "Voice", inviteeUserIds = new[] { bob } }, alice)).Body.GetProperty("code").GetString());

        // 还没人接的那种：解散即取消
        var second = await CreateGroupAsync(server, alice, "Second", bob);
        var (_, ringing) = await CallAsync(server, HttpMethod.Post, "/api/chat-calls:start", new { conversationId = second, media = "Voice", inviteeUserIds = new[] { bob } }, alice);
        Assert.Equal("Ringing", ringing.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:dissolve", new { conversationId = second }, alice)).Status);
        var cancelled = await UntilAsync(bobCalls, c => c.GetProperty("callId").GetString() == ringing.GetProperty("callId").GetString() && c.GetProperty("status").GetString() == "Ended");
        Assert.Equal("Cancelled", cancelled.GetProperty("endReason").GetString());
    }

    [Fact]
    public async Task Call_EndsWhenAParticipantGoesOffline()
    {
        var (server, users) = await StartAsync();
        await using var running = server;
        var (alice, bob) = (users["alice"], users["bob"]);
        var conversation = await OpenSingleAsync(server, alice, bob);

        async Task<string> RingAsync()
        {
            var (status, body) = await CallAsync(server, HttpMethod.Post, "/api/chat-calls:start", new { conversationId = conversation, media = "Voice" }, alice);
            Assert.True(status == HttpStatusCode.OK, body.ToString());
            return body.GetProperty("callId").GetString()!;
        }

        static async Task<string?> EndReasonAsync(ChannelReader<JsonElement> calls, string callId)
        {
            while (true)
            {
                var pushed = await NextAsync(calls);
                if (pushed.GetProperty("callId").GetString() == callId && pushed.GetProperty("status").GetString() == "Ended")
                {
                    return pushed.GetProperty("endReason").GetString();
                }
            }
        }

        // 通话中有人离线（宽限过后）：记中断
        var aliceHub = await ConnectAsync(server, "alice");
        var bobHub = await ConnectAsync(server, "bob");
        var aliceCalls = Watch(aliceHub, ChatRealtimeMethods.ChatCallChanged);
        var ongoing = await RingAsync();
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Post, "/api/chat-calls:accept", new { callId = ongoing }, bob)).Status);
        await bobHub.DisposeAsync();
        Assert.Equal("Interrupted", await EndReasonAsync(aliceCalls, ongoing));
        Assert.Null(await ActiveAsync(server, alice));

        // 响铃时受邀的人离线：记未接听
        bobHub = await ConnectAsync(server, "bob");
        var unanswered = await RingAsync();
        await bobHub.DisposeAsync();
        Assert.Equal("Missed", await EndReasonAsync(aliceCalls, unanswered));

        // 响铃时发起人离线：取消
        bobHub = await ConnectAsync(server, "bob");
        var bobCalls = Watch(bobHub, ChatRealtimeMethods.ChatCallChanged);
        var dropped = await RingAsync();
        await aliceHub.DisposeAsync();
        Assert.Equal("Cancelled", await EndReasonAsync(bobCalls, dropped));
        Assert.Null(await ActiveAsync(server, bob));
        await bobHub.DisposeAsync();
    }

    [Fact]
    public async Task CallSettings_ComeFromThePolicy_AndAWrongValueRefusesCalls()
    {
        var (server, users) = await StartAsync();
        await using var running = server;
        var (alice, bob) = (users["alice"], users["bob"]);
        var conversation = await OpenSingleAsync(server, alice, bob);
        var platform = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        await using var bobHub = await ConnectAsync(server, "bob");

        // 没写的取缺省：响铃 60 秒，语音 8 人，视频 6 人
        var (_, defaults) = await CallAsync(server, HttpMethod.Get, "/api/chat-calls:settings", null, alice);
        Assert.Equal((60, 8, 6), (defaults.GetProperty("ringSeconds").GetInt32(), defaults.GetProperty("maxVoiceParticipants").GetInt32(), defaults.GetProperty("maxVideoParticipants").GetInt32()));

        Assert.Equal(HttpStatusCode.OK, (await SettingsTestHost.SetBuiltInConfigAsync(server, platform, "chat.policy", """{"retentionDays":365,"sensitiveWords":[],"callRingSeconds":30}""")).Status);
        Assert.Equal(30, (await CallAsync(server, HttpMethod.Get, "/api/chat-calls:settings", null, alice)).Body.GetProperty("ringSeconds").GetInt32());
        var (_, started) = await CallAsync(server, HttpMethod.Post, "/api/chat-calls:start", new { conversationId = conversation, media = "Video" }, alice);
        Assert.Equal(TimeSpan.FromSeconds(30), started.GetProperty("ringExpiresAt").GetDateTimeOffset() - started.GetProperty("startedAt").GetDateTimeOffset());
        Assert.Equal(HttpStatusCode.OK, (await CallAsync(server, HttpMethod.Post, "/api/chat-calls:hangup", new { callId = started.GetProperty("callId").GetString() }, alice)).Status);

        // 响铃秒数不在 10 到 300 之间：策略整个不认，通话打不出去
        Assert.Equal(HttpStatusCode.OK, (await SettingsTestHost.SetBuiltInConfigAsync(server, platform, "chat.policy", """{"retentionDays":365,"sensitiveWords":[],"callRingSeconds":5}""")).Status);
        Assert.Equal("CHAT_VALIDATION_024", (await CallAsync(server, HttpMethod.Post, "/api/chat-calls:start", new { conversationId = conversation, media = "Voice" }, alice)).Body.GetProperty("code").GetString());

        // 人数上限越界同样不认：语音 2 到 12，视频 2 到 8
        foreach (var wrong in new[] { """{"callMaxVoiceParticipants":1}""", """{"callMaxVoiceParticipants":13}""", """{"callMaxVideoParticipants":9}""" })
        {
            Assert.Equal(HttpStatusCode.OK, (await SettingsTestHost.SetBuiltInConfigAsync(server, platform, "chat.policy", wrong)).Status);
            Assert.Equal("CHAT_VALIDATION_024", (await CallAsync(server, HttpMethod.Get, "/api/chat-calls:settings", null, alice)).Body.GetProperty("code").GetString());
        }
    }

    [Fact]
    public async Task IceServers_ComeFromTheHostConfiguration_WithTurnCredentialsSignedPerUser()
    {
        const string secret = "test-turn-secret";
        var (server, users) = await StartAsync(
            new($"{CallsSection}:StunUrls:0", "stun:turn.example.com:3478"),
            new($"{CallsSection}:TurnUrls:0", "turn:turn.example.com:3478?transport=udp"),
            new($"{CallsSection}:TurnUrls:1", "turns:turn.example.com:5349?transport=tcp"),
            new($"{CallsSection}:TurnSecret", secret),
            new($"{CallsSection}:TurnCredentialLifetime", "00:10:00"));
        await using var running = server;
        var user = Guid.Parse(users["alice"]);

        var (status, body) = await CallAsync(server, HttpMethod.Get, "/api/chat-calls:ice-servers", null, users["alice"]);
        Assert.True(status == HttpStatusCode.OK, body.ToString());
        var servers = body.GetProperty("iceServers").EnumerateArray().ToList();
        Assert.Equal(2, servers.Count);
        Assert.Equal(["stun:turn.example.com:3478"], servers[0].GetProperty("urls").EnumerateArray().Select(static u => u.GetString()));
        Assert.False(servers[0].TryGetProperty("credential", out var none) && none.ValueKind != JsonValueKind.Null);
        Assert.Equal(["turn:turn.example.com:3478?transport=udp", "turns:turn.example.com:5349?transport=tcp"], servers[1].GetProperty("urls").EnumerateArray().Select(static u => u.GetString()));

        // 用户名是「到期秒数:用户」，凭据是它按共享密钥算的 HMAC-SHA1；密钥本身不下发
        var expiresAt = body.GetProperty("expiresAt").GetDateTimeOffset();
        Assert.Equal(_clock.GetUtcNow().AddMinutes(10), expiresAt);
        var username = servers[1].GetProperty("username").GetString()!;
        Assert.Equal($"{expiresAt.ToUnixTimeSeconds()}:{user:N}", username);
#pragma warning disable CA5350 // TURN 的限时凭据由协议定为 HMAC-SHA1
        Assert.Equal(Convert.ToBase64String(HMACSHA1.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(username))), servers[1].GetProperty("credential").GetString());
#pragma warning restore CA5350
        Assert.DoesNotContain(secret, body.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void CallOptions_DefaultToNoServers()
    {
        var options = ChatCallOptions.FromConfiguration(new ConfigurationBuilder().Build());
        Assert.Equal(TimeSpan.FromHours(1), options.TurnCredentialLifetime);
        var servers = options.IceServersFor(Guid.CreateVersion7(), DateTimeOffset.UtcNow);
        Assert.Empty(servers.IceServers);
        Assert.Null(servers.ExpiresAt);
    }

    [Theory]
    [InlineData("StunUrls:0", "http://turn.example.com")]
    [InlineData("StunUrls:0", "stun:")]
    [InlineData("TurnUrls:0", "stun:turn.example.com:3478")]
    [InlineData("TurnUrls:0", "turn:turn.example.com:3478")]
    [InlineData("TurnCredentialLifetime", "3600")]
    [InlineData("TurnCredentialLifetime", "01:00")]
    [InlineData("TurnCredentialLifetime", "00:00:00")]
    public void AWrongCallOption_IsRejected_NamingTheSection(string key, string value)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection([new KeyValuePair<string, string?>($"{CallsSection}:{key}", value)]).Build();
        var rejected = Assert.Throws<ArgumentException>(() => ChatCallOptions.FromConfiguration(configuration));
        Assert.Contains(CallsSection, rejected.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AWrongCallOption_FailsTheStartup()
    {
        var failed = await Assert.ThrowsAnyAsync<Exception>(() => BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new($"{CallsSection}:TurnUrls:0", "turn:turn.example.com:3478")]));
        Assert.Contains($"{CallsSection}:TurnSecret", failed.ToString(), StringComparison.Ordinal);
    }

    private static string Token(string userId) => TestTokens.Issue(userId, "acme", Chatter);

    /// <summary>
    /// 起宿主并在租户 acme 建好 alice、bob、carol
    /// </summary>
    private Task<(BasicAppTestServer Server, Dictionary<string, string> Users)> StartAsync(params KeyValuePair<string, string?>[] settings)
        => StartWithAsync(["alice", "bob", "carol"], settings);

    /// <summary>
    /// 起宿主并在租户 acme 建好名单里的用户
    /// </summary>
    /// <remarks>
    /// 钟是手拨的，离线宽限调到 1 秒；真实用户按用户授予聊天权限（令牌里的角色不替真实用户授权）
    /// </remarks>
    private async Task<(BasicAppTestServer Server, Dictionary<string, string> Users)> StartWithAsync(string[] names, params KeyValuePair<string, string?>[] settings)
    {
        var server = await BasicAppTestServer.StartAsync(
            _database.ConnectionString,
            settings: [new("BasicApp:Realtime:Presence:OfflineGrace", "00:00:01"), .. settings],
            configure: builder => builder.Services.AddSingleton<TimeProvider>(_clock));
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var users = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            var (created, user) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName = name, displayName = name.ToUpperInvariant(), password = Password }), TestTokens.Issue("root", "acme", "admin")));
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            users[name] = user.GetProperty("id").GetString()!;
        }

        await server.SeedAsync([], [.. users.Values.SelectMany(static id => ChatPermissions.All.Select(p => TestGrants.AnyTenant(GrantSubject.ForUser(id), p.Code)))]);
        return (server, users);
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> CallAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string tokenOrUser, string? locale = null)
    {
        var token = Guid.TryParse(tokenOrUser, out _) ? Token(tokenOrUser) : tokenOrUser;
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body is null ? null : JsonSerializer.Serialize(body), token, locale));
        return (response.StatusCode, json);
    }

    private static async Task<string> OpenSingleAsync(BasicAppTestServer server, string userId, string peerUserId)
    {
        var (status, body) = await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:open-single", new { peerUserId }, userId);
        Assert.True(status == HttpStatusCode.OK, body.ToString());
        return body.GetProperty("conversationId").GetString()!;
    }

    private static async Task<string> CreateGroupAsync(BasicAppTestServer server, string ownerUserId, string conversationName, params string[] memberUserIds)
    {
        var (status, body) = await CallAsync(server, HttpMethod.Post, "/api/chat-conversations:create-group", new { conversationName, memberUserIds }, ownerUserId);
        Assert.True(status == HttpStatusCode.OK, body.ToString());
        return body.GetProperty("conversationId").GetString()!;
    }

    private static async Task<JsonElement> ConversationAsync(BasicAppTestServer server, string userId, string conversationId)
        => (await CallAsync(server, HttpMethod.Get, "/api/chat-conversations:mine", null, userId)).Body.GetProperty("items").EnumerateArray().Single(c => c.GetProperty("conversationId").GetString() == conversationId);

    private static async Task<List<JsonElement>> HistoryAsync(BasicAppTestServer server, string userId, string conversationId)
    {
        var (status, body) = await CallAsync(server, HttpMethod.Get, $"/api/chat-messages:history?conversationId={conversationId}&take=50", null, userId);
        Assert.True(status == HttpStatusCode.OK, body.ToString());
        return [.. body.GetProperty("items").EnumerateArray()];
    }

    /// <summary>
    /// 这个人此刻所在的通话
    /// </summary>
    /// <remarks>
    /// 没有为空
    /// </remarks>
    private static async Task<string?> ActiveAsync(BasicAppTestServer server, string userId)
    {
        var (status, body) = await CallAsync(server, HttpMethod.Get, "/api/chat-calls:active", null, userId);
        Assert.True(status == HttpStatusCode.OK, body.ToString());
        return body.TryGetProperty("call", out var call) && call.ValueKind == JsonValueKind.Object ? call.GetProperty("callId").GetString() : null;
    }

    /// <summary>
    /// 会话里还没结束的那通电话
    /// </summary>
    /// <remarks>
    /// 没有为空
    /// </remarks>
    private static async Task<string?> OngoingAsync(BasicAppTestServer server, string userId, string conversationId)
    {
        var (status, body) = await CallAsync(server, HttpMethod.Get, $"/api/chat-calls:ongoing?conversationId={conversationId}", null, userId);
        Assert.True(status == HttpStatusCode.OK, body.ToString());
        return body.TryGetProperty("call", out var call) && call.ValueKind == JsonValueKind.Object ? call.GetProperty("callId").GetString() : null;
    }

    private static async Task SweepAsync(BasicAppTestServer server)
    {
        var ran = await SettingsTestHost.InvokeAsync(server, null, async ctx => await ctx.Services.GetRequiredService<ChatCallSweepJob>().RunAsync(JobRun.Create(new JobName(ChatCallSweepJob.Name), DateTimeOffset.UtcNow, JobTrigger.Manual), ctx));
        Assert.True(ran.IsSuccess, ran.Error?.Code);
    }

    private static (string? CallId, string? Status) StateOf(JsonElement call) => (call.GetProperty("callId").GetString(), call.GetProperty("status").GetString());

    private static List<(string UserId, string Status)> ParticipantsOf(JsonElement call)
        => [.. call.GetProperty("participants").EnumerateArray().Select(static p => (p.GetProperty("userId").GetString()!, p.GetProperty("status").GetString()!))];

    /// <summary>
    /// 参与者与各自的状态，按用户排好
    /// </summary>
    /// <remarks>
    /// 多人通话里参与者的先后按用户 ID 排，用例里只比对谁是什么状态
    /// </remarks>
    private static List<(string UserId, string Status)> StatusesOf(JsonElement call) => [.. ParticipantsOf(call).OrderBy(static p => p.UserId, StringComparer.Ordinal)];

    private static List<(string UserId, string Status)> Statuses(params (string UserId, string Status)[] expected) => [.. expected.OrderBy(static p => p.UserId, StringComparer.Ordinal)];

    private static (string? FromUserId, string? Data) SignalOf(JsonElement signal) => (signal.GetProperty("fromUserId").GetString(), signal.GetProperty("data").GetString());

    /// <summary>
    /// 通话记录里没接到的人
    /// </summary>
    private static List<string> MissedOf(JsonElement message)
    {
        using var content = JsonDocument.Parse(message.GetProperty("content").GetString()!);
        return [.. content.RootElement.GetProperty("missedUserIds").EnumerateArray().Select(static id => id.GetString()!)];
    }

    /// <summary>
    /// 通话记录消息的正文
    /// </summary>
    private static (string? CallId, string? Media, string? Reason, int DurationSeconds) RecordOf(JsonElement message)
    {
        using var content = JsonDocument.Parse(message.GetProperty("content").GetString()!);
        var record = content.RootElement;
        return (record.GetProperty("callId").GetString(), record.GetProperty("media").GetString(), record.GetProperty("reason").GetString(), record.GetProperty("durationSeconds").GetInt32());
    }

    /// <summary>
    /// 以真实登录连上聊天 Hub，回来时这个人已经算在线
    /// </summary>
    /// <remarks>
    /// Hub 在 <c>OnConnectedAsync</c> 做完连接登记之后才处理调用，所以连上后先打一个来回
    /// </remarks>
    private static async Task<HubConnection> ConnectAsync(BasicAppTestServer server, string userName)
    {
        var token = (await AuthFlowTests.LoginAsync(server, userName, Password, "acme")).GetProperty("accessToken").GetString()!;
        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost" + RealtimeHubs.Chat, options =>
            {
                options.HttpMessageHandlerFactory = _ => server.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();
        await connection.StartAsync();
        await connection.InvokeAsync(ChatRealtimeMethods.LeaveConversation, Guid.Empty.ToString());
        return connection;
    }

    /// <summary>
    /// 把某个方法的推送按到达的先后收进队列
    /// </summary>
    private static ChannelReader<JsonElement> Watch(HubConnection connection, string method)
    {
        var channel = Channel.CreateUnbounded<JsonElement>();
        connection.On<JsonElement>(method, payload => channel.Writer.TryWrite(payload));
        return channel.Reader;
    }

    private static async Task<JsonElement> NextAsync(ChannelReader<JsonElement> pushes) => await pushes.ReadAsync().AsTask().WaitAsync(_wait);

    /// <summary>
    /// 等到一条合乎条件的推送，前面不合的丢掉
    /// </summary>
    private static async Task<JsonElement> UntilAsync(ChannelReader<JsonElement> pushes, Func<JsonElement, bool> matches)
    {
        while (true)
        {
            var pushed = await NextAsync(pushes);
            if (matches(pushed))
            {
                return pushed;
            }
        }
    }
}
