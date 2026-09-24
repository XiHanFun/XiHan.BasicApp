// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Options;
using Moq;
using XiHan.BasicApp.Saas.Infrastructure.Upgrade;
using XiHan.Framework.Data.SqlSugar.Initializers;
using XiHan.Framework.Data.SqlSugar.Options;
using XiHan.Framework.Upgrade.Abstractions;
using XiHan.Framework.Upgrade.Enums;
using XiHan.Framework.Upgrade.Models;
using XiHan.Framework.Upgrade.Options;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 新建的平台库登记为最新版本，不从 0.0.0 补跑历史脚本
/// </summary>
/// <remarks>
/// 起因：新建库后启动，版本记录按 0.0.0 建出，引擎把全部历史脚本在最新结构上跑了一遍，其中一句写错列名直接中断启动。
/// 按当前实体建出来的库本就是最新结构，历史脚本只该在它所属版本之前建的库上执行。
/// </remarks>
public sealed class SaasSchemaUpgraderTests
{
    private readonly List<string> _calls = [];

    private readonly TestCurrentTenant _currentTenant = new(7);

    [Fact]
    public async Task 新建的平台库_先登记最新版本再做升级检查()
    {
        var upgrader = CreateUpgrader(autoCheck: true);

        await upgrader.UpgradeAsync(new DbSchemaUpgradeContext(["Default"]));

        Assert.Equal(["baseline:platform", "ensure-initialized", "execute"], _calls);
        Assert.Equal(7, _currentTenant.Id);
    }

    [Fact]
    public async Task 存量平台库_不登记_照常升级()
    {
        var upgrader = CreateUpgrader(autoCheck: true);

        await upgrader.UpgradeAsync(new DbSchemaUpgradeContext(["Default_Erp"]));

        Assert.Equal(["ensure-initialized", "execute"], _calls);
    }

    [Fact]
    public async Task 关闭启动自动升级_新库照样登记()
    {
        var upgrader = CreateUpgrader(autoCheck: false);

        await upgrader.UpgradeAsync(new DbSchemaUpgradeContext(["Default"]));

        Assert.Equal(["baseline:platform"], _calls);
    }

    [Fact]
    public async Task 升级失败_中断初始化()
    {
        var upgrader = CreateUpgrader(autoCheck: true, status: UpgradeStatus.Failed);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => upgrader.UpgradeAsync(new DbSchemaUpgradeContext([])));
    }

    private SaasSchemaUpgrader CreateUpgrader(bool autoCheck, UpgradeStatus status = UpgradeStatus.Completed)
    {
        var statusService = new Mock<IUpgradeStatusService>();
        _ = statusService
            .Setup(service => service.EnsureInitializedAsync())
            .Callback(() => _calls.Add("ensure-initialized"))
            .Returns(Task.CompletedTask);

        var engine = new Mock<IUpgradeEngine>();
        _ = engine
            .Setup(value => value.BaselineAsync(It.IsAny<CancellationToken>()))
            .Callback(() => _calls.Add(_currentTenant.Id is null ? "baseline:platform" : $"baseline:{_currentTenant.Id}"))
            .ReturnsAsync(true);
        _ = engine
            .Setup(value => value.ExecuteAsync(It.IsAny<CancellationToken>()))
            .Callback(() => _calls.Add("execute"))
            .ReturnsAsync(new UpgradeStartResult { Status = status, Message = "升级结果" });

        return new SaasSchemaUpgrader(
            statusService.Object,
            engine.Object,
            _currentTenant,
            Options.Create(new XiHanUpgradeOptions { EnableAutoCheckOnStartup = autoCheck }),
            Options.Create(new XiHanSqlSugarCoreOptions { DefaultConfigId = "Default" }));
    }
}
