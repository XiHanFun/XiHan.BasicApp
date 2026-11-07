// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Entities;

namespace XiHan.BasicApp.CodeGeneration.Seeds;

/// <summary>
/// 一份内置模板
/// </summary>
/// <param name="Code">编码</param>
/// <param name="Name">名称</param>
/// <param name="Group">分组</param>
/// <param name="Resource">嵌入的模板文件（<c>Seeds/Templates</c> 下的相对路径）</param>
/// <param name="FileName">文件名表达式</param>
/// <param name="FilePath">目录表达式</param>
/// <param name="Extension">扩展名</param>
/// <param name="WriteMode">写入方式</param>
/// <param name="TemplateType">适用的生成模式</param>
public sealed record BuiltInTemplate(string Code, string Name, string Group, string Resource, string FileName, string FilePath, string Extension, CodeGenWriteMode WriteMode, CodeGenTemplateType TemplateType = CodeGenTemplateType.Universal);

/// <summary>
/// 内置模板的目录
/// </summary>
/// <remarks>
/// 用户已定的写入方式（照 4.x 成对）：每次覆盖的是实体的生成部分、请求与响应和映射、查询白名单；只首次写的是实体的手写部分、各个操作与查询的手写部分
/// 分组 <c>backend-crud</c> / <c>frontend-crud</c> 两种目标都出，<c>backend-package</c> / <c>frontend-package</c> 只在新建包时出（包级文件只首次）
/// 路径相对那一侧的根目录（<c>backend-next</c> 或 <c>frontend-next</c>）；按勾选的操作一份模板一个文件，没勾的渲染为空、不产出
/// 前端照 4.x 按生成模式分单表、主从与树表三组（单表与主从共用一份模板），页面与 schema 的手写部分只首次、schema 的生成部分每次覆盖
/// </remarks>
public static class BuiltInTemplates
{
    /// <summary>
    /// 两种目标都出的后端模板分组
    /// </summary>
    public const string BackendCrud = "backend-crud";

    /// <summary>
    /// 只在新建包时出的后端包级模板分组
    /// </summary>
    public const string BackendPackage = "backend-package";

    /// <summary>
    /// 两种目标都出的前端模板分组
    /// </summary>
    public const string FrontendCrud = "frontend-crud";

    /// <summary>
    /// 只在新建包时出的前端包级模板分组
    /// </summary>
    public const string FrontendPackage = "frontend-package";

    private const string PackageDir = "src/{{ Namespace }}";
    private const string EntityDir = PackageDir + "/Domain/Entities";
    private const string OperationDir = PackageDir + "/Operations/{{ ResourcePascal }}";
    private const string QueryDir = PackageDir + "/Queries";
    private const string ModuleDir = "src/modules/{{ ModuleCode }}";
    private const string ViewDir = ModuleDir + "/views/{{ Resource }}";
    private const string SchemaFile = "{{ Resource }}.schema.generated.ts";
    private const string SchemaManualFile = "{{ Resource }}.schema.ts";

