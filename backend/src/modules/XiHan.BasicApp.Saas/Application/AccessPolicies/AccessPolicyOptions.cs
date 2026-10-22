// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 访问策略选项，绑定配置节 <see cref="SectionName"/>，经 <c>IOptionsMonitor</c> 读取，改了热生效
/// </summary>
public sealed class AccessPolicyOptions
{
    /// <summary>
    /// 配置节名
    /// </summary>
    public const string SectionName = "Saas:AccessPolicy";

    /// <summary>
    /// 总开关，缺省开；关掉时认证前、认证后、登录时的求值与封禁拦截全部跳过（登录失败照常计数）
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 每个节点最多隔多少秒读一次策略版本号，缺省 5
    /// </summary>
    public int VersionCheckSeconds { get; set; } = 5;

    /// <summary>
    /// 「这个 IP 没被封」的本地缓存秒数，缺省 5
    /// </summary>
    public int BanCheckCacheSeconds { get; set; } = 5;

    /// <summary>
    /// 命中记录在内存里最多合并多少个不同的键，缺省 10000；超出的命中只计数并告警
    /// </summary>
    public int HitBufferCapacity { get; set; } = 10_000;
}
