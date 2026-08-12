// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Printing.Domain.DataSources;

/// <summary>
/// 打印数据源注册表：业务模块在 ConfigureServices 阶段注册数据源定义，
/// 设计器经查询端点读取目录，模板写路径据此校验 DataSourceCode。
/// </summary>
public interface IPrintDataSourceRegistry
{
    /// <summary>
    /// 注册数据源定义；重复编码抛出异常（后注册模块不得静默覆盖既有字段契约）。
    /// </summary>
    /// <param name="definition">数据源定义</param>
    void Register(PrintDataSourceDefinition definition);

    /// <summary>
    /// 按编码查找数据源。
    /// </summary>
    /// <param name="code">数据源编码</param>
    /// <returns>数据源定义；未注册返回 null</returns>
    PrintDataSourceDefinition? Find(string code);

    /// <summary>
    /// 数据源是否已注册。
    /// </summary>
    /// <param name="code">数据源编码</param>
    bool IsRegistered(string code);

    /// <summary>
    /// 全部已注册数据源（按编码排序）。
    /// </summary>
    IReadOnlyList<PrintDataSourceDefinition> GetAll();
}
