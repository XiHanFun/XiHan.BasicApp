// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Entities;

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 产物属于哪一侧。
/// </summary>
public enum CodeGenSide
{
    /// <summary>
    /// 后端：相对 <c>backend-next</c>。
    /// </summary>
    Backend = 0,

    /// <summary>
    /// 前端：相对 <c>frontend-next</c>。
    /// </summary>
    Frontend = 1,

    /// <summary>
    /// 登记片段：只在产物包里，不写进项目。
    /// </summary>
    Registration = 2,
}

/// <summary>
/// 一个生成的文件。
/// </summary>
/// <param name="Side">属于哪一侧。</param>
/// <param name="RelativePath">相对那一侧根目录的路径（正斜杠）。</param>
/// <param name="Content">内容。</param>
/// <param name="TemplateCode">来源模板编码；引擎拼出的为空。</param>
/// <param name="WriteMode">写入方式。</param>
public sealed record CodeGenArtifact(CodeGenSide Side, string RelativePath, string Content, string? TemplateCode, CodeGenWriteMode WriteMode)
{
    /// <summary>
    /// 产物包与预览里的路径：<c>backend/…</c>、<c>frontend/…</c> 或 <c>_registration/…</c>。
    /// </summary>
    public string PackagePath => Side switch
    {
        CodeGenSide.Backend => "backend/" + RelativePath,
        CodeGenSide.Frontend => "frontend/" + RelativePath,
        _ => RelativePath,
    };
}

/// <summary>
/// 关联选择器的目标。
/// </summary>
/// <remarks>
/// 照 4.x「不焊外键」：没有导航属性也不联表，本表出一个选项操作，按目标实体查主键与显示列（关联树另带父级列）。
/// </remarks>
public sealed class CodeGenRelation
{
    /// <summary>
    /// 目标表配置 ID。
    /// </summary>
    public Guid TableId { get; init; }

    /// <summary>
    /// 目标表名。
    /// </summary>
    public string TableName { get; init; } = string.Empty;

    /// <summary>
    /// 目标的显示名（表注释，没有即类名）。
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// 目标实体类名。
    /// </summary>
    public string ClassName { get; init; } = string.Empty;

    /// <summary>
    /// 目标实体的全名。
    /// </summary>
    public string EntityTypeQualified { get; init; } = string.Empty;

    /// <summary>
    /// 目标实体所在的程序集（与本表不同时生成的包要引用它）。
    /// </summary>
    public string? EntityAssembly { get; init; }

    /// <summary>
    /// 显示列的属性名。
    /// </summary>
    public string LabelProperty { get; init; } = string.Empty;

    /// <summary>
    /// 父级列的属性名（关联树）。
    /// </summary>
    public string? ParentProperty { get; init; }

    /// <summary>
    /// 是否关联树。
    /// </summary>
    public bool IsTree { get; init; }
}

/// <summary>
/// 一列的生成模型。
/// </summary>
/// <remarks>
/// 由列配置与类型映射合成（照 4.x 的 <c>ColumnSchema</c>），模板只读它。
/// </remarks>
public sealed class CodeGenColumnModel
{
    /// <summary>
    /// 列名。
    /// </summary>
    public string ColumnName { get; init; } = string.Empty;

    /// <summary>
    /// 列注释。
    /// </summary>
    public string? ColumnComment { get; init; }

    /// <summary>
    /// 数据库类型名。
    /// </summary>
    public string? DbType { get; init; }

    /// <summary>
    /// C# 类型（可空的带 <c>?</c>）。
    /// </summary>
    public string CSharpType { get; set; } = "string";

    /// <summary>
    /// C# 属性名。
    /// </summary>
    public string CSharpProperty { get; init; } = string.Empty;

    /// <summary>
    /// TypeScript 类型。
    /// </summary>
    public string TsType { get; set; } = "string";

    /// <summary>
    /// 是否主键。
    /// </summary>
    public bool IsPrimaryKey { get; init; }

    /// <summary>
    /// 是否自增。
    /// </summary>
    public bool IsIdentity { get; init; }

    /// <summary>
    /// 是否可空。
    /// </summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// 是否必填。
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// 是否租户内唯一。
    /// </summary>
    public bool IsUnique { get; init; }

    /// <summary>
    /// 是否进列表。
    /// </summary>
    public bool IsList { get; init; }

    /// <summary>
    /// 是否进新建表单。
    /// </summary>
    public bool IsInsert { get; init; }

    /// <summary>
    /// 是否进编辑表单。
    /// </summary>
    public bool IsEdit { get; init; }

    /// <summary>
    /// 是否可查询。
    /// </summary>
    public bool IsQuery { get; init; }

    /// <summary>
    /// 长度（字符与二进制类型）。
    /// </summary>
    public int? Length { get; init; }

    /// <summary>
    /// 小数位。
    /// </summary>
    public int? DecimalDigits { get; init; }

    /// <summary>
    /// 表单控件。
    /// </summary>
    public CodeGenHtmlType HtmlType { get; init; }

    /// <summary>
    /// 查询方式。
    /// </summary>
    public CodeGenQueryType QueryType { get; init; }

    /// <summary>
    /// 选项来源。
    /// </summary>
    public CodeGenDictSelectorType? DictSelectorType { get; init; }

    /// <summary>
    /// 字典码。
    /// </summary>
    public string? DictCode { get; init; }

    /// <summary>
    /// 枚举类型名（列配置里填的）。
    /// </summary>
    public string? EnumTypeName { get; init; }

