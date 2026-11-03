// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;
using XiHan.Framework.Kernel.Hosting;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证宿主的配置文件不盖掉部署环境挂进来的绑定
/// </summary>
/// <remarks>
/// 框架的 <c>BindingResolver</c> 先读 <c>SERVICE_BINDING_ROOT</c> 目录、再读配置节，同名绑定整体由配置节覆盖：
/// <c>appsettings.json</c> 里写了值为空的 <c>db</c>、<c>cache</c>、<c>queue</c>，就会把 Kubernetes 经 Service Binding 挂进来的 Secret 整个盖掉
/// </remarks>
public sealed class HostBindingsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "xihan-basicapp-bindings-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void AppSettings_LeavesTheMountedBindingsInPlace()
    {
        (string Name, string Type, string Uri)[] mounted =
        [
            ("db", "connection:relational", "Host=db.internal;Port=5432;Database=basicapp"),
            ("cache", "cache:distributed", "redis.internal:6379"),
            ("queue", "queue", "amqp://mq.internal:5672"),
        ];
        foreach (var (name, type, uri) in mounted)
        {
            var directory = Directory.CreateDirectory(Path.Combine(_root, name));
            File.WriteAllText(Path.Combine(directory.FullName, "type"), type);
            File.WriteAllText(Path.Combine(directory.FullName, "uri"), uri);
        }

        var configuration = new ConfigurationBuilder().AddJsonFile(Path.Combine(HostDirectory(), "appsettings.json"), optional: false).Build();
        var bindings = BindingResolver.Resolve(configuration, _root);
        foreach (var (name, type, uri) in mounted)
        {
            Assert.Equal((type, uri), (bindings[name].Type, bindings[name].Value));
        }
    }

    private static string HostDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "XiHan.BasicApp.slnx")))
            {
                return Path.Combine(directory.FullName, "src", "XiHan.BasicApp.Host");
            }
        }

        throw new InvalidOperationException("XiHan.BasicApp.slnx not found above the test output directory.");
    }
}
