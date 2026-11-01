// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Printing.Domain.Entities;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Printing.Domain.Services;

/// <summary>
/// 打印模板的两侧规则与写入校验。
/// </summary>
/// <remarks>
/// 照 4.x 的领域服务与命令服务的作用域收口：写只写调用方这一侧（平台一侧即全局模板，还要 <c>printing.print-templates.global-manage</c>）；
/// 别的一侧的模板当作不存在；数据源编码非空即须已登记；模板 JSON 只做最小结构校验（根是对象、<c>schemaVersion</c> 是数字 1、<c>pages</c> 是非空的对象数组），其余原样保存。
/// </remarks>
/// <param name="templates">模板仓储。</param>
/// <param name="dataSources">数据源注册表。</param>
/// <param name="permissions">权限判定（全局管理）。</param>
public sealed class PrintTemplateService(IRepository<SysPrintTemplate, Guid> templates, PrintDataSourceRegistry dataSources, IPermissionChecker permissions)
{
    /// <summary>
    /// 支持的模板格式版本。
    /// </summary>
    public const int SchemaVersion = 1;

    /// <summary>
    /// 调用方所在的一侧。
    /// </summary>
    /// <remarks>
    /// 租户主体是本租户，平台主体是 <c>platform</c>。
    /// </remarks>
    /// <param name="context">调用上下文。</param>
    /// <returns>侧。</returns>
    public static string SideOf(InvocationContext context) => SideRows.SideOf(context);

    /// <summary>
    /// 是否为平台一侧。
    /// </summary>
    /// <param name="side">侧。</param>
    public static bool IsPlatform(string side) => string.Equals(side, TenantIds.Platform, StringComparison.Ordinal);

    /// <summary>
    /// 平台一侧写全局模板要有全局管理权限。
    /// </summary>
    /// <remarks>
    /// 租户一侧直接通过；判定本身失败（授予存储不可达等）原样返回。
    /// </remarks>
    /// <param name="context">调用上下文。</param>
    /// <returns>能写为成功，缺权限 <c>PRINTING_DENIED_001</c>。</returns>
    public async ValueTask<Result> EnsureWritableAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!IsPlatform(SideOf(context)))
        {
            return Result.Success();
        }

        var held = await permissions.HasPermissionAsync(context.GetSecurity(), PrintingPermissions.PrintTemplatesGlobalManage, context.CancellationToken).ConfigureAwait(false);
        if (!held.TryGetValue(out var granted))
        {
            return held.Error!;
        }

        return granted ? Result.Success() : Error.Denied(PrintingErrors.GlobalManageRequired, PrintingMessages.GlobalManageRequired);
    }

    /// <summary>
    /// 按 ID 取这一侧自己的模板。
    /// </summary>
    /// <param name="id">模板 ID。</param>
    /// <param name="side">侧。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>找到的模板，别的一侧的或不存在即 <c>PRINTING_NOTFOUND_001</c>。</returns>
    public async ValueTask<Result<SysPrintTemplate>> FindOwnedAsync(Guid id, string side, CancellationToken cancellationToken)
    {
        if (!(await templates.FindAsync(id, cancellationToken).ConfigureAwait(false)).TryGetValue(out var template) || !string.Equals(template.OwnerTenantId, side, StringComparison.Ordinal))
        {
            return NotFound(id);
        }

        return template;
    }

    /// <summary>
    /// 这一侧是否已有同编码的模板。
    /// </summary>
    /// <param name="side">侧。</param>
    /// <param name="code">模板编码。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async ValueTask<bool> CodeTakenAsync(string side, string code, CancellationToken cancellationToken)
        => await templates.CountAsync(EntityQuery.All<SysPrintTemplate>().Where(new Specification<SysPrintTemplate>(t => t.OwnerTenantId == side && t.TemplateCode == code)), cancellationToken).ConfigureAwait(false) > 0;

    /// <summary>
    /// 规整并核对数据源编码。
    /// </summary>
    /// <param name="code">数据源编码。</param>
    /// <returns>空白即 <see langword="null"/>（自由模板），没登记即 <c>PRINTING_VALIDATION_003</c>。</returns>
    public Result<Maybe<string>> DataSourceOf(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Maybe.None<string>();
        }

        var trimmed = code.Trim();
        return dataSources.IsRegistered(trimmed)
            ? Maybe.Some(trimmed)
            : Error.Validation(PrintingErrors.DataSourceUnregistered, PrintingMessages.DataSourceUnregistered, MessageArguments.Of(("code", trimmed)));
    }

    /// <summary>
    /// 模板 JSON 的最小结构校验。
    /// </summary>
    /// <param name="json">模板 JSON。</param>
    /// <returns>合格为成功，否则 <c>PRINTING_VALIDATION_002</c>（文案按出错的位置）。</returns>
    public static Result ValidateTemplateJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Invalid(PrintingMessages.TemplateJsonRoot);
            }

            if (!root.TryGetProperty("schemaVersion", out var version) || version.ValueKind != JsonValueKind.Number || !version.TryGetDouble(out var number) || number != SchemaVersion)
            {
                return Invalid(PrintingMessages.TemplateJsonSchemaVersion);
            }

            if (!root.TryGetProperty("pages", out var pages) || pages.ValueKind != JsonValueKind.Array || pages.GetArrayLength() == 0)
            {
                return Invalid(PrintingMessages.TemplateJsonPages);
            }

            var index = 0;
            foreach (var page in pages.EnumerateArray())
            {
                index++;
                if (page.ValueKind != JsonValueKind.Object)
                {
                    return Error.Validation(PrintingErrors.TemplateJsonInvalid, PrintingMessages.TemplateJsonPage, MessageArguments.Of(("index", index)));
                }
            }

            return Result.Success();
        }
        catch (JsonException)
        {
            return Invalid(PrintingMessages.TemplateJsonSyntax);
        }
    }

    /// <summary>
    /// 模板不存在。
    /// </summary>
    /// <param name="id">模板 ID。</param>
    public static Error NotFound(Guid id) => Error.NotFound(PrintingErrors.TemplateNotFound, PrintingMessages.TemplateNotFound, MessageArguments.Of(("id", id)));

    /// <summary>
    /// 去掉首尾空白，空白即 <see langword="null"/>。
    /// </summary>
    /// <param name="value">原值。</param>
    public static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Error Invalid(string message) => Error.Validation(PrintingErrors.TemplateJsonInvalid, message);
}
