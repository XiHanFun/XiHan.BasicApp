// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Text.Json;

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

    private static readonly HashSet<string> SupportedInputTypes = new(StringComparer.Ordinal)
    {
        "boolean", "date", "datetime", "number", "text", "textarea"
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

    /// <summary>
    /// 注册数据源定义；重复编码抛出异常（后注册模块不得静默覆盖既有字段契约）。
    /// </summary>
    /// <param name="definition">数据源定义</param>
    public void Register(PrintDataSourceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.Code);
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.Name);
        if (definition.Code.Length > 100 || definition.Code.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException($"打印数据源编码无效：{definition.Code}", nameof(definition));
        }

        ValidateSampleDataJson(definition);

        if (definition.Fields.Count == 0)
        {
            throw new ArgumentException($"打印数据源 {definition.Code} 至少需要一个字段。", nameof(definition));
        }

        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in definition.Fields)
        {
            ValidateField(definition.Code, field, keys);
        }

        if (!_sources.TryAdd(definition.Code, definition))
        {
            throw new InvalidOperationException($"打印数据源编码已注册：{definition.Code}");
        }
    }

    /// <summary>校验样例数据是合法 JSON 且根节点为对象或数组。</summary>
    private static void ValidateSampleDataJson(PrintDataSourceDefinition definition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.SampleDataJson);
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(definition.SampleDataJson);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException($"打印数据源 {definition.Code} 的样例数据不是合法 JSON。", nameof(definition), exception);
        }

        using (document)
        {
            if (document.RootElement.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
            {
                throw new ArgumentException($"打印数据源 {definition.Code} 的样例数据根节点必须是对象或数组。", nameof(definition));
            }
        }
    }

    /// <summary>校验单个字段的编码、类型、控件类型与明细表列契约。</summary>
    private static void ValidateField(string dataSourceCode, PrintDataSourceField field, HashSet<string> keys)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field.Key);
        ArgumentException.ThrowIfNullOrWhiteSpace(field.Label);
        if (field.Key.Length > 100 || field.Key.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException($"打印字段编码无效：{field.Key}", nameof(field));
        }

        if (!SupportedKinds.Contains(field.Kind))
        {
            throw new ArgumentException($"打印字段 {field.Key} 的类型无效：{field.Kind}", nameof(field));
        }

        if (!keys.Add(field.Key))
        {
            throw new ArgumentException($"打印数据源 {dataSourceCode} 存在重复字段：{field.Key}", nameof(field));
        }

        ValidateInputType(field.InputType, $"打印字段 {field.Key}");

        if (field.Kind != "table")
        {
            return;
        }

        if (field.Columns is null || field.Columns.Count == 0)
        {
            throw new ArgumentException($"明细表字段 {field.Key} 至少需要一列。", nameof(field));
        }

        var columnFields = new HashSet<string>(StringComparer.Ordinal);
        foreach (var column in field.Columns)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(column.Field);
            ArgumentException.ThrowIfNullOrWhiteSpace(column.Title);
            if (column.Field.Any(char.IsWhiteSpace))
            {
                throw new ArgumentException($"明细表 {field.Key} 的列字段无效：{column.Field}", nameof(field));
            }

            if (!columnFields.Add(column.Field))
            {
                throw new ArgumentException($"明细表 {field.Key} 存在重复列字段：{column.Field}", nameof(field));
            }

            ValidateInputType(column.InputType, $"明细表 {field.Key} 列 {column.Field}");
        }
    }

    /// <summary>校验可选样例控件类型在支持范围内。</summary>
    private static void ValidateInputType(string? inputType, string label)
    {
        if (inputType is not null && !SupportedInputTypes.Contains(inputType))
        {
            throw new ArgumentException($"{label} 的样例控件类型无效：{inputType}", nameof(inputType));
        }
    }

    /// <summary>
    /// 按编码查找数据源。
    /// </summary>
    /// <param name="code">数据源编码</param>
    /// <returns>数据源定义；未注册返回 null</returns>
    public PrintDataSourceDefinition? Find(string code)
    {
        return string.IsNullOrWhiteSpace(code) ? null : _sources.GetValueOrDefault(code.Trim());
    }

    /// <summary>
    /// 数据源是否已注册。
    /// </summary>
    /// <param name="code">数据源编码</param>
    public bool IsRegistered(string code)
    {
        return Find(code) is not null;
    }

    /// <summary>
    /// 全部已注册数据源（按编码排序）。
    /// </summary>
    public IReadOnlyList<PrintDataSourceDefinition> GetAll()
    {
        return [.. _sources.Values.OrderBy(s => s.Code, StringComparer.Ordinal)];
    }
}
