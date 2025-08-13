#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CommonEnums
// Guid:ed28152c-d6e9-4396-addb-b479254bad34
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 4:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 是或否枚举
/// </summary>
public enum YesOrNo
{
    /// <summary>
    /// 否
    /// </summary>
    No = 0,

    /// <summary>
    /// 是
    /// </summary>
    Yes = 1
}

/// <summary>
/// 配置类型枚举
/// </summary>
public enum ConfigType
{
    /// <summary>
    /// 系统配置
    /// </summary>
    System = 0,

    /// <summary>
    /// 用户配置
    /// </summary>
    User = 1,

    /// <summary>
    /// 应用配置
    /// </summary>
    Application = 2
}

/// <summary>
/// 配置数据类型枚举
/// </summary>
public enum ConfigDataType
{
    /// <summary>
    /// 字符串
    /// </summary>
    String = 0,

    /// <summary>
    /// 数字
    /// </summary>
    Number = 1,

    /// <summary>
    /// 布尔值
    /// </summary>
    Boolean = 2,

    /// <summary>
    /// JSON对象
    /// </summary>
    Json = 3,

    /// <summary>
    /// 数组
    /// </summary>
    Array = 4
}

/// <summary>
/// 租户状态枚举
/// </summary>
public enum TenantStatus
{
    /// <summary>
    /// 正常
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 暂停
    /// </summary>
    Suspended = 1,

    /// <summary>
    /// 过期
    /// </summary>
    Expired = 2,

    /// <summary>
    /// 禁用
    /// </summary>
    Disabled = 3
}

/// <summary>
/// OAuth应用类型枚举
/// </summary>
public enum OAuthAppType
{
    /// <summary>
    /// Web应用
    /// </summary>
    Web = 0,

    /// <summary>
    /// 移动应用
    /// </summary>
    Mobile = 1,

    /// <summary>
    /// 桌面应用
    /// </summary>
    Desktop = 2,

    /// <summary>
    /// 服务应用
    /// </summary>
    Service = 3
}

/// <summary>
/// 授权类型枚举
/// </summary>
public enum GrantType
{
    /// <summary>
    /// 授权码模式
    /// </summary>
    AuthorizationCode = 0,

    /// <summary>
    /// 简化模式
    /// </summary>
    Implicit = 1,

    /// <summary>
    /// 密码模式
    /// </summary>
    Password = 2,

    /// <summary>
    /// 客户端凭证模式
    /// </summary>
    ClientCredentials = 3,

    /// <summary>
    /// 刷新令牌
    /// </summary>
    RefreshToken = 4
}

/// <summary>
/// 文件类型枚举
/// </summary>
public enum FileType
{
    /// <summary>
    /// 图片
    /// </summary>
    Image = 0,

    /// <summary>
    /// 文档
    /// </summary>
    Document = 1,

    /// <summary>
    /// 视频
    /// </summary>
    Video = 2,

    /// <summary>
    /// 音频
    /// </summary>
    Audio = 3,

    /// <summary>
    /// 压缩包
    /// </summary>
    Archive = 4,

    /// <summary>
    /// 其他
    /// </summary>
    Other = 99
}

/// <summary>
/// 操作类型枚举
/// </summary>
public enum OperationType
{
    /// <summary>
    /// 登录
    /// </summary>
    Login = 0,

    /// <summary>
    /// 登出
    /// </summary>
    Logout = 1,

    /// <summary>
    /// 查询
    /// </summary>
    Query = 2,

    /// <summary>
    /// 新增
    /// </summary>
    Create = 3,

    /// <summary>
    /// 修改
    /// </summary>
    Update = 4,

    /// <summary>
    /// 删除
    /// </summary>
    Delete = 5,

    /// <summary>
    /// 导入
    /// </summary>
    Import = 6,

    /// <summary>
    /// 导出
    /// </summary>
    Export = 7,

    /// <summary>
    /// 其他
    /// </summary>
    Other = 99
}

/// <summary>
/// 通知类型枚举
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// 系统通知
    /// </summary>
    System = 0,

    /// <summary>
    /// 用户通知
    /// </summary>
    User = 1,

    /// <summary>
    /// 公告
    /// </summary>
    Announcement = 2,

    /// <summary>
    /// 警告
    /// </summary>
    Warning = 3,

    /// <summary>
    /// 错误
    /// </summary>
    Error = 4
}

/// <summary>
/// 通知状态枚举
/// </summary>
public enum NotificationStatus
{
    /// <summary>
    /// 未读
    /// </summary>
    Unread = 0,

    /// <summary>
    /// 已读
    /// </summary>
    Read = 1,

    /// <summary>
    /// 已删除
    /// </summary>
    Deleted = 2
}

