// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 代码生成引擎的管线编排测试。
/// </summary>
/// <remarks>
/// 引擎是"配置 → 上下文 → 渲染 → 产物"的唯一编排者，本文件守住三类行为：
/// <list type="number">
/// <item>结构字段 fail-closed：树表的父级/显示名列、主子表的主表/外键列解析不出来时必须返回失败结果，
/// 而不是静默降级——降级后模板会渲染出引用了不存在属性的代码，问题要到编译期才暴露。</item>
/// <item>渲染异常不冒泡：冒泡会绕过调用方的历史留痕，且异常里不带模板身份，用户只看到一句"渲染失败"。</item>
/// <item>二阶产物只在含后端的生成范围产出；权限码唯一性预检 fail-open，读权限库出错不得挡住生成。</item>
/// </list>
/// 依赖全部为 Moq / 内存假实现，既不连库也不落盘。
/// </remarks>
public sealed class CodeGenEngineOrchestrationTests
{
    private const long TableId = 1;

    private readonly Mock<ICodeGenTableRepository> _tableRepository = new();
    private readonly Mock<ICodeGenTableColumnRepository> _columnRepository = new();
    private readonly Mock<ICodeGenTemplateRepository> _templateRepository = new();
    private readonly Mock<ITemplateRendererResolver> _rendererResolver = new();
    private readonly Mock<IEnumTypeCatalog> _enumTypeCatalog = new();
    private readonly Mock<IGeneratedArtifactPackager> _packager = new();
    private readonly Mock<IGeneratedArtifactWriter> _artifactWriter = new();
    private readonly Mock<IPermissionRepository> _permissionRepository = new();
    private readonly RecordingRenderer _renderer = new();

