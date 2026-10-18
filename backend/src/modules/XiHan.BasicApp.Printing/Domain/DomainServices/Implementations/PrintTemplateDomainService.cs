// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using System.Text.Json;
using XiHan.BasicApp.Printing.Domain.DataSources;
using XiHan.BasicApp.Printing.Domain.Entities;
using XiHan.BasicApp.Printing.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Printing.Domain.Repositories;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Printing.Domain.DomainServices;

/// <summary>
/// 打印模板领域服务实现，集中执行输入、结构、状态和并发校验。
/// </summary>
/// <remarks>
/// 服务本身不保存共享可变状态，可由同一请求作用域内的多个调用链安全使用；跨请求并发由数据库唯一索引和行版本处理。
/// </remarks>
public sealed class PrintTemplateDomainService : IPrintTemplateDomainService
{
    private const int CodeMaximumLength = 100;
    private const int NameMaximumLength = 100;
    private const int RemarkMaximumLength = 500;
    private const int SupportedSchemaVersion = 1;
    private const string SchemaVersionPropertyName = "schemaVersion";
    private const string PagesPropertyName = "pages";

    private readonly IPrintTemplateRepository _repository;
    private readonly ICurrentTenant _currentTenant;
    private readonly IPrintDataSourceRegistry _dataSourceRegistry;
    private readonly ILogger<PrintTemplateDomainService> _logger;

    /// <summary>
    /// 初始化打印模板领域服务。
    /// </summary>
    /// <param name="repository">打印模板仓储。</param>
    /// <param name="currentTenant">当前租户上下文。</param>
    /// <param name="dataSourceRegistry">打印数据源注册表。</param>
    /// <param name="logger">结构化日志记录器。</param>
    public PrintTemplateDomainService(
        IPrintTemplateRepository repository,
        ICurrentTenant currentTenant,
        IPrintDataSourceRegistry dataSourceRegistry,
        ILogger<PrintTemplateDomainService> logger)
    {
        _repository = repository;
        _currentTenant = currentTenant;
        _dataSourceRegistry = dataSourceRegistry;
        _logger = logger;
    }

    /// <summary>
    /// 校验可选数据源编码：为空表示自由模板；非空必须已在数据源注册表登记。
    /// </summary>
    private void EnsureDataSourceRegistered(string? dataSourceCode)
    {
        if (dataSourceCode is not null && !_dataSourceRegistry.IsRegistered(dataSourceCode))
        {
            throw new UserFriendlyException($"打印数据源未注册：{dataSourceCode}。");
        }
    }

    /// <summary>
    /// 在当前租户上下文中创建打印模板。
    /// </summary>
    /// <param name="command">创建命令。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>创建结果。</returns>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">模板参数无效或编码已存在。</exception>
    public async Task<PrintTemplateCommandResult> CreateAsync(
        PrintTemplateCreateCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var ownerTenantId = _currentTenant.Id ?? 0;
        var templateCode = NormalizeCode(command.TemplateCode, "模板编码");
        var dataSourceCode = NormalizeOptionalCode(command.DataSourceCode, "数据源编码");
        EnsureDataSourceRegistered(dataSourceCode);
        ValidateEditableFields(
            command.TemplateName,
            command.TemplateJson,
            command.Sort,
            command.Remark);
        ValidateEnum(command.Status, "模板状态");

        // 预检用于提供友好反馈，数据库唯一索引仍是两个并发创建请求之间的最终一致性边界。
        if (await _repository.FindByCodeInScopeAsync(ownerTenantId, templateCode, false, cancellationToken) is not null)
        {
            throw new UserFriendlyException("当前作用域中已存在相同编码的打印模板。");
        }

        var template = new SysPrintTemplate
        {
            TenantId = ownerTenantId,
            TemplateCode = templateCode,
            DataSourceCode = dataSourceCode,
            TemplateName = command.TemplateName.Trim(),
            TemplateJson = command.TemplateJson,
            // 租户私有模板不允许携带全局开放标记，避免后续误判可见边界。
            AllowTenantUse = ownerTenantId == 0 && command.AllowTenantUse,
            Status = command.Status,
            Sort = command.Sort,
            Remark = NormalizeNullable(command.Remark)
        };

        try
        {
            var saved = await _repository.AddAsync(template, cancellationToken);
            LogResult("Create", saved, true, null);
            return new PrintTemplateCommandResult(saved);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // 并发重复由数据库唯一约束拒绝；统一异常中间件会隐藏底层 SQL，但日志仍保留堆栈供运维定位。
            LogResult("Create", template, false, exception);
            throw;
        }
    }

