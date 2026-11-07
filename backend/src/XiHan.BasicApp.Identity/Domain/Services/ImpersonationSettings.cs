// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Domain.Services;

/// <summary>
/// 模仿登录的参数
/// </summary>
/// <remarks>
/// 系统参数 <see cref="ConfigKey"/> 的 JSON 值，按发起人所在的一侧取（租户建了同键参数即用租户的）；没建即缺省值
/// 写错或超出范围即 <c>IDENTITY_VALIDATION_027</c>，不发起模仿（越界的分钟数不归到上下限，参数写错一律让人看见）
/// </remarks>
/// <param name="SessionMinutes">模仿会话存活分钟数（1–480）</param>
/// <param name="NotifyTarget">是否向被模仿者投递安全通知</param>
public sealed record ImpersonationSettings(int SessionMinutes = 30, bool NotifyTarget = true)
{
    /// <summary>
    /// 系统参数键
    /// </summary>
    public const string ConfigKey = "identity.impersonation";

    /// <summary>
    /// 模仿会话存活分钟数下限
    /// </summary>
    public const int MinSessionMinutes = 1;

    /// <summary>
    /// 模仿会话存活分钟数上限
    /// </summary>
    public const int MaxSessionMinutes = 480;

    /// <summary>
    /// 缺省值
    /// </summary>
    public static ImpersonationSettings Default { get; } = new();

    /// <summary>
    /// 各项是否在范围内
    /// </summary>
    public bool IsValid => SessionMinutes is >= MinSessionMinutes and <= MaxSessionMinutes;

    /// <summary>
    /// 模仿会话存活时长
    /// </summary>
    public TimeSpan SessionLifetime => TimeSpan.FromMinutes(SessionMinutes);

    /// <summary>
    /// 读模仿登录的参数
    /// </summary>
    /// <remarks>
    /// 没装 Settings 或没建参数即缺省值
    /// </remarks>
    /// <param name="context">调用上下文</param>
    /// <returns>参数</returns>
    public static async ValueTask<Result<ImpersonationSettings>> ReadAsync(InvocationContext context)
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

        ImpersonationSettings? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize(json, IdentityJsonContext.Default.ImpersonationSettings);
        }
        catch (JsonException)
        {
            parsed = null;
        }

        return parsed is { IsValid: true }
            ? parsed
            : Error.Validation(IdentityErrors.ImpersonationSettingsInvalid, IdentityMessages.ImpersonationSettingsInvalid, MessageArguments.Of(("key", ConfigKey)));
    }
}
