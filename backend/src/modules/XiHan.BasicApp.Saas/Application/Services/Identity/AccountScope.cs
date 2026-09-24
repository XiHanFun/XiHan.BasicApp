// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 账号域写入作用域实现
/// </summary>
public sealed class AccountScope(ICurrentTenant currentTenant, IUserRepository userRepository) : IAccountScope
{
    /// <summary>
    /// 切入账号的注册地租户，释放后回到原上下文
    /// </summary>
    public async Task<IDisposable> EnterAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdIgnoreTenantAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前用户不存在。");
        return currentTenant.Change(user.TenantId);
    }
}
