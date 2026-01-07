# XiHan.BasicApp.Rbac 服务实现总结

## 📋 概述

本文档总结了 `XiHan.BasicApp.Rbac` 模块中新实现的 Application Services，这些服务遵循 **DDD（领域驱动设计）** 和 **CQRS（命令查询职责分离）** 架构模式。

---

## 🎯 实现原则

### 1. CQRS 分离

- **Command Services**: 处理写操作（创建、更新、删除）
- **Query Services**: 处理读操作（查询、检索）

### 2. 职责清晰

- **Application Services**: 用例编排、DTO 映射、权限校验
- **Domain Services**: 跨聚合业务规则
- **Repository**: 数据持久化

### 3. 继承框架基类

- Command Services 继承自 `CrudApplicationServiceBase`
- Query Services 继承自 `ApplicationServiceBase`
- 充分利用框架提供的通用功能

---

## 📦 新实现的服务（共 20 个）

### 一、文件管理服务（File）

#### 1. FileCommandService

**职责**: 处理文件的写操作

**核心方法**:

- `CreateAsync()` - 创建文件记录（含哈希去重检查）
- `DeleteAsync()` - 删除文件记录（含物理文件删除逻辑预留）
- `BatchDeleteAsync()` - 批量删除文件

**业务亮点**:

- 文件哈希去重检测
- 物理文件删除接口预留

#### 2. FileQueryService

**职责**: 处理文件的读操作

**核心方法**:

- `GetByIdAsync()` - 根据 ID 获取文件
- `GetByFileHashAsync()` - 根据文件哈希查询（去重场景）
- `GetByStoragePathAsync()` - 根据存储路径查询
- `GetByUploadUserIdAsync()` - 获取用户上传的文件列表
- `GetByFileTypeAsync()` - 根据文件类型获取文件列表
- `GetByTimeRangeAsync()` - 获取指定时间段内的文件
- `GetTotalFileSizeAsync()` - 获取文件总大小统计
- `GetPagedAsync()` - 分页查询

---

### 二、OAuth 认证服务

#### 3. OAuthAppCommandService

**职责**: 处理 OAuth 应用的写操作

**核心方法**:

- `CreateAsync()` - 创建 OAuth 应用（自动生成 ClientId 和 ClientSecret）
- `ResetClientSecretAsync()` - 重置应用密钥
- `SetEnabledAsync()` - 启用或禁用应用

**业务亮点**:

- 自动生成安全的 ClientId 和 ClientSecret
- 应用名称唯一性检查

#### 4. OAuthAppQueryService

**职责**: 处理 OAuth 应用的读操作

**核心方法**:

- `GetByIdAsync()` - 根据 ID 获取应用
- `GetByClientIdAsync()` - 根据 ClientId 获取应用
- `GetEnabledAppsAsync()` - 获取所有启用的应用
- `ValidateClientCredentialsAsync()` - 验证 ClientId 和 ClientSecret
- `GetPagedAsync()` - 分页查询

#### 5. OAuthTokenCommandService

**职责**: 处理 OAuth 令牌的写操作

**核心方法**:

- `CreateAsync()` - 创建令牌（自动生成 AccessToken 和 RefreshToken）
- `RefreshTokenAsync()` - 刷新令牌（验证旧令牌并生成新令牌）
- `RevokeTokenAsync()` - 撤销令牌
- `CleanupExpiredTokensAsync()` - 清理过期令牌

**业务亮点**:

- 访问令牌 2 小时有效期
- 刷新令牌 30 天有效期
- 刷新时自动撤销旧令牌

#### 6. OAuthTokenQueryService

**职责**: 处理 OAuth 令牌的读操作

**核心方法**:

- `GetByAccessTokenAsync()` - 根据访问令牌获取信息
- `GetByRefreshTokenAsync()` - 根据刷新令牌获取信息
- `GetValidTokensByUserIdAsync()` - 获取用户的有效令牌列表
- `ValidateAccessTokenAsync()` - 验证访问令牌
- `GetPagedAsync()` - 分页查询

