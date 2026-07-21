#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SeederArtifactGenerator
// Guid:f91d4a15-75ac-4082-b677-ac927444fc93
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/21 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using Shared = XiHan.BasicApp.CodeGeneration.Infrastructure.Generation.MenuPermissionArtifactShared;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 种子骨架二阶产物生成器（{Class}PermissionSeeder.cs + {Class}MenuSeeder.cs）
/// </summary>
/// <remarks>
/// 产出可编译的种子骨架，镜像本仓库既有 Seeder 样板（DataSeederBase + 资源/权限/授权/菜单）。
/// Order 为占位、需人工确认不冲突，故标 <see cref="ArtifactWriteMode.WriteOnce"/>（首次创建后永不覆盖）。
/// 权限项来源为同批生成的 {Class}PermissionDefinitions，避免两处描述。
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
            .Replace("%PATH%", $"/{Shared.ModuleLower(context)}/{Shared.Kebab(context)}")
            .Replace("%COMPONENT%", Shared.Component(context))
            .Replace("%ROUTE%", Shared.RouteName(context));
    }

    private const string PermissionSeederTemplate = """
// 本文件为代码生成器产出的种子骨架：仅首次创建、重新生成不覆盖，可自由编辑。
// Order 为占位（默认 200 段），并入前请确认不与既有 Seeder 冲突。
using Microsoft.Extensions.Logging;
using %NS%.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace %NS%.Infrastructure.Seeders;

/// <summary>
/// %DISPLAY% 资源 + 权限 + 超管授权种子（生成骨架）
/// </summary>
/// <remarks>
/// 依赖平台操作字典（SysOperation：read/create/update/delete…）已由既有种子登记。
/// 须置于 %CLASS%MenuSeeder 之前：菜单建立时需解析 %RESOURCE%:read。
/// </remarks>
public sealed class %CLASS%PermissionSeeder : DataSeederBase
{
    /// <summary>构造函数</summary>
    public %CLASS%PermissionSeeder(ISqlSugarClientResolver clientResolver, ILogger<%CLASS%PermissionSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>种子优先级（TODO：确认不冲突，保持 权限→菜单 顺序）</summary>
    public override int Order => 200;

    /// <summary>种子名称</summary>
    public override string Name => "[%MODULE%]%DISPLAY%权限种子数据";

    /// <summary>种子实现</summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;

        // 1) 资源（幂等）
        var resource = await client.Queryable<SysResource>().FirstAsync(r => r.ResourceCode == %CLASS%PermissionDefinitions.Resource);
        if (resource is null)
        {
            await BulkInsertAsync(new List<SysResource>
            {
                new()
                {
                    ResourceCode = %CLASS%PermissionDefinitions.Resource,
                    ResourceName = %CLASS%PermissionDefinitions.ResourceName,
                    ResourceType = ResourceType.Api,
                    ResourcePath = %CLASS%PermissionDefinitions.ResourcePath,
                    Description = %CLASS%PermissionDefinitions.ResourceName + " API",
                    AccessLevel = ResourceAccessLevel.Authorized,
                    Status = EnableStatus.Enabled,
                    Sort = 0
                }
            });
            resource = await client.Queryable<SysResource>().FirstAsync(r => r.ResourceCode == %CLASS%PermissionDefinitions.Resource);
        }

        // 2) 权限（资源 × 操作）
        var operationMap = (await client.Queryable<SysOperation>().ToListAsync()).ToDictionary(o => o.OperationCode, o => o);
        var codes = %CLASS%PermissionDefinitions.Items.Select(i => $"{resource.ResourceCode}:{i.Action}").ToList();
        var existingCodes = (await client.Queryable<SysPermission>().Where(p => codes.Contains(p.PermissionCode)).ToListAsync())
            .Select(p => p.PermissionCode).ToHashSet();
        var permissionAddList = new List<SysPermission>();
        foreach (var item in %CLASS%PermissionDefinitions.Items)
        {
            var permissionCode = $"{resource.ResourceCode}:{item.Action}";
            if (existingCodes.Contains(permissionCode))
            {
                continue;
            }

            if (!operationMap.TryGetValue(item.Action, out var operation))
            {
                Logger.LogWarning("操作字典缺少 {Action}，跳过权限 {Code}", item.Action, permissionCode);
                continue;
            }

            permissionAddList.Add(new SysPermission
            {
                ResourceId = resource.BasicId,
                OperationId = operation.BasicId,
                PermissionCode = permissionCode,
                PermissionName = item.Name,
                PermissionDescription = item.Description,
                IsRequireAudit = item.IsRequireAudit,
                Tags = %CLASS%PermissionDefinitions.Resource,
                Status = EnableStatus.Enabled,
                Sort = 900 + permissionAddList.Count
            });
        }

        if (permissionAddList.Count > 0)
        {
            await BulkInsertAsync(permissionAddList);
        }

        // 3) 超管授权
        var superRole = await client.Queryable<SysRole>().FirstAsync(r => r.RoleCode == "super_admin");
        if (superRole is null)
        {
            Logger.LogWarning("super_admin 角色不存在，跳过 %RESOURCE% 超管授权");
            return;
        }

        var resourcePrefix = %CLASS%PermissionDefinitions.Resource + ":";
        var permissions = await client.Queryable<SysPermission>().Where(p => p.PermissionCode.StartsWith(resourcePrefix)).ToListAsync();
        var permissionIds = permissions.Select(p => p.BasicId).ToList();
        var grantedIds = (await client.Queryable<SysRolePermission>()
                .Where(rp => rp.RoleId == superRole.BasicId && permissionIds.Contains(rp.PermissionId)).ToListAsync())
            .Select(rp => rp.PermissionId).ToHashSet();
        var grantAddList = permissions.Where(p => !grantedIds.Contains(p.BasicId))
            .Select(p => new SysRolePermission { RoleId = superRole.BasicId, PermissionId = p.BasicId }).ToList();
        if (grantAddList.Count > 0)
        {
            await BulkInsertAsync(grantAddList);
        }

        Logger.LogInformation("%DISPLAY% 权限种子：新增权限 {P} 个、授权 {G} 个", permissionAddList.Count, grantAddList.Count);
    }
}
""";

    private const string MenuSeederTemplate = """
// 本文件为代码生成器产出的种子骨架：仅首次创建、重新生成不覆盖，可自由编辑。
// Order 为占位（默认 201），须 > %CLASS%PermissionSeeder.Order，并入前确认不冲突。
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace %NS%.Infrastructure.Seeders;

/// <summary>
/// %DISPLAY% 菜单种子（生成骨架）
/// </summary>
/// <remarks>
/// 须置于 %CLASS%PermissionSeeder 之后：菜单建立时需解析 %RESOURCE%:read 绑定可见性。
/// 如挂父菜单：参照本仓库 SysMenuSeeder，插入后用 Updateable 按 MenuCode 回写 ParentId。
/// 若走 Saas PageRegistry 单一事实源，则删除本种子、改用 %CLASS%PageRegistry.snippet.txt 的条目。
/// </remarks>
public sealed class %CLASS%MenuSeeder : DataSeederBase
{
    /// <summary>构造函数</summary>
    public %CLASS%MenuSeeder(ISqlSugarClientResolver clientResolver, ILogger<%CLASS%MenuSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>种子优先级（TODO：确认 &gt; %CLASS%PermissionSeeder.Order 且不冲突）</summary>
    public override int Order => 201;

    /// <summary>种子名称</summary>
    public override string Name => "[%MODULE%]%DISPLAY%菜单种子数据";

    /// <summary>种子实现</summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;

        var readPermission = await client.Queryable<SysPermission>().FirstAsync(p => p.PermissionCode == "%RESOURCE%:read");
        if (readPermission is null)
        {
            Logger.LogWarning("%RESOURCE%:read 权限不存在，跳过 %DISPLAY% 菜单种子");
            return;
        }

        var exists = await client.Queryable<SysMenu>().AnyAsync(m => m.MenuCode == "%RESOURCE%");
        if (exists)
        {
            Logger.LogInformation("%DISPLAY% 菜单已存在，跳过");
            return;
        }

        await BulkInsertAsync(new List<SysMenu>
        {
            new()
            {
                ParentId = null, // TODO：如需挂父目录，插入后按 MenuCode 回写 ParentId
                PermissionId = readPermission.BasicId,
                MenuName = "%DISPLAY%",
                MenuCode = "%RESOURCE%",
                MenuType = MenuType.Menu,
                Path = "%PATH%",
                Component = "%COMPONENT%",
                RouteName = "%ROUTE%",
                Icon = "lucide:table",
                Title = "%DISPLAY%",
                I18nKey = "menu.%RESOURCE%",
                IsExternal = false,
                IsCache = true,
                IsVisible = true,
                IsAffix = false,
                Status = EnableStatus.Enabled,
                Sort = 999,
                Remark = "%DISPLAY%"
            }
        });

        Logger.LogInformation("初始化 %DISPLAY% 菜单");
    }
}
""";
}
