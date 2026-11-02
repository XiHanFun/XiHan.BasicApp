// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Persistence.SqlSugar;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 用户的界面语言（读偏好快照）。
/// </summary>
/// <remarks>
/// 前端把整份偏好同步成一条用户设置（场景偏好、键 <see cref="PreferenceKey"/>），界面语言是其中的 <see cref="LocaleProperty"/>；
/// 键名随前端偏好的存储约定，那边改名这里跟着改。载荷不是 JSON 对象或没有这个字段的算没有。
/// </remarks>
/// <param name="unitOfWork">工作单元。</param>
public sealed class UserLocales(SqlSugarUnitOfWork unitOfWork) : IUserLocales
{
    /// <summary>
    /// 偏好快照的设置键。
    /// </summary>
    public const string PreferenceKey = "global";

    /// <summary>
    /// 偏好快照里界面语言的字段名。
    /// </summary>
    public const string LocaleProperty = "xihan_locale";

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> PreferredAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        var ids = userIds.Distinct().ToList();
        var locales = new Dictionary<Guid, string>();
        if (ids.Count == 0)
        {
            return locales;
        }

        var rows = await unitOfWork.ConnectionFor(typeof(SysUserSetting)).Queryable<SysUserSetting>()
            .Where(s => ids.Contains(s.UserId) && !s.IsDeleted && s.Scene == UserSettingScene.Preference && s.Key == PreferenceKey)
            .Select(static s => new { s.UserId, s.Value })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var row in rows)
        {
            if (LocaleOf(row.Value) is { } locale)
            {
                locales[row.UserId] = locale;
            }
        }

        return locales;
    }

    private static string? LocaleOf(string? snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(snapshot);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty(LocaleProperty, out var value)
                && value.ValueKind == JsonValueKind.String
                && value.GetString() is { Length: > 0 } locale
                ? locale
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
