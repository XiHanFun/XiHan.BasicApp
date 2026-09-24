// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 账号域写入作用域
/// </summary>
/// <remarks>
/// 账号域数据（安全、个人设置、通知偏好、API 凭证、三方绑定、密码历史）落在账号的注册地租户，
/// 不随当前所在上下文：外部成员在别的租户里保存自己的设置，行也写回注册地。
/// </remarks>
public interface IAccountScope
{
    /// <summary>
    /// 切入账号的注册地租户，释放后回到原上下文
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>作用域</returns>
    Task<IDisposable> EnterAsync(long userId, CancellationToken cancellationToken = default);
}
