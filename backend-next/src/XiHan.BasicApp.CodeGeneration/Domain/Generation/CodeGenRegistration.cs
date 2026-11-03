// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Services;

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 登记片段：<c>_registration/&lt;资源&gt;.md</c>。
/// </summary>
/// <remarks>
/// 用户已定：片段由引擎拼出、不做成模板（照 4.x 的接线产物也是 C# 拼的）。
/// 「加入已有包」按文件列出要人工合入的行（错误码序号写成待填 <c>NNN</c>）；「新建包」写明加进解决方案、宿主与架构测试的 Tier 表（按 Tier 3 登记）。
/// 操作 ID 已被别的包占用时在开头告警（照 4.x 生成前的权限码预检，只提示、不拦）。
/// </remarks>
public static class CodeGenRegistration
{
    /// <summary>
    /// 新建包登记的页面图标。
    /// </summary>
    public const string PageIcon = "lucide:table";

    /// <summary>
    /// 新建包登记的页面排序号。
    /// </summary>
    public const int PageOrder = 100;

    /// <summary>
    /// 片段的路径。
    /// </summary>
    /// <param name="context">生成上下文。</param>
    public static string PathOf(CodeGenContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return $"_registration/{context.Resource}.md";
    }

    /// <summary>
    /// 拼出片段。
    /// </summary>
    /// <param name="context">生成上下文。</param>
    /// <param name="items">要登记的操作、权限码与错误码。</param>
    /// <param name="takenOperations">已被占用的操作 ID。</param>
    public static string Build(CodeGenContext context, CodeGenItems items, IReadOnlyCollection<string> takenOperations)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(takenOperations);
        var md = new StringBuilder();
        md.Append("# ").Append(context.DisplayName).Append("（`").Append(context.Resource).AppendLine("`）的登记片段").AppendLine();
        if (takenOperations.Count > 0)
        {
            md.AppendLine("> **注意**：下列操作 ID 已被别的包占用，合入前请改类名或资源名重新生成：").AppendLine(">");
            foreach (var id in takenOperations)
            {
                md.Append("> - `").Append(id).AppendLine("`");
            }

            md.AppendLine();
        }

        if (context.Table.Target == CodeGenTarget.NewPackage)
        {
            NewPackage(md, context);
        }
        else
        {
            ExistingPackage(md, context, items);
        }

