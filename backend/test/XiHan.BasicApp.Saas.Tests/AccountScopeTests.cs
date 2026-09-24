// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 账号域写入作用域：切入账号的注册地租户，释放后回到调用方上下文；账号不存在直接拒绝。
/// </summary>
public sealed class AccountScopeTests
{
    /// <summary>
    /// 外部成员在别的租户里改个人设置：写入落在其注册地，释放后回到原租户
    /// </summary>
    [Fact]
    public async Task Enter_SwitchesToHomeTenantAndRestores()
    {
        var currentTenant = new TestCurrentTenant(7);
        var user = new SysUser { TenantId = 9 };
        SaasTestHelper.SetBasicId(user, 101);
        var users = new Mock<IUserRepository>();
        _ = users.Setup(repo => repo.GetByIdIgnoreTenantAsync(101, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var scope = new AccountScope(currentTenant, users.Object);

        using (await scope.EnterAsync(101))
        {
            Assert.Equal(9, currentTenant.Id);
        }

        Assert.Equal(7, currentTenant.Id);
    }

    /// <summary>
    /// 平台账号的注册地是 0 号租户
    /// </summary>
    [Fact]
    public async Task Enter_PlatformAccount_SwitchesToTenantZero()
    {
        var currentTenant = new TestCurrentTenant(7);
        var user = new SysUser { TenantId = 0 };
        SaasTestHelper.SetBasicId(user, 1);
        var users = new Mock<IUserRepository>();
        _ = users.Setup(repo => repo.GetByIdIgnoreTenantAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        using (await new AccountScope(currentTenant, users.Object).EnterAsync(1))
        {
            Assert.Equal(0, currentTenant.Id);
        }
    }

    /// <summary>
    /// 账号不存在：拒绝，不切换上下文
    /// </summary>
    [Fact]
    public async Task Enter_UnknownAccount_Throws()
    {
        var currentTenant = new TestCurrentTenant(7);
        var scope = new AccountScope(currentTenant, new Mock<IUserRepository>().Object);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => scope.EnterAsync(404));
        Assert.Equal(7, currentTenant.Id);
    }
}