---

### 三、通知系统服务

#### 7. NotificationCommandService

**职责**: 处理通知的写操作

**核心方法**:

- `CreateAsync()` - 创建并发送通知
- `MarkAsReadAsync()` - 标记通知为已读
- `BatchMarkAsReadAsync()` - 批量标记为已读
- `DeleteNotificationAsync()` - 删除通知
- `DeleteExpiredNotificationsAsync()` - 批量删除过期通知

#### 8. NotificationQueryService

**职责**: 处理通知的读操作

**核心方法**:

- `GetByIdAsync()` - 根据 ID 获取通知
- `GetUserNotificationsAsync()` - 获取用户的通知列表（支持只查未读）
- `GetUnreadCountAsync()` - 获取用户的未读通知数量
- `GetByNotificationTypeAsync()` - 根据通知类型获取列表
- `GetPagedAsync()` - 分页查询

#### 9. EmailCommandService

**职责**: 处理邮件的写操作

**核心方法**:

- `CreateAsync()` - 创建邮件记录并触发发送
- `UpdateSendStatusAsync()` - 更新邮件发送状态
- `RetryFailedEmailAsync()` - 重试发送失败的邮件
- `DeleteSentEmailsBeforeDateAsync()` - 批量删除已发送的旧邮件

**业务亮点**:

- 发送状态管理（Pending、Sent、Failed）
- 重试机制支持
- 错误信息记录

#### 10. EmailQueryService

**职责**: 处理邮件的读操作

**核心方法**:

- `GetByIdAsync()` - 根据 ID 获取邮件
- `GetBySendStatusAsync()` - 根据发送状态获取邮件列表
- `GetPendingEmailsAsync()` - 获取待发送的邮件列表
- `GetFailedEmailsAsync()` - 获取发送失败的邮件列表
- `GetPagedAsync()` - 分页查询

#### 11. SmsCommandService

**职责**: 处理短信的写操作

**核心方法**:

- `CreateAsync()` - 创建短信记录并触发发送
- `UpdateSendStatusAsync()` - 更新短信发送状态
- `RetryFailedSmsAsync()` - 重试发送失败的短信
- `DeleteSentSmsBeforeDateAsync()` - 批量删除已发送的旧短信

**业务亮点**:

- 与邮件服务类似的状态管理
- 重试计数器
- 自动清理历史数据

#### 12. SmsQueryService

**职责**: 处理短信的读操作

**核心方法**:

- `GetByIdAsync()` - 根据 ID 获取短信
- `GetByPhoneNumberAsync()` - 根据手机号获取短信列表
- `GetBySendStatusAsync()` - 根据发送状态获取短信列表
- `GetPendingSmsAsync()` - 获取待发送的短信列表
- `GetFailedSmsAsync()` - 获取发送失败的短信列表
- `GetPagedAsync()` - 分页查询

---

### 四、会话管理服务

#### 13. UserSessionCommandService

**职责**: 处理用户会话的写操作

**核心方法**:

- `CreateAsync()` - 创建用户会话（自动生成 SessionToken）
- `UpdateLastAccessTimeAsync()` - 更新会话最后访问时间
- `LogoutAsync()` - 注销会话
- `LogoutUserSessionsAsync()` - 批量注销用户的所有会话
- `CleanupExpiredSessionsAsync()` - 清理过期会话
- `ForceLogoutDeviceAsync()` - 强制注销指定设备的会话

**业务亮点**:

- 会话 Token 自动生成
- 会话过期时间管理（默认 7 天）
- 多设备会话管理
- 支持强制注销

#### 14. UserSessionQueryService

**职责**: 处理用户会话的读操作

**核心方法**:

