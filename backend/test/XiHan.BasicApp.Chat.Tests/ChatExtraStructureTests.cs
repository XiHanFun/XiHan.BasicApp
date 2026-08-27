// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using SqlSugar;
using System.Reflection;
using XiHan.BasicApp.Chat.Application.AppServices;
using XiHan.BasicApp.Chat.Application.Contracts;
using XiHan.BasicApp.Chat.Application.Pages;
using XiHan.BasicApp.Chat.Application.QueryServices;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Domain.Permissions;
using XiHan.BasicApp.Chat.Hubs;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Entities.Abstracts;
using XiHan.Framework.Uow.Attributes;
using XiHan.Framework.Web.RealTime.Attributes;

namespace XiHan.BasicApp.Chat.Tests;

/// <summary>
/// 聊天模块的反射型结构约束测试：动态 API 暴露面的鉴权与工作单元、契约与实现一致性、
/// 持久化实体的表与索引形状、枚举取值与页面登记表的自洽。
/// </summary>
/// <remarks>
/// 本文件守的都是「写错了也照常编译、照常启动、只在生产环境静默出事」的约定：
/// 少一个 <c>PermissionAuthorize</c> 端点就降级成任意登录用户可访问；少一个 <c>UnitOfWork</c>
/// 写路径就失去原子性（消息落了库但成员未读没加）；唯一索引漏 <c>IsDeleted</c> 会让软删过的
/// 单聊配对键永远无法重建；枚举数值漂移则历史行的语义整体错位。
/// </remarks>
public sealed class ChatExtraStructureTests
{
    /// <summary>
    /// 被动态 API 暴露的三个聊天应用服务。
    /// </summary>
    public static TheoryData<Type> DynamicApiServices =>
    [
        typeof(ChatAppService),
        typeof(ChatQueryService),
        typeof(ChatAuditQueryService)
    ];

    /// <summary>
    /// 聊天模块四张持久化表对应的实体。
    /// </summary>
    public static TheoryData<Type> PersistedEntities =>
    [
        typeof(SysChatConversation),
        typeof(SysChatConversationMember),
        typeof(SysChatMessage),
        typeof(SysChatMessageReaction)
    ];

    /// <summary>
    /// 每个暴露服务都必须在类级声明 Authorize 与 DynamicApi，鉴权不依赖框架的全局兜底开关。
    /// </summary>
    /// <param name="serviceType">被检查的应用服务类型。</param>
    [Theory]
    [MemberData(nameof(DynamicApiServices))]
    public void DynamicApiServices_ShouldDeclareClassLevelAuthorizeAndDynamicApi(Type serviceType)
    {
        Assert.True(
            serviceType.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any(),
            $"{serviceType.Name} 未声明 Authorize，其端点鉴权将取决于框架默认开关（默认不要求登录）。");
        Assert.True(
            serviceType.GetCustomAttributes<DynamicApiAttribute>(inherit: true).Any(),
            $"{serviceType.Name} 未声明 DynamicApi，端点不会被生成，前端会整片 404。");
    }

