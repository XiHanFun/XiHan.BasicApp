// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;

namespace XiHan.BasicApp.Plugins.Domain.Packages;

/// <summary>
/// 声明式页面的描述 <c>pages/&lt;页&gt;.json</c>
/// </summary>
/// <remarks>
/// 前端按它渲染一张列表页与新建 / 编辑弹窗：列表、新建、修改、删除各绑插件的一个操作，按钮的权限取操作要的权限码；不认得的成员按不合格式拒绝
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PluginSchemaView
{
    /// <summary>
    /// 列表操作：应答带 <c>items</c> 数组，一次取回全部行
    /// </summary>
    public string List { get; set; } = string.Empty;

    /// <summary>
    /// 新建操作：请求是表单的字段
    /// </summary>
    public string? Create { get; set; }

    /// <summary>
    /// 修改操作：请求是行键加表单的字段
    /// </summary>
    public string? Update { get; set; }

    /// <summary>
    /// 删除操作：请求只有行键
    /// </summary>
    public string? Delete { get; set; }

    /// <summary>
    /// 行键的字段名，缺省 <c>id</c>
    /// </summary>
    public string? RowKey { get; set; }

    /// <summary>
    /// 列表的列
    /// </summary>
    public IReadOnlyList<PluginSchemaColumn> Columns { get; set; } = [];

    /// <summary>
    /// 新建与编辑弹窗的字段
    /// </summary>
    /// <remarks>
    /// 有新建或修改操作时必填
    /// </remarks>
    public IReadOnlyList<PluginSchemaField> Form { get; set; } = [];
}

/// <summary>
/// 声明式页面的一列
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PluginSchemaColumn
{
    /// <summary>
    /// 行里的字段名
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 列标题的文案键
    /// </summary>
    public string TitleKey { get; set; } = string.Empty;

    /// <summary>
    /// 值的种类：<c>text</c>（缺省）、<c>number</c>、<c>boolean</c>、<c>datetime</c>
    /// </summary>
    public string? Type { get; set; }
}

/// <summary>
/// 新建与编辑弹窗的一个字段
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PluginSchemaField
{
    /// <summary>
    /// 请求里的字段名
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 字段名的文案键
    /// </summary>
    public string LabelKey { get; set; } = string.Empty;

    /// <summary>
    /// 字段说明的文案键：显示在字段名后面的问号提示里
    /// </summary>
    public string? HelpKey { get; set; }

    /// <summary>
    /// 输入控件：<c>text</c>（缺省）、<c>textarea</c>、<c>number</c>、<c>switch</c>
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool Required { get; set; }
}
