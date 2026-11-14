// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Settings;

/// <summary>
/// 一本内建字典的元数据
/// </summary>
/// <remarks>
/// 业务按编码依赖这本字典的包经 <see cref="DictCatalogServiceCollectionExtensions.AddDictCatalog"/> 登记，Settings 的种子据此写成平台的内建字典
/// 名称、分类、说明与登记的项（名称、附加值、上级）每次对齐；状态、排序号与缺省项只在首次写入，之后归运营；运营另加的项不动
/// </remarks>
/// <param name="Code">字典编码，首段是登记它的包名</param>
/// <param name="Name">名称</param>
/// <param name="Category">分类</param>
/// <param name="Description">说明</param>
/// <param name="Items">登记的项，上级排在下级前面</param>
/// <param name="Order">首次写入时的排序号</param>
public sealed record DictDescriptor(string Code, string Name, string Category, string Description, IReadOnlyList<DictItemDescriptor> Items, int Order = 0)
{
    /// <summary>
    /// 字典与字典项编码的正则
    /// </summary>
    /// <remarks>
    /// 字母开头，只含字母、数字、<c>.</c>、<c>_</c>、<c>-</c>，最长 100
    /// </remarks>
    public const string CodePattern = "^[A-Za-z][A-Za-z0-9._-]{0,99}$";
}

/// <summary>
/// 内建字典里登记的一项
/// </summary>
/// <param name="Code">项编码，字典内唯一</param>
/// <param name="Name">显示名</param>
/// <param name="Value">附加值</param>
/// <param name="Parent">上级项的编码</param>
/// <param name="IsDefault">首次写入时是否缺省项（至多一项）</param>
public sealed record DictItemDescriptor(string Code, string Name, string? Value = null, string? Parent = null, bool IsDefault = false);