    /// <summary>
    /// 预置各依赖的安全默认返回，避免用例被无关的空引用干扰。
    /// </summary>
    public CodeGenEngineOrchestrationTests()
    {
        _rendererResolver.Setup(resolver => resolver.Resolve(It.IsAny<TemplateEngine>())).Returns(_renderer);
        _columnRepository
            .Setup(repository => repository.GetByTableIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _tableRepository
            .Setup(repository => repository.GetByMasterTableIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _templateRepository
            .Setup(repository => repository.GetEnabledByTypeAsync(It.IsAny<TemplateType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _templateRepository
            .Setup(repository => repository.GetByCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _permissionRepository
            .Setup(repository => repository.GetByCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    /// <summary>
    /// 构造被测引擎（类型映射用真实实现）。
    /// </summary>
    private CodeGenerationEngine CreateEngine()
    {
        return new CodeGenerationEngine(
            _tableRepository.Object,
            _columnRepository.Object,
            _templateRepository.Object,
            _rendererResolver.Object,
            new DefaultTypeMappingProvider(),
            _enumTypeCatalog.Object,
            _packager.Object,
            _artifactWriter.Object,
            _permissionRepository.Object,
            NullLogger<CodeGenerationEngine>.Instance);
    }

    /// <summary>
    /// 构造一条表配置。
    /// </summary>
    /// <param name="id">主键</param>
    /// <param name="templateType">模板类型</param>
    /// <param name="scope">生成范围</param>
    /// <param name="enabledActions">包含操作</param>
    /// <param name="genPath">生成路径</param>
    /// <param name="treeParentColumn">树表父级列</param>
    /// <param name="treeNameColumn">树表显示名列</param>
    /// <param name="masterTableId">主表主键</param>
    /// <param name="masterForeignKey">指向主表的外键列</param>
    /// <param name="primaryKeyColumn">主键列名</param>
    private static SysCodeGenTable Table(
        long id = TableId,
        TemplateType templateType = TemplateType.Single,
        GenerationScope scope = GenerationScope.All,
        string? enabledActions = null,
        string? genPath = null,
        string? treeParentColumn = null,
        string? treeNameColumn = null,
        long? masterTableId = null,
        string? masterForeignKey = null,
        string? primaryKeyColumn = "Basic_Id")
    {
        return CodeGenerationTestHelper.WithId(
            new SysCodeGenTable
            {
                TableName = "sys_product",
                TableComment = "产品表",
                ClassName = "SysProduct",
                Namespace = "XiHan.BasicApp.Catalog",
                ModuleName = "Catalog",
                BusinessName = "产品",
                FunctionName = "产品",
                Author = "tester",
                TemplateType = templateType,
                GenerationScope = scope,
                EnabledActions = enabledActions,
                GenPath = genPath,
                TreeParentColumn = treeParentColumn,
                TreeNameColumn = treeNameColumn,
                MasterTableId = masterTableId,
                MasterForeignKey = masterForeignKey,
                PrimaryKeyColumn = primaryKeyColumn,
                DatabaseType = DatabaseType.MySql
            },
            id);
    }

    /// <summary>
    /// 构造一条列配置。
    /// </summary>
    /// <param name="columnName">列名</param>
    /// <param name="csharpType">C# 类型</param>
    /// <param name="tsType">TS 类型</param>
    /// <param name="columnType">DB 类型</param>
    /// <param name="isPrimaryKey">是否主键</param>
    /// <param name="isNullable">是否可空</param>
    /// <param name="dictSelectorType">字典选择器类型</param>
    /// <param name="enumTypeName">枚举类型全名</param>
    private static SysCodeGenTableColumn Column(
        string columnName,
        string? csharpType = "string",
        string? tsType = "string",
        string? columnType = "varchar",
        bool isPrimaryKey = false,
        bool isNullable = false,
        DictSelectorType? dictSelectorType = null,
        string? enumTypeName = null)
    {
        return new SysCodeGenTableColumn
        {
            TableId = TableId,
            ColumnName = columnName,
            CSharpProperty = columnName,
            CSharpType = csharpType,
            TsType = tsType,
            ColumnType = columnType,
            IsPrimaryKey = isPrimaryKey,
            IsNullable = isNullable,
            DictSelectorType = dictSelectorType,
            EnumTypeName = enumTypeName
        };
    }

    /// <summary>
    /// 构造一条模板配置。
    /// </summary>
    /// <param name="code">模板编码</param>
    /// <param name="name">模板名称</param>
    /// <param name="group">模板分组</param>
    /// <param name="content">模板内容</param>
    /// <param name="fileNameExpression">文件名表达式</param>
    /// <param name="filePathExpression">路径表达式</param>
    /// <param name="fileExtension">扩展名</param>
    /// <param name="writeMode">写入策略</param>
    private static SysCodeGenTemplate Template(
        string code = "backend.entity",
        string name = "后端实体",
        string? group = "backend-crud",
        string? content = "ENTITY",
        string? fileNameExpression = null,
        string? filePathExpression = null,
        string? fileExtension = ".cs",
        ArtifactWriteMode writeMode = ArtifactWriteMode.AlwaysOverwrite)
    {
        return new SysCodeGenTemplate
        {
            TemplateCode = code,
            TemplateName = name,
            TemplateGroup = group,
            TemplateContent = content,
            FileNameExpression = fileNameExpression,
            FilePathExpression = filePathExpression,
            FileExtension = fileExtension,
            WriteMode = writeMode,
            TemplateEngine = TemplateEngine.Scriban
        };
    }

    /// <summary>
    /// 挂上表配置。
    /// </summary>
    /// <param name="table">表配置</param>
    private void GivenTable(SysCodeGenTable table)
    {
        _tableRepository
            .Setup(repository => repository.GetByIdAsync(table.BasicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
    }

    /// <summary>
    /// 挂上指定表的列配置。
    /// </summary>
    /// <param name="tableId">表主键</param>
    /// <param name="columns">列配置</param>
    private void GivenColumns(long tableId, params SysCodeGenTableColumn[] columns)
    {
        _columnRepository
            .Setup(repository => repository.GetByTableIdAsync(tableId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(columns);
    }

    /// <summary>
    /// 挂上按模板类型取到的模板集。
    /// </summary>
    /// <param name="templates">模板集</param>
    private void GivenTemplates(params SysCodeGenTemplate[] templates)
    {
        _templateRepository
            .Setup(repository => repository.GetEnabledByTypeAsync(It.IsAny<TemplateType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);
    }

    /// <summary>
    /// 生成请求为空必须直接拒绝。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_NullRequestShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateEngine().PreviewAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateEngine().GenerateAsync(null!));
    }

    /// <summary>
    /// 表配置不存在时返回失败结果并带上主键，便于定位。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_MissingTableShouldFailWithTableId()
    {
        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = 99 });

        Assert.False(result.Success);
        Assert.Contains("99", result.Message!, StringComparison.Ordinal);
        Assert.Empty(result.Artifacts);
    }

    /// <summary>
    /// 一个可用模板都没有时返回失败结果，而不是产出只有二阶产物的空包。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_NoTemplateShouldFail()
    {
        GivenTable(Table());

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.False(result.Success);
        Assert.Contains("未找到可用模板", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 正常路径：模板产物之外必须追加六个菜单/权限二阶产物。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_ShouldAppendSixMenuPermissionArtifacts()
    {
        GivenTable(Table());
        GivenTemplates(Template());

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.True(result.Success);
        Assert.Equal(7, result.Artifacts.Count);
        var secondOrder = result.Artifacts
            .Where(artifact => artifact.TemplateCode == CodeGenerationTestHelper.ArtifactTemplateCode)
            .Select(artifact => artifact.FileName)
            .ToList();
        Assert.Equal(
            [
                "SysProductPermissionCodes.cs",
                "README.md",
                "SysProductPermissionDefinitions.cs",
                "SysProductPageRegistry.snippet.txt",
                "SysProductPermissionSeeder.cs",
                "SysProductMenuSeeder.cs"
            ],
            secondOrder);
    }

    /// <summary>
    /// 纯前端生成不产出二阶产物：后端与菜单权限已在别处到位。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_FrontendOnlyScopeShouldSkipSecondOrderArtifacts()
    {
        GivenTable(Table(scope: GenerationScope.FrontendOnly));
        GivenTemplates(Template(code: "frontend.api", group: "frontend-crud"));

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.True(result.Success);
        Assert.Single(result.Artifacts);
        Assert.DoesNotContain(
            result.Artifacts,
            artifact => artifact.TemplateCode == CodeGenerationTestHelper.ArtifactTemplateCode);
    }

    /// <summary>
    /// 生成范围按模板分组前缀裁剪模板集。
    /// </summary>
    /// <param name="scope">生成范围</param>
    /// <param name="expectedTemplateCode">期望保留的模板编码</param>
    [Theory]
    [InlineData(GenerationScope.BackendOnly, "backend.entity")]
    [InlineData(GenerationScope.FrontendOnly, "frontend.api")]
    public async Task PreviewAsync_ScopeShouldFilterTemplatesByGroupPrefix(GenerationScope scope, string expectedTemplateCode)
    {
        GivenTable(Table(scope: scope));
        GivenTemplates(
            Template(code: "backend.entity", group: "backend-crud"),
            Template(code: "frontend.api", group: "frontend-crud"));

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        var templateArtifacts = result.Artifacts
            .Where(artifact => artifact.TemplateCode != CodeGenerationTestHelper.ArtifactTemplateCode)
            .ToList();
        Assert.Single(templateArtifacts);
        Assert.Equal(expectedTemplateCode, templateArtifacts[0].TemplateCode, StringComparer.Ordinal);
    }

    /// <summary>
    /// 全量范围不裁剪模板，分组为空的模板也保留。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_AllScopeShouldKeepEveryTemplateIncludingUngrouped()
    {
        GivenTable(Table());
        GivenTemplates(Template(code: "a", group: null), Template(code: "b", group: "frontend-crud"));

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.Equal(8, result.Artifacts.Count);
    }

    /// <summary>
    /// 裁剪后模板集为空同样返回"未找到可用模板"。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_ScopeFilteringOutEveryTemplateShouldFail()
    {
        GivenTable(Table(scope: GenerationScope.FrontendOnly));
        GivenTemplates(Template(code: "backend.entity", group: "backend-crud"));

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.False(result.Success);
        Assert.Contains("未找到可用模板", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 指定模板编码时按编码取模板，不再按模板类型取。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_ExplicitTemplateCodesShouldBypassTypeLookup()
    {
        GivenTable(Table());
        _templateRepository
            .Setup(repository => repository.GetByCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Template(code: "custom.one")]);

        var result = await CreateEngine().PreviewAsync(new GenerationRequest
        {
            TableId = TableId,
            TemplateCodes = ["custom.one"]
        });

        Assert.True(result.Success);
        _templateRepository.Verify(
            repository => repository.GetEnabledByTypeAsync(It.IsAny<TemplateType>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 产物的模板编码与写入策略必须原样取自模板配置。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_ArtifactShouldCarryTemplateIdentityAndWriteMode()
    {
        GivenTable(Table());
        GivenTemplates(Template(code: "backend.entity.manual", writeMode: ArtifactWriteMode.WriteOnce));

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        var artifact = result.Artifacts[0];
        Assert.Equal("backend.entity.manual", artifact.TemplateCode, StringComparer.Ordinal);
        Assert.Equal(ArtifactWriteMode.WriteOnce, artifact.WriteMode);
        Assert.Equal("ENTITY", artifact.Content, StringComparer.Ordinal);
    }

    /// <summary>
    /// 未配置文件名表达式时回退"类名 + 扩展名"，扩展名缺点号会自动补上。
    /// </summary>
    /// <param name="fileExtension">模板扩展名配置</param>
    /// <param name="expected">期望文件名</param>
    [Theory]
    [InlineData(".cs", "SysProduct.cs")]
    [InlineData("cs", "SysProduct.cs")]
    [InlineData("ts", "SysProduct.ts")]
    [InlineData(null, "SysProduct.cs")]
    [InlineData("   ", "SysProduct.cs")]
    public async Task PreviewAsync_FileNameShouldFallBackToClassNamePlusExtension(string? fileExtension, string expected)
    {
        GivenTable(Table());
        GivenTemplates(Template(fileExtension: fileExtension));

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.Equal(expected, result.Artifacts[0].FileName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 文件名与路径表达式渲染成功时拼成相对路径，路径分隔符统一为正斜杠且不留尾斜杠。
    /// </summary>
    /// <param name="filePathExpression">路径表达式渲染结果</param>
    /// <param name="expected">期望相对路径</param>
    [Theory]
    [InlineData("Domain/Entities", "Domain/Entities/SysProduct.Generated.cs")]
    [InlineData("Domain\\Entities\\", "Domain/Entities/SysProduct.Generated.cs")]
    [InlineData("  Domain/Entities  ", "Domain/Entities/SysProduct.Generated.cs")]
    public async Task PreviewAsync_RelativePathShouldCombineDirectoryAndFileName(string filePathExpression, string expected)
    {
        GivenTable(Table());
        GivenTemplates(Template(fileNameExpression: "SysProduct.Generated.cs", filePathExpression: filePathExpression));

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.Equal(expected, result.Artifacts[0].RelativePath, StringComparer.Ordinal);
    }

    /// <summary>
    /// 未配置路径表达式时相对路径就是文件名，不得凭空生成目录。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_BlankFilePathExpressionShouldProduceFlatRelativePath()
    {
        GivenTable(Table());
        GivenTemplates(Template(fileNameExpression: "A.cs", filePathExpression: "   "));

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.Equal("A.cs", result.Artifacts[0].RelativePath, StringComparer.Ordinal);
    }

    /// <summary>
    /// 模板正文渲染失败时返回失败结果，并把模板编码与名称带进消息里。
    /// </summary>
    /// <remarks>异常冒泡会绕过调用方的历史留痕，用户只能看到一句没有模板身份的"渲染失败"。</remarks>
    [Fact]
    public async Task PreviewAsync_TemplateRenderFailureShouldReturnFailureWithTemplateIdentity()
    {
        GivenTable(Table());
        GivenTemplates(Template(code: "backend.entity", name: "后端实体", content: "BOOM"));
        _renderer.OnRender = (source, _) => source == "BOOM"
            ? throw new InvalidOperationException("语法错误")
            : source;

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.False(result.Success);
        Assert.Contains("backend.entity", result.Message!, StringComparison.Ordinal);
        Assert.Contains("后端实体", result.Message!, StringComparison.Ordinal);
        Assert.Contains("语法错误", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 文件名表达式渲染失败只回退默认命名，不得让整次生成失败。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_FileNameExpressionFailureShouldFallBackInsteadOfFailing()
    {
        GivenTable(Table());
        GivenTemplates(Template(fileNameExpression: "BAD_NAME", filePathExpression: "BAD_PATH"));
        _renderer.OnRender = (source, _) => source.StartsWith("BAD", StringComparison.Ordinal)
            ? throw new InvalidOperationException("表达式错误")
            : source;

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.True(result.Success);
        Assert.Equal("SysProduct.cs", result.Artifacts[0].FileName, StringComparer.Ordinal);
        Assert.Equal("SysProduct.cs", result.Artifacts[0].RelativePath, StringComparer.Ordinal);
    }

    /// <summary>
    /// 文件名表达式渲染出空白时同样回退默认命名。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_BlankRenderedFileNameShouldFallBack()
    {
        GivenTable(Table());
        GivenTemplates(Template(fileNameExpression: "   "));

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.Equal("SysProduct.cs", result.Artifacts[0].FileName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 包含操作未配置时归一化为写操作全集，二阶产物随之给出全部权限码。
    /// </summary>
    /// <param name="enabledActions">表配置的包含操作</param>
    /// <param name="expectedActions">期望生效的写操作</param>
    [Theory]
    [InlineData(null, "create,update,delete")]
    [InlineData("", "create,update,delete")]
    [InlineData("   ", "create,update,delete")]
    [InlineData("create", "create")]
    [InlineData("delete,create", "create,delete")]
    [InlineData("CREATE, Update ", "create,update")]
    [InlineData("approve", "")]
    public async Task PreviewAsync_EnabledActionsShouldBeNormalizedIntoContext(string? enabledActions, string expectedActions)
    {
        GivenTable(Table(enabledActions: enabledActions));
        GivenTemplates(Template());

        await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        var expected = expectedActions.Split(',', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(expected, _renderer.LastContext!.EnabledActions);
    }

    /// <summary>
    /// 归一化结果直接决定二阶产物里的权限码集合。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_NormalizedActionsShouldDrivePermissionCodeArtifact()
    {
        GivenTable(Table(enabledActions: "delete"));
        GivenTemplates(Template());

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        var permissionCodes = result.Artifacts.Single(artifact => artifact.FileName == "SysProductPermissionCodes.cs");
        Assert.Contains("sys_product:delete", permissionCodes.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("sys_product:create", permissionCodes.Content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 树表未配置父级列必须失败，并指明缺的是哪一项。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_TreeWithoutParentColumnShouldFail()
    {
        GivenTable(Table(templateType: TemplateType.Tree));
        GivenTemplates(Template());

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.False(result.Success);
        Assert.Contains("TreeParentColumn", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 树表配置的父级列不在列配置中必须失败，提示重新同步表结构。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_TreeParentColumnNotInColumnsShouldFail()
    {
        GivenTable(Table(templateType: TemplateType.Tree, treeParentColumn: "parent_id", treeNameColumn: "name"));
        GivenColumns(TableId, Column("name"));
        GivenTemplates(Template());

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.False(result.Success);
        Assert.Contains("parent_id", result.Message!, StringComparison.Ordinal);
        Assert.Contains("同步表结构", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 树表未配置显示名列必须失败。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_TreeWithoutNameColumnShouldFail()
    {
        GivenTable(Table(templateType: TemplateType.Tree, treeParentColumn: "parent_id"));
        GivenColumns(TableId, Column("parent_id"));
        GivenTemplates(Template());

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.False(result.Success);
        Assert.Contains("TreeNameColumn", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 树表配置的显示名列不在列配置中必须失败。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_TreeNameColumnNotInColumnsShouldFail()
    {
        GivenTable(Table(templateType: TemplateType.Tree, treeParentColumn: "parent_id", treeNameColumn: "title"));
        GivenColumns(TableId, Column("parent_id"));
        GivenTemplates(Template());

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.False(result.Success);
        Assert.Contains("title", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 树表结构列解析成功后，上下文里必须挂上强类型的父级列与显示名列（列名比对大小写不敏感）。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_TreeColumnsShouldBeResolvedCaseInsensitively()
    {
        GivenTable(Table(templateType: TemplateType.Tree, treeParentColumn: "PARENT_ID", treeNameColumn: "Name"));
        GivenColumns(TableId, Column("parent_id"), Column("name"));
        GivenTemplates(Template());

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.True(result.Success);
        Assert.Equal("parent_id", _renderer.LastContext!.TreeParentColumn!.ColumnName, StringComparer.Ordinal);
        Assert.Equal("name", _renderer.LastContext.TreeNameColumn!.ColumnName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 主子表未配置主表必须失败。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_MasterDetailWithoutMasterTableShouldFail()
    {
        GivenTable(Table(templateType: TemplateType.MasterDetail));
        GivenTemplates(Template());

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.False(result.Success);
        Assert.Contains("MasterTableId", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 主子表未配置外键列必须失败。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_MasterDetailWithoutForeignKeyShouldFail()
    {
        GivenTable(Table(templateType: TemplateType.MasterDetail, masterTableId: 2));
        GivenTemplates(Template());

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.False(result.Success);
        Assert.Contains("MasterForeignKey", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 主子表的外键列不在列配置中必须失败。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_MasterDetailForeignKeyNotInColumnsShouldFail()
    {
        GivenTable(Table(templateType: TemplateType.MasterDetail, masterTableId: 2, masterForeignKey: "order_id"));
        GivenColumns(TableId, Column("name"));
        GivenTemplates(Template());

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.False(result.Success);
        Assert.Contains("order_id", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 配置的主表不存在必须失败，提示重新选择主表。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_MissingMasterTableShouldFail()
    {
        GivenTable(Table(templateType: TemplateType.MasterDetail, masterTableId: 2, masterForeignKey: "order_id"));
        GivenColumns(TableId, Column("order_id"));
        GivenTemplates(Template());

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.False(result.Success);
        Assert.Contains("重新选择主表", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 主表解析成功后，上下文中的主表引用必须带上外键列与三种命名形态。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_ResolvedMasterTableShouldExposeForeignKeyAndNamings()
    {
        var master = CodeGenerationTestHelper.WithId(
            new SysCodeGenTable { TableName = "sys_order", ClassName = "SysOrder", ModuleName = "Sale" },
            2);
        GivenTable(Table(templateType: TemplateType.MasterDetail, masterTableId: 2, masterForeignKey: "order_id"));
        GivenColumns(TableId, Column("order_id", csharpType: "long"));
        _tableRepository.Setup(repository => repository.GetByIdAsync(2L, It.IsAny<CancellationToken>())).ReturnsAsync(master);
        _columnRepository.Setup(repository => repository.GetByTableIdAsync(2L, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        GivenTemplates(Template());

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.True(result.Success);
        var masterRef = _renderer.LastContext!.MasterTable!;
        Assert.Equal("SysOrder", masterRef.ClassName, StringComparer.Ordinal);
        Assert.Equal("sysOrder", masterRef.ClassNameCamel, StringComparer.Ordinal);
        Assert.Equal("sys-order", masterRef.ClassNameKebab, StringComparer.Ordinal);
        Assert.Equal("order_id", masterRef.ForeignKeyColumn, StringComparer.Ordinal);
    }

    /// <summary>
    /// 以本表为主表的子表被反查出来，外键列缺失的子表被跳过而不是让主表整体失败。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_DetailTableWithoutForeignKeyShouldBeSkippedNotFail()
    {
        var goodDetail = CodeGenerationTestHelper.WithId(
            new SysCodeGenTable { TableName = "sys_order_item", ClassName = "SysOrderItem", MasterForeignKey = "order_id" },
            10);
        var badDetail = CodeGenerationTestHelper.WithId(
            new SysCodeGenTable { TableName = "sys_order_log", ClassName = "SysOrderLog", MasterForeignKey = "order_id" },
            11);
        GivenTable(Table());
        _tableRepository
            .Setup(repository => repository.GetByMasterTableIdAsync(TableId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([goodDetail, badDetail]);
        _columnRepository
            .Setup(repository => repository.GetByTableIdAsync(10L, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Column("order_id", csharpType: "long")]);
        _columnRepository
            .Setup(repository => repository.GetByTableIdAsync(11L, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Column("other")]);
        GivenTemplates(Template());

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.True(result.Success);
        var detail = Assert.Single(_renderer.LastContext!.DetailTables);
        Assert.Equal("SysOrderItem", detail.ClassName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 主键列优先取标了主键的列；没有主键列时按表配置的主键列名匹配。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_PrimaryKeyShouldPreferFlaggedColumnThenConfiguredName()
    {
        GivenTable(Table(primaryKeyColumn: "code"));
        GivenColumns(TableId, Column("code"), Column("id", isPrimaryKey: true));
        GivenTemplates(Template());

        await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });
        Assert.Equal("id", _renderer.LastContext!.PrimaryKey!.ColumnName, StringComparer.Ordinal);

        GivenColumns(TableId, Column("code"));
        await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });
        Assert.Equal("code", _renderer.LastContext!.PrimaryKey!.ColumnName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 列配置没填 C#/TS 类型时按 DB 类型回落到类型映射器，不能把空串带进模板。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_BlankColumnTypesShouldFallBackToTypeMapping()
    {
        GivenTable(Table());
        GivenColumns(TableId, Column("amount", csharpType: null, tsType: "  ", columnType: "decimal", isNullable: true));
        GivenTemplates(Template());

        await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        var column = _renderer.LastContext!.Columns[0];
        Assert.Equal("decimal?", column.CSharpType, StringComparer.Ordinal);
        Assert.Equal("number", column.TsType, StringComparer.Ordinal);
    }

    /// <summary>
    /// 枚举列在"持久化的 C# 类型 == 解析出的枚举短名"时才补齐枚举事实。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_EnumColumnShouldBeEnrichedWhenDeclaredTypeMatches()
    {
        var facts = new EnumTypeFacts("EnableStatus", "XiHan.BasicApp.Saas.Domain.Enums", "Disabled");
        _enumTypeCatalog.Setup(catalog => catalog.TryResolve("EnableStatus", out facts)).Returns(true);
        GivenTable(Table());
        GivenColumns(TableId, Column(
            "status",
            csharpType: "EnableStatus?",
            dictSelectorType: DictSelectorType.EnumSelector,
            enumTypeName: "EnableStatus"));
        GivenTemplates(Template());

        await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        var column = _renderer.LastContext!.Columns[0];
        Assert.Equal("EnableStatus", column.EnumTypeShortName, StringComparer.Ordinal);
        Assert.Equal("XiHan.BasicApp.Saas.Domain.Enums", column.EnumNamespace, StringComparer.Ordinal);
        Assert.Equal("Disabled", column.EnumDefaultMember, StringComparer.Ordinal);
    }

    /// <summary>
    /// 旧表配置里枚举列的 C# 类型仍是 int 时不补枚举事实，只降级不阻断。
    /// </summary>
    /// <remarks>
    /// 这条判据保证契约变更只发生在用户主动重新同步之后，已上线的表不会被动改。
    /// </remarks>
    [Fact]
    public async Task PreviewAsync_LegacyIntEnumColumnShouldNotBeEnriched()
    {
        var facts = new EnumTypeFacts("EnableStatus", "XiHan.BasicApp.Saas.Domain.Enums", "Disabled");
        _enumTypeCatalog.Setup(catalog => catalog.TryResolve("EnableStatus", out facts)).Returns(true);
        GivenTable(Table());
        GivenColumns(TableId, Column(
            "status",
            csharpType: "int",
            dictSelectorType: DictSelectorType.EnumSelector,
            enumTypeName: "EnableStatus"));
        GivenTemplates(Template());

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.True(result.Success);
        Assert.Null(_renderer.LastContext!.Columns[0].EnumTypeShortName);
    }

    /// <summary>
    /// 字典选择器列当前没有选项通道，只告警不阻断，也不会被当成枚举列处理。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_DictSelectorColumnShouldNotBlockGeneration()
    {
        GivenTable(Table());
        GivenColumns(TableId, Column("status", dictSelectorType: DictSelectorType.DictSelector));
        GivenTemplates(Template());

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.True(result.Success);
        Assert.Null(_renderer.LastContext!.Columns[0].EnumTypeShortName);
    }

    /// <summary>
    /// 上下文的扩展选项必须带上结构字段，供模板与二阶产物读取。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_ContextOptionsShouldCarryStructuralConfiguration()
    {
        var table = Table(primaryKeyColumn: "Basic_Id", masterTableId: 2, masterForeignKey: "order_id");
        table.ParentMenuId = 801;
        GivenTable(table);
        GivenTemplates(Template());

        await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        var options = _renderer.LastContext!.Options;
        Assert.Equal("Basic_Id", options["PrimaryKeyColumn"]);
        Assert.Equal("2", options["MasterTableId"]);
        Assert.Equal("order_id", options["MasterForeignKey"]);
        Assert.Equal("801", options["ParentMenuId"]);
    }

    /// <summary>
    /// 权限码唯一性预检 fail-open：读权限库抛异常不得挡住生成。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_PermissionLookupFailureShouldNotBlockGeneration()
    {
        _permissionRepository
            .Setup(repository => repository.GetByCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("权限库不可用"));
        GivenTable(Table());
        GivenTemplates(Template());

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.True(result.Success);
        var readme = result.Artifacts.Single(artifact => artifact.FileName == "README.md");
        Assert.DoesNotContain("权限码冲突", readme.Content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 预检查到已存在的权限码时必须写进 README 的冲突告警。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_CollidingPermissionCodesShouldSurfaceInReadme()
    {
        _permissionRepository
            .Setup(repository => repository.GetByCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new SysPermission { PermissionCode = "sys_product:read" }]);
        GivenTable(Table());
        GivenTemplates(Template());

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        var readme = result.Artifacts.Single(artifact => artifact.FileName == "README.md");
        Assert.Contains("权限码冲突", readme.Content, StringComparison.Ordinal);
        Assert.Contains("`sys_product:read`", readme.Content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 预览路径不打包也不落盘。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_ShouldNeitherPackNorWrite()
    {
        GivenTable(Table());
        GivenTemplates(Template());

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId, GenType = GenType.Zip });

        Assert.Null(result.Package);
        _packager.Verify(
            packager => packager.PackAsync(It.IsAny<IEnumerable<GeneratedArtifact>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _artifactWriter.Verify(
            writer => writer.WriteAsync(It.IsAny<IReadOnlyList<GeneratedArtifact>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Zip 方式生成时把产物交给打包器，并把字节流挂到结果上。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ZipShouldAttachPackageBytes()
    {
        GivenTable(Table());
        GivenTemplates(Template());
        _packager
            .Setup(packager => packager.PackAsync(It.IsAny<IEnumerable<GeneratedArtifact>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([1, 2, 3]);

        var result = await CreateEngine().GenerateAsync(new GenerationRequest { TableId = TableId, GenType = GenType.Zip });

        Assert.True(result.Success);
        Assert.Equal([1, 2, 3], result.Package);
    }

    /// <summary>
    /// 落盘方式生成时按表配置的生成路径写入，并回填写入数与跳过清单。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CustomPathShouldWriteToConfiguredGenPath()
    {
        GivenTable(Table(genPath: "D:/out"));
        GivenTemplates(Template());
        _artifactWriter
            .Setup(writer => writer.WriteAsync(It.IsAny<IReadOnlyList<GeneratedArtifact>>(), "D:/out", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GeneratedArtifactWriteResult.Ok(5, 1, ["Domain/Entities/SysProduct.cs"]));

        var result = await CreateEngine().GenerateAsync(new GenerationRequest
        {
            TableId = TableId,
            GenType = GenType.CustomPath
        });

        Assert.True(result.Success);
        Assert.Equal(5, result.WrittenCount);
        Assert.Equal(["Domain/Entities/SysProduct.cs"], result.SkippedPaths);
    }

    /// <summary>
    /// 落盘被安全策略拒绝时整次生成返回失败，并原样带出拒绝原因。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CustomPathWriteFailureShouldFailWholeGeneration()
    {
        GivenTable(Table(genPath: "D:/out"));
        GivenTemplates(Template());
        _artifactWriter
            .Setup(writer => writer.WriteAsync(It.IsAny<IReadOnlyList<GeneratedArtifact>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GeneratedArtifactWriteResult.Fail("生成路径不在白名单内：D:/out"));

        var result = await CreateEngine().GenerateAsync(new GenerationRequest
        {
            TableId = TableId,
            GenType = GenType.CustomPath
        });

        Assert.False(result.Success);
        Assert.Contains("不在白名单内", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 渲染阶段就失败时不得再进入打包/落盘分支。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_FailedRenderShouldSkipPackagingAndWriting()
    {
        var result = await CreateEngine().GenerateAsync(new GenerationRequest { TableId = 404, GenType = GenType.Zip });

        Assert.False(result.Success);
        _packager.Verify(
            packager => packager.PackAsync(It.IsAny<IEnumerable<GeneratedArtifact>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 令牌已取消时必须在读表配置前抛出。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_CancelledTokenShouldThrowBeforeRepository()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId }, cts.Token));

        _tableRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 渲染器抛出的取消异常必须原样冒泡，不得被"渲染失败"包住。
    /// </summary>
    /// <remarks>包住会让取消看起来像模板语法错误，并在历史里留下一条假失败。</remarks>
    [Fact]
    public async Task PreviewAsync_RendererCancellationShouldPropagate()
    {
        GivenTable(Table());
        GivenTemplates(Template());
        _renderer.OnRender = (_, _) => throw new OperationCanceledException();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId }));
    }

    /// <summary>
    /// 成功结果必须带上耗时统计。
    /// </summary>
    [Fact]
    public async Task PreviewAsync_SuccessResultShouldCarryDuration()
    {
        GivenTable(Table());
        GivenTemplates(Template());

        var result = await CreateEngine().PreviewAsync(new GenerationRequest { TableId = TableId });

        Assert.True(result.DurationMilliseconds >= 0);
    }

    /// <summary>
    /// 记录调用的假模板渲染器：默认原样回显模板源码，可按需改写或抛异常。
    /// </summary>
    private sealed class RecordingRenderer : ITemplateRenderer
    {
        /// <summary>渲染委托（为空表示原样回显）</summary>
        public Func<string, CodeGenerationContext, string>? OnRender { get; set; }

        /// <summary>最近一次渲染收到的上下文</summary>
        public CodeGenerationContext? LastContext { get; private set; }

        /// <inheritdoc />
        public TemplateEngine Engine => TemplateEngine.Scriban;

        /// <inheritdoc />
        public Task<string> RenderAsync(string templateSource, CodeGenerationContext context, CancellationToken cancellationToken = default)
        {
            LastContext = context;
            return Task.FromResult(OnRender is null ? templateSource : OnRender(templateSource, context));
        }

        /// <inheritdoc />
        public TemplateRenderValidation Validate(string templateSource) => TemplateRenderValidation.Valid();
    }
}
