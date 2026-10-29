// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Identity.Domain.Entities;

namespace XiHan.BasicApp.Identity.Operations.UserSettings;

/// <summary>
/// 一条用户设置。
/// </summary>
/// <remarks>
/// 还没存过时 <see cref="Value"/> 与 <see cref="ModifiedAt"/> 为 <c>null</c>。
/// </remarks>
/// <param name="Scene">场景。</param>
/// <param name="Key">设置键。</param>
/// <param name="Value">设置载荷（JSON）。</param>
/// <param name="ModifiedAt">最后写入时间（UTC）。</param>
public sealed record UserSettingResponse(UserSettingScene Scene, string Key, string? Value, DateTimeOffset? ModifiedAt);

/// <summary>
/// 用户设置键的文法。
/// </summary>
/// <remarks>
/// 字母或数字开头，只含字母、数字、<c>.</c>、<c>_</c>、<c>:</c>、<c>-</c>，最长 100（页面编码如 <c>identity.users</c>、看板如 <c>workbench:widgets</c>）。
/// </remarks>
public static class UserSettingKeys
{
    /// <summary>
    /// 键的正则。
    /// </summary>
    public const string Pattern = "^[A-Za-z0-9][A-Za-z0-9._:-]{0,99}$";

    /// <summary>
    /// 载荷的长度上限（字符）。
    /// </summary>
    public const int MaxValueLength = 262_144;
}

/// <summary>
/// <c>user-settings.get</c> 的请求。
/// </summary>
/// <param name="Scene">场景。</param>
/// <param name="Key">设置键。</param>
public sealed record GetUserSettingRequest(
    UserSettingScene Scene,
    [Required, StringLength(100, MinimumLength = 1), RegularExpression(UserSettingKeys.Pattern)] string Key);

/// <summary>
/// <c>user-settings.update</c> 的请求。
/// </summary>
/// <remarks>
/// 整体覆盖，<see cref="Value"/> 为 <c>null</c> 即清空。
/// </remarks>
/// <param name="Scene">场景。</param>
/// <param name="Key">设置键。</param>
/// <param name="Value">设置载荷（JSON）。</param>
/// <param name="ClientId">发起写入的客户端标识（每个标签页一个），原样放进推给其他端的 <c>UserSettingChanged</c>，发起端据此忽略自己的回显；不落库。</param>
/// <param name="Origin">随推送中转给其他端的附带信息（主题切换的点击位置）；不落库。</param>
public sealed record UpdateUserSettingRequest(
    UserSettingScene Scene,
    [Required, StringLength(100, MinimumLength = 1), RegularExpression(UserSettingKeys.Pattern)] string Key,
    [StringLength(UserSettingKeys.MaxValueLength)] string? Value = null,
    [StringLength(64)] string? ClientId = null,
    [StringLength(64)] string? Origin = null);
