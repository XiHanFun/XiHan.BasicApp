#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenEnums
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567003
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/23 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.CodeGeneration.Enums;

/// <summary>
/// 代码生成模板类型
/// </summary>
public enum TemplateType
{
    /// <summary>
    /// 单表
    /// </summary>
    [Description("单表")]
    Single = 0,

    /// <summary>
    /// 树表
    /// </summary>
    [Description("树表")]
    Tree = 1,

    /// <summary>
    /// 主子表
    /// </summary>
    [Description("主子表")]
    MasterDetail = 2
}

/// <summary>
/// 代码生成状态
/// </summary>
public enum GenStatus
{
    /// <summary>
    /// 未生成
    /// </summary>
    [Description("未生成")]
    NotGenerated = 0,

    /// <summary>
    /// 已生成
    /// </summary>
    [Description("已生成")]
    Generated = 1,

    /// <summary>
    /// 生成失败
    /// </summary>
    [Description("生成失败")]
    Failed = 2
}

/// <summary>
/// 查询方式
/// </summary>
public enum QueryType
{
    /// <summary>
    /// 等于
    /// </summary>
    [Description("等于")]
    Equal = 0,

    /// <summary>
    /// 不等于
    /// </summary>
    [Description("不等于")]
    NotEqual = 1,

    /// <summary>
    /// 大于
    /// </summary>
    [Description("大于")]
    GreaterThan = 2,

    /// <summary>
    /// 大于等于
    /// </summary>
    [Description("大于等于")]
    GreaterThanOrEqual = 3,

    /// <summary>
    /// 小于
    /// </summary>
    [Description("小于")]
    LessThan = 4,

    /// <summary>
    /// 小于等于
    /// </summary>
    [Description("小于等于")]
    LessThanOrEqual = 5,

    /// <summary>
    /// 模糊查询
    /// </summary>
    [Description("模糊查询")]
    Like = 6,

    /// <summary>
    /// 左模糊
    /// </summary>
    [Description("左模糊")]
    LikeLeft = 7,

    /// <summary>
    /// 右模糊
    /// </summary>
    [Description("右模糊")]
    LikeRight = 8,

    /// <summary>
    /// 范围查询
    /// </summary>
    [Description("范围查询")]
    Between = 9,

    /// <summary>
    /// 包含
    /// </summary>
    [Description("包含")]
    In = 10,

    /// <summary>
    /// 不包含
    /// </summary>
    [Description("不包含")]
    NotIn = 11
}

/// <summary>
/// 表单显示类型
/// </summary>
public enum HtmlType
{
    /// <summary>
    /// 文本框
    /// </summary>
    [Description("文本框")]
    Input = 0,

    /// <summary>
    /// 文本域
    /// </summary>
    [Description("文本域")]
    Textarea = 1,

    /// <summary>
    /// 下拉框
    /// </summary>
    [Description("下拉框")]
    Select = 2,

    /// <summary>
    /// 单选框
    /// </summary>
    [Description("单选框")]
    Radio = 3,

    /// <summary>
    /// 复选框
    /// </summary>
    [Description("复选框")]
    Checkbox = 4,

    /// <summary>
    /// 日期控件
    /// </summary>
    [Description("日期控件")]
    DatePicker = 5,

    /// <summary>
    /// 日期时间控件
    /// </summary>
    [Description("日期时间控件")]
    DateTimePicker = 6,

    /// <summary>
    /// 时间控件
    /// </summary>
    [Description("时间控件")]
    TimePicker = 7,

    /// <summary>
    /// 图片上传
    /// </summary>
    [Description("图片上传")]
    ImageUpload = 8,

    /// <summary>
    /// 文件上传
    /// </summary>
    [Description("文件上传")]
    FileUpload = 9,

    /// <summary>
    /// 富文本编辑器
    /// </summary>
    [Description("富文本编辑器")]
    Editor = 10,

    /// <summary>
    /// 数字输入框
    /// </summary>
    [Description("数字输入框")]
    InputNumber = 11,

    /// <summary>
    /// 开关
    /// </summary>
    [Description("开关")]
    Switch = 12,

    /// <summary>
    /// 树形选择
    /// </summary>
    [Description("树形选择")]
    TreeSelect = 13
}

/// <summary>
/// 生成代码方式
/// </summary>
public enum GenType
{
    /// <summary>
    /// 压缩包下载
    /// </summary>
    [Description("压缩包下载")]
    Zip = 0,

    /// <summary>
    /// 自定义路径
    /// </summary>
    [Description("自定义路径")]
    CustomPath = 1,

    /// <summary>
    /// 预览
    /// </summary>
    [Description("预览")]
    Preview = 2
}

/// <summary>
/// 模板引擎类型
/// </summary>
public enum TemplateEngine
{
    /// <summary>
    /// Razor
    /// </summary>
    [Description("Razor")]
    Razor = 0,

    /// <summary>
    /// Scriban
    /// </summary>
    [Description("Scriban")]
    Scriban = 1,

    /// <summary>
    /// T4
    /// </summary>
    [Description("T4")]
    T4 = 2
}

/// <summary>
/// 数据库类型
/// </summary>
public enum DatabaseType
{
    /// <summary>
    /// MySQL
    /// </summary>
    [Description("MySQL")]
    MySql = 0,

    /// <summary>
    /// SQL Server
    /// </summary>
    [Description("SQL Server")]
    SqlServer = 1,

    /// <summary>
    /// PostgreSQL
    /// </summary>
    [Description("PostgreSQL")]
    PostgreSql = 2,

    /// <summary>
    /// Oracle
    /// </summary>
    [Description("Oracle")]
    Oracle = 3,

    /// <summary>
    /// SQLite
    /// </summary>
    [Description("SQLite")]
    Sqlite = 4
}
