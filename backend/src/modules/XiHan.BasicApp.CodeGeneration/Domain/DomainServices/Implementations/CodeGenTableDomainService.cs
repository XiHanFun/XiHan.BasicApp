// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;

namespace XiHan.BasicApp.CodeGeneration.Domain.DomainServices;

/// <summary>
/// 代码生成表配置领域服务实现
/// </summary>
public sealed class CodeGenTableDomainService : ICodeGenTableDomainService
{
    private readonly ICodeGenTableRepository _tableRepository;

    private readonly ICodeGenTableColumnRepository _columnRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CodeGenTableDomainService(
        ICodeGenTableRepository tableRepository,
        ICodeGenTableColumnRepository columnRepository)
    {
        _tableRepository = tableRepository;
        _columnRepository = columnRepository;
    }

    /// <summary>
    /// 更新表配置（表名唯一，排除自身；load-then-mutate）
    /// </summary>
    public async Task<CodeGenTableCommandResult> UpdateTableAsync(CodeGenTableUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "表配置主键必须大于 0。");
        ValidateEnum(command.TemplateType, nameof(command.TemplateType));
        EnsureTableTemplateType(command.TemplateType);
        ValidateEnum(command.GenType, nameof(command.GenType));
        ValidateEnum(command.GenerationScope, nameof(command.GenerationScope));
        ValidateEnum(command.DatabaseType, nameof(command.DatabaseType));
        ValidateEnum(command.Status, nameof(command.Status));

        var tableName = Required(command.TableName, 200, nameof(command.TableName), "数据库表名不能为空。", "数据库表名长度不能超过 200 个字符。");
        var className = Required(command.ClassName, 200, nameof(command.ClassName), "实体类名称不能为空。", "实体类名称长度不能超过 200 个字符。");

        // 表名唯一（排除自身）
        if (await _tableRepository.ExistsTableNameAsync(tableName, command.BasicId, cancellationToken))
        {
            throw new InvalidOperationException("数据库表名已配置。");
        }

        var table = await GetTableOrThrowAsync(command.BasicId, cancellationToken);

        // dirty-tracking：应用前快照，应用后 diff，把变化的字段并入"已人工修改"集合
        var snapshot = UserModifiedFieldSet.Snapshot(table, TrackedTableFields);

        table.TableName = tableName;
        table.TableComment = Optional(command.TableComment, 500, nameof(command.TableComment), "表描述长度不能超过 500 个字符。");
        table.ClassName = className;
        table.Namespace = Optional(command.Namespace, 500, nameof(command.Namespace), "命名空间长度不能超过 500 个字符。");
        table.ModuleName = Optional(command.ModuleName, 100, nameof(command.ModuleName), "模块名称长度不能超过 100 个字符。");
        table.BusinessName = Optional(command.BusinessName, 100, nameof(command.BusinessName), "业务名称长度不能超过 100 个字符。");
        table.FunctionName = Optional(command.FunctionName, 100, nameof(command.FunctionName), "功能名称长度不能超过 100 个字符。");
        table.Author = Optional(command.Author, 100, nameof(command.Author), "作者长度不能超过 100 个字符。");
        table.TemplateType = command.TemplateType;
        table.GenType = command.GenType;
        table.GenerationScope = command.GenerationScope;
        table.EnabledActions = Optional(command.EnabledActions, 200, nameof(command.EnabledActions), "包含操作长度不能超过 200 个字符。");
        table.GenPath = Optional(command.GenPath, 500, nameof(command.GenPath), "生成路径长度不能超过 500 个字符。");
        table.ParentMenuId = command.ParentMenuId;
        table.PrimaryKeyColumn = Optional(command.PrimaryKeyColumn, 100, nameof(command.PrimaryKeyColumn), "主键列名长度不能超过 100 个字符。");
        table.TreeParentColumn = Optional(command.TreeParentColumn, 100, nameof(command.TreeParentColumn), "树表父级字段长度不能超过 100 个字符。");
        table.TreeNameColumn = Optional(command.TreeNameColumn, 100, nameof(command.TreeNameColumn), "树表名称字段长度不能超过 100 个字符。");
        table.MasterTableId = command.MasterTableId;
        table.MasterForeignKey = Optional(command.MasterForeignKey, 100, nameof(command.MasterForeignKey), "主子表关联外键列长度不能超过 100 个字符。");
        table.DatabaseType = command.DatabaseType;
        table.DataSourceId = command.DataSourceId;
        table.Options = NormalizeNullable(command.Options);
        table.Status = command.Status;
        table.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注长度不能超过 500 个字符。");

