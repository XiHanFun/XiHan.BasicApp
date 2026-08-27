// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using System.Reflection;
using XiHan.BasicApp.Web.Core.Upgrade;
using XiHan.Framework.Upgrade.Abstractions;

namespace XiHan.BasicApp.Web.Core.Tests;

/// <summary>
/// 升级维护模式管理器测试：锁定框架升级引擎与本应用维护标志位之间的映射契约。
/// </summary>
/// <remarks>
/// 该适配器是框架 <c>IUpgradeMaintenanceModeManager</c> 的替换实现。一旦它没有把注入的那一个
/// <see cref="MaintenanceModeState"/> 置位/复位，或者签名与接口对不上被静默旁路回框架的空实现
/// <c>DefaultUpgradeMaintenanceModeManager</c>，维护模式就会在不报任何错的情况下彻底失效。
/// </remarks>
public sealed class BasicAppUpgradeMaintenanceModeManagerTests
{
    /// <summary>
    /// EnterAsync 必须把注入的那一个状态实例置位，而不是另建实例。
    /// </summary>
    [Fact]
    public async Task EnterAsync_ShouldActivateInjectedState()
    {
        var state = new MaintenanceModeState();
        var manager = CreateManager(state, out _);

        await manager.EnterAsync();

        Assert.True(state.IsActive);
    }

    /// <summary>
    /// ExitAsync 必须把同一个状态实例复位，否则升级完成后系统卡死在 503。
    /// </summary>
    [Fact]
    public async Task ExitAsync_ShouldDeactivateInjectedState()
    {
        var state = new MaintenanceModeState();
        var manager = CreateManager(state, out _);
        await manager.EnterAsync();

        await manager.ExitAsync();

        Assert.False(state.IsActive);
    }

    /// <summary>
    /// 两个方法都是同步完成（Task.CompletedTask），升级引擎不应在置位处产生额外的异步等待。
    /// </summary>
    [Fact]
    public void EnterExitAsync_ShouldReturnCompletedTask()
    {
        var state = new MaintenanceModeState();
        var manager = CreateManager(state, out _);

        var enterTask = manager.EnterAsync();
        var exitTask = manager.ExitAsync();

        Assert.True(enterTask.IsCompletedSuccessfully);
        Assert.True(exitTask.IsCompletedSuccessfully);
        Assert.Same(Task.CompletedTask, enterTask);
        Assert.Same(Task.CompletedTask, exitTask);
    }

    /// <summary>
    /// 【回归锚点·最高优先级】ExitAsync 传入已取消的令牌时必须照常复位且不抛。
    /// 框架 UpgradeEngine 的 catch 块用的是同一个 cancellationToken 调 ExitAsync
    /// （见 XiHan.Framework.Upgrade/Services/UpgradeEngine.cs 失败分支），
    /// 若这里改成尊重取消（ThrowIfCancellationRequested），因取消而失败的升级
    /// 将让系统永久停留在 503 且没有任何自愈路径。
    /// </summary>
    [Fact]
    public async Task ExitAsync_CanceledTokenShouldStillDeactivate()
    {
        var state = new MaintenanceModeState();
        var manager = CreateManager(state, out _);
        await manager.EnterAsync();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await manager.ExitAsync(cancellation.Token);

        Assert.False(state.IsActive);
    }

    /// <summary>
    /// EnterAsync 传入已取消的令牌时同样不抛，行为与正常令牌一致，避免取消竞态在置位阶段就崩。
    /// </summary>
    [Fact]
    public async Task EnterAsync_CanceledTokenShouldStillActivate()
    {
        var state = new MaintenanceModeState();
        var manager = CreateManager(state, out _);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await manager.EnterAsync(cancellation.Token);

        Assert.True(state.IsActive);
    }

    /// <summary>
    /// 进入维护模式是异常态，必须记 Warning，运维才能在 503 期间定位到原因。
    /// </summary>
    [Fact]
    public async Task EnterAsync_ShouldLogWarning()
    {
        var state = new MaintenanceModeState();
        var manager = CreateManager(state, out var logger);

        await manager.EnterAsync();

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.NotEmpty(entry.Message);
    }

