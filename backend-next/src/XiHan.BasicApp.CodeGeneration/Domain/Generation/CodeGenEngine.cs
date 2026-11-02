// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Scriban.Syntax;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 代码生成的管线：建上下文 → 选模板 → 渲染 → 产出。
/// </summary>
/// <remarks>
/// 照 4.x 的 <c>CodeGenerationEngine</c>：
/// 选模板时显式给了编码就用那几个，否则取启用的通用模板与本类型的模板；按生成范围裁（分组含 backend / frontend）；沿用已有实体时不出实体。
/// 一份模板渲染失败即整次失败，原因带模板编码（4.x 同）。
/// 与 4.x 不同（6.0 的两种目标）：目标是「加入已有包」时去掉包级模板（分组 <c>*-package</c>）；
/// 引擎另外拼出「新建包」的 <c>Localization/&lt;区域&gt;.json</c> 与两种目标都有的 <c>_registration/&lt;资源&gt;.md</c>；
/// 一份模板渲染出空白即不产出这个文件（按勾选的操作一份模板一个文件）。
/// </remarks>
/// <param name="builder">上下文的建法。</param>
/// <param name="templates">模板仓储。</param>
/// <param name="operations">操作登记表（查操作 ID 是否已被占用）。</param>
public sealed class CodeGenEngine(CodeGenContextBuilder builder, IRepository<SysCodeGenTemplate, Guid> templates, OperationRegistry operations)
{
    /// <summary>
    /// 实体模板（自动部分）的编码。
    /// </summary>
    public const string EntityTemplate = "backend.entity";

    /// <summary>
    /// 实体模板（手写部分）的编码。
    /// </summary>
    public const string EntityManualTemplate = "backend.entity.manual";

    /// <summary>
    /// 渲染出一张表的全部产物。
    /// </summary>
    /// <param name="tableId">表配置 ID。</param>
    /// <param name="templateCodes">指定的模板编码；为空按表的生成模式取。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<Result<IReadOnlyList<CodeGenArtifact>>> RenderAsync(Guid tableId, IReadOnlyList<string>? templateCodes, CancellationToken cancellationToken)
    {
        var built = await builder.BuildAsync(tableId, cancellationToken).ConfigureAwait(false);
        if (!built.TryGetValue(out var context))
        {
            return built.Error!;
        }

        var table = context.Table;
        var selected = Filter(await SelectAsync(table, templateCodes, cancellationToken).ConfigureAwait(false), context);
        if (selected.Count == 0)
        {
            return Error.Validation(CodeGenerationErrors.NoTemplates, CodeGenerationMessages.GenerateNoTemplates);
        }

        var items = CodeGenItems.Of(context);
        var model = CodeGenRenderer.Model(context, items);
        var artifacts = new List<CodeGenArtifact>();
        foreach (var template in selected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var error = RenderOne(template, model, table, out var artifact);
            if (error is not null)
            {
                return error;
            }

            if (artifact is not null)
            {
                artifacts.Add(artifact);
            }
        }

        var backend = table.Scope != CodeGenScope.FrontendOnly;
        if (backend && table.Target == CodeGenTarget.NewPackage)
        {
            artifacts.AddRange(CodeGenPhrases.Locales.Select(locale => new CodeGenArtifact(
                CodeGenSide.Backend,
                $"src/{context.Namespace}/Localization/{locale}.json",
                CodeGenLocalization.Json(CodeGenLocalization.Entries(context, items, locale, includeGroup: true)),
                TemplateCode: null,
                CodeGenWriteMode.WriteOnce)));
        }

        var taken = items.Operations.Select(static o => o.Id).Where(id => operations.Find(id).HasValue).ToList();
        artifacts.Add(new CodeGenArtifact(CodeGenSide.Registration, CodeGenRegistration.PathOf(context), CodeGenRegistration.Build(context, items, taken), TemplateCode: null, CodeGenWriteMode.AlwaysOverwrite));
        return artifacts;
    }

    // 按范围与目标裁；沿用已有实体时不出实体（否则与它重复定义，照 4.x）
    private static List<SysCodeGenTemplate> Filter(IEnumerable<SysCodeGenTemplate> candidates, CodeGenContext context)
        => [.. candidates.Where(t => context.Table.Scope switch
            {
                CodeGenScope.BackendOnly => IsBackend(t.TemplateGroup),
                CodeGenScope.FrontendOnly => IsFrontend(t.TemplateGroup),
                _ => true,
            })
            .Where(t => context.Table.Target == CodeGenTarget.NewPackage || t.TemplateGroup?.EndsWith("-package", StringComparison.OrdinalIgnoreCase) != true)
            .Where(t => context.ExistingEntity is null || t.TemplateCode is not (EntityTemplate or EntityManualTemplate))];

