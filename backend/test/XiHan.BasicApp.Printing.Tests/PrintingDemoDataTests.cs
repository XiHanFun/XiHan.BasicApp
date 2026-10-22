// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Printing.Domain.DataSources;
using XiHan.BasicApp.Printing.Extensions;
using XiHan.BasicApp.Printing.Infrastructure.Seeders;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;

namespace XiHan.BasicApp.Printing.Tests;

/// <summary>
/// 打印演示数据：演示数据源只在演示开关打开时注册，演示模板与数据源一一对得上，种子排在演示阶段。
/// </summary>
public sealed class PrintingDemoDataTests
{
    /// <summary>
    /// 演示开关关着：只登记模板种子（种子自己按开关跳过），五个演示数据源都不注册。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    public void AddPrintingDemoData_SwitchOff_ShouldNotRegisterDemoDataSources(string? value)
    {
        var services = Build(value);

        Assert.Empty(DemoRegistrations(services));
        Assert.Contains(services, descriptor => descriptor.ImplementationType == typeof(PrintingDemoTemplateSeeder));
    }

    /// <summary>
    /// 演示开关打开：五个演示数据源与内置示例一起被注册表收纳，字段契约与样例数据都合法。
    /// </summary>
    [Fact]
    public void AddPrintingDemoData_SwitchOn_ShouldRegisterAllDemoDataSources()
    {
        var services = Build("true");

        Assert.Equal(
            PrintingDemoCatalog.DataSourceCodes.Order(StringComparer.Ordinal),
            DemoRegistrations(services).Select(registration => registration.Definition.Code).Order(StringComparer.Ordinal));
        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IPrintDataSourceRegistry>();
        Assert.All(PrintingDemoCatalog.DataSourceCodes, code => Assert.True(registry.IsRegistered(code), $"注册表里没有 {code}"));
        Assert.True(registry.IsRegistered(BuiltInPrintDataSources.SystemPrintDemo.Code));
    }

    /// <summary>
    /// 每张演示模板：编码唯一且合法，所绑数据源是演示数据源或内置示例，模板 JSON 过得了服务端的结构检查。
    /// </summary>
    [Fact]
    public void Templates_ShouldBindKnownDataSourcesAndPassServerChecks()
    {
        var knownSources = PrintingDemoCatalog.DataSourceCodes.Append(BuiltInPrintDataSources.SystemPrintDemo.Code).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(PrintingDemoCatalog.Templates.Count, PrintingDemoCatalog.Templates.Select(template => template.Code).Distinct(StringComparer.Ordinal).Count());
        Assert.All(PrintingDemoCatalog.Templates, template =>
        {
            Assert.True(template.Code.Length <= 100 && !template.Code.Any(char.IsWhiteSpace), template.Code);
            Assert.Contains(template.DataSourceCode, knownSources);

            using var document = JsonDocument.Parse(PrintingDemoCatalog.LoadTemplateJson(template.Code));
            var root = document.RootElement;
            Assert.Equal(JsonValueKind.Object, root.ValueKind);
            Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
            var pages = root.GetProperty("pages");
            Assert.True(pages.GetArrayLength() > 0, $"{template.Code} 没有页面");
            Assert.All(pages.EnumerateArray(), page => Assert.Equal(JsonValueKind.Object, page.ValueKind));
        });
    }

    /// <summary>
    /// 五个演示数据源都有模板用到，没有闲置的。
    /// </summary>
    [Fact]
    public void DataSources_ShouldEachBeUsedByATemplate()
    {
        Assert.All(PrintingDemoCatalog.DataSourceCodes, code =>
            Assert.Contains(PrintingDemoCatalog.Templates, template => template.DataSourceCode == code));
    }

    /// <summary>
    /// 模板种子排在演示阶段打印的号段，名称带模块前缀。
    /// </summary>
    [Fact]
    public void TemplateSeeder_ShouldRunInTheDemoPhase()
    {
        var seeder = (PrintingDemoTemplateSeeder)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(PrintingDemoTemplateSeeder));

        Assert.Equal(SeedOrders.Demo + 50, seeder.Order);
        Assert.StartsWith("[Printing]", seeder.Name, StringComparison.Ordinal);
    }

    private static ServiceCollection Build(string? demoSwitch)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [DemoDataSeederBase.EnableDemoDataKey] = demoSwitch })
            .Build();
        var services = new ServiceCollection();
        services.AddPrintingDataSources();
        services.AddPrintingDemoData(configuration);
        return services;
    }

    private static IEnumerable<PrintDataSourceRegistration> DemoRegistrations(IServiceCollection services)
    {
        return services
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<PrintDataSourceRegistration>()
            .Where(registration => registration.Definition.Code.StartsWith("demo.", StringComparison.Ordinal));
    }
}