    /// <summary>
    /// 全部内置模板，按排序
    /// </summary>
    public static IReadOnlyList<BuiltInTemplate> All { get; } =
    [
        new("backend.entity", "后端实体", BackendCrud, "Backend/Entity.sbn", "{{ ClassName }}.Generated.cs", EntityDir, ".cs", CodeGenWriteMode.AlwaysOverwrite),
        new("backend.entity.manual", "后端实体（自定义）", BackendCrud, "Backend/Entity.Manual.sbn", "{{ ClassName }}.cs", EntityDir, ".cs", CodeGenWriteMode.WriteOnce),
        new("backend.contracts", "后端请求与响应", BackendCrud, "Backend/Contracts.sbn", "{{ ClassName }}Contracts.Generated.cs", OperationDir, ".cs", CodeGenWriteMode.AlwaysOverwrite),
        new("backend.contracts.manual", "后端请求与响应（自定义）", BackendCrud, "Backend/Contracts.Manual.sbn", "{{ ClassName }}Contracts.cs", OperationDir, ".cs", CodeGenWriteMode.WriteOnce),
        new("backend.queries", "后端列表查询", BackendCrud, "Backend/Queries.sbn", "{{ ClassName }}Queries.Generated.cs", QueryDir, ".cs", CodeGenWriteMode.AlwaysOverwrite),
        new("backend.queries.manual", "后端列表查询（自定义）", BackendCrud, "Backend/Queries.Manual.sbn", "{{ ClassName }}Queries.cs", QueryDir, ".cs", CodeGenWriteMode.WriteOnce),
        new("backend.operation.list", "后端操作：列表", BackendCrud, "Backend/OperationList.sbn", "List{{ ResourcePascal }}.cs", OperationDir, ".cs", CodeGenWriteMode.WriteOnce),
        new("backend.operation.get", "后端操作：详情", BackendCrud, "Backend/OperationGet.sbn", "Get{{ ClassName }}.cs", OperationDir, ".cs", CodeGenWriteMode.WriteOnce),
        new("backend.operation.create", "后端操作：新建", BackendCrud, "Backend/OperationCreate.sbn", "Create{{ ClassName }}.cs", OperationDir, ".cs", CodeGenWriteMode.WriteOnce),
        new("backend.operation.update", "后端操作：修改", BackendCrud, "Backend/OperationUpdate.sbn", "Update{{ ClassName }}.cs", OperationDir, ".cs", CodeGenWriteMode.WriteOnce),
        new("backend.operation.delete", "后端操作：删除", BackendCrud, "Backend/OperationDelete.sbn", "Delete{{ ResourcePascal }}.cs", OperationDir, ".cs", CodeGenWriteMode.WriteOnce),
        new("backend.operation.set-status", "后端操作：启停", BackendCrud, "Backend/OperationSetStatus.sbn", "Set{{ ClassName }}Status.cs", OperationDir, ".cs", CodeGenWriteMode.WriteOnce),
        new("backend.operation.tree", "后端操作：树", BackendCrud, "Backend/OperationTree.sbn", "Get{{ ClassName }}Tree.cs", OperationDir, ".cs", CodeGenWriteMode.WriteOnce, CodeGenTemplateType.Tree),
        new("backend.operation.options", "后端操作：关联选项", BackendCrud, "Backend/OperationOptions.sbn", "{{ ClassName }}OptionOperations.cs", OperationDir, ".cs", CodeGenWriteMode.WriteOnce),
        new("backend.print-data-source", "后端打印数据源", BackendCrud, "Backend/PrintDataSource.sbn", "{{ ClassName }}PrintDataSource.Generated.cs", OperationDir, ".cs", CodeGenWriteMode.AlwaysOverwrite),
        new("backend.package.project", "后端包：工程文件", BackendPackage, "Backend/Package/Project.sbn", "{{ Namespace }}.csproj", PackageDir, ".csproj", CodeGenWriteMode.WriteOnce),
        new("backend.package.feature", "后端包：特性", BackendPackage, "Backend/Package/Feature.sbn", "{{ ModuleName }}Feature.cs", PackageDir, ".cs", CodeGenWriteMode.WriteOnce),
        new("backend.package.capabilities", "后端包：能力", BackendPackage, "Backend/Package/Capabilities.sbn", "{{ ModuleName }}Capabilities.cs", PackageDir, ".cs", CodeGenWriteMode.WriteOnce),
        new("backend.package.permissions", "后端包：权限码", BackendPackage, "Backend/Package/Permissions.sbn", "{{ ModuleName }}Permissions.cs", PackageDir, ".cs", CodeGenWriteMode.WriteOnce),
        new("backend.package.pages", "后端包：页面登记", BackendPackage, "Backend/Package/Pages.sbn", "{{ ModuleName }}Pages.cs", PackageDir, ".cs", CodeGenWriteMode.WriteOnce),
        new("backend.package.errors", "后端包：错误码", BackendPackage, "Backend/Package/Errors.sbn", "{{ ModuleName }}Errors.cs", PackageDir, ".cs", CodeGenWriteMode.WriteOnce),
        new("backend.package.messages", "后端包：消息键", BackendPackage, "Backend/Package/Messages.sbn", "{{ ModuleName }}Messages.cs", PackageDir, ".cs", CodeGenWriteMode.WriteOnce),
        new("backend.package.json", "后端包：JSON 序列化", BackendPackage, "Backend/Package/JsonContext.sbn", "{{ ModuleName }}JsonContext.cs", PackageDir, ".cs", CodeGenWriteMode.WriteOnce),
        new("backend.package.readme", "后端包：说明", BackendPackage, "Backend/Package/Readme.sbn", "README.md", PackageDir, ".md", CodeGenWriteMode.WriteOnce),
        new("frontend.schema", "前端页面 schema（单表）", FrontendCrud, "Frontend/Schema.sbn", SchemaFile, ViewDir, ".ts", CodeGenWriteMode.AlwaysOverwrite, CodeGenTemplateType.Single),
        new("frontend.schema.manual", "前端页面 schema（单表·自定义）", FrontendCrud, "Frontend/Schema.Manual.sbn", SchemaManualFile, ViewDir, ".ts", CodeGenWriteMode.WriteOnce, CodeGenTemplateType.Single),
        new("frontend.page", "前端列表页（单表）", FrontendCrud, "Frontend/Page.sbn", "index.vue", ViewDir, ".vue", CodeGenWriteMode.WriteOnce, CodeGenTemplateType.Single),
        new("frontend.schema.masterdetail", "前端页面 schema（主从）", FrontendCrud, "Frontend/Schema.sbn", SchemaFile, ViewDir, ".ts", CodeGenWriteMode.AlwaysOverwrite, CodeGenTemplateType.MasterDetail),
        new("frontend.schema.masterdetail.manual", "前端页面 schema（主从·自定义）", FrontendCrud, "Frontend/Schema.Manual.sbn", SchemaManualFile, ViewDir, ".ts", CodeGenWriteMode.WriteOnce, CodeGenTemplateType.MasterDetail),
        new("frontend.page.masterdetail", "前端列表页（主从）", FrontendCrud, "Frontend/Page.sbn", "index.vue", ViewDir, ".vue", CodeGenWriteMode.WriteOnce, CodeGenTemplateType.MasterDetail),
        new("frontend.schema.tree", "前端页面 schema（树表）", FrontendCrud, "Frontend/TreeSchema.sbn", SchemaFile, ViewDir, ".ts", CodeGenWriteMode.AlwaysOverwrite, CodeGenTemplateType.Tree),
        new("frontend.schema.tree.manual", "前端页面 schema（树表·自定义）", FrontendCrud, "Frontend/TreeSchema.Manual.sbn", SchemaManualFile, ViewDir, ".ts", CodeGenWriteMode.WriteOnce, CodeGenTemplateType.Tree),
        new("frontend.page.tree", "前端列表页（树表）", FrontendCrud, "Frontend/TreePage.sbn", "index.vue", ViewDir, ".vue", CodeGenWriteMode.WriteOnce, CodeGenTemplateType.Tree),
        new("frontend.package.setup", "前端包：启动钩子", FrontendPackage, "Frontend/Package/Setup.sbn", "setup.ts", ModuleDir, ".ts", CodeGenWriteMode.WriteOnce),
    ];
}
