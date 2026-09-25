// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Domain.Permissions;
using Shared = XiHan.BasicApp.CodeGeneration.Infrastructure.Generation.MenuPermissionArtifactShared;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 种子骨架二阶产物生成器（{Class}PermissionSeeder.cs + {Class}MenuSeeder.cs）
/// </summary>
/// <remarks>
/// 产出可编译的种子骨架，与本仓库各模块同一套基类：权限种子继承 PermissionCatalogSeederBase（资源 + 资源 × 操作的权限），
/// 菜单种子继承 PageRegistryMenuSeederBase（页面行 + 写操作按钮行）。
/// Order 为占位、需人工确认不冲突，故标 <see cref="ArtifactWriteMode.WriteOnce"/>（首次创建后永不覆盖）。
/// 权限项来源为同批生成的 {Class}PermissionDefinitions，按钮的权限码来自同批生成的 {Class}PermissionCodes，避免两处描述。
/// </remarks>
internal static class SeederArtifactGenerator
{
    /// <summary>
    /// 构建两个种子骨架
    /// </summary>
    public static IReadOnlyList<GeneratedArtifact> Build(CodeGenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return
        [
            BuildPermissionSeeder(context),
            BuildMenuSeeder(context)
        ];
    }

    private static GeneratedArtifact BuildPermissionSeeder(CodeGenerationContext context)
    {
        var content = Fill(PermissionSeederTemplate, context);
        var fileName = $"{context.ClassName}PermissionSeeder.cs";
        return new GeneratedArtifact($"{Shared.OutputFolder}/{fileName}", fileName, content, Shared.TemplateCode, ArtifactWriteMode.WriteOnce);
    }

    private static GeneratedArtifact BuildMenuSeeder(CodeGenerationContext context)
    {
        var content = Fill(MenuSeederTemplate, context);
        var fileName = $"{context.ClassName}MenuSeeder.cs";
        return new GeneratedArtifact($"{Shared.OutputFolder}/{fileName}", fileName, content, Shared.TemplateCode, ArtifactWriteMode.WriteOnce);
    }

    /// <summary>
    /// 占位替换（原始字符串模板含大量 C# 花括号/内插，用 %TOKEN% 占位避免转义）
    /// </summary>
    private static string Fill(string template, CodeGenerationContext context)
    {
        return template
            .Replace("%NS%", Shared.ResolveNamespace(context))
            .Replace("%CLASS%", context.ClassName)
            .Replace("%MODULE%", Shared.ModuleSegment(context))
            .Replace("%DISPLAY%", Shared.Display(context))
            .Replace("%RESOURCE%", Shared.Resource(context))
            .Replace("%PAGECODE%", $"{Shared.ModuleLower(context)}.{Shared.Kebab(context)}")
            .Replace("%PATH%", $"/{Shared.ModuleLower(context)}/{Shared.Kebab(context)}")
            .Replace("%COMPONENT%", Shared.Component(context))
            .Replace("%ROUTE%", Shared.RouteName(context))
            .Replace("%BUTTONS%", BuildButtons(context));
    }

    /// <summary>
    /// 写操作按钮行（已启用的新增/编辑/删除/导入/导出）；查询与详情走列表页的读取权限，没有独立按钮
    /// </summary>
    private static string BuildButtons(CodeGenerationContext context)
    {
        var pageCode = Shared.PageCode(context);
        var effective = Shared.EffectiveActions(context);
        var sb = new StringBuilder();
        var sort = 1;
        foreach (var button in ButtonPermissionMappings.Buttons)
        {
            if (button.Action == "read" || !effective.Contains(button.Action))
            {
                continue;
            }

            sb.AppendLine($"        new(\"{pageCode}.{button.Key}\", \"{button.Title}\", \"{pageCode}\", {context.ClassName}PermissionCodes.{Shared.Pascalize(button.Action)}, {sort}),");
            sort++;
        }

        return sb.ToString().TrimEnd();
    }