    /// <summary>
    /// 每个暴露方法都必须挂上聊天权限码清单内的权限特性，不得只靠类级登录态门控。
    /// </summary>
    /// <param name="serviceType">被检查的应用服务类型。</param>
    [Theory]
    [MemberData(nameof(DynamicApiServices))]
    public void ExposedMethods_ShouldCarryPermissionAuthorizeWithRegisteredCode(Type serviceType)
    {
        var violations = EnumerateExposedMethods(serviceType)
            .Select(method => (method.Name, Codes: method
                .GetCustomAttributes<PermissionAuthorizeAttribute>(inherit: true)
                .Select(attribute => attribute.PermissionCode)
                .ToList()))
            .Where(item => item.Codes.Count == 0 || item.Codes.Exists(code => !ChatPermissionCodes.All.Contains(code)))
            .Select(item => item.Codes.Count == 0
                ? $"{serviceType.Name}.{item.Name}：缺少 PermissionAuthorize"
                : $"{serviceType.Name}.{item.Name}：权限码 {string.Join('/', item.Codes)} 不在 ChatPermissionCodes.All 内")
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToList();

        Assert.True(violations.Count == 0,
            $"下列 {violations.Count} 个聊天端点的权限判定不成立：" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 端点方法名到权限码的完整矩阵：任何一个端点换码、新增端点漏挂码都必须让本用例变红。
    /// </summary>
    /// <remarks>
    /// 这里不做"抽样"，而是把整张表钉死：聊天的四个权限码里，read 是会话可见性、send 是发言权、
    /// manage 是群治理、audit 是跨会话合规查询，任意一个端点错挂档次都直接构成越权面。
    /// </remarks>
    [Fact]
    public void ExposedMethods_PermissionMatrixShouldMatchDeclaredContract()
    {
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ChatAppService.OpenSingleConversationAsync"] = ChatPermissionCodes.Read,
            ["ChatAppService.OpenDepartmentConversationAsync"] = ChatPermissionCodes.Read,
            ["ChatAppService.TogglePinConversationAsync"] = ChatPermissionCodes.Read,
            ["ChatAppService.ToggleMuteConversationAsync"] = ChatPermissionCodes.Read,
            ["ChatAppService.MarkReadAsync"] = ChatPermissionCodes.Read,
            ["ChatAppService.SendMessageAsync"] = ChatPermissionCodes.Send,
            ["ChatAppService.RecallMessageAsync"] = ChatPermissionCodes.Send,
            ["ChatAppService.EditMessageAsync"] = ChatPermissionCodes.Send,
            ["ChatAppService.ToggleReactionAsync"] = ChatPermissionCodes.Send,
            ["ChatAppService.PinMessageAsync"] = ChatPermissionCodes.Send,
            ["ChatAppService.UnpinMessageAsync"] = ChatPermissionCodes.Send,
            ["ChatAppService.CreateGroupConversationAsync"] = ChatPermissionCodes.Manage,
            ["ChatAppService.AddMembersAsync"] = ChatPermissionCodes.Manage,
            ["ChatAppService.RemoveMemberAsync"] = ChatPermissionCodes.Manage,
            ["ChatAppService.UpdateConversationInfoAsync"] = ChatPermissionCodes.Manage,
            ["ChatAppService.TransferOwnerAsync"] = ChatPermissionCodes.Manage,
            ["ChatAppService.SetMemberSilenceAsync"] = ChatPermissionCodes.Manage,
            ["ChatAppService.SetMemberRoleAsync"] = ChatPermissionCodes.Manage,
            ["ChatQueryService.GetMyConversationsAsync"] = ChatPermissionCodes.Read,
            ["ChatQueryService.GetMessageHistoryAsync"] = ChatPermissionCodes.Read,
            ["ChatQueryService.GetMessageSearchAsync"] = ChatPermissionCodes.Read,
            ["ChatQueryService.GetReadPositionsAsync"] = ChatPermissionCodes.Read,
            ["ChatQueryService.GetPinnedMessagesAsync"] = ChatPermissionCodes.Read,
            ["ChatQueryService.GetMembersAsync"] = ChatPermissionCodes.Read,
            ["ChatQueryService.GetDepartmentTreeAsync"] = ChatPermissionCodes.Read,
            ["ChatQueryService.GetUserOptionsAsync"] = ChatPermissionCodes.Read,
            ["ChatAuditQueryService.GetChatMessagePageAsync"] = ChatPermissionCodes.Audit
        };

        var actual = new[] { typeof(ChatAppService), typeof(ChatQueryService), typeof(ChatAuditQueryService) }
            .SelectMany(serviceType => EnumerateExposedMethods(serviceType)
                .Select(method => (
                    Key: $"{serviceType.Name}.{method.Name}",
                    Code: method.GetCustomAttributes<PermissionAuthorizeAttribute>(inherit: true)
                        .Select(attribute => attribute.PermissionCode)
                        .FirstOrDefault())))
            .ToDictionary(item => item.Key, item => item.Code, StringComparer.Ordinal);

        var missing = expected.Keys.Where(key => !actual.ContainsKey(key)).OrderBy(key => key, StringComparer.Ordinal).ToList();
        var added = actual.Keys.Where(key => !expected.ContainsKey(key)).OrderBy(key => key, StringComparer.Ordinal).ToList();
        var changed = expected
            .Where(pair => actual.TryGetValue(pair.Key, out var code) && !string.Equals(code, pair.Value, StringComparison.Ordinal))
            .Select(pair => $"{pair.Key}：期望 {pair.Value}，实际 {actual[pair.Key] ?? "(无)"}")
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0 && added.Count == 0 && changed.Count == 0,
            $"聊天端点权限矩阵与本用例声明不一致。{Environment.NewLine}" +
            $"已删除或改名（{missing.Count}）：{string.Join("、", missing)}{Environment.NewLine}" +
            $"新增未登记（{added.Count}）：{string.Join("、", added)}{Environment.NewLine}" +
            $"权限码变更（{changed.Count}）：{string.Join(Environment.NewLine, changed)}");
    }