    private static bool IsBackend(string? group) => group?.Contains("backend", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsFrontend(string? group) => group?.Contains("frontend", StringComparison.OrdinalIgnoreCase) == true;

    // 渲染一份模板：出错返回错误；渲染为空白时产物为空
    private static Error? RenderOne(SysCodeGenTemplate template, Scriban.Runtime.ScriptObject model, SysCodeGenTable table, out CodeGenArtifact? artifact)
    {
        artifact = null;
        string content;
        string fileName;
        string directory;
        try
        {
            content = CodeGenRenderer.Render(template.TemplateContent, model);
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            fileName = string.IsNullOrWhiteSpace(template.FileNameExpression) ? string.Empty : CodeGenRenderer.Render(template.FileNameExpression, model).Trim();
            directory = string.IsNullOrWhiteSpace(template.FilePathExpression) ? string.Empty : CodeGenRenderer.Render(template.FilePathExpression, model).Trim().Replace('\\', '/').Trim('/');
        }
        catch (CodeGenRenderException exception)
        {
            return Error.Validation(CodeGenerationErrors.GenerationInvalid, exception.MessageKey, MessageArguments.Of([.. exception.Arguments]));
        }
        catch (ScriptRuntimeException exception) when (exception.InnerException is CodeGenRenderException inner)
        {
            return Error.Validation(CodeGenerationErrors.GenerationInvalid, inner.MessageKey, MessageArguments.Of([.. inner.Arguments]));
        }
        catch (Exception exception) when (exception is ScriptRuntimeException or InvalidOperationException)
        {
            return Error.Validation(CodeGenerationErrors.TemplateRenderFailed, CodeGenerationMessages.GenerateRenderFailed, MessageArguments.Of(("template", template.TemplateCode), ("reason", exception.Message)));
        }

        // 文件名没给按类名 + 扩展名（照 4.x）
        if (fileName.Length == 0)
        {
            var extension = CodeGenDataSourceService.Trimmed(template.FileExtension) ?? ".cs";
            fileName = table.ClassName + (extension.StartsWith('.') ? extension : "." + extension);
        }

        var path = directory.Length == 0 ? fileName : $"{directory}/{fileName}";
        if (!IsSafe(path))
        {
            return Error.Validation(CodeGenerationErrors.TemplateRenderFailed, CodeGenerationMessages.GeneratePathInvalid, MessageArguments.Of(("template", template.TemplateCode), ("path", path)));
        }

        var side = IsBackend(template.TemplateGroup) ? CodeGenSide.Backend : IsFrontend(template.TemplateGroup) ? CodeGenSide.Frontend : CodeGenSide.Registration;
        artifact = new CodeGenArtifact(side, path, content, template.TemplateCode, template.WriteMode);
        return null;
    }

    // 产物路径只能落在那一侧的根目录之下：相对路径、不带盘符与 ..
    private static bool IsSafe(string path)
        => !Path.IsPathRooted(path)
            && !path.Contains(':', StringComparison.Ordinal)
            && path.Split('/').All(static segment => segment.Length > 0 && segment is not ("." or ".."))
            && path.IndexOfAny(Path.GetInvalidPathChars()) < 0;

    private async Task<IReadOnlyList<SysCodeGenTemplate>> SelectAsync(SysCodeGenTable table, IReadOnlyList<string>? codes, CancellationToken cancellationToken)
    {
        if (codes is { Count: > 0 })
        {
            var wanted = codes.Select(static c => c.Trim()).ToList();
            var chosen = await templates.ListAsync(EntityQuery.All<SysCodeGenTemplate>().Where(new Specification<SysCodeGenTemplate>(t => wanted.Contains(t.TemplateCode))), cancellationToken).ConfigureAwait(false);
            return [.. chosen.OrderBy(static t => t.Order).ThenBy(static t => t.TemplateCode, StringComparer.Ordinal)];
        }

        var type = table.TemplateType;
        var enabled = await templates.ListAsync(
            EntityQuery.All<SysCodeGenTemplate>().Where(new Specification<SysCodeGenTemplate>(t => t.Status == CodeGenStatus.Active && (t.TemplateType == CodeGenTemplateType.Universal || t.TemplateType == type))),
            cancellationToken).ConfigureAwait(false);
        return [.. enabled.OrderBy(static t => t.Order).ThenBy(static t => t.TemplateCode, StringComparer.Ordinal)];
    }
}
