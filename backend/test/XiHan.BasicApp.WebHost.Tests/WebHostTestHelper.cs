// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.VectorData;
using System.Runtime.CompilerServices;

namespace XiHan.BasicApp.WebHost.Tests;

/// <summary>
/// 宿主测试共用的路径锚点。
/// </summary>
/// <remarks>
/// 本项目所有涉及文件系统的断言都以本文件所在位置为锚点向上回溯到被测源码目录，
/// 不依赖运行目录、不依赖绝对路径，保证测试可离线、并行、乱序执行。
/// </remarks>
internal static class WebHostTestHelper
{
    /// <summary>
    /// 定位被测 WebHost 源码工程根目录。
    /// </summary>
    /// <remarks>
    /// <c>backend/test/XiHan.BasicApp.WebHost.Tests/</c> 向上回溯到 <c>backend/src/main/XiHan.BasicApp.WebHost/</c>。
    /// </remarks>
    /// <param name="testFilePath">编译期注入的本文件绝对路径。</param>
    /// <returns>WebHost 工程根目录绝对路径。</returns>
    public static string ResolveWebHostProjectRoot([CallerFilePath] string testFilePath = "")
    {
        var testDirectory = Path.GetDirectoryName(testFilePath)
            ?? throw new InvalidOperationException("无法解析测试源文件目录。");

        return Path.GetFullPath(Path.Combine(
            testDirectory, "..", "..", "src", "main", "XiHan.BasicApp.WebHost"));
    }
}

/// <summary>
/// 可编排的向量库测试替身：控制集合名枚举的产出、抛出与挂起，并记录枚举器被推进的次数。
/// </summary>
/// <remarks>
/// <see cref="VectorStore"/> 的 ListCollectionNamesAsync 返回 <see cref="IAsyncEnumerable{T}"/>，
/// 用 Moq 很难编排「产出一个后停止」「推进时抛」「永久挂起」这三种形态，也拿不到推进次数，
/// 因此改用手写替身。<see cref="MoveNextCount"/> 用来锁定健康检查只推进一次就 break 的约定。
/// </remarks>
internal sealed class StubVectorStore : VectorStore
{
    private readonly string[] _names;
    private readonly Exception? _throwOnEnumerate;
    private readonly bool _hangForever;

    private StubVectorStore(string[] names, Exception? throwOnEnumerate, bool hangForever)
    {
        _names = names;
        _throwOnEnumerate = throwOnEnumerate;
        _hangForever = hangForever;
    }

    /// <summary>
    /// 枚举器被推进（产出一个元素或进入挂起、抛出分支）的次数。
    /// </summary>
    public int MoveNextCount { get; private set; }

    /// <summary>
    /// 构造一个会按序产出给定集合名的替身。
    /// </summary>
    /// <param name="names">集合名序列。</param>
    /// <returns>向量库替身。</returns>
    public static StubVectorStore WithNames(params string[] names) => new(names, null, false);

    /// <summary>
    /// 构造一个集合列表为空的替身（服务可达但一个集合都没建）。
    /// </summary>
    /// <returns>向量库替身。</returns>
    public static StubVectorStore Empty() => new([], null, false);

    /// <summary>
    /// 构造一个在枚举时抛出指定异常的替身。
    /// </summary>
    /// <param name="exception">枚举时抛出的异常。</param>
    /// <returns>向量库替身。</returns>
    public static StubVectorStore Throwing(Exception exception) => new([], exception, false);

    /// <summary>
    /// 构造一个枚举时永久挂起（直到令牌取消）的替身，模拟目标主机不可路由。
    /// </summary>
    /// <returns>向量库替身。</returns>
    public static StubVectorStore Hanging() => new([], null, true);

