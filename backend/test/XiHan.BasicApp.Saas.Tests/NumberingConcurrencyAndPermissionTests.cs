// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Permissions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 进程内规则键锁和编号权限边界测试。
/// </summary>
public sealed class NumberingConcurrencyAndPermissionTests
{
    /// <summary>
    /// 相同所属租户与规则主键必须共享同一异步锁，确保单节点临界区串行执行。
    /// </summary>
    [Fact]
    public async Task LockProvider_SameRuleShouldSerializeConcurrentWork()
    {
        var provider = new NumberingLockProvider();
        var counter = 0;
        var tasks = Enumerable.Range(0, 100).Select(async _ =>
        {
            var gate = provider.Get(7, 42);
            await gate.WaitAsync();
            try
            {
                var snapshot = counter;
                await Task.Yield();
                counter = snapshot + 1;
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);

        Assert.Equal(100, counter);
    }

    /// <summary>
    /// 独立租户库可能复用相同规则主键，因此锁键必须包含规则所属租户。
    /// </summary>
    [Fact]
    public void LockProvider_DifferentOwnersShouldNotShareLock()
    {
        var provider = new NumberingLockProvider();

        Assert.NotSame(provider.Get(1, 42), provider.Get(2, 42));
        Assert.Same(provider.Get(1, 42), provider.Get(1, 42));
    }

    /// <summary>
    /// 全局管理权限必须是平台专属，而租户仍可获得查看、生成和私有规则维护权限。
    /// </summary>
    [Fact]
    public void Permissions_ShouldKeepOnlyGlobalManagementPlatformExclusive()
    {
        Assert.Contains(SaasPermissionCodes.Numbering.GlobalManage, SaasPermissionCodes.All);
        Assert.Contains(SaasPermissionCodes.Numbering.GlobalManage, SaasPlatformPermissions.PlatformOnlyCodes);
        Assert.False(SaasPlatformPermissions.IsTenantGrantable(SaasPermissionCodes.Numbering.GlobalManage));

        Assert.True(SaasPlatformPermissions.IsTenantGrantable(SaasPermissionCodes.Numbering.Read));
        Assert.True(SaasPlatformPermissions.IsTenantGrantable(SaasPermissionCodes.Numbering.Create));
        Assert.True(SaasPlatformPermissions.IsTenantGrantable(SaasPermissionCodes.Numbering.Generate));
        Assert.True(SaasPlatformPermissions.IsTenantGrantable(SaasPermissionCodes.Numbering.AllocationRead));
    }
}
