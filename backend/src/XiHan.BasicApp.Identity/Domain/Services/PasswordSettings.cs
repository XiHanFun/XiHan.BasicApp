// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Domain.Services;

/// <summary>
/// 密码设置
/// </summary>
/// <remarks>
/// 系统参数 <see cref="ConfigKey"/> 的 JSON 值，只认平台的（登录在进入租户之前）；没建即缺省值，缺省关
/// 打开之后，密码由他人设置（<see cref="Entities.SysUserSecurity.MustChangePassword"/>）的账号用密码登录时锁定到改密为止；关着只是留着标记，打开即对已有的账号生效
/// 写错即 <c>IDENTITY_VALIDATION_025</c>，只影响有标记的账号的密码登录（只在有标记时才读）
/// </remarks>
/// <param name="ForceChange">是否强制修改由他人设置的密码</param>
public sealed record PasswordSettings(bool ForceChange = false)
{
    /// <summary>
    /// 系统参数键
    /// </summary>
    public const string ConfigKey = "identity.password";

    /// <summary>
    /// 缺省值（关）
    /// </summary>
    public static PasswordSettings Default { get; } = new();

    /// <summary>
    /// 读密码设置
    /// </summary>
    /// <remarks>
    /// 没装 Settings 或没建参数即缺省值
    /// </remarks>
    /// <param name="context">调用上下文</param>
    /// <returns>参数</returns>
    public static async ValueTask<Result<PasswordSettings>> ReadAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Services.GetService<IConfigValueReader>() is not { } reader)
        {
            return Default;
        }

        var configured = await reader.GetAsync(ConfigKey, context).ConfigureAwait(false);
        if (!configured.TryGetValue(out var value))
        {
            return configured.Error!;
        }

        if (!value.TryGetValue(out var json))
        {
            return Default;
        }

        PasswordSettings? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize(json, IdentityJsonContext.Default.PasswordSettings);
        }
        catch (JsonException)
        {
            parsed = null;
        }

        return parsed is not null
            ? parsed
            : Error.Validation(IdentityErrors.PasswordSettingsInvalid, IdentityMessages.PasswordSettingsInvalid, MessageArguments.Of(("key", ConfigKey)));
    }
}
