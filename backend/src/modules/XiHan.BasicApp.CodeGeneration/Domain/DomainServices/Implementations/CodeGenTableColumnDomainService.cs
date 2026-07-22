// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;

namespace XiHan.BasicApp.CodeGeneration.Domain.DomainServices;

/// <summary>
/// 代码生成列配置领域服务实现
/// </summary>
public sealed class CodeGenTableColumnDomainService : ICodeGenTableColumnDomainService
{
    private readonly ICodeGenTableColumnRepository _columnRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CodeGenTableColumnDomainService(ICodeGenTableColumnRepository columnRepository)
    {
        _columnRepository = columnRepository;
    }

    /// <inheritdoc />
    public async Task<CodeGenTableColumnBatchSaveResult> BatchSaveColumnsAsync(CodeGenTableColumnBatchSaveCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.TableId, "所属表主键必须大于 0。");

        if (command.Columns is null || command.Columns.Count == 0)
        {
            return new CodeGenTableColumnBatchSaveResult([]);
        }

        var updatedColumns = new List<SysCodeGenTableColumn>(command.Columns.Count);
        foreach (var columnCommand in command.Columns)
        {
            ArgumentNullException.ThrowIfNull(columnCommand);

            var column = await GetColumnOrThrowAsync(columnCommand.BasicId, cancellationToken);
            if (column.TableId != command.TableId)
            {
                throw new InvalidOperationException("批量保存的列配置必须属于同一张表。");
            }

            ApplyMutableFields(column, columnCommand);
            updatedColumns.Add(column);
        }

