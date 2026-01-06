#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ResourceEnums
// Guid:1a2b3c4d-5e6f-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/7 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 资源类型枚举
/// </summary>
public enum ResourceType
{
    /// <summary>
    /// 菜单资源（目录、菜单项）
    /// </summary>
    Menu = 0,

    /// <summary>
    /// API接口资源
    /// </summary>
    Api = 1,

    /// <summary>
    /// 按钮资源（页面操作按钮）
    /// </summary>
    Button = 2,

    /// <summary>
    /// 文件资源
    /// </summary>
    File = 3,

    /// <summary>
    /// 数据表资源
    /// </summary>
    DataTable = 4,

    /// <summary>
    /// 页面元素资源（Tab、区块等）
    /// </summary>
    Element = 5,

    /// <summary>
    /// 业务对象资源
    /// </summary>
    BusinessObject = 6,

    /// <summary>
    /// 其他资源
    /// </summary>
    Other = 99
}

/// <summary>
/// 操作类型枚举（RBAC 权限操作）
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
    /// 执行/操作
    /// </summary>
    Execute = 4,

    /// <summary>
    /// 审批
    /// </summary>
    Approve = 5,

    /// <summary>
    /// 导入
    /// </summary>
    Import = 6,

    /// <summary>
    /// 导出
    /// </summary>
    Export = 7,

    /// <summary>
    /// 下载
    /// </summary>
    Download = 8,

    /// <summary>
    /// 上传
    /// </summary>
    Upload = 9,

    /// <summary>
    /// 打印
    /// </summary>
    Print = 10,

    /// <summary>
    /// 分享
    /// </summary>
    Share = 11,

    /// <summary>
    /// 授权/授予
    /// </summary>
    Grant = 12,

    /// <summary>
    /// 撤销/收回
    /// </summary>
    Revoke = 13,

    /// <summary>
    /// 查看详情
    /// </summary>
    View = 14,

    /// <summary>
    /// 启用
    /// </summary>
    Enable = 15,

    /// <summary>
    /// 禁用
    /// </summary>
    Disable = 16,

    /// <summary>
    /// 自定义操作
    /// </summary>
    Custom = 99
}

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

/// <summary>
/// 约束规则类型枚举
/// </summary>
public enum ConstraintType
{
    /// <summary>
    /// 静态职责分离（Static Separation of Duty）
    /// 用户不能同时拥有互斥的角色
    /// </summary>
    SSD = 0,

    /// <summary>
    /// 动态职责分离（Dynamic Separation of Duty）
    /// 同一会话不能同时激活互斥的角色
    /// </summary>
    DSD = 1,

    /// <summary>
    /// 互斥约束
    /// 某些权限不能同时授予
    /// </summary>
    MutualExclusion = 2,

    /// <summary>
    /// 基数约束
    /// 限制角色或权限的数量
    /// </summary>
    Cardinality = 3,

    /// <summary>
    /// 先决条件约束
    /// 获得某角色/权限前必须先拥有其他角色/权限
    /// </summary>
    Prerequisite = 4,

    /// <summary>
    /// 时间约束
    /// 基于时间的访问控制
    /// </summary>
    Temporal = 5,

    /// <summary>
    /// 位置约束
    /// 基于位置的访问控制
    /// </summary>
    Location = 6,

    /// <summary>
    /// 自定义约束
    /// </summary>
    Custom = 99
}

/// <summary>
/// 违规处理方式枚举
/// </summary>
public enum ViolationAction
{
    /// <summary>
    /// 拒绝操作
    /// </summary>
    Deny = 0,

    /// <summary>
    /// 警告但允许
    /// </summary>
    Warning = 1,

    /// <summary>
    /// 仅记录日志
    /// </summary>
    Log = 2,

    /// <summary>
    /// 需要审批
    /// </summary>
    RequireApproval = 3
}

/// <summary>
/// 会话角色状态枚举
/// </summary>
public enum SessionRoleStatus
{
    /// <summary>
    /// 已激活
    /// </summary>
    Active = 0,

    /// <summary>
    /// 已停用
    /// </summary>
    Inactive = 1,

    /// <summary>
    /// 已过期
    /// </summary>
    Expired = 2
}
