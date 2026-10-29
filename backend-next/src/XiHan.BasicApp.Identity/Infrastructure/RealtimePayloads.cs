// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// <c>ForceLogout</c> 的载荷：客户端在 <see cref="TargetSessionIds"/> 里找到自己的 <c>sid</c> 才退出登录，弹窗显示 <see cref="Reason"/>（为空即客户端的缺省文案）。
/// </summary>
/// <param name="Reason">管理员填写的原因；系统吊销（改密、锁定、删除）为空。</param>
/// <param name="TargetSessionIds">被吊销的会话。</param>
public sealed record ForceLogoutPayload(string? Reason, IReadOnlyList<Guid> TargetSessionIds);

/// <summary>
/// <c>UserSettingChanged</c> 的载荷：同一用户在别的端写了设置；客户端按 <see cref="SourceClientId"/> 忽略自己发起的回显。
/// </summary>
/// <param name="Scene">场景。</param>
/// <param name="SettingKey">设置键。</param>
/// <param name="SettingValue">新的值（JSON），清空为空。</param>
/// <param name="SourceClientId">发起写入的客户端标识（每个标签页一个）。</param>
/// <param name="Origin">发起端附带的中转信息（主题切换的点击位置），不落库。</param>
public sealed record UserSettingChangedPayload(UserSettingScene Scene, string SettingKey, string? SettingValue, string? SourceClientId, string? Origin);
