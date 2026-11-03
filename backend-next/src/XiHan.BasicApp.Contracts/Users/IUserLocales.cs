// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Users;

/// <summary>
/// 用户的界面语言
/// </summary>
/// <remarks>
/// 取用户偏好里同步到后端的界面语言（偏好快照 <c>global</c> 里的 <c>xihan_locale</c>，前端偏好同步缺省开着）；用户关掉了偏好同步、或还没推过快照时没有
/// 给用户出文案而调用本身不代表收件人时（如后台触发的站内提醒）按它选区域，没有的由调用方回退。Identity 实现
/// </remarks>
public interface IUserLocales
{
    /// <summary>
    /// 取一组用户的界面语言
    /// </summary>
    /// <remarks>
    /// 只返回存了语言的那些；值是前端存的原样（如 <c>zh-CN</c>），调用方再与支持的区域协商
    /// </remarks>
    /// <param name="userIds">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户 ID 到界面语言</returns>
    Task<IReadOnlyDictionary<Guid, string>> PreferredAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
}
