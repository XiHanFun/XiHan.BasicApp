// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.RegularExpressions;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 内置模板渲染测试。
/// </summary>
/// <remarks>
/// 用程序集里嵌入的真实内置模板渲染，钉住几条跨模板的行为约定：
/// 报文可空性跟列本身走（非空列留空时发缺省值，而不是 null 整单 400 或落库撞 NOT NULL）；
/// 树表写接口挡住自环、挂到下级与删带子节点的父级；删除不存在的记录如实报错；
/// 主子表页面的样式只用语义令牌（生成页不在 px 登记表里）。
/// 产物能否编译、能否过前端类型检查与门禁，由探针在真实工程里验证，这里只锁住容易回退的关键片段。
/// </remarks>
public sealed partial class CodeGenBuiltInTemplateTests
{
    private readonly ScribanTemplateRenderer _renderer = new();

    /// <summary>
    /// 树表 AppService 在新增与更新时校验父级、删除时拦下带子节点的父级。
    /// </summary>
    [Fact]
    public async Task TreeAppService_ShouldGuardParentChainAndChildren()
    {
        var content = await RenderAsync("Backend/AppService.sbn", TreeContext(nullableParent: true));

        Assert.Contains("await EnsureValidParentAsync(input.ParentId, null, cancellationToken);", content, StringComparison.Ordinal);
        Assert.Contains("await EnsureValidParentAsync(input.ParentId, entity.BasicId, cancellationToken);", content, StringComparison.Ordinal);
        Assert.Contains("_repository.AnyAsync(entity => entity.ParentId == id, cancellationToken)", content, StringComparison.Ordinal);
        Assert.Contains("产品分类存在子节点，不能直接删除。", content, StringComparison.Ordinal);
        Assert.Contains("产品分类不能选择自身作为父级。", content, StringComparison.Ordinal);
        Assert.Contains("产品分类不能挂到自己的下级下面。", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 单表 AppService 不产树校验；更新与删除找不到记录时如实报错，不静默成功。
    /// 表注释只进普通字符串字面量：注释里的花括号进插值串会被当成插值而编译不过。
    /// </summary>
    [Fact]
    public async Task SingleAppService_ShouldReportMissingRecordWithoutInterpolatingComment()
    {
        var context = SingleContext();
        context.TableComment = "产品{表}";

        var content = await RenderAsync("Backend/AppService.sbn", context);

        Assert.DoesNotContain("EnsureValidParentAsync", content, StringComparison.Ordinal);
        Assert.Contains("?? throw new InvalidOperationException(\"产品{表}不存在。\");", content, StringComparison.Ordinal);
        Assert.Contains("if (!await _repository.DeleteByIdAsync(id, cancellationToken))", content, StringComparison.Ordinal);
        Assert.DoesNotContain("$\"", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 前端 DTO 类型的可空性只跟列本身走：非空列即便没勾必填也不是可选字段。
    /// </summary>
    [Fact]
    public async Task Types_ShouldFollowColumnNullabilityNotRequiredFlag()
    {
        var content = await RenderAsync("Frontend/Types.sbn", SingleContext());

        Assert.Contains("  stock: number\n", content, StringComparison.Ordinal);
        Assert.Contains("  remark?: string | null\n", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 单表页面：非空列留空发缺省值、可空文本清空发 null、必填下拉判 null、整数列拦小数。
    /// </summary>
    [Fact]
    public async Task Page_ShouldSubmitWireValuesThatTheBackendAccepts()
    {
        var content = await RenderAsync("Frontend/Page.sbn", SingleContext());

        Assert.Contains("stock: form.value.stock ?? 0,", content, StringComparison.Ordinal);
        Assert.Contains("remark: form.value.remark || null,", content, StringComparison.Ordinal);
        Assert.Contains("if (form.value.level == null) {", content, StringComparison.Ordinal);
        Assert.Contains("toast.warning('请选择级别')", content, StringComparison.Ordinal);
        Assert.Contains("if (form.value.stock != null && !Number.isInteger(form.value.stock)) {", content, StringComparison.Ordinal);
        Assert.Contains("toast.warning('库存只能填整数')", content, StringComparison.Ordinal);
        Assert.DoesNotContain(":min=\"0\"", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 主子表页面的样式只用语义令牌：生成页不在 px 登记表里，写裸 px 过不了前端门禁。
    /// </summary>
    [Fact]
    public async Task MasterDetailPage_StyleShouldUseTokensOnly()
    {
        var context = SingleContext();
        context.DetailTables =
        [
            new RelatedTableRef
            {
                TableId = 2,
                TableName = "sys_product_sku",
                TableComment = "产品规格",
                ClassName = "SysProductSku",
                ClassNameCamel = "sysProductSku",
                ClassNameKebab = "sys-product-sku",
                ModuleName = "Catalog",
                Namespace = "XiHan.BasicApp.Catalog",
                ForeignKeyColumn = "ProductId",
                ForeignKeyProperty = "ProductId",
                Columns = [Column("ProductId", "long", "string", isRequired: true), Column("SkuName", "string", "string", isRequired: true)]
            }
        ];

        var content = await RenderAsync("Frontend/Page.sbn", context);
        var style = content[content.IndexOf("<style scoped>", StringComparison.Ordinal)..];

        Assert.DoesNotMatch(BarePxRegex(), style);
        Assert.Contains("var(--xh-space-3)", style, StringComparison.Ordinal);
    }

    /// <summary>
    /// 非空父级以 0 表示根节点：不选时提交 '0'，回填时把 0 还原成「未选」。
    /// </summary>
    [Fact]
    public async Task TreePage_NotNullParentShouldUseZeroAsRoot()
    {
        var content = await RenderAsync("Frontend/TreePage.sbn", TreeContext(nullableParent: false));

        Assert.Contains("parentId: form.value.parentId || '0',", content, StringComparison.Ordinal);
        Assert.Contains("parentId: src.parentId && src.parentId !== '0' ? src.parentId : null,", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 可空父级不选即 null，回填原样。
    /// </summary>
    [Fact]
    public async Task TreePage_NullableParentShouldUseNullAsRoot()
    {
        var content = await RenderAsync("Frontend/TreePage.sbn", TreeContext(nullableParent: true));

        Assert.Contains("parentId: form.value.parentId || null,", content, StringComparison.Ordinal);
        Assert.Contains("parentId: src.parentId ?? null,", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 勾了导出、导入：页面元信息带两个按钮码，新增列进导入模板（必填随表单），
    /// 不进列表的新增列以隐藏字段进导入，常量下拉带选项供导入按文本反查。
    /// </summary>
    [Fact]
    public async Task Schema_ImportAndExportShouldFollowEnabledActions()
    {
        var context = ImportContext();

        var content = await RenderAsync("Frontend/Schema.sbn", context);

        Assert.Contains("  exportPermission: 'catalog.sys-product.export',\n", content, StringComparison.Ordinal);
        Assert.Contains("  importPermission: 'catalog.sys-product.import',\n", content, StringComparison.Ordinal);
        Assert.Matches(@"key: 'productName', [^\n]* importable: true, required: true, minWidth", content);
        Assert.Matches(@"key: 'remark', [^\n]* importable: true, minWidth", content);
        Assert.Matches(@"key: 'level', title: '级别', dataType: 'enum', options: \[[^\n]* importable: true, required: true, minWidth", content);
        Assert.Matches(@"key: 'internalNote', [^\n]* visible: false, importable: true, minWidth", content);
    }

    /// <summary>
    /// 导入记录换算成新增入参：必填列原样取、没填的列按表单留空同口径补齐、日期归一成本地时间文本。
    /// </summary>
    [Fact]
    public async Task Schema_ImportRecordShouldMapToCreateInput()
    {
        var content = await RenderAsync("Frontend/Schema.sbn", ImportContext());

        Assert.Contains("export function toCreateInputFromImport(record: Record<string, unknown>): SysProductCreateDto {", content, StringComparison.Ordinal);
        Assert.Contains("    productName: record.productName as SysProductCreateDto['productName'],\n", content, StringComparison.Ordinal);
        Assert.Contains("    stock: (record.stock as SysProductCreateDto['stock'] | undefined) ?? 0,\n", content, StringComparison.Ordinal);
        Assert.Contains("    remark: (record.remark as SysProductCreateDto['remark'] | undefined) ?? null,\n", content, StringComparison.Ordinal);
        Assert.Contains("    publishDate: toImportDateText(record.publishDate, false),\n", content, StringComparison.Ordinal);
        Assert.Contains("    endTime: record.endTime == null ? null : toImportDateText(record.endTime, true),\n", content, StringComparison.Ordinal);
        Assert.Contains("function toImportDateText(value: unknown, withTime: boolean) {", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 没勾导出、导入：不声明两个按钮码、不标导入字段，但查询入参与导入换算照常产出，之后勾选时页面只需补一行。
    /// </summary>
    [Fact]
    public async Task Schema_WithoutImportAndExportShouldNotDeclareButtons()
    {
        var context = ImportContext();
        context.EnabledActions = [CodeGenActions.Create, CodeGenActions.Update, CodeGenActions.Delete];

        var content = await RenderAsync("Frontend/Schema.sbn", context);

        Assert.DoesNotContain("exportPermission: ", content, StringComparison.Ordinal);
        Assert.DoesNotContain("importPermission: ", content, StringComparison.Ordinal);
        Assert.DoesNotContain("importable", content, StringComparison.Ordinal);
        Assert.DoesNotContain("key: 'internalNote'", content, StringComparison.Ordinal);
        Assert.Contains("export function buildPageQuery(params: SchemaQueryParams): SysProductPageQueryDto {", content, StringComparison.Ordinal);
        Assert.Contains("export function toCreateInputFromImport(", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 页面：列表取数与导出中心共用 schema 里的查询入参，导入逐行走新增接口。
    /// </summary>
    [Fact]
    public async Task Page_ShouldWireImportAndExportResource()
    {
        var content = await RenderAsync("Frontend/Page.sbn", ImportContext());

        Assert.Contains("    page: params => sysProductApi.page(buildPageQuery(params)) as unknown as Promise<PageResult<Record<string, unknown>>>,\n", content, StringComparison.Ordinal);
        Assert.Contains("    create: record => sysProductApi.create(toCreateInputFromImport(record)),\n", content, StringComparison.Ordinal);
        Assert.Contains("    export: { businessType: pageMeta.pageCode, buildQuery: buildPageQuery },\n", content, StringComparison.Ordinal);
        Assert.Contains("import { actions, buildPageQuery, createDefaultForm, fields, pageMeta, toCreateInputFromImport } from './sys-product.schema'", content, StringComparison.Ordinal);
        Assert.DoesNotContain("createPageRequest", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 页面：没勾导出、导入时 resource 不接这两项。
    /// </summary>
    [Fact]
    public async Task Page_WithoutImportAndExportShouldNotWireThem()
    {
        var content = await RenderAsync("Frontend/Page.sbn", SingleContext());

        Assert.DoesNotContain("create: record", content, StringComparison.Ordinal);
        Assert.DoesNotContain("export: {", content, StringComparison.Ordinal);
        Assert.DoesNotContain("toCreateInputFromImport", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 手动 schema 把查询入参与导入换算转出，页面只从手动 schema 导入。
    /// </summary>
    [Fact]
    public async Task ManualSchema_ShouldReExportQueryAndImportMapping()
    {
        var content = await RenderAsync("Frontend/Schema.Manual.sbn", ImportContext());

        Assert.Contains("export { buildPageQuery, toCreateInputFromImport } from './sys-product.schema.generated'\n", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 勾了导出才产出导出 Provider：业务类型即页面码、校验导出权限、复用查询服务的分页。
    /// </summary>
    [Fact]
    public async Task ExportProvider_ShouldBindPageCodeAndExportPermission()
    {
        var content = await RenderAsync("Backend/ExportProvider.sbn", ImportContext());

        Assert.Contains("[ExposeServices(typeof(IExportProvider))]", content, StringComparison.Ordinal);
        Assert.Contains("public sealed class SysProductExportProvider", content, StringComparison.Ordinal);
        Assert.Contains("QueryServiceExportProviderBase<SysProductPageQueryDto, SysProductListItemDto>, IScopedDependency", content, StringComparison.Ordinal);
        Assert.Contains("public override string BusinessType => \"catalog.sys-product\";", content, StringComparison.Ordinal);
        Assert.Contains("public override string RequiredPermission => SysProductPermissionCodes.Export;", content, StringComparison.Ordinal);
        Assert.Contains("return _queryService.GetSysProductPageAsync(query, cancellationToken);", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 没勾导出时文件照常覆盖但不含类：取消导出后旧 Provider 不会留着引用已不存在的导出权限码。
    /// </summary>
    [Fact]
    public async Task ExportProvider_WithoutExportShouldRenderNoClass()
    {
        var content = await RenderAsync("Backend/ExportProvider.sbn", SingleContext());

        Assert.DoesNotContain("class ", content, StringComparison.Ordinal);
        Assert.Contains("没有勾选「导出」", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 树表导入：父级不按必填导入，留空即根节点（非空父级发 '0'）。
    /// </summary>
    [Fact]
    public async Task TreeSchema_ImportBlankParentShouldBeRoot()
    {
        var context = TreeContext(nullableParent: false);
        context.EnabledActions = CodeGenActions.Defaults;

        var content = await RenderAsync("Frontend/TreeSchema.sbn", context);

        Assert.Contains("    parentId: (record.parentId as SysCategoryCreateDto['parentId'] | undefined) || '0',\n", content, StringComparison.Ordinal);
        Assert.Matches(@"key: 'parentId', [^\n]* visible: false, importable: true, minWidth", content);
        Assert.Matches(@"key: 'categoryName', [^\n]* treeColumn: true, importable: true, required: true, minWidth", content);
        Assert.Contains("export function buildPageQuery(params: SchemaQueryParams): SysCategoryPageQueryDto {", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 字典下拉接系统字典选项通道，上传列出文件引用上传控件；不再有「选项来源未接通」的占位项。
    /// </summary>
    [Fact]
    public async Task Page_DictSelectAndUploadShouldBeWired()
    {
        var content = await RenderAsync("Frontend/Page.sbn", DictUploadContext());

        Assert.Contains("const customerLevelOptions = useDictOptions('demo_customer_level')\n", content, StringComparison.Ordinal);
        Assert.Contains("import { toast, useDictOptions } from '~/composables'\n", content, StringComparison.Ordinal);
        Assert.Contains("import XFileRefUpload from '@/components/FileRefUpload.vue'\n", content, StringComparison.Ordinal);
        Assert.Contains("<XFileRefUpload v-model:value=\"form.avatar\" kind=\"image\" />", content, StringComparison.Ordinal);
        Assert.Contains("<XFileRefUpload v-model:value=\"form.attachment\" kind=\"file\" />", content, StringComparison.Ordinal);
        Assert.DoesNotContain("选项来源未接通", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 列表字段：字典列声明 dictCode（显示名称、搜索下拉、导入按名称反查），上传列按 image / file 渲染。
    /// </summary>
    [Fact]
    public async Task Schema_DictAndUploadFieldsShouldDeclareTheirSource()
    {
        var content = await RenderAsync("Frontend/Schema.sbn", DictUploadContext());

        Assert.Contains("{ key: 'customerLevel', title: '客户等级', dataType: 'enum', dictCode: 'demo_customer_level',", content, StringComparison.Ordinal);
        Assert.Contains("{ key: 'avatar', title: '头像', dataType: 'image',", content, StringComparison.Ordinal);
        Assert.Contains("{ key: 'attachment', title: '附件', dataType: 'file',", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 树表页面同样接通字典与上传。
    /// </summary>
    [Fact]
    public async Task TreePage_DictSelectAndUploadShouldBeWired()
    {
        var context = TreeContext(nullableParent: true);
        context.Columns = [.. context.Columns, .. DictUploadColumns()];

        var content = await RenderAsync("Frontend/TreePage.sbn", context);

        Assert.Contains("const customerLevelOptions = useDictOptions('demo_customer_level')\n", content, StringComparison.Ordinal);
        Assert.Contains("import XFileRefUpload from '@/components/FileRefUpload.vue'\n", content, StringComparison.Ordinal);
        Assert.Contains("<XFileRefUpload v-model:value=\"form.avatar\" kind=\"image\" />", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 仓储按关联实体查「主键 + 显示列（+ 上级）」，走跨实体查询入口，租户与软删过滤照常生效。
    /// </summary>
    [Fact]
    public async Task Repository_RelationOptionsShouldQueryTargetEntity()
    {
        var context = RelationContext();

        var repository = await RenderAsync("Backend/Repository.sbn", context);
        var contract = await RenderAsync("Backend/IRepository.sbn", context);

        Assert.Contains("using XiHan.BasicApp.Core.Dtos;\n", repository, StringComparison.Ordinal);
        Assert.Contains("CreateQueryable<XiHan.BasicApp.Catalog.Domain.Entities.SysCategory>()", repository, StringComparison.Ordinal);
        Assert.Contains(".Select(target => new { target.BasicId, target.CategoryName })", repository, StringComparison.Ordinal);
        Assert.Contains("CreateQueryable<XiHan.BasicApp.Saas.Domain.Entities.SysDepartment>()", repository, StringComparison.Ordinal);
        Assert.Contains(".Select(target => new { target.BasicId, target.DepartmentName, target.ParentId })", repository, StringComparison.Ordinal);
        Assert.Contains("ParentValue = row.ParentId", repository, StringComparison.Ordinal);
        Assert.Contains("Task<IReadOnlyList<RelationOptionDto>> GetCategoryIdOptionsAsync(CancellationToken cancellationToken = default);", contract, StringComparison.Ordinal);
    }

    /// <summary>
    /// 查询服务按本表的查看权限暴露选项接口，契约同步声明。
    /// </summary>
    [Fact]
    public async Task QueryService_RelationOptionsShouldUseReadPermission()
    {
        var context = RelationContext();

        var service = await RenderAsync("Backend/QueryService.sbn", context);
        var contracts = await RenderAsync("Backend/Contracts.sbn", context);

        Assert.Matches(@"\[PermissionAuthorize\(SysProductPermissionCodes\.Read\)\]\n\s+\[HttpGet\]\n\s+public virtual Task<IReadOnlyList<RelationOptionDto>> GetSysProductCategoryIdOptionsAsync\(", service);
        Assert.Contains("return _repository.GetDepartmentIdOptionsAsync(cancellationToken);", service, StringComparison.Ordinal);
        Assert.Contains("Task<IReadOnlyList<RelationOptionDto>> GetSysProductDepartmentIdOptionsAsync(CancellationToken cancellationToken = default);", contracts, StringComparison.Ordinal);
    }

    /// <summary>
    /// 没有关联列时仓储保持空壳，不引入用不上的 using。
    /// </summary>
    [Fact]
    public async Task Repository_WithoutRelationsShouldStayEmpty()
    {
        var repository = await RenderAsync("Backend/Repository.sbn", SingleContext());

        Assert.DoesNotContain("XiHan.BasicApp.Core.Dtos", repository, StringComparison.Ordinal);
        Assert.Contains("ISysProductRepository\n{\n}", repository, StringComparison.Ordinal);
    }

    /// <summary>
    /// 前端：接口出选项方法，列表字段按选项显示名称（不可排序），表单出下拉与树形下拉。
    /// </summary>
    [Fact]
    public async Task Frontend_RelationShouldWireOptionsIntoFieldsAndForm()
    {
        var context = RelationContext();

        var api = await RenderAsync("Frontend/Api.sbn", context);
        var schema = await RenderAsync("Frontend/Schema.sbn", context);
        var page = await RenderAsync("Frontend/Page.sbn", context);

        Assert.Contains("import type { ApiId, RelationOptionDto } from '../../types'", api, StringComparison.Ordinal);
        Assert.Contains("return sysProductQueryApi.get<RelationOptionDto[]>('SysProductCategoryIdOptions')", api, StringComparison.Ordinal);

        Assert.Contains("import { sysProductApi } from '@/api/modules/catalog/sys-product'\n", schema, StringComparison.Ordinal);
        Assert.Contains("{ key: 'categoryId', title: '所属分类', dataType: 'enum', optionsLoader: sysProductApi.categoryIdOptions, searchable: true,", schema, StringComparison.Ordinal);

        Assert.Contains("const categoryIdOptions = useAsyncOptions(sysProductApi.categoryIdOptions)\n", page, StringComparison.Ordinal);
        Assert.Contains("const departmentIdTreeOptions = computed(() => relationOptionsToTree(departmentIdRelation.value))\n", page, StringComparison.Ordinal);
        Assert.Contains("<XTreeSelect v-model:value=\"form.departmentId\" clearable :options=\"departmentIdTreeOptions\"", page, StringComparison.Ordinal);
        Assert.Contains("import { toast, useAsyncOptions } from '~/composables'\n", page, StringComparison.Ordinal);
        Assert.Contains("import { relationOptionsToTree } from '~/utils'\n", page, StringComparison.Ordinal);
        Assert.Contains(", XSelect, XTreeSelect } from '~/components'", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// 唯一列：实体出租户内唯一索引（带 IsDeleted），新增与更新前查重，更新时排除自身。
    /// </summary>
    [Fact]
    public async Task Backend_UniqueColumnShouldGetIndexAndDuplicateCheck()
    {
        var context = AllActionsContext();

        var entity = await RenderAsync("Backend/Entity.sbn", context);
        var service = await RenderAsync("Backend/AppService.sbn", context);

        Assert.Contains("[SugarIndex(\"UX_{table}_TeId_ProductCode_IsDe\", nameof(TenantId), OrderByType.Asc, nameof(ProductCode), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]", entity, StringComparison.Ordinal);
        Assert.DoesNotContain("UX_{table}_TeId_Cover_IsDe", entity, StringComparison.Ordinal);
        Assert.Contains("await EnsureProductCodeUniqueAsync(input.ProductCode, null, cancellationToken);", service, StringComparison.Ordinal);
        Assert.Contains("await EnsureProductCodeUniqueAsync(input.ProductCode, entity.BasicId, cancellationToken);", service, StringComparison.Ordinal);
        Assert.Contains("protected virtual async Task EnsureProductCodeUniqueAsync(string value, long? currentId, CancellationToken cancellationToken)", service, StringComparison.Ordinal);
        Assert.Contains("if (string.IsNullOrWhiteSpace(value))", service, StringComparison.Ordinal);
        Assert.Contains("entity => entity.ProductCode == value && entity.BasicId != id", service, StringComparison.Ordinal);
        Assert.Contains("throw new InvalidOperationException(\"产品编码「\" + value + \"」已存在。\");", service, StringComparison.Ordinal);
    }

    /// <summary>
    /// 状态切换：DTO、契约与命令服务按状态列生成，要状态权限；前端出按钮码、行内启停、批量启停与接口方法。
    /// </summary>
    [Fact]
    public async Task StatusToggleShouldBeWiredAcrossLayers()
    {
        var context = AllActionsContext();

        var dtos = await RenderAsync("Backend/Dtos.sbn", context);
        var service = await RenderAsync("Backend/AppService.sbn", context);
        var contracts = await RenderAsync("Backend/Contracts.sbn", context);
        var api = await RenderAsync("Frontend/Api.sbn", context);
        var types = await RenderAsync("Frontend/Types.sbn", context);
        var schema = await RenderAsync("Frontend/Schema.sbn", context);
        var page = await RenderAsync("Frontend/Page.sbn", context);

        Assert.Contains("public sealed partial class SysProductStatusUpdateDto : BasicAppDto", dtos, StringComparison.Ordinal);
        Assert.Contains("public XiHan.BasicApp.Saas.Domain.Enums.EnableStatus Status { get; set; }", dtos, StringComparison.Ordinal);
        Assert.Matches(@"\[PermissionAuthorize\(SysProductPermissionCodes\.Status\)\]\n\s+public virtual async Task<SysProductDetailDto> UpdateSysProductStatusAsync\(SysProductStatusUpdateDto input", service);
        Assert.Contains("if (!Enum.IsDefined(input.Status))", service, StringComparison.Ordinal);
        Assert.Contains("entity.Status = input.Status;", service, StringComparison.Ordinal);
        Assert.Contains("Task<SysProductDetailDto> UpdateSysProductStatusAsync(SysProductStatusUpdateDto input", contracts, StringComparison.Ordinal);
        Assert.Contains("return sysProductCommandApi.put<SysProductDetailDto, SysProductStatusUpdateDto>('SysProductStatus', input)", api, StringComparison.Ordinal);
        Assert.Contains("export interface SysProductStatusUpdateDto extends BasicDto {\n  status: string\n}", types, StringComparison.Ordinal);
        Assert.Contains("  statusPermission: 'catalog.sys-product.status',\n", schema, StringComparison.Ordinal);
        Assert.Contains("{ key: 'toggle', title: '启用/停用', scope: 'row', icon: 'lucide:power', permission: 'catalog.sys-product.status', confirm: true,", schema, StringComparison.Ordinal);
        Assert.Contains("updateStatus: (id, enabled) => sysProductApi.updateStatus({ basicId: id, status: enabled ? 'Enabled' : 'Disabled' }),", page, StringComparison.Ordinal);
        Assert.Contains("const enable = row.status !== 'Enabled'", page, StringComparison.Ordinal);
        Assert.Contains("    case 'toggle':\n", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// 没勾状态切换时：命令服务不出状态接口，页面不接启停。
    /// </summary>
    [Fact]
    public async Task StatusShouldStayOffUntilEnabled()
    {
        var context = AllActionsContext();
        context.EnabledActions = CodeGenActions.Defaults;
        context.StatusColumn = null;

        var service = await RenderAsync("Backend/AppService.sbn", context);
        var schema = await RenderAsync("Frontend/Schema.sbn", context);
        var page = await RenderAsync("Frontend/Page.sbn", context);

        Assert.DoesNotContain("UpdateSysProductStatusAsync", service, StringComparison.Ordinal);
        Assert.DoesNotContain("statusPermission: '", schema, StringComparison.Ordinal);
        Assert.DoesNotContain("handleToggleStatus", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// 从程序集嵌入资源读取内置模板
    /// </summary>
    private static string LoadTemplate(string resourceFile)
    {
        var suffix = $".Templates.{resourceFile.Replace('/', '.')}";
        var assembly = CodeGenerationTestHelper.ModuleAssembly;
        var resourceName = assembly.GetManifestResourceNames().Single(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private async Task<string> RenderAsync(string resourceFile, CodeGenerationContext context)
        => (await _renderer.RenderAsync(LoadTemplate(resourceFile), context)).Replace("\r\n", "\n", StringComparison.Ordinal);

    private static ColumnSchema Column(string property, string csharpType, string tsType, HtmlType htmlType = HtmlType.Input, bool isNullable = false, bool isRequired = false)
    {
        var column = CodeGenerationTestHelper.CreateColumn(property, csharpType, tsType, htmlType: htmlType);
        column.IsNullable = isNullable;
        column.IsRequired = isRequired;
        return column;
    }

    private static CodeGenerationContext SingleContext()
    {
        var level = Column("Level", "int", "number", HtmlType.Select, isRequired: true);
        level.ColumnComment = "级别";
        level.DictSelectorType = DictSelectorType.ConstSelector;
        level.ConstValues = """[{"label":"低","value":1},{"label":"高","value":2}]""";
        var stock = Column("Stock", "int", "number", HtmlType.InputNumber);
        stock.ColumnComment = "库存";
        return CodeGenerationTestHelper.CreateContext(columns:
        [
            Column("BasicId", "long", "string"),
            Column("ProductName", "string", "string", isRequired: true),
            Column("Remark", "string?", "string", HtmlType.Textarea, isNullable: true),
            level,
            stock
        ]);
    }

    /// <summary>
    /// 全开操作的单表：在基础列之外补一个不进列表的新增列、一个必填日期与一个可空日期时间
    /// </summary>
    private static CodeGenerationContext ImportContext()
    {
        var context = SingleContext();
        var internalNote = Column("InternalNote", "string?", "string", isNullable: true);
        internalNote.IsList = false;
        var publishDate = Column("PublishDate", "DateTimeOffset", "string", HtmlType.DatePicker, isRequired: true);
        var endTime = Column("EndTime", "DateTimeOffset?", "string", HtmlType.DateTimePicker, isNullable: true);
        context.Columns = [.. context.Columns, internalNote, publishDate, endTime];
        context.EnabledActions = CodeGenActions.Defaults;
        return context;
    }

    /// <summary>
    /// 单表：主键 + 字典下拉 + 图片与附件上传
    /// </summary>
    private static CodeGenerationContext DictUploadContext()
        => CodeGenerationTestHelper.CreateContext(columns: [Column("BasicId", "long", "string"), .. DictUploadColumns()]);

    private static ColumnSchema[] DictUploadColumns()
    {
        var level = Column("CustomerLevel", "string?", "string", HtmlType.Select, isNullable: true);
        level.ColumnComment = "客户等级";
        level.DictSelectorType = DictSelectorType.DictSelector;
        level.DictCode = "demo_customer_level";
        var avatar = Column("Avatar", "string?", "string", HtmlType.ImageUpload, isNullable: true);
        avatar.ColumnComment = "头像";
        var attachment = Column("Attachment", "string?", "string", HtmlType.FileUpload, isNullable: true);
        attachment.ColumnComment = "附件";
        return [level, avatar, attachment];
    }

    /// <summary>
    /// 全开操作的单表：一个必填、参与查询的关联表列（产品分类），一个可空的关联树列（系统部门）
    /// </summary>
    private static CodeGenerationContext RelationContext()
    {
        var category = Column("CategoryId", "long", "string", HtmlType.Select, isRequired: true);
        category.ColumnComment = "所属分类";
        category.IsQuery = true;
        category.DictSelectorType = DictSelectorType.TableSelector;
        category.Relation = new RelationTarget
        {
            TableId = 2,
            TableName = "sys_category",
            TableComment = "产品分类",
            ClassName = "SysCategory",
            EntityTypeQualified = "XiHan.BasicApp.Catalog.Domain.Entities.SysCategory",
            LabelProperty = "CategoryName"
        };
        var department = Column("DepartmentId", "long?", "string", HtmlType.TreeSelect, isNullable: true);
        department.ColumnComment = "所属部门";
        department.DictSelectorType = DictSelectorType.TreeSelector;
        department.Relation = new RelationTarget
        {
            TableId = 3,
            TableName = "Sys_Department",
            TableComment = "部门",
            ClassName = "SysDepartment",
            EntityTypeQualified = "XiHan.BasicApp.Saas.Domain.Entities.SysDepartment",
            LabelProperty = "DepartmentName",
            ParentProperty = "ParentId",
            IsTree = true
        };
        var context = CodeGenerationTestHelper.CreateContext(columns: [Column("BasicId", "long", "string"), category, department]);
        context.EnabledActions = CodeGenActions.Defaults;
        return context;
    }

    /// <summary>
    /// 全部操作的单表：唯一的产品编码、EnableStatus 状态列（状态切换用）、图片列
    /// </summary>
    private static CodeGenerationContext AllActionsContext()
    {
        var code = Column("ProductCode", "string", "string", isRequired: true);
        code.ColumnComment = "产品编码";
        code.IsUnique = true;
        var status = Column("Status", "EnableStatus", "string", HtmlType.Select, isRequired: true);
        status.ColumnComment = "状态";
        status.DictSelectorType = DictSelectorType.EnumSelector;
        status.EnumTypeName = "EnableStatus";
        status.EnumTypeShortName = "EnableStatus";
        status.EnumNamespace = "XiHan.BasicApp.Saas.Domain.Enums";
        status.EnumDefaultMember = "Disabled";
        var cover = Column("Cover", "string?", "string", HtmlType.ImageUpload, isNullable: true);
        cover.ColumnComment = "封面";
        var context = CodeGenerationTestHelper.CreateContext(columns: [Column("BasicId", "long", "string"), code, status, cover]);
        context.EnabledActions = CodeGenActions.All;
        context.StatusColumn = status;
        return context;
    }

    private static CodeGenerationContext TreeContext(bool nullableParent)
    {
        var parent = Column("ParentId", nullableParent ? "long?" : "long", "string", isNullable: nullableParent, isRequired: !nullableParent);
        var name = Column("CategoryName", "string", "string", isRequired: true);
        var context = CodeGenerationTestHelper.CreateContext(
            tableName: "sys_category",
            className: "SysCategory",
            templateType: TemplateType.Tree,
            columns: [Column("BasicId", "long", "string"), parent, name]);
        context.TableComment = "产品分类";
        context.TreeParentColumn = parent;
        context.TreeNameColumn = name;
        return context;
    }

    [GeneratedRegex(@"\d+px")]
    private static partial Regex BarePxRegex();
}
