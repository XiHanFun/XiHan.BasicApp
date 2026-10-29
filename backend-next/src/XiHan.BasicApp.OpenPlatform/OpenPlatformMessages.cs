// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.OpenPlatform;

/// <summary>
/// 开放平台包的消息键：操作说明与错误文案，十种区域文件在 <c>Localization/</c>。
/// </summary>
public static class OpenPlatformMessages
{
    /// <summary>
    /// 消息目录名。
    /// </summary>
    public const string CatalogName = "basicapp.openplatform";

    /// <summary>
    /// 操作 <c>open.ping</c> 的说明。
    /// </summary>
    public const string OpenPingDescription = "open.ping.description";

    /// <summary>
    /// 操作 <c>open.echo</c> 的说明。
    /// </summary>
    public const string OpenEchoDescription = "open.echo.description";

    /// <summary>
    /// 验签通过的说明（自检与回显的响应）。
    /// </summary>
    public const string SignatureVerified = "openplatform.signing.verified";

    /// <summary>
    /// 缺少签名请求头；参数 <c>headers</c>（缺的头）。
    /// </summary>
    public const string SignatureHeadersMissing = "openplatform.signing.headers_missing";

    /// <summary>
    /// 时间戳超出时间窗；参数 <c>seconds</c>（允许的偏差秒数）。
    /// </summary>
    public const string TimestampExpired = "openplatform.signing.timestamp_expired";

    /// <summary>
    /// AppKey 不存在、凭证停用或归属人账号不正常。
    /// </summary>
    public const string AccessKeyInvalid = "openplatform.signing.access_key_invalid";

    /// <summary>
    /// 客户端地址不在凭证归属人设定的 IP 白名单内；参数 <c>ip</c>。
    /// </summary>
    public const string IpNotAllowed = "openplatform.signing.ip_not_allowed";

    /// <summary>
    /// 缺少内容签名（<c>X-Content-Sign</c>，GET 也要带空串的散列）。
    /// </summary>
    public const string ContentSignMissing = "openplatform.signing.content_sign_missing";

    /// <summary>
    /// 内容签名与请求体明文的散列不符。
    /// </summary>
    public const string ContentSignMismatch = "openplatform.signing.content_sign_mismatch";

    /// <summary>
    /// 请求签名不符。
    /// </summary>
    public const string SignatureMismatch = "openplatform.signing.signature_mismatch";

    /// <summary>
    /// <c>X-Timestamp</c> 不是 Unix 秒。
    /// </summary>
    public const string TimestampInvalid = "openplatform.signing.timestamp_invalid";

    /// <summary>
    /// <c>X-Nonce</c> 太长（最多 128 个字符）。
    /// </summary>
    public const string NonceInvalid = "openplatform.signing.nonce_invalid";

    /// <summary>
    /// <c>X-Sign-Algorithm</c> 与凭证归属人设定的算法不一致；参数 <c>value</c>、<c>expected</c>。
    /// </summary>
    public const string SignAlgorithmMismatch = "openplatform.signing.algorithm_mismatch";

    /// <summary>
    /// 内容签名算法不认识；参数 <c>value</c>。
    /// </summary>
    public const string ContentAlgorithmUnsupported = "openplatform.signing.content_algorithm_unsupported";

    /// <summary>
    /// 加密算法不认识；参数 <c>value</c>。
    /// </summary>
    public const string EncryptAlgorithmUnsupported = "openplatform.signing.encrypt_algorithm_unsupported";

    /// <summary>
    /// 请求体超过上限；参数 <c>max</c>（字节）。
    /// </summary>
    public const string BodyTooLarge = "openplatform.signing.body_too_large";

    /// <summary>
    /// 加密的请求体解不开（格式不对、IV 不对或密钥不符）。
    /// </summary>
    public const string BodyDecryptFailed = "openplatform.signing.body_decrypt_failed";

    /// <summary>
    /// 随机串在时间窗内已经用过（重放）。
    /// </summary>
    public const string NonceReplayed = "openplatform.signing.nonce_replayed";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(OpenPlatformMessages)));

    /// <summary>
    /// 本包的消息目录（嵌入的 10 种区域文件）。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
