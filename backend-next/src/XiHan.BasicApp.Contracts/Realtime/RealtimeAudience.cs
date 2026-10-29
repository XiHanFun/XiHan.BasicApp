// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Realtime;

/// <summary>
/// 推送的受众种类。
/// </summary>
public enum RealtimeAudienceKind
{
    /// <summary>
    /// 指定会话（访问令牌的 <c>sid</c>）上的连接。
    /// </summary>
    Sessions = 0,

    /// <summary>
    /// 指定用户在某一侧的连接（同一个人切到别的租户时收不到）。
    /// </summary>
    UsersInSide = 1,

    /// <summary>
    /// 指定用户在所有侧的连接（随人走的数据，如用户设置）。
    /// </summary>
    Users = 2,

    /// <summary>
    /// 某一侧的全部连接。
    /// </summary>
    Side = 3,
}

/// <summary>
/// 推送的受众：会话、某一侧的用户、所有侧的用户或某一侧的全部连接。侧是租户 ID，平台一侧为 <see cref="TenantIds.Platform"/>。
/// </summary>
public sealed record RealtimeAudience
{
    private RealtimeAudience(RealtimeAudienceKind kind, string? side, IReadOnlyList<Guid> ids)
    {
        Kind = kind;
        SideId = side;
        Ids = ids;
    }

    /// <summary>
    /// 受众种类。
    /// </summary>
    public RealtimeAudienceKind Kind { get; }

    /// <summary>
    /// 侧（<see cref="RealtimeAudienceKind.UsersInSide"/> 与 <see cref="RealtimeAudienceKind.Side"/>）；其余为 <see langword="null"/>。
    /// </summary>
    public string? SideId { get; }

    /// <summary>
    /// 会话或用户 ID；<see cref="RealtimeAudienceKind.Side"/> 为空。
    /// </summary>
    public IReadOnlyList<Guid> Ids { get; }

    /// <summary>
    /// 指定会话上的连接。
    /// </summary>
    public static RealtimeAudience Sessions(IEnumerable<Guid> sessionIds) => new(RealtimeAudienceKind.Sessions, null, Unique(sessionIds));

    /// <summary>
    /// 指定用户在某一侧的连接。
    /// </summary>
    public static RealtimeAudience UsersIn(string side, IEnumerable<Guid> userIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(side);
        return new(RealtimeAudienceKind.UsersInSide, side, Unique(userIds));
    }

    /// <summary>
    /// 指定用户在所有侧的连接。
    /// </summary>
    public static RealtimeAudience Users(IEnumerable<Guid> userIds) => new(RealtimeAudienceKind.Users, null, Unique(userIds));

    /// <summary>
    /// 某一侧的全部连接。
    /// </summary>
    public static RealtimeAudience Side(string side)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(side);
        return new(RealtimeAudienceKind.Side, side, []);
    }

    /// <summary>
    /// 没有任何接收者（空的会话或用户列表）。
    /// </summary>
    public bool IsEmpty => Kind != RealtimeAudienceKind.Side && Ids.Count == 0;

    private static Guid[] Unique(IEnumerable<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        return ids.Distinct().ToArray();
    }
}