    private const string PermissionSeederTemplate = """
// 本文件为代码生成器产出的种子骨架：仅首次创建、重新生成不覆盖，可自由编辑。
using Microsoft.Extensions.Logging;
using %NS%.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace %NS%.Infrastructure.Seeders;

/// <summary>
/// %DISPLAY% 权限目录（生成骨架）：资源与「资源 × 已启用操作」的权限
/// </summary>
/// <remarks>
/// 操作来自平台操作字典（OperationSeeds），资源、权限是平台目录：在平台上下文播、只落平台库。
/// 作用侧默认两侧生效；只给业务租户用的功能改成 PermissionSide.Tenant，平台专用的改成 PermissionSide.Platform。
/// 权限只需声明，超管在平台天然拥有全部权限，租户的角色与套餐白名单由运营授予。
/// </remarks>
public sealed class %CLASS%PermissionSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<%CLASS%PermissionSeeder> logger,
    IServiceProvider serviceProvider)
    : PermissionCatalogSeederBase(clientResolver, logger, serviceProvider)
{
    private static readonly ResourceSeed Resource = new(
        %CLASS%PermissionDefinitions.Resource,
        %CLASS%PermissionDefinitions.ResourceName,
        %CLASS%PermissionDefinitions.ResourcePath,
        %CLASS%PermissionDefinitions.ResourceName + "接口",
        0);

    /// <summary>种子优先级（TODO：权限目录阶段内按模块错开，确认不与本模块其它目录种子冲突）</summary>
    public override int Order => SeedOrders.PermissionCatalog + 90;

    /// <summary>种子名称</summary>
    public override string Name => "[%MODULE%]%DISPLAY%权限目录";

    /// <summary>模块编码</summary>
    public override string ModuleCode => %CLASS%PermissionDefinitions.Module;

    /// <summary>本模块的资源</summary>
    public override IReadOnlyList<ResourceSeed> Resources { get; } = [Resource];

    /// <summary>本模块的权限（资源 × 已启用操作）</summary>
    public override IReadOnlyList<PermissionSeed> Permissions { get; } =
    [
        .. %CLASS%PermissionDefinitions.Items.Select((item, index) => new PermissionSeed(
            $"{Resource.Code}:{item.Action}",
            item.Name,
            item.Description,
            Resource.Code,
            PermissionSide.Both,
            item.IsRequireAudit,
            9000 + index,
            Resource,
            OperationSeeds.All.Single(operation => operation.Code == item.Action)))
    ];
}
""";

    private const string MenuSeederTemplate = """
// 本文件为代码生成器产出的种子骨架：仅首次创建、重新生成不覆盖，可自由编辑。
using Microsoft.Extensions.Logging;
using %NS%.Domain.Permissions;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace %NS%.Infrastructure.Seeders;

/// <summary>
/// %DISPLAY% 菜单（生成骨架）：页面行与写操作按钮行
/// </summary>
/// <remarks>
/// 页面绑定 %RESOURCE%:read 控制可见；生成页面的写操作按钮用按钮码 %PAGECODE%.{create|update|delete…} 门控，
/// 按钮码只由菜单的按钮行下发，所以按钮行与页面行一起登记在这里。
/// 如需挂父目录，把页面的 ParentCode 换成父页面码（父页面须先于本种子登记）。
/// 若改走应用级 PageRegistry 单一事实源（见 %CLASS%PageRegistry.snippet.txt），就删掉本种子，不要两边都登记。
/// </remarks>
public sealed class %CLASS%MenuSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<%CLASS%MenuSeeder> logger,
    IServiceProvider serviceProvider)
    : PageRegistryMenuSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>种子优先级（TODO：菜单阶段内按模块错开，确认不冲突）</summary>
    public override int Order => SeedOrders.Menus + 90;

    /// <summary>种子名称</summary>
    public override string Name => "[%MODULE%]%DISPLAY%菜单";

    /// <summary>页面</summary>
    protected override IReadOnlyList<PageDescriptor> Pages { get; } =
    [
        new("%PAGECODE%", "%DISPLAY%", "menu.%RESOURCE%", MenuType.Menu, "%PATH%", "%ROUTE%", "%COMPONENT%",
            ParentCode: null, %CLASS%PermissionCodes.Read, "lucide:table", 999),
    ];

    /// <summary>写操作按钮</summary>
    protected override IReadOnlyList<ButtonDescriptor> Buttons { get; } =
    [
%BUTTONS%
    ];
}
""";
}
