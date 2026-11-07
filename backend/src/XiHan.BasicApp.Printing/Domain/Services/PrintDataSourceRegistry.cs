// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Contracts.Printing;

namespace XiHan.BasicApp.Printing.Domain.Services;

/// <summary>
/// 打印数据源的注册表
/// </summary>
/// <remarks>
/// 单例：构造时收下各包经 <see cref="PrintDataSourceServiceCollectionExtensions.AddPrintDataSource"/> 投进来的全部登记并逐个校验，
/// 编码重复或字段契约不合法即抛出（Feature 把构造登记为启动核对，应用在启动阶段就失败）
/// 规则：编码不超过 100 个字符、不含空白；样例是根为对象或数组的 JSON；至少一个字段、键不重复；
/// 字段种类只有 text / image / barcode / qrcode / table，样例控件只有 boolean / date / datetime / number / text / textarea；明细表至少一列、列不重复，别的种类不能带列
/// 设计器经 <c>print-data-sources.list</c> 读目录，模板写入时据此校验数据源编码；解析时不查（前端打印时数据源没登记即报错）
/// </remarks>
public sealed class PrintDataSourceRegistry
{
    /// <summary>
    /// 编码与字段键的最大长度
    /// </summary>
    public const int MaxCodeLength = 100;

    private static readonly HashSet<string> _kinds = new(StringComparer.Ordinal)
    {
        PrintFieldKinds.Text, PrintFieldKinds.Image, PrintFieldKinds.Barcode, PrintFieldKinds.QrCode, PrintFieldKinds.Table,
    };

    private static readonly HashSet<string> _inputTypes = new(StringComparer.Ordinal)
    {
        "boolean", "date", "datetime", "number", "text", "textarea",
    };

    private readonly SortedDictionary<string, PrintDataSourceDefinition> _sources = new(StringComparer.Ordinal);

    /// <summary>
    /// 收下全部登记并校验
    /// </summary>
    /// <param name="registrations">各包的登记</param>
    /// <exception cref="ArgumentException">编码、样例或字段契约不合法</exception>
    /// <exception cref="InvalidOperationException">编码重复（后登记的不能静默覆盖已有的字段契约）</exception>
    public PrintDataSourceRegistry(IEnumerable<PrintDataSourceRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        foreach (var registration in registrations)
        {
            Register(registration.Definition);
        }
    }

    /// <summary>
    /// 全部数据源，按编码排序
    /// </summary>
    public IReadOnlyList<PrintDataSourceDefinition> All => [.. _sources.Values];

    /// <summary>
    /// 按编码查找数据源
    /// </summary>
    /// <param name="code">数据源编码</param>
    /// <returns>没登记为 <see langword="null"/></returns>
    public PrintDataSourceDefinition? Find(string? code)
        => string.IsNullOrWhiteSpace(code) ? null : _sources.GetValueOrDefault(code.Trim());

    /// <summary>
    /// 数据源是否已登记
    /// </summary>
    /// <param name="code">数据源编码</param>
    public bool IsRegistered(string? code) => Find(code) is not null;

    private void Register(PrintDataSourceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.Code);
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.Name);
        if (definition.Code.Length > MaxCodeLength || definition.Code.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException($"Print data source code '{definition.Code}' must be at most {MaxCodeLength} characters without whitespace.", nameof(definition));
        }

        ValidateSample(definition);
        if (definition.Fields is not { Count: > 0 })
        {
            throw new ArgumentException($"Print data source '{definition.Code}' needs at least one field.", nameof(definition));
        }

        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in definition.Fields)
        {
            ValidateField(definition.Code, field, keys);
        }

        if (!_sources.TryAdd(definition.Code, definition))
        {
            throw new InvalidOperationException($"Print data source '{definition.Code}' is registered more than once.");
        }
    }

    private static void ValidateSample(PrintDataSourceDefinition definition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.SampleDataJson);
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(definition.SampleDataJson);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException($"The sample data of print data source '{definition.Code}' is not valid JSON.", nameof(definition), exception);
        }

        using (document)
        {
            if (document.RootElement.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
            {
                throw new ArgumentException($"The sample data of print data source '{definition.Code}' must be a JSON object or array.", nameof(definition));
            }
        }
    }

    private static void ValidateField(string source, PrintDataSourceField field, HashSet<string> keys)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentException.ThrowIfNullOrWhiteSpace(field.Key, nameof(field));
        ArgumentException.ThrowIfNullOrWhiteSpace(field.Label, nameof(field));
        if (field.Key.Length > MaxCodeLength || field.Key.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException($"Print field key '{field.Key}' of '{source}' must be at most {MaxCodeLength} characters without whitespace.", nameof(field));
        }

        if (!_kinds.Contains(field.Kind))
        {
            throw new ArgumentException($"Print field '{field.Key}' of '{source}' has an unknown kind '{field.Kind}'.", nameof(field));
        }

        if (!keys.Add(field.Key))
        {
            throw new ArgumentException($"Print data source '{source}' declares field '{field.Key}' more than once.", nameof(field));
        }

        ValidateInputType(field.InputType, $"print field '{field.Key}' of '{source}'");
        if (!string.Equals(field.Kind, PrintFieldKinds.Table, StringComparison.Ordinal))
        {
            // 列只对明细表有意义：别的种类带列是写错了种类或多写了列，在设计器与解析里都是死数据，登记时就拒绝
            if (field.Columns is { Count: > 0 })
            {
                throw new ArgumentException($"Print field '{field.Key}' of '{source}' is '{field.Kind}' and cannot declare table columns.", nameof(field));
            }

            return;
        }

        if (field.Columns is not { Count: > 0 })
        {
            throw new ArgumentException($"Table field '{field.Key}' of '{source}' needs at least one column.", nameof(field));
        }

        var columns = new HashSet<string>(StringComparer.Ordinal);
        foreach (var column in field.Columns)
        {
            ArgumentNullException.ThrowIfNull(column);
            ArgumentException.ThrowIfNullOrWhiteSpace(column.Field, nameof(field));
            ArgumentException.ThrowIfNullOrWhiteSpace(column.Title, nameof(field));
            if (column.Field.Any(char.IsWhiteSpace))
            {
                throw new ArgumentException($"Column '{column.Field}' of table '{field.Key}' in '{source}' must not contain whitespace.", nameof(field));
            }

            if (!columns.Add(column.Field))
            {
                throw new ArgumentException($"Table '{field.Key}' of '{source}' declares column '{column.Field}' more than once.", nameof(field));
            }

            ValidateInputType(column.InputType, $"column '{column.Field}' of table '{field.Key}' in '{source}'");
        }
    }

    private static void ValidateInputType(string? inputType, string label)
    {
        if (inputType is not null && !_inputTypes.Contains(inputType))
        {
            throw new ArgumentException($"The sample input type '{inputType}' of {label} is not supported.", nameof(inputType));
        }
    }
}