- `GetByIdAsync()` - 根据 ID 获取会话
- `GetBySessionTokenAsync()` - 根据会话 Token 获取会话
- `GetActiveSessionsByUserIdAsync()` - 获取用户的所有活跃会话
- `ValidateSessionAsync()` - 验证会话是否有效
- `GetSessionStatsAsync()` - 获取用户的会话统计信息
- `GetPagedAsync()` - 分页查询

---

### 五、日志查询服务（仅 Query Services）

#### 15. OperationLogQueryService

**职责**: 处理操作日志的读操作

**核心方法**:

- `GetByIdAsync()` - 根据 ID 获取操作日志
- `GetByUserIdAsync()` - 根据用户 ID 获取日志
- `GetByOperationTypeAsync()` - 根据操作类型获取日志
- `GetByModuleNameAsync()` - 根据模块名称获取日志
- `GetByTimeRangeAsync()` - 根据时间范围获取日志
- `GetByOperationResultAsync()` - 根据操作结果获取日志（成功/失败）
- `GetPagedAsync()` - 分页查询

#### 16. LoginLogQueryService

**职责**: 处理登录日志的读操作

**核心方法**:

- `GetByIdAsync()` - 根据 ID 获取登录日志
- `GetByUserIdAsync()` - 根据用户 ID 获取日志
- `GetByUsernameAsync()` - 根据用户名获取日志
- `GetByIpAddressAsync()` - 根据 IP 地址获取日志
- `GetByLoginResultAsync()` - 根据登录结果获取日志
- `GetFailedLoginsAsync()` - 获取失败登录日志
- `GetByTimeRangeAsync()` - 根据时间范围获取日志
- `GetRecentLoginsByUserIdAsync()` - 获取用户最近 N 次登录记录
- `GetPagedAsync()` - 分页查询

#### 17. ApiLogQueryService

**职责**: 处理 API 调用日志的读操作

**核心方法**:

- `GetByIdAsync()` - 根据 ID 获取 API 日志
- `GetByUserIdAsync()` - 根据用户 ID 获取日志
- `GetByApiPathAsync()` - 根据 API 路径获取日志
- `GetByHttpMethodAsync()` - 根据 HTTP 方法获取日志
- `GetByStatusCodeAsync()` - 根据 HTTP 状态码获取日志
- `GetByTimeRangeAsync()` - 根据时间范围获取日志
- `GetSlowRequestsAsync()` - 获取慢请求日志
- `GetErrorLogsAsync()` - 获取错误日志（状态码>=400）
- `GetPagedAsync()` - 分页查询

#### 18. AccessLogQueryService

**职责**: 处理访问日志的读操作

**核心方法**:

- `GetByIdAsync()` - 根据 ID 获取访问日志
- `GetByUserIdAsync()` - 根据用户 ID 获取日志
- `GetByResourceIdAsync()` - 根据资源 ID 获取日志
- `GetByIpAddressAsync()` - 根据 IP 地址获取日志
- `GetByAccessResultAsync()` - 根据访问结果获取日志
- `GetDeniedAccessLogsAsync()` - 获取拒绝访问的日志
- `GetByTimeRangeAsync()` - 根据时间范围获取日志
- `GetPagedAsync()` - 分页查询

---

## 📊 统计数据

### 服务数量统计

| 类别                    | Command Services | Query Services | 总计   |
| ----------------------- | ---------------- | -------------- | ------ |
| 文件管理                | 1                | 1              | 2      |
| OAuth 认证              | 2                | 2              | 4      |
| 通知系统                | 3                | 3              | 6      |
| 会话管理                | 1                | 1              | 2      |
| 日志查询                | 0                | 4              | 4      |
| **核心 RBAC（已实现）** | 8                | 8              | 16     |
| **新增服务**            | 7                | 11             | 18     |
| **总计**                | **15**           | **19**         | **34** |

### 方法数量统计

