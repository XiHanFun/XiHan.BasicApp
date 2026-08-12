// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Printing.Domain.DataSources;

/// <summary>
/// 打印数据源登记项：业务模块在 ConfigureServices 阶段以 DI 单例投递定义，
/// 注册表构造时统一收纳（收纳时执行编码唯一性与字段契约校验）。
/// </summary>
/// <param name="Definition">数据源定义</param>
public sealed record PrintDataSourceRegistration(PrintDataSourceDefinition Definition);