    /// <summary>
    /// 列出集合名。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>集合名异步序列。</returns>
    public override async IAsyncEnumerable<string> ListCollectionNamesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_throwOnEnumerate is not null)
        {
            MoveNextCount++;
            await Task.Yield();
            throw _throwOnEnumerate;
        }

        if (_hangForever)
        {
            MoveNextCount++;
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            yield break;
        }

        foreach (var name in _names)
        {
            MoveNextCount++;
            yield return name;
        }
    }

    /// <summary>
    /// 健康检查用不到，调用即视为测试写错。
    /// </summary>
    /// <param name="name">集合名。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>不返回。</returns>
    public override Task<bool> CollectionExistsAsync(string name, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("健康检查不应触碰具体集合。");

    /// <summary>
    /// 健康检查用不到，调用即视为测试写错。
    /// </summary>
    /// <param name="name">集合名。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>不返回。</returns>
    public override Task EnsureCollectionDeletedAsync(string name, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("健康检查不应删除集合。");

    /// <summary>
    /// 健康检查用不到，调用即视为测试写错。
    /// </summary>
    /// <typeparam name="TKey">主键类型。</typeparam>
    /// <typeparam name="TRecord">记录类型。</typeparam>
    /// <param name="name">集合名。</param>
    /// <param name="definition">集合定义。</param>
    /// <returns>不返回。</returns>
    public override VectorStoreCollection<TKey, TRecord> GetCollection<TKey, TRecord>(
        string name, VectorStoreCollectionDefinition? definition = null)
        => throw new NotSupportedException("健康检查不应获取集合。");

    /// <summary>
    /// 健康检查用不到，调用即视为测试写错。
    /// </summary>
    /// <param name="name">集合名。</param>
    /// <param name="definition">集合定义。</param>
    /// <returns>不返回。</returns>
    public override VectorStoreCollection<object, Dictionary<string, object?>> GetDynamicCollection(
        string name, VectorStoreCollectionDefinition definition)
        => throw new NotSupportedException("健康检查不应获取动态集合。");

    /// <summary>
    /// 元数据探测：替身不提供任何附加服务。
    /// </summary>
    /// <param name="serviceType">服务类型。</param>
    /// <param name="serviceKey">服务键。</param>
    /// <returns>恒为 null。</returns>
    public override object? GetService(Type serviceType, object? serviceKey = null) => null;
}

/// <summary>
/// 只记录中间件注册次数的 <see cref="IApplicationBuilder"/> 假件。
/// </summary>
/// <remarks>
/// 用于验证模块生命周期钩子究竟往管线里塞了几个中间件；
/// Build 刻意返回空委托，从而不会真正实例化任何中间件、不触发外部依赖。
/// </remarks>
internal class RecordingApplicationBuilder : IApplicationBuilder
{
    /// <summary>
    /// 构造函数。
    /// </summary>
    /// <param name="applicationServices">应用级服务提供者。</param>
    public RecordingApplicationBuilder(IServiceProvider applicationServices)
    {
        ApplicationServices = applicationServices;
    }

    /// <summary>
    /// 应用级服务提供者。
    /// </summary>
    public IServiceProvider ApplicationServices { get; set; }

    /// <summary>
    /// 服务器特性集合（测试中不使用）。
    /// </summary>
    public IFeatureCollection ServerFeatures { get; } = new FeatureCollection();

    /// <summary>
    /// 构建器属性字典。
    /// </summary>
    public IDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>
    /// Use 被调用的次数，即注册进管线的中间件数量。
    /// </summary>
    public int UseCount { get; private set; }

    /// <summary>
    /// 构建请求管线（返回空委托，测试不发真实请求）。
    /// </summary>
    /// <returns>空请求委托。</returns>
    public RequestDelegate Build() => _ => Task.CompletedTask;

    /// <summary>
    /// 创建同类构建器。
    /// </summary>
    /// <returns>新的记录型构建器。</returns>
    public IApplicationBuilder New() => new RecordingApplicationBuilder(ApplicationServices);

    /// <summary>
    /// 注册一个中间件并计数。
    /// </summary>
    /// <param name="middleware">中间件委托。</param>
    /// <returns>自身。</returns>
    public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
    {
        UseCount++;
        return this;
    }
}

/// <summary>
/// 同时实现 <see cref="IApplicationBuilder"/> 与 <see cref="IEndpointRouteBuilder"/> 的假件。
/// </summary>
/// <remarks>
/// 宿主模块的 OnApplicationInitialization 会按构建器是否实现 <see cref="IEndpointRouteBuilder"/>
/// 走端点分支或中间件兜底分支，本假件用于驱动端点分支并读回注册出来的端点。
/// </remarks>
internal sealed class RecordingEndpointApplicationBuilder : RecordingApplicationBuilder, IEndpointRouteBuilder
{
    /// <summary>
    /// 构造函数。
    /// </summary>
    /// <param name="applicationServices">应用级服务提供者。</param>
    public RecordingEndpointApplicationBuilder(IServiceProvider applicationServices)
        : base(applicationServices)
    {
    }

    /// <summary>
    /// 端点数据源集合。
    /// </summary>
    public ICollection<EndpointDataSource> DataSources { get; } = [];

    /// <summary>
    /// 端点路由构建器使用的服务提供者。
    /// </summary>
    public IServiceProvider ServiceProvider => ApplicationServices;

    /// <summary>
    /// 为端点创建子管线构建器。
    /// </summary>
    /// <returns>记录型构建器。</returns>
    public IApplicationBuilder CreateApplicationBuilder() => new RecordingApplicationBuilder(ApplicationServices);
}

/// <summary>
/// 按调用次序返回不同解析结果的服务提供者假件。
/// </summary>
/// <remarks>
/// 用于验证健康检查每次执行都重新解析可选依赖，而不是把首次解析结果缓存进字段。
/// </remarks>
internal sealed class SequencedServiceProvider : IServiceProvider
{
    private readonly Type _serviceType;
    private readonly object?[] _results;

    /// <summary>
    /// 构造函数。
    /// </summary>
    /// <param name="serviceType">被编排的服务类型。</param>
    /// <param name="results">按调用次序返回的结果，越界后保持返回最后一个。</param>
    public SequencedServiceProvider(Type serviceType, params object?[] results)
    {
        _serviceType = serviceType;
        _results = results;
    }

    /// <summary>
    /// 该类型被解析的次数。
    /// </summary>
    public int ResolveCount { get; private set; }

    /// <summary>
    /// 解析服务。
    /// </summary>
    /// <param name="serviceType">服务类型。</param>
    /// <returns>按编排次序返回的实例，未编排的类型一律返回 null。</returns>
    public object? GetService(Type serviceType)
    {
        if (serviceType != _serviceType)
        {
            return null;
        }

        var result = _results[Math.Min(ResolveCount, _results.Length - 1)];
        ResolveCount++;
        return result;
    }
}