    /// <summary>
    /// 命令服务的每个端点都必须带事务型工作单元，落库与扇出不能各写各的。
    /// </summary>
    [Fact]
    public void ChatAppService_EveryExposedMethodShouldCarryTransactionalUnitOfWork()
    {
        var violations = EnumerateExposedMethods(typeof(ChatAppService))
            .Where(method => !method.GetCustomAttributes<UnitOfWorkAttribute>(inherit: true).Any())
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(violations.Count == 0,
            $"下列 {violations.Count} 个聊天写端点缺少 UnitOfWork，其多表写入不在同一事务内，" +
            $"中途失败会留下半截数据：{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 查询服务不得声明工作单元：只读路径开事务只会白占连接与锁。
    /// </summary>
    /// <param name="serviceType">被检查的查询服务类型。</param>
    [Theory]
    [InlineData(typeof(ChatQueryService))]
    [InlineData(typeof(ChatAuditQueryService))]
    public void QueryServices_ShouldNotDeclareUnitOfWork(Type serviceType)
    {
        var violations = EnumerateExposedMethods(serviceType)
            .Where(method => method.GetCustomAttributes<UnitOfWorkAttribute>(inherit: true).Any())
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(violations.Count == 0,
            $"{serviceType.Name} 的下列只读端点声明了 UnitOfWork：{string.Join("、", violations)}");
    }

    /// <summary>
    /// 每个暴露端点的最后一个形参必须是带默认值的 <see cref="CancellationToken"/>，取消才能一路透传到仓储。
    /// </summary>
    /// <param name="serviceType">被检查的应用服务类型。</param>
    [Theory]
    [MemberData(nameof(DynamicApiServices))]
    public void ExposedMethods_ShouldEndWithOptionalCancellationToken(Type serviceType)
    {
        var violations = EnumerateExposedMethods(serviceType)
            .Where(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 0
                    || parameters[^1].ParameterType != typeof(CancellationToken)
                    || !parameters[^1].HasDefaultValue;
            })
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(violations.Count == 0,
            $"{serviceType.Name} 的下列端点末位不是可选 CancellationToken，取消信号会在此处断掉：" +
            $"{string.Join("、", violations)}");
    }

    /// <summary>
    /// 契约接口声明的每个方法都必须由对应实现以同名、同参、同返回类型落地。
    /// </summary>
    /// <param name="contractType">契约接口类型。</param>
    /// <param name="implementationType">实现类型。</param>
    [Theory]
    [InlineData(typeof(IChatAppService), typeof(ChatAppService))]
    [InlineData(typeof(IChatQueryService), typeof(ChatQueryService))]
    [InlineData(typeof(IChatAuditQueryService), typeof(ChatAuditQueryService))]
    public void Contracts_ShouldBeImplementedWithIdenticalSignatures(Type contractType, Type implementationType)
    {
        Assert.True(
            implementationType.IsAssignableTo(contractType),
            $"{implementationType.Name} 未实现 {contractType.Name}。");

        var violations = new List<string>();
        foreach (var declared in contractType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            var parameterTypes = declared.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
            var implemented = implementationType.GetMethod(
                declared.Name,
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                parameterTypes,
                modifiers: null);

            if (implemented is null)
            {
                violations.Add($"{declared.Name}：实现类缺少同名同参方法");
                continue;
            }

            if (implemented.ReturnType != declared.ReturnType)
            {
                violations.Add($"{declared.Name}：返回类型为 {implemented.ReturnType.Name}，契约声明 {declared.ReturnType.Name}");
            }
        }

        Assert.True(violations.Count == 0,
            $"{implementationType.Name} 与 {contractType.Name} 的签名不一致：" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 四张聊天表的实体必须带 SugarTable、以 Sys_Chat 前缀命名，并声明严格租户隔离。
    /// </summary>
    /// <remarks>
    /// 聊天数据不是"读共享"的平台字典：漏掉 <see cref="IStrictMultiTenantEntity"/> 会让平台态读到
    /// 全部租户的会话与消息，是最直接的越权读。
    /// </remarks>
    /// <param name="entityType">被检查的实体类型。</param>
    [Theory]
    [MemberData(nameof(PersistedEntities))]
    public void ChatEntities_ShouldDeclareSugarTableAndStrictTenancy(Type entityType)
    {
        var table = entityType.GetCustomAttribute<SugarTable>();

        Assert.NotNull(table);
        Assert.StartsWith("Sys_Chat", table.TableName, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(table.TableDescription), $"{entityType.Name} 的表描述为空。");
        Assert.True(
            entityType.IsAssignableTo(typeof(IStrictMultiTenantEntity)),
            $"{entityType.Name} 未声明 IStrictMultiTenantEntity，聊天数据会在平台态跨租户可见。");
    }

    /// <summary>
    /// 软删实体的每一个唯一索引都必须包含 IsDeleted 列。
    /// </summary>
    /// <remarks>
    /// 漏掉后，被软删的行仍占着唯一键：退群再重新入群、软删的单聊会话再次发起，都会撞唯一约束失败。
    /// </remarks>
    /// <param name="entityType">被检查的实体类型。</param>
    [Theory]
    [MemberData(nameof(PersistedEntities))]
    public void SoftDeletableEntities_UniqueIndexesShouldIncludeIsDeleted(Type entityType)
    {
        if (entityType.GetProperty("IsDeleted") is null)
        {
            // 仅追加、无软删的实体（消息与表情回应）不适用本约束
            return;
        }

        var violations = entityType.GetCustomAttributes<SugarIndexAttribute>()
            .Where(index => index.IsUnique)
            .Where(index => !index.IndexFields.Keys.Contains("IsDeleted", StringComparer.Ordinal))
            .Select(index => index.IndexName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(violations.Count == 0,
            $"{entityType.Name} 的下列唯一索引未包含 IsDeleted，软删行会长期占用唯一键：" +
            $"{string.Join("、", violations)}");
    }

    /// <summary>
    /// 单聊配对键与会话成员的唯一索引必须以 TenantId 打头，唯一性只在租户内成立。
    /// </summary>
    [Fact]
    public void UniqueIndexes_ShouldBeScopedByTenantId()
    {
        var pairKeyIndex = Assert.Single(
            typeof(SysChatConversation).GetCustomAttributes<SugarIndexAttribute>(),
            index => index.IsUnique);
        var memberIndex = Assert.Single(
            typeof(SysChatConversationMember).GetCustomAttributes<SugarIndexAttribute>(),
            index => index.IsUnique);

        Assert.Equal("TenantId", pairKeyIndex.IndexFields.Keys.First(), StringComparer.Ordinal);
        Assert.Contains("PairKey", pairKeyIndex.IndexFields.Keys, StringComparer.Ordinal);
        Assert.Equal("TenantId", memberIndex.IndexFields.Keys.First(), StringComparer.Ordinal);
        Assert.Contains("ConversationId", memberIndex.IndexFields.Keys, StringComparer.Ordinal);
        Assert.Contains("UserId", memberIndex.IndexFields.Keys, StringComparer.Ordinal);
    }

    /// <summary>
    /// 表情回应的唯一索引必须锁住「租户 + 消息 + 用户 + 表情」四元组，toggle 语义依赖它。
    /// </summary>
    [Fact]
    public void ReactionUniqueIndex_ShouldCoverMessageUserAndEmoji()
    {
        var index = Assert.Single(
            typeof(SysChatMessageReaction).GetCustomAttributes<SugarIndexAttribute>(),
            item => item.IsUnique);

        Assert.Equal(
            new[] { "TenantId", "MessageId", "UserId", "Emoji" },
            index.IndexFields.Keys.ToArray(),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 会话类型枚举的名称与数值必须整表稳定：历史行存的是数字，改一位就整体错位。
    /// </summary>
    [Fact]
    public void ChatConversationType_ShouldKeepStableNumericValues()
    {
        Assert.Equal(
            Expect(("Single", 1), ("Group", 2), ("Department", 3), ("Assistant", 4)),
            EnumMap<ChatConversationType>());
    }

    /// <summary>
    /// 成员角色枚举的名称与数值必须整表稳定；成员列表还依赖 Owner &lt; Admin &lt; Member 的数值序排序。
    /// </summary>
    [Fact]
    public void ChatMemberRole_ShouldKeepStableNumericValuesInPrivilegeOrder()
    {
        Assert.Equal(
            Expect(("Owner", 1), ("Admin", 2), ("Member", 3)),
            EnumMap<ChatMemberRole>());
        Assert.True(ChatMemberRole.Owner < ChatMemberRole.Admin && ChatMemberRole.Admin < ChatMemberRole.Member,
            "成员列表按 MemberRole 升序排序，群主必须排在管理员之前、管理员排在普通成员之前。");
    }

    /// <summary>
    /// 消息类型枚举的名称与数值必须整表稳定，系统提示固定占 99 段位以便与业务类型区隔。
    /// </summary>
    [Fact]
    public void ChatMessageType_ShouldKeepStableNumericValues()
    {
        Assert.Equal(
            Expect(("Text", 1), ("Image", 2), ("Voice", 3), ("File", 4), ("Assistant", 5), ("System", 99)),
            EnumMap<ChatMessageType>());
    }

    /// <summary>
    /// 权限码必须唯一、以模块编码打头、全小写，且全集与可授租户集一致（聊天无平台专属码）。
    /// </summary>
    [Fact]
    public void PermissionCodes_ShouldBeUniqueLowerCaseAndModulePrefixed()
    {
        var codes = ChatPermissionCodes.All;

        Assert.Equal(codes.Count, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(codes, code => Assert.StartsWith(ChatPermissionCodes.Module + ":", code, StringComparison.Ordinal));
        Assert.All(codes, code => Assert.Equal(code.ToLowerInvariant(), code, StringComparer.Ordinal));
        Assert.Equal(codes.ToArray(), ChatPermissionCodes.TenantGrantable.ToArray());
    }

    /// <summary>
    /// 页面登记表必须自洽：页码唯一、子页面的父目录先于自身出现、绑定的权限码在权限清单内。
    /// </summary>
    /// <remarks>菜单种子按登记顺序解析 ParentId，父项排在后面会让整棵子树被静默跳过。</remarks>
    [Fact]
    public void PageRegistry_ShouldDeclareParentsBeforeChildrenWithRegisteredPermissionCodes()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var violations = new List<string>();

        foreach (var page in PageRegistry.All)
        {
            if (!seen.Add(page.Code))
            {
                violations.Add($"{page.Code}：页码重复登记");
            }

            if (page.ParentCode is { } parent && !seen.Contains(parent))
            {
                violations.Add($"{page.Code}：父目录 {parent} 未在其之前登记");
            }

            if (page.PermissionCode is { } permission && !ChatPermissionCodes.All.Contains(permission))
            {
                violations.Add($"{page.Code}：绑定的权限码 {permission} 不在 ChatPermissionCodes.All 内");
            }
        }

        foreach (var button in PageRegistry.Buttons)
        {
            if (!seen.Contains(button.ParentCode))
            {
                violations.Add($"按钮 {button.Code}：所属页面 {button.ParentCode} 未登记");
            }

            if (!ChatPermissionCodes.All.Contains(button.PermissionCode))
            {
                violations.Add($"按钮 {button.Code}：权限码 {button.PermissionCode} 不在 ChatPermissionCodes.All 内");
            }
        }

        Assert.True(violations.Count == 0,
            $"聊天页面登记表存在 {violations.Count} 处不自洽：" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 聊天目录必须是登记表的第一项，且它自身不绑权限码（目录可见性由子菜单决定）。
    /// </summary>
    [Fact]
    public void PageRegistry_ChatDirectoryShouldLeadTheListWithoutPermissionCode()
    {
        Assert.Same(PageRegistry.ChatDirectory, PageRegistry.All[0]);
        Assert.Equal(PageRegistry.ChatDirectoryCode, PageRegistry.ChatDirectory.Code);
        Assert.Null(PageRegistry.ChatDirectory.PermissionCode);
        Assert.Null(PageRegistry.ChatDirectory.ParentCode);
    }

    /// <summary>
    /// Hub 必须声明连接级鉴权，且客户端可调用方法的会话ID形参一律是字符串。
    /// </summary>
    /// <remarks>
    /// 雪花 ID 超出 JS number 的 2^53 安全区间：形参写成 long 时前端传来的数值已经被截断，
    /// 会静默进错组或校验错会话。
    /// </remarks>
    [Fact]
    public void ChatHub_ShouldBeAuthorizedAndTakeConversationIdAsString()
    {
        Assert.True(
            typeof(BasicAppChatHub).GetCustomAttributes<AuthorizeHubAttribute>(inherit: true).Any(),
            "BasicAppChatHub 未声明 AuthorizeHub，任何匿名连接都能调用其方法。");

        var violations = typeof(BasicAppChatHub)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Where(method => method.GetParameters().Any(parameter => parameter.ParameterType != typeof(string)))
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(violations.Count == 0,
            $"Hub 的下列方法存在非字符串形参，雪花 ID 会在前端被精度截断：{string.Join("、", violations)}");
    }

    /// <summary>
    /// 枚举的「名称=数值」清单，按名称排序以便与期望值做顺序无关的比对。
    /// </summary>
    /// <typeparam name="TEnum">枚举类型。</typeparam>
    /// <returns>形如 <c>Text=1</c> 的有序清单。</returns>
    private static string[] EnumMap<TEnum>()
        where TEnum : struct, Enum
    {
        return [.. Enum.GetValues<TEnum>()
            .Select(value => $"{value}={Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture)}")
            .OrderBy(text => text, StringComparer.Ordinal)];
    }

    /// <summary>
    /// 把期望的名称到数值映射整理成与 <see cref="EnumMap{TEnum}"/> 同形状的有序清单。
    /// </summary>
    /// <param name="expected">期望的名称到数值映射。</param>
    /// <returns>形如 <c>Text=1</c> 的有序清单。</returns>
    private static string[] Expect(params (string Name, int Value)[] expected)
    {
        return [.. expected
            .Select(item => $"{item.Name}={item.Value}")
            .OrderBy(text => text, StringComparer.Ordinal)];
    }

    /// <summary>
    /// 枚举应用服务上被动态 API 暴露的方法（公开实例方法，排除属性访问器与基类成员）。
    /// </summary>
    /// <param name="serviceType">应用服务类型。</param>
    /// <returns>暴露方法集合。</returns>
    private static IReadOnlyList<MethodInfo> EnumerateExposedMethods(Type serviceType)
    {
        return [.. serviceType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Where(method => typeof(Task).IsAssignableFrom(method.ReturnType))];
    }
}
