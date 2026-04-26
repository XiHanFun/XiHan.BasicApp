#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOperation.Enum
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 操作分类枚举
/// </summary>
public enum OperationCategory
{
    /// <summary>
    /// CRUD 操作（基础增删改查）
    /// </summary>
    Crud = 0,

    /// <summary>
    /// 业务操作（审批、提交等）
    /// </summary>
    Business = 1,

    /// <summary>
    /// 管理操作（授权、配置等）
    /// </summary>
    Admin = 2,

    /// <summary>
    /// 系统操作（导入导出、备份等）
    /// </summary>
    System = 3,

    /// <summary>
    /// 自定义操作
    /// </summary>
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
    Create = 0,

    /// <summary>
    /// 读取/查询
    /// </summary>
    Read = 1,

    /// <summary>
    /// 更新/修改
    /// </summary>
    Update = 2,

    /// <summary>
    /// 删除
    /// </summary>
    Delete = 3,

    /// <summary>
    /// 查看详情
    /// </summary>
    View = 4,

    /// <summary>
    /// 审批
    /// </summary>
    Approve = 10,

    /// <summary>
    /// 执行/操作
    /// </summary>
    Execute = 11,

    /// <summary>
    /// 导入
    /// </summary>
    Import = 20,

    /// <summary>
    /// 导出
    /// </summary>
    Export = 21,

    /// <summary>
    /// 上传
    /// </summary>
    Upload = 22,

    /// <summary>
    /// 下载
    /// </summary>
    Download = 23,

    /// <summary>
    /// 打印
    /// </summary>
    Print = 24,

    /// <summary>
    /// 分享
    /// </summary>
    Share = 25,

    /// <summary>
    /// 授权/授予
    /// </summary>
    Grant = 30,

    /// <summary>
    /// 撤销/收回
    /// </summary>
    Revoke = 31,

    /// <summary>
    /// 启用
    /// </summary>
    Enable = 32,

    /// <summary>
    /// 禁用
    /// </summary>
    Disable = 33,

    /// <summary>
    /// 自定义操作
    /// </summary>
    Custom = 99
}

/// <summary>
/// HTTP 方法枚举
/// </summary>
public enum HttpMethodType
{
    /// <summary>
    /// GET 请求
    /// </summary>
    GET = 0,

    /// <summary>
    /// POST 请求
    /// </summary>
    POST = 1,

    /// <summary>
    /// PUT 请求
    /// </summary>
    PUT = 2,

    /// <summary>
    /// DELETE 请求
    /// </summary>
    DELETE = 3,

    /// <summary>
    /// PATCH 请求
    /// </summary>
    PATCH = 4,

    /// <summary>
    /// HEAD 请求
    /// </summary>
    HEAD = 5,

    /// <summary>
    /// OPTIONS 请求
    /// </summary>
    OPTIONS = 6,

    /// <summary>
    /// 所有方法
    /// </summary>
    ALL = 99
}

