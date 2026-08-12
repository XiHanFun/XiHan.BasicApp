// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;

namespace XiHan.BasicApp.Printing.Domain.DataSources;

/// <summary>
/// <see cref="IPrintDataSourceRegistry"/> 的进程内实现；注册为单例，注册动作发生在应用启动阶段。
/// </summary>
public sealed class PrintDataSourceRegistry : IPrintDataSourceRegistry
{
    private static readonly HashSet<string> SupportedKinds = new(StringComparer.Ordinal)
    {
        "text", "image", "barcode", "qrcode", "table"
    };

    private readonly ConcurrentDictionary<string, PrintDataSourceDefinition> _sources = new(StringComparer.Ordinal);

    /// <summary>
    /// 构造注册表并收纳全部 DI 登记项（重复编码或非法字段契约在此抛出，应用启动即失败）。
    /// </summary>
    /// <param name="registrations">各模块投递的数据源登记项</param>
    public PrintDataSourceRegistry(IEnumerable<PrintDataSourceRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            Register(registration.Definition);
        }
    }

    /// <inheritdoc />
    public void Register(PrintDataSourceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.Code);
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.SampleDataJson);
        if (definition.Code.Length > 100 || definition.Code.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException($"打印数据源编码无效：{definition.Code}", nameof(definition));
        }

        if (definition.Fields.Count == 0)
        {
            throw new ArgumentException($"打印数据源 {definition.Code} 至少需要一个字段。", nameof(definition));
        }

        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in definition.Fields)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(field.Key);
            ArgumentException.ThrowIfNullOrWhiteSpace(field.Label);
            if (!SupportedKinds.Contains(field.Kind))
            {
                throw new ArgumentException($"打印字段 {field.Key} 的类型无效：{field.Kind}", nameof(definition));
            }

            if (!keys.Add(field.Key))
            {
                throw new ArgumentException($"打印数据源 {definition.Code} 存在重复字段：{field.Key}", nameof(definition));
            }

            if (field.Kind == "table" && (field.Columns is null || field.Columns.Count == 0))
            {
                throw new ArgumentException($"明细表字段 {field.Key} 至少需要一列。", nameof(definition));
            }
        }

        if (!_sources.TryAdd(definition.Code, definition))
        {
            throw new InvalidOperationException($"打印数据源编码已注册：{definition.Code}");
        }
    }

    /// <inheritdoc />
    public PrintDataSourceDefinition? Find(string code)
    {
        return string.IsNullOrWhiteSpace(code) ? null : _sources.GetValueOrDefault(code.Trim());
    }

    /// <inheritdoc />
    public bool IsRegistered(string code)
    {
        return Find(code) is not null;
    }

    /// <inheritdoc />
    public IReadOnlyList<PrintDataSourceDefinition> GetAll()
    {
        return [.. _sources.Values.OrderBy(s => s.Code, StringComparer.Ordinal)];
    }
}
