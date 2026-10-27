// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Identity;

/// <summary>
/// 身份包提供的包间能力 ID；<c>Tenancy</c> 等下游包以 <c>basicapp.identity@1</c> 声明依赖。
/// </summary>
public static class IdentityCapabilities
{
    /// <summary>身份能力：用户、会话、个人中心。</summary>
    public const string Identity = AppInfo.CapabilityPrefix + "identity";

    /// <summary>能力版本。</summary>
    public const int Version = AppInfo.CapabilityVersion;
}