        var changed = UserModifiedFieldSet.DiffChanged(table, snapshot);
        table.UserModifiedFields = UserModifiedFieldSet.Merge(table.UserModifiedFields, changed);

        return new CodeGenTableCommandResult(await _tableRepository.UpdateAsync(table, cancellationToken));
    }

    /// <summary>
    /// 参与 dirty-tracking 的表字段（会被同步表结构重新推断覆盖的字段）
    /// </summary>
    private static readonly string[] TrackedTableFields =
    [
        nameof(SysCodeGenTable.ClassName),
        nameof(SysCodeGenTable.Namespace),
        nameof(SysCodeGenTable.ModuleName),
        nameof(SysCodeGenTable.BusinessName),
        nameof(SysCodeGenTable.FunctionName),
        nameof(SysCodeGenTable.Author),
        nameof(SysCodeGenTable.TemplateType),
        nameof(SysCodeGenTable.TreeParentColumn),
        nameof(SysCodeGenTable.TreeNameColumn)
    ];

    /// <summary>
    /// 更新表配置状态（仅改 Status/Remark；备注留空即不修改）
    /// </summary>
    public async Task<CodeGenTableCommandResult> UpdateTableStatusAsync(CodeGenTableStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "表配置主键必须大于 0。");
        ValidateEnum(command.Status, nameof(command.Status));

        var table = await GetTableOrThrowAsync(command.BasicId, cancellationToken);

        table.Status = command.Status;
        // 留空即不修改备注，与数据源/模板的状态变更同口径：状态切换弹窗通常不回填原备注，
        // 直接覆盖会把原备注静默抹掉。要清空备注请走完整的更新表配置。
        table.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注长度不能超过 500 个字符。") ?? table.Remark;

        return new CodeGenTableCommandResult(await _tableRepository.UpdateAsync(table, cancellationToken));
    }

    /// <summary>
    /// 删除表配置（软删表 + 级联软删其列配置）
    /// </summary>
    public async Task DeleteTableAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var table = await GetTableOrThrowAsync(id, cancellationToken);

        // 先级联软删列配置，再软删表本身
        await _columnRepository.DeleteByTableIdAsync(table.BasicId, cancellationToken);

        if (!await _tableRepository.DeleteAsync(table, cancellationToken))
        {
            throw new InvalidOperationException("表配置删除失败。");
        }
    }

    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), message);
        }
    }

    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    /// <summary>
    /// 表配置的模板类型必须是具体类型：Universal 是模板侧「适用全部类型」的取值，表不能取它
    /// </summary>
    private static void EnsureTableTemplateType(TemplateType templateType)
    {
        if (templateType == TemplateType.Universal)
        {
            throw new InvalidOperationException("表配置的模板类型必须是单表/树表/主子表之一，「通用」仅用于模板。");
        }
    }

    /// <summary>
    /// 校验必填字符串：为空与超长是两种错误，各自给出独立提示，避免超长时报"不能为空"
    /// </summary>
    private static string Required(string? value, int maxLength, string paramName, string blankMessage, string tooLongMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(blankMessage, paramName);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException(tooLongMessage, paramName);
        }

        return trimmed;
    }

    private static string? Optional(string? value, int maxLength, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException(message, paramName);
        }

        return trimmed;
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task<SysCodeGenTable> GetTableOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "表配置主键必须大于 0。");
        return await _tableRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("表配置不存在。");
    }
}
