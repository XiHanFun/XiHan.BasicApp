// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.OpenPlatform.Operations.MyOauthApps;
using XiHan.BasicApp.OpenPlatform.Operations.OauthApps;
using XiHan.BasicApp.OpenPlatform.Operations.OauthConsents;
using XiHan.BasicApp.OpenPlatform.Operations.Open;

namespace XiHan.BasicApp.OpenPlatform;

/// <summary>
/// 开放平台包操作的请求 / 响应的序列化上下文（源生成；枚举按字符串）。
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(OpenPingRequest))]
[JsonSerializable(typeof(OpenPingResponse))]
[JsonSerializable(typeof(OpenEchoRequest))]
[JsonSerializable(typeof(OpenEchoResponse))]
[JsonSerializable(typeof(PageRequest))]
[JsonSerializable(typeof(PageResult<OAuthAppResponse>))]
[JsonSerializable(typeof(BatchResult))]
[JsonSerializable(typeof(OAuthAppResponse))]
[JsonSerializable(typeof(OAuthAppSecretResponse))]
[JsonSerializable(typeof(CreateOAuthAppRequest))]
[JsonSerializable(typeof(UpdateOAuthAppRequest))]
[JsonSerializable(typeof(OAuthAppIdRequest))]
[JsonSerializable(typeof(OAuthAppIdsRequest))]
[JsonSerializable(typeof(MyOAuthAppsRequest))]
[JsonSerializable(typeof(MyOAuthAppsResponse))]
[JsonSerializable(typeof(CreateMyOAuthAppRequest))]
[JsonSerializable(typeof(UpdateMyOAuthAppRequest))]
[JsonSerializable(typeof(OAuthAuthorizeRequest))]
[JsonSerializable(typeof(OAuthConsentPreviewResponse))]
[JsonSerializable(typeof(OAuthConsentRedirectResponse))]
[JsonSerializable(typeof(OAuthConsentsRequest))]
[JsonSerializable(typeof(OAuthConsentResponse))]
[JsonSerializable(typeof(OAuthConsentsResponse))]
[JsonSerializable(typeof(RevokeOAuthConsentRequest))]
[JsonSerializable(typeof(RevokeOAuthConsentResponse))]
public sealed partial class OpenPlatformJsonContext : JsonSerializerContext;
