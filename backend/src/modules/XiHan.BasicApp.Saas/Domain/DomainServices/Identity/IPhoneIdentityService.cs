// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 手机号码写入口径：所有写入手机号码的入口都经过这里，保证存储格式与唯一性一致
/// </summary>
public interface IPhoneIdentityService
{
    /// <summary>
    /// 正规化并校验唯一性，返回可直接落库的 E.164
    /// </summary>
    /// <param name="rawPhone">原始输入；空白视为未填写</param>
    /// <param name="excludeUserId">排除的用户标识（更新自身时传入）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>E.164 手机号码；未填写时为 null</returns>
    Task<string?> ResolveForWriteAsync(string? rawPhone, long? excludeUserId, CancellationToken cancellationToken = default);
}
