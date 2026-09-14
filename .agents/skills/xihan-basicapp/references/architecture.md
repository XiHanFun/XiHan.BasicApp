# 全栈架构与功能归属

## 总体原则

一项功能从领域和契约开始，而不是从页面或接口入口开始。先确定所属模块，再让 Domain、Application、Infrastructure、WebHost 和前端按同一业务边界协作。

## 后端模块

| 模块 | 主要职责 |
| --- | --- |
| Saas | 租户、用户、组织、角色、RBAC/ABAC、配置、日志与平台基础能力 |
| AI | 模型、Provider、提示词、知识与 AI 应用能力 |
| Chat | 会话、消息、实时通信与协作 |
| CodeGeneration | 代码生成元数据、模板和运行流程 |
| Printing | 打印模板、数据与渲染流程 |
| Workflow | 流程定义、实例、任务与审批 |
| business/Sample | 业务扩展示例，不是通用能力倾倒区 |

新能力优先归入已有模块。只有职责稳定、可独立启停、具有清晰契约与依赖时才新建模块。

## 后端分层

- `Domain/Entities`：实体、值对象、枚举和领域状态。
- `Domain/Services`：跨实体业务规则与领域操作。
- `Domain/Repositories`：仓储契约，不泄漏 SqlSugar 查询实现。
- `Application/Contracts` 与 `Dtos`：网络契约和用例接口。
- `Application/AppServices`：写命令和用例编排。
- `Application/QueryServices`：读取、分页和投影。
- `Application/Mappers`：显式 DTO 映射。
- `Infrastructure/Repositories`：SqlSugar 数据访问实现。
- `Infrastructure/Seeders`：权限、菜单、角色和默认数据。
- `Extensions` 与 Module 类：服务注册、依赖和初始化。

不要把业务规则放入 WebHost、Controller、前端或仓储 SQL 表达式中。

## 前端归属

- 路由页面放在 `frontend/src/views/<domain>`。
- API facade 和 DTO 类型放在 `frontend/src/api/modules/<domain>`。
- 可选模块前端放在 `frontend/src/modules/<module>`，只包含该模块的页面、API、语言、`setup.ts` 和说明。
- 跨业务共享能力放在 `frontend/packages` 对应包；只有单页使用的状态留在页面。
- 应用主题桥位于现有 XiHan.UI 样式接入层，不复制 XiHan.UI 组件皮肤。

## 完整功能顺序

1. 定义领域模型、权限和租户语义。
2. 固定 DTO、命令/查询边界和错误语义。
3. 实现仓储、领域服务、Application Service 和种子数据。
4. 实现类型化前端 API、页面、状态、路由和权限可见性。
5. 添加后端、前端和契约测试。
6. 验证真实数据库/服务接线、权限、租户隔离和浏览器交互。

跨端改动应形成同一可运行闭环，不能先发布半套协议再依赖静默兼容。