    /// <summary>
    /// 解析出的枚举（选项来源是枚举且解析成功时）。
    /// </summary>
    public CodeGenEnumFacts? Enum { get; set; }

    /// <summary>
    /// 常量选项 JSON。
    /// </summary>
    public string? ConstValues { get; init; }

    /// <summary>
    /// 关联的表配置 ID。
    /// </summary>
    public Guid? RelationTableId { get; init; }

    /// <summary>
    /// 关联的显示列。
    /// </summary>
    public string? RelationLabelColumn { get; init; }

    /// <summary>
    /// 解析出的关联目标。
    /// </summary>
    public CodeGenRelation? Relation { get; set; }

    /// <summary>
    /// 是否实体基类的列（主键、租户、审计、软删、并发戳）。
    /// </summary>
    public bool IsBaseColumn { get; init; }

    /// <summary>
    /// 是否业务列：不是基类的列也不是主键，只有业务列进实体、请求与表单。
    /// </summary>
    public bool IsBusiness => !IsBaseColumn && !IsPrimaryKey;
}

/// <summary>
/// 主从关系里的另一张表。
/// </summary>
public sealed class CodeGenRelatedTable
{
    /// <summary>
    /// 表配置 ID。
    /// </summary>
    public Guid TableId { get; init; }

    /// <summary>
    /// 表名。
    /// </summary>
    public string TableName { get; init; } = string.Empty;

    /// <summary>
    /// 显示名。
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// 类名。
    /// </summary>
    public string ClassName { get; init; } = string.Empty;

    /// <summary>
    /// 资源名（kebab 复数）。
    /// </summary>
    public string Resource { get; init; } = string.Empty;

    /// <summary>
    /// 包短名的小写（前端模块目录）。
    /// </summary>
    public string ModuleCode { get; init; } = string.Empty;

    /// <summary>
    /// 明细表里指向主表的外键列。
    /// </summary>
    public string ForeignKeyColumn { get; init; } = string.Empty;

    /// <summary>
    /// 外键列的属性名。
    /// </summary>
    public string ForeignKeyProperty { get; init; } = string.Empty;

    /// <summary>
    /// 列。
    /// </summary>
    public IReadOnlyList<CodeGenColumnModel> Columns { get; init; } = [];
}

/// <summary>
/// 一次生成的上下文。
/// </summary>
/// <remarks>
/// 由表配置、列配置与派生的名字合成，是模板、文案与登记片段共同的数据源。
/// </remarks>
public sealed class CodeGenContext
{
    /// <summary>
    /// 表配置。
    /// </summary>
    public required SysCodeGenTable Table { get; init; }

    /// <summary>
    /// 包的根命名空间，即工程名（如 <c>XiHan.BasicApp.Demo</c>）。
    /// </summary>
    public required string Namespace { get; init; }

    /// <summary>
    /// 包短名（如 <c>Demo</c>）。
    /// </summary>
    public required string ModuleName { get; init; }

    /// <summary>
    /// 包短名的小写：权限码、页面码、消息键与前端模块目录的首段。
    /// </summary>
    public required string ModuleCode { get; init; }

    /// <summary>
    /// 资源名：类名的 kebab 复数（如 <c>sales-orders</c>），即操作 ID 的首段。
    /// </summary>
    public required string Resource { get; init; }

    /// <summary>
    /// 资源名的 PascalCase（如 <c>SalesOrders</c>）：操作目录与权限常量名的前缀。
    /// </summary>
    public required string ResourcePascal { get; init; }

    /// <summary>
    /// 界面上的业务对象名（业务名，没有即表注释，再没有即类名）。
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// 实体所在命名空间。
    /// </summary>
    public required string EntityNamespace { get; init; }

    /// <summary>
    /// 沿用的已有实体（主库的表登记过实体时）。
    /// </summary>
    public Type? ExistingEntity { get; init; }

    /// <summary>
    /// 包含的操作（已归一化，空即缺省集合）。
    /// </summary>
    public required IReadOnlyList<string> Actions { get; init; }

    /// <summary>
    /// 全部列。
    /// </summary>
    public required IReadOnlyList<CodeGenColumnModel> Columns { get; init; }

    /// <summary>
    /// 主键列。
    /// </summary>
    public CodeGenColumnModel? PrimaryKey { get; init; }

    /// <summary>
    /// 状态列（勾了状态切换时）。
    /// </summary>
    public CodeGenColumnModel? StatusColumn { get; set; }

    /// <summary>
    /// 树表的父级列。
    /// </summary>
    public CodeGenColumnModel? TreeParentColumn { get; set; }

    /// <summary>
    /// 树表的名称列。
    /// </summary>
    public CodeGenColumnModel? TreeNameColumn { get; set; }

    /// <summary>
    /// 本表是明细表时的主表。
    /// </summary>
    public CodeGenRelatedTable? MasterTable { get; set; }

    /// <summary>
    /// 以本表为主表的明细表。
    /// </summary>
    public IReadOnlyList<CodeGenRelatedTable> DetailTables { get; set; } = [];

    /// <summary>
    /// 生成的包要额外引用的 BasicApp 程序集（枚举、关联实体所在的包）。
    /// </summary>
    public IReadOnlyList<string> ReferencedPackages { get; set; } = [];

    /// <summary>
    /// 是否包含某个操作。
    /// </summary>
    /// <param name="action">操作键。</param>
    public bool Can(string action) => Actions.Contains(action, StringComparer.Ordinal);
}
