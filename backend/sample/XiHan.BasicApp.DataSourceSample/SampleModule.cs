// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Data;

namespace XiHan.BasicApp.DataSourceSample;

/// <summary>
/// 示例模块：只挂数据访问模块，连接与建表开关全部来自 appsettings.json
/// </summary>
[DependsOn(typeof(XiHanDataModule))]
public class SampleModule : XiHanModule
{
}
