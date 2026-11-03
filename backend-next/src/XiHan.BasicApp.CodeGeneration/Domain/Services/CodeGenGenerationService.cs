// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Operations.CodegenHistories;
using XiHan.BasicApp.CodeGeneration.Operations.CodegenTables;
using XiHan.BasicApp.Contracts.Files;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.CodeGeneration.Domain.Services;

/// <summary>
/// 生成代码：打包或写进项目，并记生成历史。
/// </summary>
/// <remarks>
/// 照 4.x 的 <c>GenerateAsync</c>：渲染之后按交付方式分流，产物包存成 Files 的临时文件（7 天，同导出中心），写进项目经 <see cref="CodeGenProjectWriter"/>；
/// 每次生成不论成败都记一条历史，成功才回写表的生成状态与时间。失败的那条在独立的短事务里写（调用失败会回滚自己的写入）。
/// 与 4.x 不同：产物包不再以 Base64 放进响应，由历史的下载端点取（方案已定）。
/// </remarks>
/// <param name="engine">生成管线。</param>
/// <param name="tables">表配置服务。</param>
/// <param name="tableRows">表配置仓储。</param>
/// <param name="histories">生成历史仓储。</param>
/// <param name="writer">写进项目。</param>
/// <param name="files">临时文件。</param>
/// <param name="ids">ID 生成器。</param>
/// <param name="scopes">作用域工厂。</param>
/// <param name="clock">时钟。</param>
public sealed class CodeGenGenerationService(
    CodeGenEngine engine,
    CodeGenTableService tables,
    IRepository<SysCodeGenTable, Guid> tableRows,
    IRepository<SysCodeGenHistory, Guid> histories,
    CodeGenProjectWriter writer,
    ITemporaryFiles files,
    IIdGenerator<Guid> ids,
    IServiceScopeFactory scopes,
    TimeProvider clock)
{
    /// <summary>
    /// 产物包保留多久。
    /// </summary>
    public static readonly TimeSpan PackageLifetime = TimeSpan.FromDays(7);

    /// <summary>
    /// 产物包在 Files 里记的来源。
    /// </summary>
    public const string PackageSource = "codegeneration";

    /// <summary>
    /// 生成一张表的代码。
    /// </summary>
    /// <param name="request">生成请求。</param>
    /// <param name="context">调用上下文。</param>
    public async Task<Result<CodegenGenerateResponse>> GenerateAsync(GenerateCodegenTableRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var started = Stopwatch.GetTimestamp();
        if (!(await tables.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var table))
        {
            return CodeGenTableService.NotFound(request.Id);
        }

        var history = Start(table, request.DeliveryType ?? table.DeliveryType, context);
        var rendered = await engine.RenderAsync(table.Id, request.TemplateCodes, context.CancellationToken).ConfigureAwait(false);
        if (!rendered.TryGetValue(out var artifacts))
        {
            return await FailAsync(history, rendered.Error!, started, context).ConfigureAwait(false);
        }

        history.FileCount = artifacts.Count;
        history.TotalSize = artifacts.Sum(static a => (long)Encoding.UTF8.GetByteCount(a.Content));
        history.GeneratedFiles = JsonOf(artifacts.Select(static a => a.PackagePath));
        history.UsedTemplates = JsonOf(artifacts.Select(static a => a.TemplateCode).OfType<string>().Distinct(StringComparer.Ordinal));
        var written = new CodeGenWriteResult(0, [], []);
        if (history.DeliveryType == CodeGenDeliveryType.Zip)
        {
            using var package = new MemoryStream(CodeGenPackager.Pack(artifacts));
            var saved = await files.SaveAsync(context, PackageSource, package, $"{table.TableName}-{history.BatchNumber}.zip", "application/zip", PackageLifetime).ConfigureAwait(false);
            if (!saved.TryGetValue(out var file))
            {
                return await FailAsync(history, saved.Error!, started, context).ConfigureAwait(false);
            }

            history.FileId = file.Id;
        }
        else
        {
            var names = CodeGenContextBuilder.NamesOf(table);
            Result<CodeGenWriteResult> outcome = names.TryGetValue(out var name)
                ? await writer.WriteAsync(table, name.Namespace, artifacts, context.CancellationToken).ConfigureAwait(false)
                : names.Error!;
            if (!outcome.TryGetValue(out written))
            {
                return await FailAsync(history, outcome.Error!, started, context).ConfigureAwait(false);
            }

            history.GenPath = Clip(string.Join("; ", written.Roots), 1000);
        }

        history.GenerationStatus = CodeGenGenerationStatus.Generated;
        history.Duration = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        var added = await histories.AddAsync(history, context.CancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        table.GenerationStatus = CodeGenGenerationStatus.Generated;
        table.LastGeneratedAt = history.GeneratedAt;
        var updated = await tableRows.UpdateAsync(table, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure
            ? updated.Error!
            : new CodegenGenerateResponse(history.Id, history.DeliveryType, history.FileCount, history.Duration, history.FileId, written.Written, written.Skipped, written.Roots);
    }

    // 历史的公共部分：批次号、表配置快照与操作人（照 4.x）
    private SysCodeGenHistory Start(SysCodeGenTable table, CodeGenDeliveryType delivery, InvocationContext context)
    {
        var now = clock.GetUtcNow();
        var principal = context.GetSecurity().Principal;
        return new SysCodeGenHistory
        {
            Id = ids.NewId(),
            TableId = table.Id,
            TableName = table.TableName,
            BatchNumber = $"CG{now:yyyyMMddHHmmssfff}{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            DeliveryType = delivery,
            GeneratedAt = now,
            TableSnapshot = JsonSerializer.Serialize(
                new CodegenTableSnapshot(table.TableName, table.TableComment, table.ClassName, table.Namespace, table.ModuleName, table.BusinessName, table.TemplateType, table.Scope, table.Target, table.EnabledActions),
                CodeGenerationJsonContext.Default.CodegenTableSnapshot),
            OperatorId = Clip(principal.Id, 64),
            OperatorName = Clip(principal.ClaimValues(IdentityClaims.UserName).FirstOrDefault(), 100),
            OperatorIp = context.GetOrigin().TryGetValue(out var origin) ? Clip(origin.RemoteAddress, 64) : null,
        };
    }

    // 失败也记一条历史：调用失败会回滚自己的写入，所以在独立的短事务里写；写不进只丢这条历史，原样返回失败
    private async Task<Result<CodegenGenerateResponse>> FailAsync(SysCodeGenHistory history, Error error, long started, InvocationContext context)
    {
        history.GenerationStatus = CodeGenGenerationStatus.Failed;
        history.Duration = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        history.ErrorMessage = $"{error.Code}: {context.Localize(error)}";
        _ = await StoreScope.WriteAsync(scopes, async (services, ct) =>
        {
            var added = await services.GetRequiredService<IRepository<SysCodeGenHistory, Guid>>().AddAsync(history, ct).ConfigureAwait(false);
            return added.IsFailure ? added.Error! : Result.Success();
        }, context.CancellationToken).ConfigureAwait(false);
        return error;
    }

    private static string? JsonOf(IEnumerable<string> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? null : JsonSerializer.Serialize(list, CodeGenerationJsonContext.Default.ListString);
    }

    private static string? Clip(string? value, int max)
        => string.IsNullOrWhiteSpace(value) ? null : value.Length <= max ? value : value[..max];
}
