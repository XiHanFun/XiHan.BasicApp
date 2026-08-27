// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 代码生成选项测试。
/// </summary>
/// <remarks>
/// 选项承担两件安全职责：落盘白名单（fail-closed）与表前缀剥离。
/// 配置节名改动会让绑定静默失效，落盘从"受控放行"变成"永远拒绝或永远放行"；
/// 前缀解析出空串会让剥离逻辑命中所有表名而把类名切错。
/// </remarks>
public sealed class CodeGenerationOptionsTests
{
    /// <summary>
    /// 配置节名是绑定的唯一锚点，改名等于让整段安全配置静默失效。
    /// </summary>
    [Fact]
    public void SectionName_ShouldStayCodeGeneration()
    {
        Assert.Equal("CodeGeneration", CodeGenerationOptions.SectionName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 默认必须是最保守的一档：不启用自定义路径落盘、白名单为空，
    /// 生产环境不显式开启就永远落不了盘。
    /// </summary>
    [Fact]
    public void Defaults_ShouldDisableCustomPathDiskAndKeepAllowListEmpty()
    {
        var options = new CodeGenerationOptions();

        Assert.False(options.EnableCustomPathDisk);
        Assert.Empty(options.AllowedRootPaths);
    }

    /// <summary>
    /// 默认表前缀解析出本系统的两个前缀，顺序与配置书写顺序一致。
    /// </summary>
    [Fact]
    public void ResolvedTablePrefixes_DefaultShouldBeSysAndSaas()
    {
        var options = new CodeGenerationOptions();

        Assert.Equal(["Sys_", "Saas_"], options.ResolvedTablePrefixes);
    }

    /// <summary>
    /// 前缀解析按逗号切分并去除空项与首尾空白。
    /// </summary>
    /// <param name="configured">配置值</param>
    /// <param name="expected">期望解析结果（以逗号连接便于书写）</param>
    [Theory]
    [InlineData("Sys_", "Sys_")]
    [InlineData("Sys_,Saas_", "Sys_|Saas_")]
    [InlineData("  Sys_ , Saas_  ", "Sys_|Saas_")]
    [InlineData("Sys_,,Saas_", "Sys_|Saas_")]
    [InlineData("Sys_,   ,Saas_", "Sys_|Saas_")]
    public void ResolvedTablePrefixes_ShouldSplitTrimAndDropEmpty(string configured, string expected)
    {
        var options = new CodeGenerationOptions { TablePrefixes = configured };

        Assert.Equal(expected.Split('|'), options.ResolvedTablePrefixes);
    }

    /// <summary>
    /// 配置为空串时解析出空集合，而不是含一个空串的集合——
    /// 含空串会让前缀剥离命中所有表名，把类名整体切错。
    /// </summary>
    /// <param name="configured">空配置值</param>
    [Theory]
    [InlineData("")]
    [InlineData(",")]
    [InlineData(",,,")]
    [InlineData("   ")]
    [InlineData(" , ")]
    public void ResolvedTablePrefixes_BlankConfigShouldResolveToEmptyCollection(string configured)
    {
        var options = new CodeGenerationOptions { TablePrefixes = configured };

        Assert.Empty(options.ResolvedTablePrefixes);
    }

    /// <summary>
    /// 每次读取都基于当前配置值重新解析，改了配置立即生效（属性无缓存）。
    /// </summary>
    [Fact]
    public void ResolvedTablePrefixes_ShouldReflectLatestConfiguredValue()
    {
        var options = new CodeGenerationOptions();
        Assert.Equal(2, options.ResolvedTablePrefixes.Count);

        options.TablePrefixes = "Biz_";

        Assert.Equal(["Biz_"], options.ResolvedTablePrefixes);
    }
}