    /// <summary>
    /// 更新当前作用域中的打印模板。
    /// </summary>
    /// <param name="command">更新命令。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>更新结果。</returns>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">模板不存在、参数无效或行版本冲突。</exception>
    public async Task<PrintTemplateCommandResult> UpdateAsync(
        PrintTemplateUpdateCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateEditableFields(
            command.TemplateName,
            command.TemplateJson,
            command.Sort,
            command.Remark);

        var template = await GetEditableOrThrowAsync(command.Id, cancellationToken);
        EnsureExpectedVersion(template, command.ExpectedRowVersion);
        var updatedDataSourceCode = NormalizeOptionalCode(command.DataSourceCode, "数据源编码");
        EnsureDataSourceRegistered(updatedDataSourceCode);
        template.DataSourceCode = updatedDataSourceCode;
        template.TemplateName = command.TemplateName.Trim();
        template.TemplateJson = command.TemplateJson;
        template.AllowTenantUse = template.IsGlobal && command.AllowTenantUse;
        template.Sort = command.Sort;
        template.Remark = NormalizeNullable(command.Remark);

        try
        {
            var saved = await _repository.UpdateAsync(template, cancellationToken);
            LogResult("Update", saved, true, null);
            return new PrintTemplateCommandResult(saved);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogResult("Update", template, false, exception);
            throw;
        }
    }

    /// <summary>
    /// 启用或停用当前作用域中的打印模板。
    /// </summary>
    /// <param name="command">状态命令。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>更新结果。</returns>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">模板不存在、状态无效或行版本冲突。</exception>
    public async Task<PrintTemplateCommandResult> UpdateStatusAsync(
        PrintTemplateStatusChangeCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateEnum(command.Status, "模板状态");
        ValidateRemark(command.Remark);

        var template = await GetEditableOrThrowAsync(command.Id, cancellationToken);
        EnsureExpectedVersion(template, command.ExpectedRowVersion);
        template.Status = command.Status;
        template.Remark = NormalizeNullable(command.Remark) ?? template.Remark;

        try
        {
            var saved = await _repository.UpdateAsync(template, cancellationToken);
            LogResult("Status", saved, true, null);
            return new PrintTemplateCommandResult(saved);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogResult("Status", template, false, exception);
            throw;
        }
    }

    /// <summary>
    /// 软删除已经停用的打印模板。
    /// </summary>
    /// <param name="command">删除命令。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">模板不存在、仍在启用或行版本冲突。</exception>
    public async Task DeleteAsync(
        PrintTemplateDeleteCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var template = await GetEditableOrThrowAsync(command.Id, cancellationToken);
        EnsureExpectedVersion(template, command.ExpectedRowVersion);
        if (template.Status != EnableStatus.Disabled)
        {
            throw new UserFriendlyException("打印模板必须先停用后才能删除。");
        }

        try
        {
            if (!await _repository.DeleteAsync(template, cancellationToken))
            {
                throw new UserFriendlyException("打印模板删除失败，请刷新后重试。");
            }

            LogResult("Delete", template, true, null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogResult("Delete", template, false, exception);
            throw;
        }
    }

    /// <summary>
    /// 加载当前租户上下文确切所属的可维护模板。
    /// </summary>
    private async Task<SysPrintTemplate> GetEditableOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new UserFriendlyException("打印模板主键必须大于 0。");
        }

