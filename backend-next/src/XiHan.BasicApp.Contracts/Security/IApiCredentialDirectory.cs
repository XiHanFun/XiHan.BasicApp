// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 开放接口凭证目录（Identity 实现）：签名开放接口的入口（OpenPlatform）按 AppKey 取凭证验签，不直接读 Identity 的表。
/// 凭证是个人的（个人中心「开放接口」自助管理），调用方不成为主体，归属人只用于日志与停用判断。
/// </summary>
public interface IApiCredentialDirectory
{
    /// <summary>
    /// 按 AppKey 取凭证；不存在或密钥解不开为 <see langword="null"/>。在 Authenticate 阶段被调用（还没有工作单元），实现自己开只读作用域。
    /// </summary>
    /// <param name="appKey">AppKey。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>凭证。</returns>
    ValueTask<ApiCredentialRecord?> FindAsync(string appKey, CancellationToken cancellationToken);

    /// <summary>
    /// 记下最近一次验签通过的时间（在调用之外的独立事务里写，调用失败也保留）。
    /// </summary>
    /// <param name="credentialId">凭证 ID。</param>
    /// <param name="usedAt">时间。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    ValueTask TouchAsync(Guid credentialId, DateTimeOffset usedAt, CancellationToken cancellationToken);
}

/// <summary>
/// 验签用的凭证。
/// </summary>
/// <param name="Id">凭证 ID。</param>
/// <param name="AppKey">AppKey。</param>
/// <param name="Name">凭证名称。</param>
/// <param name="Secret">密钥明文（HMAC 密钥与 AES 密钥）。</param>
/// <param name="IsEnabled">凭证是否启用。</param>
/// <param name="UserId">归属人。</param>
/// <param name="UserName">归属人的登录名。</param>
/// <param name="HomeTenantId">归属人的注册地；平台账号为 <see cref="TenantIds.Platform"/>。</param>
/// <param name="UserActive">归属人的账号是否正常（停用、锁定中的账号的凭证不能用）。</param>
/// <param name="SignatureAlgorithm">归属人设定的签名算法（<see cref="ApiSignatureAlgorithms"/>）。</param>
/// <param name="IpAllowList">归属人设定的 IP 白名单（IP 或 CIDR）；空即不限。</param>
public sealed record ApiCredentialRecord(
    Guid Id,
    string AppKey,
    string Name,
    string Secret,
    bool IsEnabled,
    Guid UserId,
    string UserName,
    string HomeTenantId,
    bool UserActive,
    string SignatureAlgorithm,
    IReadOnlyList<string> IpAllowList);

/// <summary>
/// 开放接口的签名算法（4.x 个人中心的取值）。
/// </summary>
public static class ApiSignatureAlgorithms
{
    /// <summary>
    /// HMAC-SHA256（缺省）。
    /// </summary>
    public const string HmacSha256 = "HmacSHA256";

    /// <summary>
    /// HMAC-SHA512。
    /// </summary>
    public const string HmacSha512 = "HmacSHA512";

    /// <summary>
    /// 全部算法。
    /// </summary>
    public static IReadOnlyList<string> All { get; } = [HmacSha256, HmacSha512];

    /// <summary>
    /// 规整成标准写法（不区分大小写，也认 4.x 请求头的 <c>HMACSHA256</c>）；不认识的为 <see langword="null"/>。
    /// </summary>
    /// <param name="value">算法名。</param>
    /// <returns>标准写法。</returns>
    public static string? Normalize(string? value)
        => All.FirstOrDefault(algorithm => string.Equals(algorithm, value?.Trim(), StringComparison.OrdinalIgnoreCase));
}
