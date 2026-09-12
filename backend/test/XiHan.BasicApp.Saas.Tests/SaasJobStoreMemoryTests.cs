// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Linq.Expressions;
using System.Reflection;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Extensions;
using XiHan.BasicApp.Saas.Infrastructure.Auth;
using XiHan.BasicApp.Saas.Infrastructure.Tasks;
using XiHan.Framework.Authentication.Jwt;
using XiHan.Framework.Tasks.ScheduledJobs.Models;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// SaaS 任务存储与认证存储内存边界测试
/// </summary>
public class SaasJobStoreMemoryTests
{
    /// <summary>
    /// 调度实例进入终态后移除仅供运行期关联使用的映射
    /// </summary>
    [Fact]
    public async Task UpdateJobStatusAsync_WithTerminalStatus_RemovesInstanceMapping()
    {
        var task = new SysTask
        {
            TaskCode = "memory-test",
            TaskName = "memory-test"
        };
        var repository = new Mock<ITaskRepository>();
        repository
            .Setup(item => item.GetListAsync(
                It.IsAny<Expression<Func<SysTask, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([task]);
        repository
            .Setup(item => item.UpdateAsync(It.IsAny<SysTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysTask entity, CancellationToken _) => entity);

        var services = new ServiceCollection();
        services.AddSingleton(repository.Object);
        using var provider = services.BuildServiceProvider();
        var store = new SaasJobStore(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SaasJobStore>.Instance);
        var instance = new JobInstance
        {
            InstanceId = "instance-1",
            JobName = task.TaskCode,
            Status = JobStatus.Running,
            StartedAt = DateTimeOffset.UtcNow
        };

        await store.SaveJobInstanceAsync(instance);
        await store.UpdateJobStatusAsync(instance.InstanceId, JobStatus.Succeeded);

        Assert.Equal(0, GetInstanceMappingCount(store));
    }

    /// <summary>
    /// SaaS 模块使用应用层分布式缓存刷新令牌存储
    /// </summary>
    [Fact]
    public void AddSaasAuthStores_RegistersSaasRefreshTokenStore()
    {
        var services = new ServiceCollection();

        services.AddSaasAuthStores();

        var descriptor = services.Last(item => item.ServiceType == typeof(IRefreshTokenStore));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(SaasRefreshTokenStore), descriptor.ImplementationType);
    }

    /// <summary>
    /// 应用层刷新令牌存储按主体校验、隐藏令牌明文并支持移除
    /// </summary>
    [Fact]
    public void SaasRefreshTokenStore_SaveValidateAndRemove_RoundTrips()
    {
        var cache = new RecordingDistributedCache();
        var store = new SaasRefreshTokenStore(cache);

        store.Save("plain-refresh-token", "user-1", DateTime.UtcNow.AddDays(1));

        Assert.True(store.Validate("plain-refresh-token", "user-1"));
        Assert.False(store.Validate("plain-refresh-token", "user-2"));
        Assert.DoesNotContain("plain-refresh-token", Assert.Single(cache.Keys), StringComparison.Ordinal);

        store.Remove("plain-refresh-token");

        Assert.False(store.Validate("plain-refresh-token", "user-1"));
    }

    private static int GetInstanceMappingCount(SaasJobStore store)
    {
        var mappings = typeof(SaasJobStore)
            .GetField("_instanceMappings", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(store)!;
        return (int)mappings.GetType().GetProperty("Count")!.GetValue(mappings)!;
    }

    private sealed class RecordingDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, byte[]> _entries = new(StringComparer.Ordinal);

        public IReadOnlyList<string> Keys => [.. _entries.Keys];

        public byte[]? Get(string key)
        {
            return _entries.GetValueOrDefault(key);
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            return Task.FromResult(Get(key));
        }

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _entries.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _entries[key] = value;
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }
    }
}
