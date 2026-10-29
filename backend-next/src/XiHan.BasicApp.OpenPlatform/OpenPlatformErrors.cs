// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.OpenPlatform;

/// <summary>
/// 开放平台包的错误码：<c>OPENPLATFORM_&lt;KIND&gt;_&lt;NNN&gt;</c>。
/// </summary>
public static class OpenPlatformErrors
{
    /// <summary>
    /// 缺少签名请求头；参数 <c>headers</c>（缺的头）。
    /// </summary>
    public const string SignatureHeadersMissing = "OPENPLATFORM_DENIED_001";

    /// <summary>
    /// 时间戳超出时间窗；参数 <c>seconds</c>（允许的偏差秒数）。
    /// </summary>
    public const string TimestampExpired = "OPENPLATFORM_DENIED_002";

    /// <summary>
    /// AppKey 不存在、凭证停用或归属人账号不正常。
    /// </summary>
    public const string AccessKeyInvalid = "OPENPLATFORM_DENIED_003";

    /// <summary>
    /// 客户端地址不在凭证归属人设定的 IP 白名单内；参数 <c>ip</c>。
    /// </summary>
    public const string IpNotAllowed = "OPENPLATFORM_DENIED_004";

    /// <summary>
    /// 缺少内容签名（<c>X-Content-Sign</c>，GET 也要带空串的散列）。
    /// </summary>
    public const string ContentSignMissing = "OPENPLATFORM_DENIED_005";

    /// <summary>
    /// 内容签名与请求体明文的散列不符。
    /// </summary>
    public const string ContentSignMismatch = "OPENPLATFORM_DENIED_006";

    /// <summary>
    /// 请求签名不符。
    /// </summary>
    public const string SignatureMismatch = "OPENPLATFORM_DENIED_007";

    /// <summary>
    /// <c>X-Timestamp</c> 不是 Unix 秒。
    /// </summary>
    public const string TimestampInvalid = "OPENPLATFORM_VALIDATION_001";

    /// <summary>
    /// <c>X-Nonce</c> 太长（最多 128 个字符）。
    /// </summary>
    public const string NonceInvalid = "OPENPLATFORM_VALIDATION_002";

    /// <summary>
    /// <c>X-Sign-Algorithm</c> 与凭证归属人设定的算法不一致；参数 <c>value</c>、<c>expected</c>。
    /// </summary>
    public const string SignAlgorithmMismatch = "OPENPLATFORM_VALIDATION_003";

    /// <summary>
    /// 内容签名算法不认识；参数 <c>value</c>。
    /// </summary>
    public const string ContentAlgorithmUnsupported = "OPENPLATFORM_VALIDATION_004";

    /// <summary>
    /// 加密算法不认识；参数 <c>value</c>。
    /// </summary>
    public const string EncryptAlgorithmUnsupported = "OPENPLATFORM_VALIDATION_005";

    /// <summary>
    /// 请求体超过上限；参数 <c>max</c>（字节）。
    /// </summary>
    public const string BodyTooLarge = "OPENPLATFORM_VALIDATION_006";

    /// <summary>
    /// 加密的请求体解不开（格式不对、IV 不对或密钥不符）。
    /// </summary>
    public const string BodyDecryptFailed = "OPENPLATFORM_VALIDATION_007";

    /// <summary>
    /// 随机串在时间窗内已经用过（重放）。
    /// </summary>
    public const string NonceReplayed = "OPENPLATFORM_CONFLICT_001";

    /// <summary>
    /// OAuth 应用不存在（我的应用里也指别人的与平台的）；参数 <c>id</c>。
    /// </summary>
    public const string OAuthAppNotFound = "OPENPLATFORM_NOTFOUND_001";

    /// <summary>
    /// ClientId 已被占用（全局唯一）；参数 <c>clientId</c>。
    /// </summary>
    public const string ClientIdTaken = "OPENPLATFORM_CONFLICT_002";

    /// <summary>
    /// 公开客户端没有密钥，不能重置。
    /// </summary>
    public const string PublicClientHasNoSecret = "OPENPLATFORM_CONFLICT_003";

    /// <summary>
    /// ClientId 格式不对（3 到 100 位字母、数字、<c>. _ -</c>）；参数 <c>value</c>。
    /// </summary>
    public const string ClientIdInvalid = "OPENPLATFORM_VALIDATION_008";

    /// <summary>
    /// 授权类型不支持（须含 <c>authorization_code</c>，可加 <c>refresh_token</c>）；参数 <c>value</c>。
    /// </summary>
    public const string GrantTypeUnsupported = "OPENPLATFORM_VALIDATION_009";

    /// <summary>
    /// 授权范围不支持（须含 <c>openid</c>，可加 <c>profile email phone</c>）；参数 <c>value</c>。
    /// </summary>
    public const string ScopeUnsupported = "OPENPLATFORM_VALIDATION_010";

    /// <summary>
    /// 回调地址不对（1 到 10 条绝对地址、不带片段，http 只限本机）；参数 <c>value</c>。
    /// </summary>
    public const string RedirectUriInvalid = "OPENPLATFORM_VALIDATION_011";

    /// <summary>
    /// 主页或 Logo 不是 http / https 地址；参数 <c>value</c>。
    /// </summary>
    public const string UrlInvalid = "OPENPLATFORM_VALIDATION_012";

    /// <summary>
    /// 没有授权过这个应用；参数 <c>clientId</c>。
    /// </summary>
    public const string ConsentNotFound = "OPENPLATFORM_NOTFOUND_002";

    /// <summary>
    /// 授权请求不对；参数 <c>error</c>（OAuth 错误码）、<c>description</c>。
    /// </summary>
    public const string AuthorizeRequestInvalid = "OPENPLATFORM_VALIDATION_014";

    /// <summary>
    /// 不支持的筛选或排序；消息用 <c>FilterUnsupported</c> 或 <c>SortUnsupported</c>。
    /// </summary>
    public const string QueryUnsupported = "OPENPLATFORM_VALIDATION_013";
}
