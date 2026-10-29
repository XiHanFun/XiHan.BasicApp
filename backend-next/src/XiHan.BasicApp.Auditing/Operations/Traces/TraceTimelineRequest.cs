// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Auditing.Operations.Traces;

/// <summary>
/// <c>traces.timeline</c> 的请求。
/// </summary>
/// <remarks>
/// 按一个维度取当前一侧的各类日志。
/// 有缺省值的属性用 <c>set</c>：源生成的序列化把 init 属性当构造参数，没写的成员会以 <see langword="null"/> 覆盖缺省值。
/// </remarks>
public sealed record TraceTimelineRequest
{
    /// <summary>
    /// 每类日志最多取的条数上限。
    /// </summary>
    public const int MaxTake = 500;

    /// <summary>
    /// 聚合维度。
    /// </summary>
    public TraceDimension Dimension { get; set; }

    /// <summary>
    /// 维度的值（trace-id、用户主键、登录名、会话主键或客户端地址）。
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 起始时间（含）。
    /// </summary>
    /// <remarks>
    /// 不填不限。
    /// </remarks>
    public DateTimeOffset? From { get; set; }

    /// <summary>
    /// 结束时间（含）。
    /// </summary>
    /// <remarks>
    /// 不填不限。
    /// </remarks>
    public DateTimeOffset? To { get; set; }

    /// <summary>
    /// 只取这几类日志。
    /// </summary>
    /// <remarks>
    /// 不填取全部。
    /// </remarks>
    [MaxLength(8)]
    public IReadOnlyList<TraceLogType>? LogTypes { get; set; }

    /// <summary>
    /// 每类日志最多取多少条（1 到 <see cref="MaxTake"/>，缺省 200）。
    /// </summary>
    /// <remarks>
    /// 超出即标记截断。
    /// </remarks>
    [Range(1, MaxTake)]
    public int MaxPerType { get; set; } = 200;
}

/// <summary>
/// 链路聚合的维度。
/// </summary>
public enum TraceDimension
{
    /// <summary>
    /// trace-id：一次调用的全部日志。
    /// </summary>
    TraceId = 0,

    /// <summary>
    /// 用户主键。
    /// </summary>
    UserId = 1,

    /// <summary>
    /// 登录名。
    /// </summary>
    UserName = 2,

    /// <summary>
    /// 会话主键。
    /// </summary>
    SessionId = 3,

    /// <summary>
    /// 客户端地址。
    /// </summary>
    ClientIp = 4,
}

/// <summary>
/// 时间线上的日志类型。
/// </summary>
public enum TraceLogType
{
    /// <summary>
    /// 访问日志。
    /// </summary>
    Access = 0,

    /// <summary>
    /// 开放接口日志。
    /// </summary>
    OpenApi = 1,

    /// <summary>
    /// 操作日志。
    /// </summary>
    Operation = 2,

    /// <summary>
    /// 登录日志。
    /// </summary>
    Login = 3,

    /// <summary>
    /// 异常日志。
    /// </summary>
    Exception = 4,

    /// <summary>
    /// 数据变更日志。
    /// </summary>
    Diff = 5,

    /// <summary>
    /// 权限变更日志。
    /// </summary>
    PermissionChange = 6,

    /// <summary>
    /// 条件命中。
    /// </summary>
    ConditionHit = 7,
}
