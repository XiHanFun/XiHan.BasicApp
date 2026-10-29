// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.OpenPlatform.Infrastructure;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;
using XiHan.Framework.Protocols.Http.Routing;

namespace XiHan.BasicApp.OpenPlatform.Middleware;

/// <summary>
/// 签名开放接口的验签。
/// </summary>
/// <remarks>
/// Authenticate 阶段，只对 <c>open.*</c> 操作：照 4.x 的请求头与规范串，依次校验请求头、时间窗、凭证（停用、归属人账号不正常都算无效）、IP 白名单、
/// 算法、请求体（超限、解密）、内容签名与请求签名，最后才占用随机串防重放（4.x 在验签之前就占用，伪造的请求也能把正常请求的随机串占掉）。
/// 加密的请求体解密后重新绑定载荷；要求加密响应或请求本身加密了，就把成功的 JSON 响应换成信封 <c>{alg, data, iv, contentSign}</c>。
/// 每次调用无论成败都登记一条开放接口日志（<see cref="OpenApiLogEntry"/>，由 Auditing 在 Observe 阶段落库），验签通过即记下凭证的最近使用时间。
/// 与 4.x 不同：明文请求不必带 <c>X-Encrypt-Algorithm: NONE</c>；算法以凭证的设置为准（请求头只能与之一致）；签名恒定时间比较；不再有「允许不签名」的开关；
/// 失败按错误类别给 400 / 403 / 409（4.x 多数是 401）。
/// </remarks>
/// <param name="credentials">凭证目录。</param>
/// <param name="nonces">随机串的占用存储。</param>
/// <param name="options">开放平台配置。</param>
/// <param name="clock">时间源。</param>
public sealed class RequestSigningMiddleware(IApiCredentialDirectory credentials, ICacheStore nonces, OpenPlatformOptions options, TimeProvider clock) : IPipelineMiddleware
{
    /// <summary>
    /// 日志里正文摘要的上限（字符）。
    /// </summary>
    private const int BodySummaryLength = 4096;

    /// <summary>
    /// 必带的请求头。
    /// </summary>
    private const string RequiredHeaders = OpenApiSignature.AccessKeyHeader + ", " + OpenApiSignature.TimestampHeader + ", " + OpenApiSignature.NonceHeader + ", " + OpenApiSignature.SignatureHeader;

