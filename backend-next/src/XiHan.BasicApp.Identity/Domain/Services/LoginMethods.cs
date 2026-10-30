// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Domain.Services;

/// <summary>
/// 开放的登录方式。
/// </summary>
/// <remarks>
/// 系统参数 <see cref="ConfigKey"/>：逗号分隔的方式名（<c>password</c>、<c>email</c>、<c>phone</c>），只认平台的（登录在进入租户之前）；
/// 没建即只有密码登录（4.x 的缺省同）。密码登录总是开着，写不写都一样；邮箱 / 手机验证码登录列出才开，服务端照此拦（4.x 只拦手机）。
/// 有认不出的方式名即 <c>IDENTITY_VALIDATION_021</c>：参数写错要让人看见，不悄悄当作没开。
/// </remarks>
public static class LoginMethods
{
    /// <summary>
    /// 系统参数的键。
    /// </summary>
    public const string ConfigKey = "identity.login-methods";

    /// <summary>
    /// 密码登录。
    /// </summary>
    public const string Password = "password";

    /// <summary>
    /// 邮箱验证码登录。
    /// </summary>
    public const string Email = "email";

    /// <summary>
    /// 手机验证码登录。
    /// </summary>
    public const string Phone = "phone";

    /// <summary>
    /// 渠道对应的方式名。
    /// </summary>
    /// <param name="channel">渠道。</param>
    /// <returns>方式名。</returns>
    public static string Of(ContactChannel channel) => channel == ContactChannel.Email ? Email : Phone;

    /// <summary>
    /// 读开放的登录方式。
    /// </summary>
    /// <remarks>
    /// 没装 Settings 或没建参数即只有密码登录；结果总以密码登录打头，其余按 <c>email</c>、<c>phone</c> 的顺序。
    /// </remarks>
    /// <param name="context">调用上下文。</param>
    /// <returns>开放的方式名。</returns>
    public static async ValueTask<Result<IReadOnlyList<string>>> ReadAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Services.GetService<IConfigValueReader>() is not { } reader)
        {
            return Result.Success<IReadOnlyList<string>>([Password]);
        }

        var configured = await reader.GetAsync(ConfigKey, context).ConfigureAwait(false);
        if (!configured.TryGetValue(out var value))
        {
            return configured.Error!;
        }

        if (!value.TryGetValue(out var text))
        {
            return Result.Success<IReadOnlyList<string>>([Password]);
        }

        var listed = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(static m => m.ToLowerInvariant()).ToHashSet(StringComparer.Ordinal);
        if (listed.Any(static m => m is not (Password or Email or Phone)))
        {
            return Error.Validation(IdentityErrors.LoginMethodsInvalid, IdentityMessages.LoginMethodsInvalid, MessageArguments.Of(("key", ConfigKey)));
        }

        return Result.Success<IReadOnlyList<string>>([Password, .. new[] { Email, Phone }.Where(listed.Contains)]);
    }
}
