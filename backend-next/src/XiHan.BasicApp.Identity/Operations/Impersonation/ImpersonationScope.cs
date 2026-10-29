// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Identity.Operations.Impersonation;

/// <summary>
/// 模仿登录的目标上下文解析。
/// </summary>
/// <remarks>
/// <c>impersonation.candidates</c> 与 <c>impersonation.start</c> 共用。
/// 不填即发起人当前所在的上下文（平台主体是平台、租户主体是自己的租户）；填 <c>platform</c> 即平台；其余是租户 ID。
/// 租户主体能否进入解析出的上下文由各操作再核对（只能是自己的租户）。
/// </remarks>
internal static class ImpersonationScope
{
    /// <summary>
    /// 解析目标上下文。
    /// </summary>
    /// <remarks>
    /// <c>null</c> 表示平台。
    /// </remarks>
    /// <param name="requested">请求里的租户 ID，可空。</param>
    /// <param name="actorTenant">发起人当前所在的租户，平台为 <c>null</c>。</param>
    /// <returns>目标租户 ID，平台为 <c>null</c>。</returns>
    public static string? Resolve(string? requested, string? actorTenant)
    {
        if (string.IsNullOrWhiteSpace(requested))
        {
            return actorTenant;
        }

        var trimmed = requested.Trim();
        return string.Equals(trimmed, TenantIds.Platform, StringComparison.Ordinal) ? null : trimmed;
    }
}