        var ownerTenantId = _currentTenant.Id ?? 0;
        return await _repository.FindByIdInScopeAsync(ownerTenantId, id, cancellationToken)
            ?? throw new UserFriendlyException("打印模板不存在或不属于当前作用域。");
    }

    /// <summary>
    /// 比较客户端版本与当前实体版本；数据库更新仍会执行第二道乐观锁检查以覆盖读取后的竞态窗口。
    /// </summary>
    private static void EnsureExpectedVersion(SysPrintTemplate template, long expectedRowVersion)
    {
        if (expectedRowVersion < 0 || template.RowVersion != expectedRowVersion)
        {
            throw new UserFriendlyException("打印模板已被其他用户修改，请刷新后重试。");
        }
    }

    /// <summary>
    /// 校验可编辑字段和打印模板 JSON 的根结构。
    /// </summary>
    private static void ValidateEditableFields(
        string templateName,
        string templateJson,
        int sort,
        string? remark)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateJson);
        if (templateName.Trim().Length > NameMaximumLength)
        {
            throw new UserFriendlyException($"模板名称不能超过 {NameMaximumLength} 个字符。");
        }

        if (sort < 0)
        {
            throw new UserFriendlyException("模板排序不能小于 0。");
        }

        ValidateRemark(remark);
        ValidateTemplateJson(templateJson);
    }

    /// <summary>
    /// 校验打印模板 JSON 的最小结构：语法合法，根节点是对象，schemaVersion 是数字 1，pages 是非空数组且每一项都是对象；其余字段不检查、原样保存。
    /// </summary>
    private static void ValidateTemplateJson(string templateJson)
    {
        try
        {
            using var document = JsonDocument.Parse(templateJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new UserFriendlyException("打印模板 JSON 根节点必须是对象。");
            }

            if (!root.TryGetProperty(SchemaVersionPropertyName, out var schemaVersion))
            {
                throw new UserFriendlyException($"打印模板缺少 {SchemaVersionPropertyName} 字段。");
            }

            if (schemaVersion.ValueKind != JsonValueKind.Number
                || !schemaVersion.TryGetDouble(out var version)
                || version != SupportedSchemaVersion)
            {
                throw new UserFriendlyException(
                    $"不支持的打印模板格式版本：{SchemaVersionPropertyName} 必须是数字 {SupportedSchemaVersion}。");
            }

            if (!root.TryGetProperty(PagesPropertyName, out var pages))
            {
                throw new UserFriendlyException($"打印模板缺少 {PagesPropertyName} 字段。");
            }

            if (pages.ValueKind != JsonValueKind.Array)
            {
                throw new UserFriendlyException($"打印模板的 {PagesPropertyName} 必须是数组。");
            }

            if (pages.GetArrayLength() == 0)
            {
                throw new UserFriendlyException($"打印模板的 {PagesPropertyName} 至少要有一页。");
            }

            var pageNumber = 0;
            foreach (var page in pages.EnumerateArray())
            {
                pageNumber++;
                if (page.ValueKind != JsonValueKind.Object)
                {
                    throw new UserFriendlyException($"打印模板的 {PagesPropertyName} 第 {pageNumber} 项必须是对象。");
                }
            }
        }
        catch (JsonException exception)
        {
            throw new UserFriendlyException("打印模板不是有效的 JSON。", innerException: exception);
        }
    }

    /// <summary>
    /// 归一编码并拒绝会导致路由、缓存键或调用契约歧义的空白字符。
    /// </summary>
    private static string NormalizeCode(string code, string fieldName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var normalized = code.Trim();
        if (normalized.Length > CodeMaximumLength)
        {
            throw new UserFriendlyException($"{fieldName}不能超过 {CodeMaximumLength} 个字符。");
        }

        if (normalized.Any(char.IsWhiteSpace))
        {
            throw new UserFriendlyException($"{fieldName}不能包含空白字符。");
        }

        return normalized;
    }

    /// <summary>
    /// 归一可选编码；空值或纯空白表示不绑定代码数据源，其余值沿用严格编码规则。
    /// </summary>
    private static string? NormalizeOptionalCode(string? code, string fieldName)
    {
        return string.IsNullOrWhiteSpace(code) ? null : NormalizeCode(code, fieldName);
    }

    /// <summary>
    /// 校验可选备注长度。
    /// </summary>
    private static void ValidateRemark(string? remark)
    {
        if (!string.IsNullOrWhiteSpace(remark) && remark.Trim().Length > RemarkMaximumLength)
        {
            throw new UserFriendlyException($"模板备注不能超过 {RemarkMaximumLength} 个字符。");
        }
    }

    /// <summary>
    /// 校验枚举值是否在定义范围内。
    /// </summary>
    private static void ValidateEnum<TEnum>(TEnum value, string fieldName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new UserFriendlyException($"{fieldName}无效。");
        }
    }

    /// <summary>
    /// 把空白可选值归一为 null。
    /// </summary>
    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// 记录不含模板正文的结构化领域审计日志。
    /// </summary>
    private void LogResult(string operation, SysPrintTemplate template, bool success, Exception? exception)
    {
        if (success)
        {
            _logger.LogInformation(
                "打印模板操作成功。Operation={Operation}, TemplateId={TemplateId}, TemplateCode={TemplateCode}, TenantId={TenantId}, Scope={Scope}, Result=Success",
                operation,
                template.BasicId,
                template.TemplateCode,
                template.TenantId,
                template.IsGlobal ? PrintTemplateScope.Global : PrintTemplateScope.Tenant);
            return;
        }

        _logger.LogError(
            exception,
            "打印模板操作失败。Operation={Operation}, TemplateId={TemplateId}, TemplateCode={TemplateCode}, TenantId={TenantId}, Scope={Scope}, Result=Failed",
            operation,
            template.BasicId,
            template.TemplateCode,
            template.TenantId,
            template.IsGlobal ? PrintTemplateScope.Global : PrintTemplateScope.Tenant);
    }
}
