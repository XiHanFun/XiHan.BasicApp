// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Settings;

/// <summary>
/// 一个内建系统参数的元数据
/// </summary>
/// <remarks>
/// 读这个参数的包经 <see cref="ConfigCatalogServiceCollectionExtensions.AddConfigCatalog"/> 登记，Settings 的种子据此写成平台的内建参数
/// 名称、分组、数据类型、缺省值、是否允许租户覆盖、是否加密与说明每次对齐；值留空（生效值即缺省值），值、状态与排序号之后归运营
/// </remarks>
/// <param name="Key">参数键，首段是登记它的包名</param>
/// <param name="Name">名称</param>
/// <param name="Group">分组</param>
/// <param name="DataType">数据类型</param>
/// <param name="DefaultValue">缺省值：值为空时的生效值，要符合数据类型；为空即值为空时由读取方用自己的缺省</param>
/// <param name="Description">说明</param>
/// <param name="AllowTenantOverride">是否允许租户同键覆盖</param>
/// <param name="IsEncrypted">值是否加密落库（密钥类参数）</param>
/// <param name="Order">首次写入时的排序号</param>
public sealed record ConfigDescriptor(string Key, string Name, string Group, ConfigDataType DataType, string? DefaultValue, string Description, bool AllowTenantOverride = false, bool IsEncrypted = false, int Order = 0);
