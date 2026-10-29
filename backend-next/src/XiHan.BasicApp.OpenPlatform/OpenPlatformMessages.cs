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

    /// <summary>
    /// 操作 <c>oauth-apps.list</c> 的说明。
    /// </summary>
    public const string OAuthAppsListDescription = "oauth_apps.list.description";

    /// <summary>
    /// 操作 <c>oauth-apps.get</c> 的说明。
    /// </summary>
    public const string OAuthAppsGetDescription = "oauth_apps.get.description";

    /// <summary>
    /// 操作 <c>oauth-apps.create</c> 的说明。
    /// </summary>
    public const string OAuthAppsCreateDescription = "oauth_apps.create.description";

    /// <summary>
    /// 操作 <c>oauth-apps.update</c> 的说明。
    /// </summary>
    public const string OAuthAppsUpdateDescription = "oauth_apps.update.description";

    /// <summary>
    /// 操作 <c>oauth-apps.reset-secret</c> 的说明。
    /// </summary>
    public const string OAuthAppsResetSecretDescription = "oauth_apps.reset_secret.description";

    /// <summary>
    /// 操作 <c>oauth-apps.delete</c> 的说明。
    /// </summary>
    public const string OAuthAppsDeleteDescription = "oauth_apps.delete.description";

    /// <summary>
    /// 操作 <c>my-oauth-apps.list</c> 的说明。
    /// </summary>
    public const string MyOAuthAppsListDescription = "my_oauth_apps.list.description";

    /// <summary>
    /// 操作 <c>my-oauth-apps.get</c> 的说明。
    /// </summary>
    public const string MyOAuthAppsGetDescription = "my_oauth_apps.get.description";

    /// <summary>
    /// 操作 <c>my-oauth-apps.create</c> 的说明。
    /// </summary>
    public const string MyOAuthAppsCreateDescription = "my_oauth_apps.create.description";

    /// <summary>
    /// 操作 <c>my-oauth-apps.update</c> 的说明。
    /// </summary>
    public const string MyOAuthAppsUpdateDescription = "my_oauth_apps.update.description";

    /// <summary>
    /// 操作 <c>my-oauth-apps.reset-secret</c> 的说明。
    /// </summary>
    public const string MyOAuthAppsResetSecretDescription = "my_oauth_apps.reset_secret.description";

    /// <summary>
    /// 操作 <c>my-oauth-apps.delete</c> 的说明。
    /// </summary>
    public const string MyOAuthAppsDeleteDescription = "my_oauth_apps.delete.description";

    /// <summary>
    /// 不支持的筛选；参数 <c>field</c>、<c>operator</c>。
    /// </summary>
    public const string FilterUnsupported = "openplatform.query.filter_unsupported";

    /// <summary>
    /// 不支持的排序；参数 <c>field</c>。
    /// </summary>
    public const string SortUnsupported = "openplatform.query.sort_unsupported";

    /// <summary>
    /// OAuth 应用不存在（我的应用里也指别人的与平台的）；参数 <c>id</c>。
    /// </summary>
    public const string OAuthAppNotFound = "openplatform.oauth_apps.not_found";

    /// <summary>
    /// ClientId 已被占用（全局唯一）；参数 <c>clientId</c>。
    /// </summary>
    public const string ClientIdTaken = "openplatform.oauth_apps.client_id_taken";

    /// <summary>
    /// 公开客户端没有密钥，不能重置。
    /// </summary>
    public const string PublicClientHasNoSecret = "openplatform.oauth_apps.public_client_no_secret";

    /// <summary>
    /// ClientId 格式不对（3 到 100 位字母、数字、<c>. _ -</c>）；参数 <c>value</c>。
    /// </summary>
    public const string ClientIdInvalid = "openplatform.oauth_apps.client_id_invalid";

    /// <summary>
    /// 授权类型不支持（须含 <c>authorization_code</c>，可加 <c>refresh_token</c>）；参数 <c>value</c>。
    /// </summary>
    public const string GrantTypeUnsupported = "openplatform.oauth_apps.grant_type_unsupported";

    /// <summary>
    /// 授权范围不支持（须含 <c>openid</c>，可加 <c>profile email phone</c>）；参数 <c>value</c>。
    /// </summary>
    public const string ScopeUnsupported = "openplatform.oauth_apps.scope_unsupported";

    /// <summary>
    /// 回调地址不对（1 到 10 条绝对地址、不带片段，http 只限本机）；参数 <c>value</c>。
    /// </summary>
    public const string RedirectUriInvalid = "openplatform.oauth_apps.redirect_uri_invalid";

    /// <summary>
    /// 主页或 Logo 不是 http / https 地址；参数 <c>value</c>。
    /// </summary>
    public const string UrlInvalid = "openplatform.oauth_apps.url_invalid";

    /// <summary>
    /// 操作 <c>oauth-consents.resolve</c> 的说明。
    /// </summary>
    public const string OAuthConsentsResolveDescription = "oauth_consents.resolve.description";

    /// <summary>
    /// 操作 <c>oauth-consents.approve</c> 的说明。
    /// </summary>
    public const string OAuthConsentsApproveDescription = "oauth_consents.approve.description";

    /// <summary>
    /// 操作 <c>oauth-consents.deny</c> 的说明。
    /// </summary>
    public const string OAuthConsentsDenyDescription = "oauth_consents.deny.description";

    /// <summary>
    /// 操作 <c>oauth-consents.list</c> 的说明。
    /// </summary>
    public const string OAuthConsentsListDescription = "oauth_consents.list.description";

    /// <summary>
    /// 操作 <c>oauth-consents.revoke</c> 的说明。
    /// </summary>
    public const string OAuthConsentsRevokeDescription = "oauth_consents.revoke.description";

    /// <summary>
    /// 没有授权过这个应用；参数 <c>clientId</c>。
    /// </summary>
    public const string ConsentNotFound = "openplatform.oauth_consents.not_found";

    /// <summary>
    /// 授权请求不对；参数 <c>error</c>（OAuth 错误码）、<c>description</c>。
    /// </summary>
    public const string AuthorizeRequestInvalid = "openplatform.oauth.authorize_invalid";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(OpenPlatformMessages)));

    /// <summary>
    /// 本包的消息目录（嵌入的 10 种区域文件）。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