    /// <inheritdoc />
    public async ValueTask InvokeAsync(InvocationContext context, PipelineStep nextStep)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);
        if (!context.GetOperation().TryGetValue(out var operation) || !OpenApiOperations.IsOpen(operation.Id))
        {
            await nextStep(context).ConfigureAwait(false);
            return;
        }

        var log = new OpenApiLogEntry();
        context.AddAuditLog(log);
        var state = context.Services.GetService<OpenApiRequest>();
        var verified = await VerifyAsync(context, operation, state, log).ConfigureAwait(false);
        if (!verified.TryGetValue(out var encryptResponse))
        {
            context.SetOutcome(verified.Error!);
            return;
        }

        var httpContext = state!.HttpContext!;
        var original = httpContext.Response.Body;
        using var captured = new MemoryStream();
        httpContext.Response.Body = captured;
        try
        {
            await nextStep(context).ConfigureAwait(false);
        }
        finally
        {
            httpContext.Response.Body = original;
        }

        var plain = captured.ToArray();
        log.ResponseBody = Summary(plain);
        var succeeded = !context.GetOutcome().TryGetValue(out var outcome) || outcome.IsSuccess;
        if (encryptResponse && succeeded && plain.Length > 0 && !httpContext.Response.HasStarted)
        {
            var envelope = Envelope(state.Secret, plain);
            if (envelope is not null)
            {
                httpContext.Response.Headers[OpenApiSignature.SecureResponseHeader] = "1";
                httpContext.Response.ContentLength = envelope.Length;
                log.ResponseSize = envelope.Length;
                await original.WriteAsync(envelope, context.CancellationToken).ConfigureAwait(false);
                return;
            }
        }

        log.ResponseSize = plain.Length;
        if (plain.Length > 0)
        {
            await original.WriteAsync(plain, context.CancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 验签，并写好调用方、解密后的载荷与日志细节。
    /// </summary>
    /// <returns>成功时为是否加密响应。</returns>
    private async Task<Result<bool>> VerifyAsync(InvocationContext context, OperationDescriptor operation, OpenApiRequest? state, OpenApiLogEntry log)
    {
        var httpContext = state?.HttpContext;
        if (httpContext is null)
        {
            // 只能经 HTTP 签名调用：没有经过缓冲（进程内调用、别的协议）一律当作没带签名
            return Error.Denied(OpenPlatformErrors.SignatureHeadersMissing, OpenPlatformMessages.SignatureHeadersMissing, MessageArguments.Of(("headers", RequiredHeaders)));
        }

        var headers = httpContext.Request.Headers;
        var appKey = headers[OpenApiSignature.AccessKeyHeader].ToString().Trim();
        var timestamp = headers[OpenApiSignature.TimestampHeader].ToString().Trim();
        var nonce = headers[OpenApiSignature.NonceHeader].ToString().Trim();
        var signature = headers[OpenApiSignature.SignatureHeader].ToString().Trim();
        log.ClientId = appKey.Length == 0 ? null : appKey;
        log.RequestSize = state!.RawBody.Length;
        var missing = new List<string>();
        AddIfEmpty(missing, OpenApiSignature.AccessKeyHeader, appKey);
        AddIfEmpty(missing, OpenApiSignature.TimestampHeader, timestamp);
        AddIfEmpty(missing, OpenApiSignature.NonceHeader, nonce);
        AddIfEmpty(missing, OpenApiSignature.SignatureHeader, signature);
        if (missing.Count > 0)
        {
            return Error.Denied(OpenPlatformErrors.SignatureHeadersMissing, OpenPlatformMessages.SignatureHeadersMissing, MessageArguments.Of(("headers", string.Join(", ", missing))));
        }

        if (nonce.Length > 128)
        {
            return Error.Validation(OpenPlatformErrors.NonceInvalid, OpenPlatformMessages.NonceInvalid);
        }

        if (!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) || seconds > DateTimeOffset.MaxValue.ToUnixTimeSeconds())
        {
            return Error.Validation(OpenPlatformErrors.TimestampInvalid, OpenPlatformMessages.TimestampInvalid);
        }

        var now = clock.GetUtcNow();
        if ((now - DateTimeOffset.FromUnixTimeSeconds(seconds)).Duration() > options.TimestampTolerance)
        {
            return Error.Denied(OpenPlatformErrors.TimestampExpired, OpenPlatformMessages.TimestampExpired, MessageArguments.Of(("seconds", options.TimestampToleranceSeconds)));
        }

        var credential = await credentials.FindAsync(appKey, context.CancellationToken).ConfigureAwait(false);
        if (credential is not null)
        {
            log.AppName = credential.Name;
            log.UserId = credential.UserId;
            log.UserName = credential.UserName;
            log.TenantId = credential.HomeTenantId;
            log.SignatureAlgorithm = credential.SignatureAlgorithm;
        }

        if (credential is not { IsEnabled: true, UserActive: true })
        {
            return Error.Denied(OpenPlatformErrors.AccessKeyInvalid, OpenPlatformMessages.AccessKeyInvalid);
        }

        var remote = context.GetOrigin().TryGetValue(out var origin) ? origin.RemoteAddress : null;
        if (!ApiIpAllowList.Allows(credential.IpAllowList, remote))
        {
            return Error.Denied(OpenPlatformErrors.IpNotAllowed, OpenPlatformMessages.IpNotAllowed, MessageArguments.Of(("ip", remote ?? string.Empty)));
        }

        var requestedAlgorithm = headers[OpenApiSignature.SignAlgorithmHeader].ToString();
        if (requestedAlgorithm.Length > 0 && !string.Equals(ApiSignatureAlgorithms.Normalize(requestedAlgorithm), credential.SignatureAlgorithm, StringComparison.Ordinal))
        {
            return Error.Validation(OpenPlatformErrors.SignAlgorithmMismatch, OpenPlatformMessages.SignAlgorithmMismatch, MessageArguments.Of(("value", requestedAlgorithm), ("expected", credential.SignatureAlgorithm)));
        }

        var contentAlgorithmHeader = headers[OpenApiSignature.ContentSignAlgorithmHeader].ToString();
        if (OpenApiSignature.NormalizeContentAlgorithm(contentAlgorithmHeader) is not { } contentAlgorithm)
        {
            return Error.Validation(OpenPlatformErrors.ContentAlgorithmUnsupported, OpenPlatformMessages.ContentAlgorithmUnsupported, MessageArguments.Of(("value", contentAlgorithmHeader)));
        }

        var encryptionHeader = headers[OpenApiSignature.EncryptAlgorithmHeader].ToString();
        if (OpenApiSignature.NormalizeEncryption(encryptionHeader) is not { } encryption)
        {
            return Error.Validation(OpenPlatformErrors.EncryptAlgorithmUnsupported, OpenPlatformMessages.EncryptAlgorithmUnsupported, MessageArguments.Of(("value", encryptionHeader)));
        }

        if (state.BodyTooLarge)
        {
            return Error.Validation(OpenPlatformErrors.BodyTooLarge, OpenPlatformMessages.BodyTooLarge, MessageArguments.Of(("max", options.MaxRequestBodyBytes)));
        }

        var encrypted = encryption == OpenApiSignature.AesCbc && state.RawBody.Length > 0;
        var plain = state.RawBody;
        if (encrypted)
        {
            var decrypted = Decrypt(credential.Secret, state.RawBody, headers[OpenApiSignature.EncryptIvHeader].ToString(), nonce);
            if (decrypted is null)
            {
                return Error.Validation(OpenPlatformErrors.BodyDecryptFailed, OpenPlatformMessages.BodyDecryptFailed);
            }

            plain = decrypted;
        }

        log.RequestBody = Summary(plain);
        var contentSign = headers[OpenApiSignature.ContentSignHeader].ToString();
        if (contentSign.Length == 0)
        {
            return Error.Denied(OpenPlatformErrors.ContentSignMissing, OpenPlatformMessages.ContentSignMissing);
        }

        var expectedContent = OpenApiSignature.ContentHash(plain, contentAlgorithm);
        if (!OpenApiSignature.ContentMatches(contentSign, expectedContent))
        {
            return Error.Denied(OpenPlatformErrors.ContentSignMismatch, OpenPlatformMessages.ContentSignMismatch);
        }

        var request = httpContext.Request;
        var canonical = OpenApiSignature.CanonicalString(request.Method, request.Path.Value ?? "/", OpenApiSignature.CanonicalQuery(request.Query), expectedContent, timestamp, nonce);
        if (!OpenApiSignature.Matches(signature, OpenApiSignature.Sign(credential.Secret, canonical, credential.SignatureAlgorithm)))
        {
            return Error.Denied(OpenPlatformErrors.SignatureMismatch, OpenPlatformMessages.SignatureMismatch);
        }

        log.SignatureValid = true;

        // 验签通过才占用随机串：保留两倍时间窗，窗口内的重放一定能识别
        var reserved = await nonces.TryAddAsync(new CacheKey($"openplatform:nonce:{credential.AppKey}:{nonce}"), "1"u8.ToArray(), CacheEntryOptions.Expires(options.TimestampTolerance * 2), context.CancellationToken).ConfigureAwait(false);
        if (!reserved)
        {
            return Error.Conflict(OpenPlatformErrors.NonceReplayed, OpenPlatformMessages.NonceReplayed);
        }

        if (encrypted)
        {
            // 解密后的明文按框架同一套规则重新绑定（路由值、请求体、查询串）
            request.Body = new MemoryStream(plain, writable: false);
            request.ContentLength = plain.Length;
            var bound = await OperationRequestBinder.BindAsync(httpContext, operation, context.CancellationToken).ConfigureAwait(false);
            if (!bound.TryGetValue(out var payload))
            {
                return bound.Error!;
            }

            context.SetOperationPayload(payload);
        }

        state.Caller = new OpenApiCaller(credential.Id, credential.AppKey, credential.Name, credential.UserId, credential.UserName);
        state.Secret = credential.Secret;
        await credentials.TouchAsync(credential.Id, now, context.CancellationToken).ConfigureAwait(false);
        return encrypted || OpenApiSignature.IsTruthy(headers[OpenApiSignature.EncryptResponseHeader].ToString());
    }

    private static void AddIfEmpty(List<string> missing, string name, string value)
    {
        if (value.Length == 0)
        {
            missing.Add(name);
        }
    }

    /// <summary>
    /// 解开加密的请求体。
    /// </summary>
    /// <remarks>
    /// 信封 <c>{"data": Base64, "iv"?: Base64}</c> 或整段 Base64（也认 JSON 字符串）。
    /// </remarks>
    /// <returns>明文；解不开为 <see langword="null"/>。</returns>
    private static byte[]? Decrypt(string secret, byte[] raw, string ivHeader, string nonce)
    {
        string? data = null;
        string? iv = null;
        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var value) && value.ValueKind == JsonValueKind.String)
            {
                data = value.GetString();
                iv = root.TryGetProperty("iv", out var ivValue) && ivValue.ValueKind == JsonValueKind.String ? ivValue.GetString() : null;
            }
            else if (root.ValueKind == JsonValueKind.String)
            {
                data = root.GetString();
            }
        }
        catch (JsonException)
        {
            data = Encoding.UTF8.GetString(raw).Trim();
        }

        try
        {
            var ivBytes = ivHeader.Length > 0 ? Convert.FromBase64String(ivHeader) : iv is { Length: > 0 } ? Convert.FromBase64String(iv) : OpenApiSignature.DefaultIv(nonce);
            return string.IsNullOrEmpty(data) || ivBytes.Length != 16 ? null : OpenApiSignature.Decrypt(secret, Convert.FromBase64String(data), ivBytes);
        }
        catch (Exception exception) when (exception is FormatException or System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }

    /// <summary>
    /// 加密响应的信封：每次随机 IV，<c>contentSign</c> 是明文的 SHA256。
    /// </summary>
    private static byte[]? Envelope(string? secret, byte[] plain)
    {
        if (secret is null)
        {
            return null;
        }

        var iv = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("alg", OpenApiSignature.AesCbc);
            writer.WriteString("data", Convert.ToBase64String(OpenApiSignature.Encrypt(secret, plain, iv)));
            writer.WriteString("iv", Convert.ToBase64String(iv));
            writer.WriteString("contentSign", OpenApiSignature.ContentHash(plain, OpenApiSignature.Sha256));
            writer.WriteEndObject();
        }

        return buffer.WrittenSpan.ToArray();
    }

    private static string? Summary(byte[] body)
    {
        if (body.Length == 0)
        {
            return null;
        }

        var text = Encoding.UTF8.GetString(body, 0, Math.Min(body.Length, BodySummaryLength * 4));
        return text.Length <= BodySummaryLength ? text : text[..BodySummaryLength];
    }
}
