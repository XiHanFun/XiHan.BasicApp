// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Realtime;

/// <summary>
/// 推送的受众种类
/// </summary>
public enum RealtimeAudienceKind
{
    /// <summary>
    /// 指定会话（访问令牌的 <c>sid</c>）上的连接
    /// </summary>
    Sessions = 0,

    /// <summary>
    /// 指定用户在某一侧的连接（同一个人切到别的租户时收不到）
    /// </summary>
    UsersInSide = 1,

    /// <summary>
    /// 指定用户在所有侧的连接（随人走的数据，如用户设置）
    /// </summary>
    Users = 2,

    /// <summary>
    /// 某一侧的全部连接
    /// </summary>
    Side = 3,

    /// <summary>
    /// Hub 自己让连接加入的组（如聊天的会话组）里的连接
    /// </summary>
    HubGroups = 4,
}

/// <summary>
/// 推送的受众：会话、某一侧的用户、所有侧的用户、某一侧的全部连接或 Hub 自己的组
/// </summary>
/// <remarks>
/// 侧是租户 ID，平台一侧为 <see cref="TenantIds.Platform"/>
/// </remarks>
public sealed record RealtimeAudience
{
    private RealtimeAudience(RealtimeAudienceKind kind, string? side, IReadOnlyList<Guid> ids, IReadOnlyList<string>? groups = null)
    {
        Kind = kind;
        SideId = side;
        Ids = ids;
        Groups = groups ?? [];
    }

    /// <summary>
    /// 受众种类
    /// </summary>
    public RealtimeAudienceKind Kind { get; }

    /// <summary>
    /// 受众所在的侧
    /// </summary>
    /// <remarks>
    /// 只有 <see cref="RealtimeAudienceKind.UsersInSide"/> 与 <see cref="RealtimeAudienceKind.Side"/> 有值；其余为 <see langword="null"/>
    /// </remarks>
    public string? SideId { get; }

    /// <summary>
    /// 会话或用户 ID
    /// </summary>
    /// <remarks>
    /// <see cref="RealtimeAudienceKind.Side"/> 与 <see cref="RealtimeAudienceKind.HubGroups"/> 为空
    /// </remarks>
    public IReadOnlyList<Guid> Ids { get; }

    /// <summary>
    /// Hub 自己的组名
    /// </summary>
    /// <remarks>
    /// 只有 <see cref="RealtimeAudienceKind.HubGroups"/> 有值
    /// </remarks>
    public IReadOnlyList<string> Groups { get; }

    /// <summary>
    /// 指定会话上的连接
    /// </summary>
    public static RealtimeAudience Sessions(IEnumerable<Guid> sessionIds) => new(RealtimeAudienceKind.Sessions, null, Unique(sessionIds));

    /// <summary>
    /// 指定用户在某一侧的连接
    /// </summary>
    public static RealtimeAudience UsersIn(string side, IEnumerable<Guid> userIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(side);
        return new(RealtimeAudienceKind.UsersInSide, side, Unique(userIds));
    }

    /// <summary>
    /// 指定用户在所有侧的连接
    /// </summary>
    public static RealtimeAudience Users(IEnumerable<Guid> userIds) => new(RealtimeAudienceKind.Users, null, Unique(userIds));

    /// <summary>
    /// 某一侧的全部连接
    /// </summary>
    public static RealtimeAudience Side(string side)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(side);
        return new(RealtimeAudienceKind.Side, side, []);
    }

    /// <summary>
    /// Hub 自己让连接加入的组里的连接
    /// </summary>
    /// <remarks>
    /// 组名由登记那个 Hub 的包定（它在 Hub 里让连接加入、退出），只有此刻在组里的连接收得到，适合只给正看着某样东西的人发的瞬时状态
    /// </remarks>
    public static RealtimeAudience HubGroups(IEnumerable<string> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        return new(RealtimeAudienceKind.HubGroups, null, [], groups.Distinct(StringComparer.Ordinal).ToArray());
    }

    /// <summary>
    /// 没有任何接收者（空的会话、用户或组列表）
    /// </summary>
    public bool IsEmpty => Kind switch
    {
        RealtimeAudienceKind.Side => false,
        RealtimeAudienceKind.HubGroups => Groups.Count == 0,
        _ => Ids.Count == 0,
    };

    private static Guid[] Unique(IEnumerable<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        return ids.Distinct().ToArray();
    }
}
