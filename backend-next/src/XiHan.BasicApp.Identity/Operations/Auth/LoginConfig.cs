// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Infrastructure.External;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// 登录页配置（匿名只读操作，登记在架构测试的匿名白名单）：<c>GET /api/auth:login-config</c>。登录页据此决定显示哪些入口、要不要图形验证码。
/// </summary>
/// <param name="options">身份配置。</param>
/// <param name="external">第三方登录配置（登录页上的提供方）。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http)]
public sealed class LoginConfig(IdentityOptions options, ExternalLoginOptions external) : IOperation<LoginConfigRequest, LoginConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.login-config";

    /// <summary>
    /// 密码登录的方式名。
    /// </summary>
    public const string PasswordMethod = "password";

    /// <inheritdoc />
    public ValueTask<Result<LoginConfigResponse>> HandleAsync(LoginConfigRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return ValueTask.FromResult<Result<LoginConfigResponse>>(new LoginConfigResponse([PasswordMethod], external.Enabled ? [.. external.Providers.Select(static p => new LoginProviderResponse(p.Name, p.DisplayName, p.Type))] : [], options.CaptchaEnabled));
    }
}
