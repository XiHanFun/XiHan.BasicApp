// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.Contracts.Seeding;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.CodeGeneration.Seeds;

/// <summary>
/// 内置模板的种子
/// </summary>
/// <remarks>
/// 号段 600 + 20（平台数据，可选模块按偏移；Access 已占 610）。照 4.x：把嵌入的 <c>Seeds/Templates/**/*.sbn</c> 写成内置模板，
/// 每次启动对齐正文、名称、分组、生成模式、扩展名、路径表达式、写入方式与排序（内置模板随程序版本走，改了模板文件已有的库也要生效），启停归运维、不动
/// 嵌入的文件缺了是构建问题，直接失败
/// </remarks>
/// <param name="templates">模板仓储</param>
/// <param name="ids">标识生成器</param>
public sealed class CodeGenTemplateSeeder(IRepository<SysCodeGenTemplate, Guid> templates, IIdGenerator<Guid> ids) : ISeeder
{
    /// <inheritdoc />
    public string Name => "codegeneration.templates";

    /// <inheritdoc />
    public int Order => SeedOrders.PlatformData + (SeedOrders.ModuleOffset * 2);

    /// <summary>
    /// 读一份内置模板的正文
    /// </summary>
    /// <param name="template">内置模板</param>
    /// <exception cref="InvalidOperationException">嵌入的文件不存在</exception>
    public static string ContentOf(BuiltInTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        var assembly = typeof(CodeGenTemplateSeeder).Assembly;
        var name = assembly.GetManifestResourceNames().FirstOrDefault(n => n.Replace('\\', '/') == "Templates/" + template.Resource)
            ?? throw new InvalidOperationException($"Built-in code generation template '{template.Code}' is missing its embedded file 'Templates/{template.Resource}'.");
        using var stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public async ValueTask<Result> SeedAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var codes = BuiltInTemplates.All.Select(static t => t.Code).ToList();
        var query = EntityQuery.All<SysCodeGenTemplate>().Where(new Specification<SysCodeGenTemplate>(t => codes.Contains(t.TemplateCode)));
        var existing = (await templates.ListAsync(query, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static t => t.TemplateCode, StringComparer.Ordinal);
        foreach (var (builtIn, index) in BuiltInTemplates.All.Select(static (t, i) => (t, i)))
        {
            var content = ContentOf(builtIn);
            var order = (index + 1) * 10;
            if (existing.TryGetValue(builtIn.Code, out var template))
            {
                if (template.IsBuiltIn && template.TemplateContent == content && template.TemplateName == builtIn.Name && template.TemplateGroup == builtIn.Group
                    && template.TemplateType == builtIn.TemplateType && template.FileExtension == builtIn.Extension && template.FileNameExpression == builtIn.FileName
                    && template.FilePathExpression == builtIn.FilePath && template.WriteMode == builtIn.WriteMode && template.Order == order)
                {
                    continue;
                }

                Apply(template, builtIn, content, order);
                var updated = await templates.UpdateAsync(template, context.CancellationToken).ConfigureAwait(false);
                if (updated.IsFailure)
                {
                    return updated.Error!;
                }

                continue;
            }

            var created = new SysCodeGenTemplate { Id = ids.NewId(), TemplateCode = builtIn.Code, Status = CodeGenStatus.Active };
            Apply(created, builtIn, content, order);
            var added = await templates.AddAsync(created, context.CancellationToken).ConfigureAwait(false);
            if (added.IsFailure)
            {
                return added.Error!;
            }
        }

        return Result.Success();
    }

    private static void Apply(SysCodeGenTemplate template, BuiltInTemplate builtIn, string content, int order)
    {
        template.TemplateName = builtIn.Name;
        template.TemplateGroup = builtIn.Group;
        template.TemplateType = builtIn.TemplateType;
        template.TemplateEngine = CodeGenTemplateEngine.Scriban;
        template.WriteMode = builtIn.WriteMode;
        template.TemplateContent = content;
        template.FileNameExpression = builtIn.FileName;
        template.FilePathExpression = builtIn.FilePath;
        template.FileExtension = builtIn.Extension;
        template.IsBuiltIn = true;
        template.Order = order;
    }
}