    /// <summary>
    /// 退出维护模式是正常收尾，记 Information，用于确认维护窗口何时闭合。
    /// </summary>
    [Fact]
    public async Task ExitAsync_ShouldLogInformation()
    {
        var state = new MaintenanceModeState();
        var manager = CreateManager(state, out var logger);

        await manager.ExitAsync();

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.NotEmpty(entry.Message);
    }

    /// <summary>
    /// 重复进入必须幂等，且 Enter→Exit→Enter 序列终态正确：
    /// 升级引擎按租户串行循环，每租户各跑一轮进出，中途状态不得错乱。
    /// </summary>
    [Fact]
    public async Task EnterExitAsync_RepeatedSequenceShouldKeepStateCorrect()
    {
        var state = new MaintenanceModeState();
        var manager = CreateManager(state, out _);

        await manager.EnterAsync();
        await manager.EnterAsync();
        Assert.True(state.IsActive);

        await manager.ExitAsync();
        Assert.False(state.IsActive);

        await manager.EnterAsync();
        Assert.True(state.IsActive);

        await manager.ExitAsync();
        await manager.ExitAsync();
        Assert.False(state.IsActive);
    }

    /// <summary>
    /// 类型必须实现框架接口：否则模块的 Replace 注册会因类型不匹配而失败，
    /// 或框架接口演进后此适配器被静默旁路回空实现。
    /// </summary>
    [Fact]
    public void Type_ShouldImplementFrameworkInterface()
    {
        Assert.Contains(typeof(IUpgradeMaintenanceModeManager), typeof(BasicAppUpgradeMaintenanceModeManager).GetInterfaces());
    }

    /// <summary>
    /// 两个方法的签名必须与接口逐字一致（返回类型、参数类型、可选参数），
    /// 且由本类型公开实现（非显式实现、非继承），保证 DI 解析到的就是这套行为。
    /// </summary>
    /// <param name="methodName">接口方法名</param>
    [Theory]
    [InlineData(nameof(IUpgradeMaintenanceModeManager.EnterAsync))]
    [InlineData(nameof(IUpgradeMaintenanceModeManager.ExitAsync))]
    public void Methods_ShouldMatchInterfaceSignature(string methodName)
    {
        var interfaceMethod = typeof(IUpgradeMaintenanceModeManager).GetMethod(methodName);
        Assert.NotNull(interfaceMethod);

        var implementation = typeof(BasicAppUpgradeMaintenanceModeManager).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public,
            [typeof(CancellationToken)]);
        Assert.NotNull(implementation);
        Assert.Equal(typeof(BasicAppUpgradeMaintenanceModeManager), implementation.DeclaringType);
        Assert.Equal(interfaceMethod.ReturnType, implementation.ReturnType);

        var interfaceParameter = Assert.Single(interfaceMethod.GetParameters());
        var implementationParameter = Assert.Single(implementation.GetParameters());
        Assert.Equal(interfaceParameter.ParameterType, implementationParameter.ParameterType);
        Assert.Equal(interfaceParameter.IsOptional, implementationParameter.IsOptional);
    }

    /// <summary>
    /// 接口映射必须指向本类型的公开方法，杜绝显式实现导致的「直接调用不生效」错觉。
    /// </summary>
    [Fact]
    public void InterfaceMap_ShouldTargetPublicMethodsOnThisType()
    {
        var map = typeof(BasicAppUpgradeMaintenanceModeManager).GetInterfaceMap(typeof(IUpgradeMaintenanceModeManager));

        Assert.Equal(2, map.TargetMethods.Length);
        Assert.All(map.TargetMethods, method =>
        {
            Assert.True(method.IsPublic);
            Assert.Equal(typeof(BasicAppUpgradeMaintenanceModeManager), method.DeclaringType);
        });
    }

    /// <summary>
    /// 构造管理器并暴露内存日志替身。
    /// </summary>
    /// <param name="state">维护模式状态</param>
    /// <param name="logger">记录日志级别与消息的替身</param>
    /// <returns>被测管理器</returns>
    private static BasicAppUpgradeMaintenanceModeManager CreateManager(
        MaintenanceModeState state,
        out RecordingLogger<BasicAppUpgradeMaintenanceModeManager> logger)
    {
        logger = new RecordingLogger<BasicAppUpgradeMaintenanceModeManager>();
        return new BasicAppUpgradeMaintenanceModeManager(state, logger);
    }
}
