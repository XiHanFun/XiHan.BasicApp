// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using XiHan.BasicApp.Web.Core.Upgrade;

namespace XiHan.BasicApp.Web.Core.Tests;

/// <summary>
/// 维护模式状态机测试：锁定升级引擎置位/复位的语义与并发安全终态。
/// </summary>
/// <remarks>
/// 该标志位是维护模式的唯一真相来源，一旦语义走样，要么升级期间流量照打正在迁移的库，
/// 要么升级结束后系统永久停留在 503。
/// </remarks>
public sealed class MaintenanceModeStateTests
{
    /// <summary>
    /// 新建实例必须是非维护态，否则进程一启动就对全部业务请求返回 503。
    /// </summary>
    [Fact]
    public void IsActive_NewInstanceShouldBeInactive()
    {
        var state = new MaintenanceModeState();

        Assert.False(state.IsActive);
    }

    /// <summary>
    /// Enter 后必须处于维护态，否则升级期间的业务流量不会被拦截。
    /// </summary>
    [Fact]
    public void Enter_ShouldActivate()
    {
        var state = new MaintenanceModeState();

        state.Enter();

        Assert.True(state.IsActive);
    }

    /// <summary>
    /// Exit 后必须回到非维护态，否则升级结束后无法恢复服务。
    /// </summary>
    [Fact]
    public void Exit_ShouldDeactivate()
    {
        var state = new MaintenanceModeState();
        state.Enter();

        state.Exit();

        Assert.False(state.IsActive);
    }

    /// <summary>
    /// Enter 必须幂等：当前实现用 Interlocked.Exchange 直接写 1，没有重入计数，
    /// 多次进入等价于一次进入。若日后改成计数语义，这条会红，从而强制显式决策。
    /// </summary>
    [Fact]
    public void Enter_RepeatedShouldStayActiveWithoutCounting()
    {
        var state = new MaintenanceModeState();

        state.Enter();
        state.Enter();
        state.Enter();

        Assert.True(state.IsActive);

        // 无重入计数：一次 Exit 即可完全解除，不需要与 Enter 次数配平
        state.Exit();
        Assert.False(state.IsActive);
    }

    /// <summary>
    /// 未进入维护时直接 Exit 不得抛异常：升级引擎的失败兜底路径会无条件调用 Exit。
    /// </summary>
    [Fact]
    public void Exit_WithoutEnterShouldBeNoOp()
    {
        var state = new MaintenanceModeState();

        state.Exit();
        state.Exit();

        Assert.False(state.IsActive);
    }

    /// <summary>
    /// Enter→Exit→Enter 必须可重复循环，否则第二次升级无法再进入维护模式。
    /// </summary>
    [Fact]
    public void EnterExitEnter_ShouldBeRepeatable()
    {
        var state = new MaintenanceModeState();

        state.Enter();
        Assert.True(state.IsActive);
        state.Exit();
        Assert.False(state.IsActive);
        state.Enter();
        Assert.True(state.IsActive);
        state.Exit();

        Assert.False(state.IsActive);
    }

    /// <summary>
    /// 并发 Enter 的终态必须是维护态，且不得抛出异常。
    /// </summary>
    [Fact]
    public void Enter_ConcurrentShouldEndActive()
    {
        var state = new MaintenanceModeState();

        _ = Parallel.For(0, 256, _ => state.Enter());

        Assert.True(state.IsActive);
    }

    /// <summary>
    /// 并发 Exit 的终态必须是非维护态，否则升级结束后可能残留 503。
    /// </summary>
    [Fact]
    public void Exit_ConcurrentShouldEndInactive()
    {
        var state = new MaintenanceModeState();
        state.Enter();

        _ = Parallel.For(0, 256, _ => state.Exit());

        Assert.False(state.IsActive);
    }

    /// <summary>
    /// 并发混合 Enter/Exit 不得抛出任何异常，最终以显式收尾操作决定终态。
    /// </summary>
    [Fact]
    public async Task EnterExit_ConcurrentMixShouldNotThrow()
    {
        var state = new MaintenanceModeState();
        var tasks = new List<Task>();
        for (var index = 0; index < 64; index++)
        {
            var enter = index % 2 == 0;
            tasks.Add(Task.Run(() =>
            {
                for (var round = 0; round < 100; round++)
                {
                    if (enter)
                    {
                        state.Enter();
                    }
                    else
                    {
                        state.Exit();
                    }

                    _ = state.IsActive;
                }
            }));
        }

        await Task.WhenAll(tasks);

        // 并发期间的中间态不可断言；收尾一次显式操作，终态必须确定
        state.Exit();
        Assert.False(state.IsActive);
        state.Enter();
        Assert.True(state.IsActive);
    }

    /// <summary>
    /// 状态必须是实例字段而非进程级静态字段：源码注释声明「进程内共享的一个标志位、由 DI 单例保证唯一」，
    /// 一旦退化为 static，多宿主与并行测试之间会互相串扰。
    /// </summary>
    [Fact]
    public void IsActive_TwoInstancesShouldBeIsolated()
    {
        var first = new MaintenanceModeState();
        var second = new MaintenanceModeState();

        first.Enter();

        Assert.True(first.IsActive);
        Assert.False(second.IsActive);

        second.Enter();
        first.Exit();

        Assert.False(first.IsActive);
        Assert.True(second.IsActive);
    }

    /// <summary>
    /// 结构约束：状态字段必须是实例字段，直接堵死「为方便改成 static」这条退化路径。
    /// </summary>
    [Fact]
    public void ActiveField_ShouldBeInstanceField()
    {
        var fields = typeof(MaintenanceModeState).GetFields(
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.DoesNotContain(fields, field => field.IsStatic);
        Assert.Contains(fields, field => !field.IsStatic && field.FieldType == typeof(int));
    }
}