| 服务名称                   | 核心方法数 |
| -------------------------- | ---------- |
| FileCommandService         | 3          |
| FileQueryService           | 8          |
| OAuthAppCommandService     | 3          |
| OAuthAppQueryService       | 5          |
| OAuthTokenCommandService   | 4          |
| OAuthTokenQueryService     | 5          |
| NotificationCommandService | 5          |
| NotificationQueryService   | 5          |
| EmailCommandService        | 4          |
| EmailQueryService          | 5          |
| SmsCommandService          | 4          |
| SmsQueryService            | 6          |
| UserSessionCommandService  | 6          |
| UserSessionQueryService    | 6          |
| OperationLogQueryService   | 7          |
| LoginLogQueryService       | 9          |
| ApiLogQueryService         | 9          |
| AccessLogQueryService      | 8          |
| **新增服务总方法数**       | **102+**   |

---

## 🏗️ 架构特点

### 1. 领域驱动设计（DDD）

- **聚合根识别清晰**: User, Role, Permission, Menu, Resource, Tenant, File, OAuthApp, OAuthToken 等
- **值对象与关系表分离**: UserRole, RolePermission 等由聚合根维护
- **领域服务与应用服务分离**: 跨聚合业务逻辑放在 Domain Services

### 2. CQRS 模式

- **读写分离**: Command Services 和 Query Services 职责明确
- **优化查询**: Query Services 可以进行特定的查询优化
- **扩展性强**: 未来可以独立扩展读写能力

### 3. 依赖注入

- 所有服务通过构造函数注入依赖
- 遵循依赖倒置原则（DIP）
- 面向接口编程

### 4. DTO 映射

- 使用 Mapster 进行高性能映射
- 输入输出 DTO 分离
- 实体与 DTO 解耦

---

## 🔄 未来扩展点

### 1. DTO 定义

当前服务使用了 `RbacDtoBase` 作为占位符，未来需要：

- 为每个实体定义专用的 EntityDto
- 为每个实体定义专用的 CreateDto 和 UpdateDto
- 配置 Mapster 映射规则

### 2. Repository 实现

当前只定义了 Repository 接口，未来需要：

- 实现 SqlSugar 的具体 Repository
- 实现复杂查询逻辑
- 实现事务管理

### 3. 业务规则

- 添加更多的业务验证逻辑
- 实现完整的错误处理
- 添加日志和审计

### 4. 集成测试

- 为每个服务添加单元测试
- 添加集成测试
- 性能测试

### 5. API 控制器

- 为每个服务创建对应的 API Controller
- 实现 RESTful API
- 添加 Swagger 文档

---

## ✅ 完成度

### 已完成

- ✅ 26 个 Repository 接口定义
- ✅ 5 个 Domain Services
- ✅ 15 个 Command Services
- ✅ 19 个 Query Services
- ✅ 架构文档和实现总结

### 待完成

- ⏳ DTO 定义和映射配置
- ⏳ Repository 具体实现（Infrastructure 层）
- ⏳ API Controllers
- ⏳ 单元测试和集成测试
- ⏳ 前端集成

---

## 📝 备注

1. **日志服务说明**: 日志服务（Operation、Login、Api、Access）通常只需要查询功能，因此只实现了 Query Services。日志的写入通常由中间件或 AOP 自动完成。

2. **OAuth Code 服务**: OAuth Code（授权码）生命周期很短，通常由 OAuth 流程自动管理，不需要独立的 Application Service。

3. **Task 服务**: 任务调度服务（SysTask）需要与调度引擎集成，建议后续单独实现。

4. **Audit 服务**: 审计服务与日志服务类似，需要结合具体的审计策略实现。

---

## 🎓 参考资料

- [领域驱动设计（DDD）](https://en.wikipedia.org/wiki/Domain-driven_design)
- [CQRS 模式](https://martinfowler.com/bliki/CQRS.html)
- [XiHan.Framework 架构文档](../../../XiHan.Framework/framework/docs/)

---

**最后更新时间**: 2025-01-07  
**作者**: XiHan Team  
**版本**: v2.0.0-alpha
