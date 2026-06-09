#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOperation.Enum
// Guid:bcb13302-56f2-4e44-a15a-dbf11097222c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// HTTP 方法枚举
/// </summary>
public enum HttpMethodType
{
    /// <summary>
    /// GET 请求
    /// </summary>
    [Description("GET")]
    GET = 0,

    /// <summary>
    /// POST 请求
    /// </summary>
    [Description("POST")]
    POST = 1,

    /// <summary>
    /// PUT 请求
    /// </summary>
    [Description("PUT")]
    PUT = 2,

    /// <summary>
    /// DELETE 请求
    /// </summary>
    [Description("DELETE")]
    DELETE = 3,

    /// <summary>
    /// PATCH 请求
    /// </summary>
    [Description("PATCH")]
    PATCH = 4,

    /// <summary>
    /// HEAD 请求
    /// </summary>
    [Description("HEAD")]
    HEAD = 5,

    /// <summary>
    /// OPTIONS 请求
    /// </summary>
    [Description("OPTIONS")]
    OPTIONS = 6,

    /// <summary>
    /// 所有方法
    /// </summary>
    [Description("所有方法")]
    ALL = 99
}

/// <summary>
/// 操作分类枚举
/// </summary>
public enum OperationCategory
{
    /// <summary>
    /// CRUD 操作（基础增删改查）
    /// </summary>
    [Description("CRUD")]
    Crud = 0,

    /// <summary>
    /// 业务操作（审批、提交等）
    /// </summary>
    [Description("业务")]
    Business = 1,

    /// <summary>
    /// 管理操作（授权、配置等）
    /// </summary>
    [Description("管理")]
    Admin = 2,

    /// <summary>
    /// 系统操作（导入导出、备份等）
    /// </summary>
    [Description("系统")]
    System = 3,

    /// <summary>
    /// 自定义操作
    /// </summary>
    [Description("自定义")]
    Custom = 99
}

/// <summary>
/// 操作类型枚举（RBAC 权限模型专用，定义 SysOperation 可执行的操作种类）
/// 注意：勿与 <see cref="OperationType"/> 混淆，后者用于审计日志记录
/// </summary>
public enum OperationTypeCode
{
    /// <summary>
    /// 创建/新增
    /// </summary>
    [Description("创建")]
    Create = 0,

    /// <summary>
    /// 读取/查询
    /// </summary>
    [Description("读取")]
    Read = 1,

    /// <summary>
    /// 更新/修改
    /// </summary>
    [Description("更新")]
    Update = 2,

    /// <summary>
    /// 删除
    /// </summary>
    [Description("删除")]
    Delete = 3,

    /// <summary>
    /// 查看详情
    /// </summary>
    [Description("查看详情")]
    View = 4,

    /// <summary>
    /// 审批
    /// </summary>
    [Description("审批")]
    Approve = 10,

    /// <summary>
    /// 执行/操作
    /// </summary>
    [Description("执行")]
    Execute = 11,

    /// <summary>
    /// 导入
    /// </summary>
    [Description("导入")]
    Import = 20,

    /// <summary>
    /// 导出
    /// </summary>
    [Description("导出")]
    Export = 21,

    /// <summary>
    /// 上传
    /// </summary>
    [Description("上传")]
    Upload = 22,

    /// <summary>
    /// 下载
    /// </summary>
    [Description("下载")]
    Download = 23,

    /// <summary>
    /// 打印
    /// </summary>
    [Description("打印")]
    Print = 24,

    /// <summary>
    /// 分享
    /// </summary>
    [Description("分享")]
    Share = 25,

    /// <summary>
    /// 授权/授予
    /// </summary>
    [Description("授权")]
    Grant = 30,

    /// <summary>
    /// 撤销/收回
    /// </summary>
    [Description("撤销")]
    Revoke = 31,

    /// <summary>
    /// 启用
    /// </summary>
    [Description("启用")]
    Enable = 32,

    /// <summary>
    /// 禁用
    /// </summary>
    [Description("禁用")]
    Disable = 33,

    /// <summary>
    /// 自定义操作
    /// </summary>
    [Description("自定义")]
    Custom = 99
}
