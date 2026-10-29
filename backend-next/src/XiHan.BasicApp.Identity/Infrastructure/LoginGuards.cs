// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 登录守卫的调用。
/// </summary>
/// <remarks>
/// Access 实现 <see cref="ILoginGuard"/>。没装即没有守卫，全部放行、不计数。
/// </remarks>
internal static class LoginGuards
{
    /// <summary>
    /// 签发之前逐个判定，第一个失败即返回。
    /// </summary>
    public static async ValueTask<Result> CheckAllAsync(this IEnumerable<ILoginGuard> guards, LoginAttempt attempt, InvocationContext context)
    {
        foreach (var guard in guards)
        {
            var checkedLogin = await guard.CheckAsync(attempt, context).ConfigureAwait(false);
            if (checkedLogin.IsFailure)
            {
                return checkedLogin;
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 记一次认证失败。
    /// </summary>
    public static async ValueTask RecordFailureAsync(this IEnumerable<ILoginGuard> guards, InvocationContext context)
    {
        foreach (var guard in guards)
        {
            await guard.RecordFailureAsync(context).ConfigureAwait(false);
        }
    }
}