        return md.ToString();
    }

    private static void NewPackage(StringBuilder md, CodeGenContext context)
    {
        var ns = context.Namespace;
        var module = context.ModuleName;
        md.Append("## 新建包 `").Append(ns).AppendLine("`").AppendLine();
        md.AppendLine("包级文件只在第一次生成时写出，之后再加资源请选「加入已有包」。按 Tier 3（可选模块，只依赖 Contracts 与 Runtime）登记：").AppendLine();
        md.AppendLine("1. `backend-next/XiHan.BasicApp.slnx` 的 `/3.Optional/` 文件夹：").AppendLine();
        Code(md, "xml", $"<Project Path=\"src/{ns}/{ns}.csproj\" />");
        md.AppendLine("2. 宿主 `src/XiHan.BasicApp.Host/XiHan.BasicApp.Host.csproj`：").AppendLine();
        Code(md, "xml", $"<ProjectReference Include=\"../{ns}/{ns}.csproj\" />");
        md.AppendLine("3. 宿主 `BasicAppHost` 装配特性的地方：").AppendLine();
        Code(md, "csharp", $"xihan.UseFeature<{ns}.{module}Feature>();");
        md.AppendLine("4. 宿主 `BasicAppOpenApi` 的分组（序号按现有分组往后排）：").AppendLine();
        Code(md, "csharp", $"Group(\"{context.ModuleCode}\", 200),");
        md.AppendLine("5. 架构测试 `test/XiHan.BasicApp.Architecture.Tests`：工程引用这个包，`Model/Packages.cs` 的 `Ranks` 与 `Assemblies`：").AppendLine();
        Code(md, "csharp", $"[\"{module}\"] = 30,", $"typeof({ns}.{module}Feature).Assembly,");
        md.AppendLine("6. 导出契约（`contracts export`）后在 `frontend-next` 运行 `pnpm gen:contracts`。").AppendLine();
        if (context.Table.Scope != CodeGenScope.BackendOnly)
        {
            md.AppendLine("7. 前端 `frontend-next/scripts/check-modules.mjs` 的 `KNOWN` 加上模块名（模块目录与后端包短名一致）；视图键 `" + context.Resource + "/index` 要全局唯一：").AppendLine();
            Code(md, "js", $"'{context.ModuleCode}',");
        }
    }

    private static void ExistingPackage(StringBuilder md, CodeGenContext context, CodeGenItems items)
    {
        var module = context.ModuleName;
        var cls = context.Table.ClassName;
        md.Append("## 加入已有包 `").Append(context.Namespace).AppendLine("`").AppendLine();
        md.AppendLine("生成的代码引用了下面这些登记项，按文件合入；错误码的 `NNN` 接着包里同类错误码往后编。").AppendLine();

        md.Append("### `").Append(module).AppendLine("Permissions.cs`").AppendLine();
        Code(md, "csharp", [.. items.Permissions.Select(static p => $"public const string {p.ConstName} = \"{p.Code}\";")]);
        md.AppendLine("`All`：").AppendLine();
        Code(md, "csharp", [.. items.Permissions.Select(p => $"new({p.ConstName}, \"{CodeGenTextEscaper.CSharpString(CodeGenPhrases.Text($"perm.{p.Action}.name", "en-US", ("name", context.DisplayName)))}\", PermissionScope.Tenant{(p.Audited ? ", audited: true" : string.Empty)}, description: \"{CodeGenTextEscaper.CSharpString(CodeGenPhrases.Text($"perm.{p.Action}.description", "en-US", ("name", context.DisplayName)))}\"),")]);

        md.Append("### `").Append(module).AppendLine("Pages.cs`").AppendLine();
        Code(md, "csharp", $"public static PageDescriptor {context.ResourcePascal} {{ get; }} = new(\"{context.ModuleCode}.{context.Resource}\", \"/{context.ModuleCode}/{context.Resource}\", \"{context.ModuleCode}.{context.Resource.Replace('-', '_')}.title\", \"{PageIcon}\", {PageOrder}, {module}Permissions.{context.ResourcePascal}Read);");
        md.Append("并加进 `All`：`").Append(context.ResourcePascal).AppendLine("`。").AppendLine();

        md.Append("### `").Append(module).AppendLine("Errors.cs`").AppendLine();
        Code(md, "csharp", [.. items.Errors.Select(static e => $"public const string {e.ConstName} = \"{e.Code}\";")]);

        md.Append("### `").Append(module).AppendLine("Messages.cs`").AppendLine();
        Code(md, "csharp",
        [
            .. items.Operations.Select(static o => $"public const string {o.DescriptionConst} = \"{o.Key}.description\";"),
            .. items.Errors.SelectMany(static e => e.Messages).Select(static m => $"public const string {m.ConstName} = \"{m.Key}\";"),
        ]);

        md.Append("### `").Append(module).AppendLine("JsonContext.cs`").AppendLine();
        md.AppendLine("已有的 `PageRequest`、`BatchResult` 不必重复。").AppendLine();
        Code(md, "csharp", [.. items.JsonTypes.Select(static t => $"[JsonSerializable(typeof({t}))]")]);

        md.Append("### `").Append(module).AppendLine("Feature.cs` 的 `Configure`").AppendLine();
        Code(md, "csharp", context.Can(CodeGenActions.Print)
            ? [$"context.Services.AddScoped<{cls}Queries>();", $"context.Services.AddPrintDataSource({cls}PrintDataSource.Definition);"]
            : [$"context.Services.AddScoped<{cls}Queries>();"]);

        if (context.ReferencedPackages.Count > 0)
        {
            md.Append("### `").Append(context.Namespace).AppendLine(".csproj`").AppendLine();
            Code(md, "xml", [.. context.ReferencedPackages.Select(static p => $"<ProjectReference Include=\"../{p}/{p}.csproj\" />")]);
        }

        if (context.Table.Scope != CodeGenScope.BackendOnly)
        {
            foreach (var locale in CodeGenPhrases.Locales)
            {
                md.Append("### `frontend-next/src/modules/").Append(context.ModuleCode).Append("/locales/").Append(locale).Append(".ts` 的 `").Append(context.ModuleCode).AppendLine("` 对象里").AppendLine();
                Code(md, "ts", CodeGenFrontendLocalization.Block(context, locale));
            }
        }

        foreach (var locale in CodeGenPhrases.Locales)
        {
            md.Append("### `Localization/").Append(locale).AppendLine(".json`").AppendLine();
            Code(md, "json", [.. CodeGenLocalization.Entries(context, items, locale, includeGroup: false).Select(static e => $"\"{e.Key}\": \"{System.Text.Json.JsonEncodedText.Encode(e.Value, System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping)}\",")]);
        }
    }

    private static void Code(StringBuilder md, string language, params string[] lines)
    {
        md.Append("```").AppendLine(language);
        foreach (var line in lines)
        {
            md.AppendLine(line);
        }

        md.AppendLine("```").AppendLine();
    }
}