        var result = await _columnRepository.UpdateRangeAsync(updatedColumns, cancellationToken);
        return new CodeGenTableColumnBatchSaveResult([.. result]);
    }

    /// <inheritdoc />
    public async Task<CodeGenTableColumnCommandResult> UpdateColumnAsync(CodeGenTableColumnUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var column = await GetColumnOrThrowAsync(command.BasicId, cancellationToken);

        // dirty-tracking：应用前后 diff，把值发生变化的字段并入"已人工修改"集合，
        // 供同步表结构时冻结（保留人工值），其余字段跟随最新表结构重新推断
        var snapshot = UserModifiedFieldSet.Snapshot(column, TrackedFields);
        ApplyMutableFields(column, command);
        var changed = UserModifiedFieldSet.DiffChanged(column, snapshot);
        column.UserModifiedFields = UserModifiedFieldSet.Merge(column.UserModifiedFields, changed);

        return new CodeGenTableColumnCommandResult(await _columnRepository.UpdateAsync(column, cancellationToken));
    }

    /// <summary>
    /// 参与 dirty-tracking 的列字段（会被同步表结构重新推断覆盖的字段；结构性字段不在内，始终以 DB 为准）
    /// </summary>
    private static readonly string[] TrackedFields =
    [
        nameof(SysCodeGenTableColumn.CSharpType),
        nameof(SysCodeGenTableColumn.CSharpProperty),
        nameof(SysCodeGenTableColumn.TsType),
        nameof(SysCodeGenTableColumn.IsRequired),
        nameof(SysCodeGenTableColumn.IsList),
        nameof(SysCodeGenTableColumn.IsInsert),
        nameof(SysCodeGenTableColumn.IsEdit),
        nameof(SysCodeGenTableColumn.IsQuery),
        nameof(SysCodeGenTableColumn.QueryType),
        nameof(SysCodeGenTableColumn.HtmlType),
        nameof(SysCodeGenTableColumn.DictSelectorType),
        nameof(SysCodeGenTableColumn.DictCode),
        nameof(SysCodeGenTableColumn.EnumTypeName),
        nameof(SysCodeGenTableColumn.ConstValues),
        nameof(SysCodeGenTableColumn.Sort)
    ];

    /// <summary>
    /// 覆盖列配置的可变字段
    /// </summary>
    private static void ApplyMutableFields(SysCodeGenTableColumn column, CodeGenTableColumnUpdateCommand command)
    {
        EnsureEnum(command.QueryType, "查询方式无效。");
        EnsureEnum(command.HtmlType, "表单显示类型无效。");
        EnsureEnum(command.Status, "状态无效。");

        column.CSharpType = NormalizeNullable(command.CSharpType, 100, "C# 类型最长 100 个字符。");
        column.CSharpProperty = NormalizeNullable(command.CSharpProperty, 200, "C# 属性名最长 200 个字符。");
        column.TsType = NormalizeNullable(command.TsType, 100, "TypeScript 类型最长 100 个字符。");
        column.IsRequired = command.IsRequired;
        column.IsList = command.IsList;
        column.IsInsert = command.IsInsert;
        column.IsEdit = command.IsEdit;
        column.IsQuery = command.IsQuery;
        column.QueryType = command.QueryType;
        column.HtmlType = command.HtmlType;
        ApplyDictSelector(column, command);
        column.DefaultValue = NormalizeNullable(command.DefaultValue, 500, "默认值最长 500 个字符。");
        column.RegexPattern = NormalizeNullable(command.RegexPattern, 500, "正则表达式最长 500 个字符。");
        column.ValidationMessage = NormalizeNullable(command.ValidationMessage, 500, "验证提示信息最长 500 个字符。");
        column.Sort = command.Sort;
        column.Status = command.Status;
    }

    /// <summary>
    /// 字典三分互斥落库：按 DictSelectorType 校验并只保留生效字段，其余清空。
    /// 关联不入生成代码（仅作表单选项来源），故此处只校验值合法性，不做跨表/外键处理。
    /// </summary>
    private static void ApplyDictSelector(SysCodeGenTableColumn column, CodeGenTableColumnUpdateCommand command)
    {
        if (command.DictSelectorType is not { } selectorType)
        {
            column.DictSelectorType = null;
            column.DictCode = null;
            column.EnumTypeName = null;
            column.ConstValues = null;
            return;
        }

        EnsureEnum(selectorType, "字典选择器类型无效。");
        var dictCode = NormalizeNullable(command.DictCode, 200, "字典码最长 200 个字符。");
        var enumTypeName = NormalizeNullable(command.EnumTypeName, 500, "枚举类型全名最长 500 个字符。");
        var constValues = NormalizeConstValues(command.ConstValues);

        switch (selectorType)
        {
            case DictSelectorType.DictSelector:
                column.DictCode = dictCode ?? throw new InvalidOperationException("系统字典选择器必须填写字典码。");
                column.EnumTypeName = null;
                column.ConstValues = null;
                break;

            case DictSelectorType.EnumSelector:
                column.EnumTypeName = enumTypeName ?? throw new InvalidOperationException("枚举选择器必须填写枚举类型全名。");
                column.DictCode = null;
                column.ConstValues = null;
                break;

            case DictSelectorType.ConstSelector:
                column.ConstValues = constValues ?? throw new InvalidOperationException("常量选择器必须填写常量项 JSON。");
                column.DictCode = null;
                column.EnumTypeName = null;
                break;

            default:
                throw new InvalidOperationException("字典选择器类型无效。");
        }

        column.DictSelectorType = selectorType;
    }

    /// <summary>
    /// 常量项 JSON 规整：空白归 null，非法 JSON 抛异常
    /// </summary>
    private static string? NormalizeConstValues(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        try
        {
            using (JsonDocument.Parse(trimmed))
            {
            }
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("常量项必须是合法 JSON。");
        }

        return trimmed;
    }

    /// <summary>
    /// 加载列配置，不存在则抛出异常
    /// </summary>
    private async Task<SysCodeGenTableColumn> GetColumnOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "列配置主键必须大于 0。");
        return await _columnRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("列配置不存在。");
    }

    /// <summary>
    /// 校验主键大于 0
    /// </summary>
    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentException(message, nameof(id));
        }
    }

    /// <summary>
    /// 校验枚举值有效
    /// </summary>
    private static void EnsureEnum<TEnum>(TEnum value, string message)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentException(message);
        }
    }

    /// <summary>
    /// 规整可空字符串：空白归 null，否则去空格并做长度校验
    /// </summary>
    private static string? NormalizeNullable(string? value, int maxLength, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException(message);
        }

        return trimmed;
    }
}