/// <summary>
/// 邮件状态枚举
/// </summary>
public enum EmailStatus
{
    /// <summary>
    /// 待发送
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 发送中
    /// </summary>
    Sending = 1,

    /// <summary>
    /// 发送成功
    /// </summary>
    Success = 2,

    /// <summary>
    /// 发送失败
    /// </summary>
    Failed = 3,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled = 4
}

/// <summary>
/// 短信状态枚举
/// </summary>
public enum SmsStatus
{
    /// <summary>
    /// 待发送
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 发送中
    /// </summary>
    Sending = 1,

    /// <summary>
    /// 发送成功
    /// </summary>
    Success = 2,

    /// <summary>
    /// 发送失败
    /// </summary>
    Failed = 3,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled = 4
}

/// <summary>
/// 邮件类型枚举
/// </summary>
public enum EmailType
{
    /// <summary>
    /// 系统邮件
    /// </summary>
    System = 0,

    /// <summary>
    /// 验证邮件
    /// </summary>
    Verification = 1,

    /// <summary>
    /// 通知邮件
    /// </summary>
    Notification = 2,

    /// <summary>
    /// 营销邮件
    /// </summary>
    Marketing = 3,

    /// <summary>
    /// 自定义邮件
    /// </summary>
    Custom = 99
}

/// <summary>
/// 短信类型枚举
/// </summary>
public enum SmsType
{
    /// <summary>
    /// 验证码
    /// </summary>
    VerificationCode = 0,

    /// <summary>
    /// 通知短信
    /// </summary>
    Notification = 1,

    /// <summary>
    /// 营销短信
    /// </summary>
    Marketing = 2,

    /// <summary>
    /// 自定义短信
    /// </summary>
    Custom = 99
}

/// <summary>
/// 审核状态枚举
/// </summary>
public enum AuditStatus
{
    /// <summary>
    /// 待审核
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 审核中
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// 审核通过
    /// </summary>
    Approved = 2,

    /// <summary>
    /// 审核拒绝
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// 已撤回
    /// </summary>
    Withdrawn = 4
}

/// <summary>
/// 审核结果枚举
/// </summary>
public enum AuditResult
{
    /// <summary>
    /// 通过
    /// </summary>
    Pass = 0,

    /// <summary>
    /// 拒绝
    /// </summary>
    Reject = 1,

    /// <summary>
    /// 退回修改
    /// </summary>
    Return = 2
}

/// <summary>
/// 任务状态枚举
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// 待执行
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 执行中
    /// </summary>
    Running = 1,

    /// <summary>
    /// 执行成功
    /// </summary>
    Success = 2,

    /// <summary>
    /// 执行失败
    /// </summary>
    Failed = 3,

    /// <summary>
    /// 已停止
    /// </summary>
    Stopped = 4,

    /// <summary>
    /// 已暂停
    /// </summary>
    Paused = 5
}

/// <summary>
/// 任务触发类型枚举
/// </summary>
public enum TriggerType
{
    /// <summary>
    /// 立即执行
    /// </summary>
    Immediate = 0,

    /// <summary>
    /// 定时执行
    /// </summary>
    Schedule = 1,

    /// <summary>
    /// 循环执行
    /// </summary>
    Recurring = 2,

    /// <summary>
    /// Cron表达式
    /// </summary>
    Cron = 3
}

/// <summary>
/// 统计时间范围枚举
/// </summary>
public enum StatisticsPeriod
{
    /// <summary>
    /// 今日
    /// </summary>
    Today = 0,

    /// <summary>
    /// 昨日
    /// </summary>
    Yesterday = 1,

    /// <summary>
    /// 本周
    /// </summary>
    ThisWeek = 2,

    /// <summary>
    /// 上周
    /// </summary>
    LastWeek = 3,

    /// <summary>
    /// 本月
    /// </summary>
    ThisMonth = 4,

    /// <summary>
    /// 上月
    /// </summary>
    LastMonth = 5,

    /// <summary>
    /// 本年
    /// </summary>
    ThisYear = 6,

    /// <summary>
    /// 去年
    /// </summary>
    LastYear = 7,

    /// <summary>
    /// 自定义
    /// </summary>
    Custom = 99
}

/// <summary>
/// 访问结果枚举
/// </summary>
public enum AccessResult
{
    /// <summary>
    /// 成功
    /// </summary>
    Success = 0,

    /// <summary>
    /// 失败
    /// </summary>
    Failed = 1,

    /// <summary>
    /// 权限不足
    /// </summary>
    Forbidden = 2,

    /// <summary>
    /// 未授权
    /// </summary>
    Unauthorized = 3,

    /// <summary>
    /// 资源不存在
    /// </summary>
    NotFound = 4,

    /// <summary>
    /// 服务器错误
    /// </summary>
    ServerError = 5
}
