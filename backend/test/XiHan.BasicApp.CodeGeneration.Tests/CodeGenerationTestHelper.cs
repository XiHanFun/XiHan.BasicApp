// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 代码生成模块测试夹具工具。
/// </summary>
/// <remarks>
/// 三类职责：构造生成上下文与列结构；反射调用 <c>internal</c> 的二阶产物生成器与共享工具
/// （被测项目不暴露 InternalsVisibleTo，测试不得改 src，故统一在此收口反射入口）；
/// 反射回填实体主键与创建/清理临时目录。反射调用统一还原 <c>TargetInvocationException.InnerException</c>，
/// 使测试可以直接断言被测方法真正抛出的异常类型。
/// </remarks>
internal static class CodeGenerationTestHelper
{
    /// <summary>
    /// 被测模块程序集（反射取 internal 类型的入口）。
    /// </summary>
    public static readonly Assembly ModuleAssembly = typeof(MenuPermissionArtifactGenerator).Assembly;

    /// <summary>
    /// 二阶产物统一输出目录（与 <c>MenuPermissionArtifactShared.OutputFolder</c> 同值）。
    /// </summary>
    public const string OutputFolder = "_GeneratedMenuPermission";

    /// <summary>
    /// 二阶产物统一模板编码（与 <c>MenuPermissionArtifactShared.TemplateCode</c> 同值）。
    /// </summary>
    public const string ArtifactTemplateCode = "_menu_permission";

    /// <summary>
    /// 构造一份最小可用的生成上下文。
    /// </summary>
    /// <param name="tableName">数据库表名（同时是权限资源段）</param>
    /// <param name="className">实体类名</param>
    /// <param name="moduleName">模块名（为空时各推导回退到类名）</param>
    /// <param name="namespaceValue">命名空间（为空时回退到模块段）</param>
    /// <param name="businessName">业务名（为空时展示名回退到类名）</param>
    /// <param name="enabledActions">已启用写操作</param>
    /// <param name="columns">列集合</param>
    /// <param name="templateType">模板类型</param>
    public static CodeGenerationContext CreateContext(
        string tableName = "sys_product",
        string className = "SysProduct",
        string? moduleName = "Catalog",
        string? namespaceValue = "XiHan.BasicApp.Catalog",
        string? businessName = "产品",
        IReadOnlyList<string>? enabledActions = null,
        IReadOnlyList<ColumnSchema>? columns = null,
        TemplateType templateType = TemplateType.Single)
    {
        return new CodeGenerationContext
        {
            TableName = tableName,
            TableComment = "产品表",
            ClassName = className,
            ModuleName = moduleName,
            Namespace = namespaceValue,
            BusinessName = businessName,
            FunctionName = businessName,
            Author = "tester",
            TemplateType = templateType,
            EnabledActions = enabledActions ?? ["create", "update", "delete"],
            Columns = columns ?? []
        };
    }

    /// <summary>
    /// 构造一列列结构。
    /// </summary>
    /// <param name="columnName">列名</param>
    /// <param name="csharpType">C# 类型</param>
    /// <param name="tsType">TS 类型</param>
    /// <param name="isPrimaryKey">是否主键</param>
    /// <param name="isQuery">是否参与查询</param>
    /// <param name="queryType">查询方式</param>
    /// <param name="htmlType">表单控件</param>
    public static ColumnSchema CreateColumn(
        string columnName,
        string csharpType = "string",
        string tsType = "string",
        bool isPrimaryKey = false,
        bool isQuery = false,
        QueryType queryType = QueryType.Equal,
        HtmlType htmlType = HtmlType.Input)
    {
        return new ColumnSchema
        {
            ColumnName = columnName,
            ColumnComment = columnName + " 注释",
            DbType = "varchar",
            CSharpType = csharpType,
            CSharpProperty = columnName,
            TsType = tsType,
            IsPrimaryKey = isPrimaryKey,
            IsQuery = isQuery,
            QueryType = queryType,
            HtmlType = htmlType
        };
    }

    /// <summary>
    /// 反射调用 <c>PermissionSeedArtifactGenerator.Build</c>。
    /// </summary>
    public static GeneratedArtifact BuildPermissionDefinitions(CodeGenerationContext context)
    {
        return (GeneratedArtifact)InvokeInternalStatic(
            "XiHan.BasicApp.CodeGeneration.Infrastructure.Generation.PermissionSeedArtifactGenerator",
            "Build",
            context)!;
    }

    /// <summary>
    /// 反射调用 <c>PageDescriptorArtifactGenerator.Build</c>。
    /// </summary>
    public static GeneratedArtifact BuildPageRegistrySnippet(CodeGenerationContext context)
    {
        return (GeneratedArtifact)InvokeInternalStatic(
            "XiHan.BasicApp.CodeGeneration.Infrastructure.Generation.PageDescriptorArtifactGenerator",
            "Build",
            context)!;
    }

    /// <summary>
    /// 反射调用 <c>SeederArtifactGenerator.Build</c>。
    /// </summary>
    public static IReadOnlyList<GeneratedArtifact> BuildSeeders(CodeGenerationContext context)
    {
        return (IReadOnlyList<GeneratedArtifact>)InvokeInternalStatic(
            "XiHan.BasicApp.CodeGeneration.Infrastructure.Generation.SeederArtifactGenerator",
            "Build",
            context)!;
    }

    /// <summary>
    /// 反射调用 <c>MenuPermissionArtifactShared</c> 上的共享推导方法。
    /// </summary>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="methodName">方法名</param>
    /// <param name="arguments">调用参数</param>
    public static TResult InvokeShared<TResult>(string methodName, params object?[] arguments)
    {
        return (TResult)InvokeInternalStatic(
            "XiHan.BasicApp.CodeGeneration.Infrastructure.Generation.MenuPermissionArtifactShared",
            methodName,
            arguments)!;
    }

    /// <summary>
    /// 反射调用 internal 静态方法，并把 <see cref="TargetInvocationException"/> 还原为原始异常。
    /// </summary>
    /// <param name="typeFullName">类型全名</param>
    /// <param name="methodName">方法名</param>
    /// <param name="arguments">调用参数</param>
    public static object? InvokeInternalStatic(string typeFullName, string methodName, params object?[] arguments)
    {
        var type = ModuleAssembly.GetType(typeFullName, throwOnError: true)!;
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"未找到方法 {typeFullName}.{methodName}");

        try
        {
            return method.Invoke(null, arguments);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    /// <summary>
    /// 反射回填实体主键（<c>BasicId</c> 由基类以私有 setter 声明，测试需构造已落库实体）。
    /// </summary>
    /// <param name="entity">实体实例</param>
    /// <param name="id">主键值</param>
    public static TEntity WithId<TEntity>(TEntity entity, long id)
        where TEntity : notnull
    {
        var property = entity.GetType().GetProperty("BasicId", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("实体缺少 BasicId 属性");
        property.SetValue(entity, id);
        return entity;
    }

    /// <summary>
    /// 创建一个进程内唯一的临时目录（测试自行在 finally / Dispose 中递归删除）。
    /// </summary>
    public static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "xihan-codegen-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// 递归删除临时目录（不存在或被占用时静默忽略，避免清理失败盖掉真正的断言失败）。
    /// </summary>
    /// <param name="path">目录路径</param>
    public static void DeleteDirectorySafely(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
