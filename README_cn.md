<div align="center">

<img src="./assets/banner_cn.png" alt="XiHan.BasicApp" />

<h1>XiHan.BasicApp</h1>

<p><b>基于 XiHan.Framework 和 XiHan.UI 的超高颜值通用中后台内核</b></p>

<p>后端基于 .NET 与 <a href="https://github.com/XiHanFun/XiHan.Framework">XiHan.Framework</a>，前端基于 Vue 与 <a href="https://github.com/XiHanFun/XiHan.UI">XiHan.UI</a><br/>多租户 · RBAC + 数据范围 + 字段脱敏 · 代码生成 · 实时通信</p>

<p><a href="./README.md">English</a> | <b>简体中文</b></p>

<p>
  <a href="https://github.com/XiHanFun/XiHan.BasicApp/stargazers"><img alt="GitHub Stars" src="https://img.shields.io/github/stars/XiHanFun/XiHan.BasicApp?style=flat-square&logo=github&label=Stars&color=1f6feb" /></a>
  <a href="https://gitee.com/XiHanFun/XiHan.BasicApp"><img alt="Gitee Stars" src="https://gitee.com/XiHanFun/XiHan.BasicApp/badge/star.svg" /></a>
  <a href="https://gitcode.com/XiHanFun/XiHan.BasicApp"><img alt="GitCode Stars" src="https://gitcode.com/XiHanFun/XiHan.BasicApp/star/badge.svg" /></a>
</p>


<p>
  <img alt=".NET" src="https://img.shields.io/badge/.NET-512BD4?style=flat-square&logo=dotnet&logoColor=white" />
  <a href="https://github.com/XiHanFun/XiHan.Framework"><img alt="XiHan.Framework" src="https://img.shields.io/badge/XiHan.Framework-6f42c1?style=flat-square" /></a>
  <img alt="Vue" src="https://img.shields.io/badge/Vue-4FC08D?style=flat-square&logo=vuedotjs&logoColor=white" />
  <img alt="TypeScript" src="https://img.shields.io/badge/TypeScript-3178C6?style=flat-square&logo=typescript&logoColor=white" />
  <img alt="Vite" src="https://img.shields.io/badge/Vite-646CFF?style=flat-square&logo=vite&logoColor=white" />
  <img alt="Tailwind CSS" src="https://img.shields.io/badge/Tailwind_CSS-06B6D4?style=flat-square&logo=tailwindcss&logoColor=white" />
  <a href="https://www.nuget.org/packages?q=XiHan.BasicApp"><img alt="NuGet" src="https://img.shields.io/nuget/v/XiHan.BasicApp.Core?style=flat-square&logo=nuget&logoColor=white&label=NuGet&color=004880" /></a>
</p>

<p>
  <a href="./LICENSE"><img alt="License" src="https://img.shields.io/github/license/XiHanFun/XiHan.BasicApp?style=flat-square&color=green" /></a>
  <a href="https://github.com/XiHanFun/XiHan.BasicApp/commits"><img alt="Last Commit" src="https://img.shields.io/github/last-commit/XiHanFun/XiHan.BasicApp?style=flat-square&color=blueviolet" /></a>
  <img alt="Commit Activity" src="https://img.shields.io/github/commit-activity/m/XiHanFun/XiHan.BasicApp?style=flat-square" />
  <a href="https://github.com/XiHanFun/XiHan.BasicApp/issues"><img alt="Issues" src="https://img.shields.io/github/issues/XiHanFun/XiHan.BasicApp?style=flat-square" /></a>
  <a href="https://github.com/XiHanFun/XiHan.BasicApp/graphs/contributors"><img alt="Contributors" src="https://img.shields.io/github/contributors/XiHanFun/XiHan.BasicApp?style=flat-square" /></a>
  <img alt="Repo Size" src="https://img.shields.io/github/repo-size/XiHanFun/XiHan.BasicApp?style=flat-square" />
</p>

<p>
  <a href="https://deepwiki.com/XiHanFun/XiHan.BasicApp"><img alt="Ask DeepWiki" src="https://deepwiki.com/badge.svg" /></a>
  <a href="https://basicapp.docs.xihanfun.com"><img alt="Docs" src="https://img.shields.io/badge/Docs-basicapp.docs.xihanfun.com-2496ED?style=flat-square&logo=readthedocs&logoColor=white" /></a>
  <a href="https://qm.qq.com/q/qYp1Urv3z2"><img alt="QQ Group" src="https://img.shields.io/badge/QQ_Group-462371834-EB1923?style=flat-square&logo=tencentqq&logoColor=white" /></a>
</p>
<p>
  <a href="https://trendshift.io/repositories/83127?utm_source=trendshift-badge&amp;utm_medium=badge&amp;utm_campaign=badge-trendshift-83127" target="_blank" rel="noopener noreferrer"><img src="https://trendshift.io/api/badge/trendshift/repositories/83127/daily?language=C%23" alt="XiHanFun%2FXiHan.BasicApp | Trendshift" width="250" height="55"/></a>
</p>


<img src="./assets/preview/login.png" alt="登录" />

</div>

## 简介

XiHan.BasicApp 采用前后端分离架构。后端遵循 DDD 分层，写路径走应用服务、读路径走查询服务，应用服务经动态 API 直接暴露为 REST 接口；前端使用 Vue + TypeScript + XiHan.UI，后端使用 .NET + XiHan.Framework。系统内置完整的身份、权限、租户与审计能力，既可作为中后台项目的起点，也可作为 .NET + Vue 全栈实践的参考。属于曦寒懿（XiHanFun）开源生态的基础应用。

## 文档

| 去处 | 内容 |
| --- | --- |
| [文档站](https://basicapp.docs.xihanfun.com) | 完整指南与逐主题专题文档 |
| [后端工程说明](./backend/README_cn.md) | 分层结构、工程清单、接口暴露、依赖构成、本地开发 |
| [前端工程说明](./frontend/README_cn.md) | 技术栈、monorepo 结构、架构约束、本地开发 |

## 预览

<table>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/dashboard.png"><img src="./assets/preview/dashboard.png" alt="工作台" /></a><br/>工作台</td>
    <td align="center" width="50%"><a href="./assets/preview/user.png"><img src="./assets/preview/user.png" alt="用户管理" /></a><br/>用户管理</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/log-trace.png"><img src="./assets/preview/log-trace.png" alt="日志链路追踪" /></a><br/>日志链路追踪</td>
    <td align="center" width="50%"><a href="./assets/preview/printing.png"><img src="./assets/preview/printing.png" alt="打印模板" /></a><br/>打印模板</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/control-center.png"><img src="./assets/preview/control-center.png" alt="控制中心" /></a><br/>控制中心</td>
    <td align="center" width="50%"><a href="./assets/preview/login.png"><img src="./assets/preview/login.png" alt="登录认证" /></a><br/>登录认证</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/profile.png"><img src="./assets/preview/profile.png" alt="个人中心" /></a><br/>个人中心</td>
    <td align="center" width="50%"><a href="./assets/preview/profile-security.png"><img src="./assets/preview/profile-security.png" alt="个人中心（安全设置）" /></a><br/>个人中心（安全设置）</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/online-user.png"><img src="./assets/preview/online-user.png" alt="在线用户" /></a><br/>在线用户</td>
    <td align="center" width="50%"><a href="./assets/preview/role.png"><img src="./assets/preview/role.png" alt="角色管理" /></a><br/>角色管理</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/org.png"><img src="./assets/preview/org.png" alt="组织机构" /></a><br/>组织机构</td>
    <td align="center" width="50%"><a href="./assets/preview/position.png"><img src="./assets/preview/position.png" alt="岗位管理" /></a><br/>岗位管理</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/permission.png"><img src="./assets/preview/permission.png" alt="权限管理" /></a><br/>权限管理</td>
    <td align="center" width="50%"><a href="./assets/preview/menu.png"><img src="./assets/preview/menu.png" alt="菜单管理" /></a><br/>菜单管理</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/field-security.png"><img src="./assets/preview/field-security.png" alt="字段安全" /></a><br/>字段安全</td>
    <td align="center" width="50%"><a href="./assets/preview/authorization.png"><img src="./assets/preview/authorization.png" alt="授权申请与委托" /></a><br/>授权申请与委托</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/review.png"><img src="./assets/preview/review.png" alt="审批中心" /></a><br/>审批中心</td>
    <td align="center" width="50%"><a href="./assets/preview/constraint.png"><img src="./assets/preview/constraint.png" alt="审批约束" /></a><br/>审批约束</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/tenant.png"><img src="./assets/preview/tenant.png" alt="租户管理" /></a><br/>租户管理</td>
    <td align="center" width="50%"><a href="./assets/preview/tenant-members.png"><img src="./assets/preview/tenant-members.png" alt="租户成员" /></a><br/>租户成员</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/edition.png"><img src="./assets/preview/edition.png" alt="版本套餐" /></a><br/>版本套餐</td>
    <td align="center" width="50%"><a href="./assets/preview/subscription.png"><img src="./assets/preview/subscription.png" alt="我的订阅" /></a><br/>我的订阅</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/notification.png"><img src="./assets/preview/notification.png" alt="通知公告" /></a><br/>通知公告</td>
    <td align="center" width="50%"><a href="./assets/preview/inbox.png"><img src="./assets/preview/inbox.png" alt="我的消息" /></a><br/>我的消息</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/message-template.png"><img src="./assets/preview/message-template.png" alt="消息模板" /></a><br/>消息模板</td>
    <td align="center" width="50%"><a href="./assets/preview/message-record.png"><img src="./assets/preview/message-record.png" alt="邮件短信" /></a><br/>邮件短信</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/chat.png"><img src="./assets/preview/chat.png" alt="在线聊天" /></a><br/>在线聊天</td>
    <td align="center" width="50%"><a href="./assets/preview/chat-audit.png"><img src="./assets/preview/chat-audit.png" alt="聊天审计" /></a><br/>聊天审计</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/file-library.png"><img src="./assets/preview/file-library.png" alt="文件管理" /></a><br/>文件管理</td>
    <td align="center" width="50%"><a href="./assets/preview/file-storage.png"><img src="./assets/preview/file-storage.png" alt="存储配置" /></a><br/>存储配置</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/export-center.png"><img src="./assets/preview/export-center.png" alt="导出中心" /></a><br/>导出中心</td>
    <td align="center" width="50%"><a href="./assets/preview/dict.png"><img src="./assets/preview/dict.png" alt="字典管理" /></a><br/>字典管理</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/config.png"><img src="./assets/preview/config.png" alt="参数配置" /></a><br/>参数配置</td>
    <td align="center" width="50%"><a href="./assets/preview/numbering.png"><img src="./assets/preview/numbering.png" alt="业务编号" /></a><br/>业务编号</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/job.png"><img src="./assets/preview/job.png" alt="任务调度" /></a><br/>任务调度</td>
    <td align="center" width="50%"><a href="./assets/preview/cache.png"><img src="./assets/preview/cache.png" alt="缓存管理" /></a><br/>缓存管理</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/server.png"><img src="./assets/preview/server.png" alt="服务监控" /></a><br/>服务监控</td>
    <td align="center" width="50%"><a href="./assets/preview/version.png"><img src="./assets/preview/version.png" alt="版本管理" /></a><br/>版本管理</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/email-config.png"><img src="./assets/preview/email-config.png" alt="邮件配置" /></a><br/>邮件配置</td>
    <td align="center" width="50%"><a href="./assets/preview/sms-config.png"><img src="./assets/preview/sms-config.png" alt="短信配置" /></a><br/>短信配置</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/bot-config.png"><img src="./assets/preview/bot-config.png" alt="机器人配置" /></a><br/>机器人配置</td>
    <td align="center" width="50%"><a href="./assets/preview/telegram-bot.png"><img src="./assets/preview/telegram-bot.png" alt="Telegram 机器人" /></a><br/>Telegram 机器人</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/openapi-app.png"><img src="./assets/preview/openapi-app.png" alt="应用管理" /></a><br/>应用管理</td>
    <td align="center" width="50%"><a href="./assets/preview/openapi-credentials.png"><img src="./assets/preview/openapi-credentials.png" alt="开放接口凭证" /></a><br/>开放接口凭证</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/log-access.png"><img src="./assets/preview/log-access.png" alt="访问日志" /></a><br/>访问日志</td>
    <td align="center" width="50%"><a href="./assets/preview/log-api.png"><img src="./assets/preview/log-api.png" alt="开放接口日志" /></a><br/>开放接口日志</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/log-operation.png"><img src="./assets/preview/log-operation.png" alt="操作日志" /></a><br/>操作日志</td>
    <td align="center" width="50%"><a href="./assets/preview/log-login.png"><img src="./assets/preview/log-login.png" alt="登录日志" /></a><br/>登录日志</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/log-exception.png"><img src="./assets/preview/log-exception.png" alt="异常日志" /></a><br/>异常日志</td>
    <td align="center" width="50%"><a href="./assets/preview/log-diff.png"><img src="./assets/preview/log-diff.png" alt="数据变更日志" /></a><br/>数据变更日志</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/log-permission.png"><img src="./assets/preview/log-permission.png" alt="权限变更日志" /></a><br/>权限变更日志</td>
    <td align="center" width="50%"><a href="./assets/preview/log-trace-timeline.png"><img src="./assets/preview/log-trace-timeline.png" alt="日志链路时间线" /></a><br/>日志链路时间线</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/log-migration.png"><img src="./assets/preview/log-migration.png" alt="升级记录" /></a><br/>升级记录</td>
    <td align="center" width="50%"><a href="./assets/preview/codegen.png"><img src="./assets/preview/codegen.png" alt="代码生成" /></a><br/>代码生成</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/ai-provider.png"><img src="./assets/preview/ai-provider.png" alt="AI 提供商" /></a><br/>AI 提供商</td>
    <td align="center" width="50%"><a href="./assets/preview/ai-prompt.png"><img src="./assets/preview/ai-prompt.png" alt="AI 提示词" /></a><br/>AI 提示词</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/knowledge.png"><img src="./assets/preview/knowledge.png" alt="知识库" /></a><br/>知识库</td>
    <td align="center" width="50%"><a href="./assets/preview/ai-assistant.png"><img src="./assets/preview/ai-assistant.png" alt="AI 助手" /></a><br/>AI 助手</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/workflow-definition.png"><img src="./assets/preview/workflow-definition.png" alt="流程定义" /></a><br/>流程定义</td>
    <td align="center" width="50%"><a href="./assets/preview/workflow-json.png"><img src="./assets/preview/workflow-json.png" alt="流程 JSON 编辑" /></a><br/>流程 JSON 编辑</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/workflow-instance.png"><img src="./assets/preview/workflow-instance.png" alt="流程实例" /></a><br/>流程实例</td>
    <td align="center" width="50%"><a href="./assets/preview/workflow-todo.png"><img src="./assets/preview/workflow-todo.png" alt="我的待办" /></a><br/>我的待办</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/preferences.png"><img src="./assets/preview/preferences.png" alt="偏好设置" /></a><br/>偏好设置</td>
    <td align="center" width="50%"><a href="./assets/preview/schema-page.png"><img src="./assets/preview/schema-page.png" alt="高级列表" /></a><br/>高级列表</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/editors.png"><img src="./assets/preview/editors.png" alt="Markdown 编辑器" /></a><br/>Markdown 编辑器</td>
    <td align="center" width="50%"><a href="./assets/preview/editor-json.png"><img src="./assets/preview/editor-json.png" alt="JSON 编辑器" /></a><br/>JSON 编辑器</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/editor-rich-text.png"><img src="./assets/preview/editor-rich-text.png" alt="富文本编辑器" /></a><br/>富文本编辑器</td>
    <td align="center" width="50%"><a href="./assets/preview/navigation.png"><img src="./assets/preview/navigation.png" alt="全局导航" /></a><br/>全局导航</td>
  </tr>
</table>

## 功能

### 工作台与身份权限

- **工作台**：通过可定制小组件展示统计、图表、公告和待办，支持调整布局与保存个人看板。
- **控制中心**：集中展示可进入的租户与平台管理入口，支持切换租户和选择工作上下文。
- **登录认证**：支持账号密码、手机和邮箱验证码、第三方登录及双因素验证，提供验证码、登录节流和账号锁定防护。
- **个人中心**：维护个人资料、密码、手机和邮箱，管理第三方绑定、登录设备、通知偏好、租户归属和个人使用统计。
- **用户管理**：查询与维护用户，支持高级搜索、启用禁用、多角色与多部门配置、直接授权、数据范围、账号锁定、密码重置、模拟登录和强制下线。
- **在线用户**：查看活跃会话、客户端和实时连接状态，支持按用户检索并吊销指定会话。
- **角色管理**：维护角色、继承关系和成员，批量添加或移除成员，配置菜单权限、操作权限和数据范围。
- **组织机构**：以树形列表管理部门层级，查看子部门和成员，维护部门负责人及成员岗位、工号、职级和入职信息。
- **岗位管理**：维护岗位名称、编码、排序和启停状态，查看岗位详情并供部门成员配置使用。
- **权限管理**：集中维护模块、资源和操作权限，配置权限编码、接口路径、请求方法、平台或租户作用域及审计要求。
- **菜单管理**：以树形结构维护目录、菜单与按钮，配置组件路径、图标、排序、可见性、缓存、外链和关联权限。
- **字段安全**：按实体字段与授权对象配置字段读取、写入和脱敏策略，控制敏感字段的展示与修改。
- **授权申请与委托**：处理权限申请的批准、拒绝和撤回，支持限时委托角色或权限并随时撤销。
- **审批中心**：查询审批事项、审批状态和处理记录，支持查看详情、批准、拒绝及撤回。
- **审批约束**：维护职责分离、互斥、基数和条件约束，配置适用对象、规则参数及违规处理方式。

### 租户与消息协作

- **租户管理**：维护租户状态、版本、到期与配额，分步初始化租户数据库和管理员，管理成员、运维支持成员及所有权转移。
- **版本套餐**：配置套餐价格、计费周期、用户与存储配额以及权限白名单，控制租户可用功能。
- **我的订阅**：查看当前租户的版本、订阅期限和用户、存储用量，了解配额与可用权益。
- **通知公告**：维护并发布通知，按用户、角色或部门定向投递，配置展示与强制阅读方式，查看阅读统计和未读人员。
- **我的消息**：查看站内通知详情，筛选待处理消息，支持单条或全部标为已读及确认阅读。
- **消息模板**：集中维护邮件、短信、站内通知和机器人模板，使用模板变量组织内容并支持租户覆盖。
- **邮件短信**：查询邮件和短信发送记录、接收对象及发送结果，查看内容详情并重发失败消息。
- **在线聊天**：支持单聊、群聊、部门会话与 AI 助手会话，通过实时推送展示消息、未读状态及会话信息。
- **聊天审计**：按会话与消息条件检索聊天记录，查看会话成员和消息详情，支持内容合规审查。

### 文件与系统管理

- **文件管理**：查询、上传、下载和预览文件，维护元数据、归档与恢复状态，查看存储副本并管理主存储。
- **存储配置**：配置本地、S3、OSS、COS 和 MinIO 等存储通道，支持启用禁用及默认存储设置。
- **导出中心**：统一查看异步导出任务的状态与结果，支持 CSV、XLSX 文件下载、取消待执行任务和删除记录。
- **字典管理**：联动维护字典分类与字典项，配置编码、值、默认项、排序和状态，为搜索与表单提供统一选项。
- **参数配置**：维护平台与租户参数，按分组和状态查询配置，查看配置详情并管理参数值。
- **业务编号**：配置编号前缀、日期、流水位数、重置周期和时区，预览格式，执行安全重置并查看永久发号记录。
- **任务调度**：维护定时与间隔任务，支持 Cron 可视化配置、启用暂停、立即执行、重试策略及运行日志详情查看。
- **缓存管理**：按键或匹配模式查询缓存，以树形分组查看键和内容，支持编辑缓存值、按键或批量清理。
- **服务监控**：展示 CPU、内存、磁盘、网络、GPU、主板和运行时信息，辅助查看服务资源与运行状态。
- **版本管理**：查看当前系统版本、版本说明与数据库迁移信息，集中核对升级状态。
- **邮件配置**：维护邮件发送通道与服务器参数，管理启用状态及默认邮件配置。
- **短信配置**：维护短信服务商与发送配置，管理启用状态及默认短信通道。
- **机器人配置**：管理钉钉、飞书和企业微信 Webhook 机器人通道，配置状态及默认通知机器人。
- **Telegram 机器人**：维护多个 Telegram Bot 实例的连接配置、启用状态和相关说明。
- **应用管理**：注册与维护 OAuth2 / OIDC 应用，配置客户端类型、授权方式和回调地址，管理应用状态及密钥。
- **开放接口凭证**：在个人中心自助管理 OpenAPI 调用凭证，申请、查看和轮换签名调用所需的密钥。

### 日志与审计

- **访问日志**：查询请求路径、请求方法、状态码、耗时和客户端信息，查看详情并跳转关联链路。
- **开放接口日志**：查询开放接口调用与签名鉴权结果，查看请求响应详情及关联链路。
- **操作日志**：记录业务操作、执行结果和耗时，查看操作描述、客户端信息及关联链路。
- **登录日志**：查询登录成功、失败、退出及模拟登录等事件，查看客户端、IP 与登录详情。
- **异常日志**：检索异常类型、错误信息与发生位置，查看堆栈和关联请求以定位问题。
- **数据变更日志**：记录实体新增、修改、删除与恢复，展示字段变更前后差异并关联操作链路。
- **权限变更日志**：记录角色、用户和权限授权变更，查询操作对象、变更内容与关联链路。
- **日志链路追踪**：按 TraceId、用户、会话或 IP 聚合多类日志，以时间线串联操作，并用流向图与时间分布分析定位异常。
- **升级记录**：查询数据库升级脚本执行台账，查看版本、执行状态、耗时与错误详情。

### 开发与扩展模块

- **代码生成**：管理数据源、表结构、字段和模板，支持单表、树形、主从全栈生成、代码预览、下载与生成历史查看。
- **AI 提供商**：配置模型服务商、连接地址和模型，安全托管密钥，测试连接并设置默认提供商。
- **AI 提示词**：集中维护提示词编码、内容和状态，为 AI 对话及业务场景复用提示词模板。
- **知识库**：导入知识文档并建立向量索引，支持重建索引、检索问答和来源引用，按租户隔离知识数据。
- **AI 助手**：配置助手身份、模型与提示词，设置默认助手并接入在线聊天会话。
- **流程定义**：可视化设计流程节点与连线，维护草稿、发布、停用和归档状态，支持版本管理及启动流程。
- **流程实例**：查看流程运行状态、节点与详情，支持挂起、恢复、取消、终止、故障重试及发送流程信号。
- **我的待办**：集中处理人工流程任务，支持批准、拒绝、转办和加签，跟踪任务办理状态。
- **打印模板**：拖拽设计文本、表格、图片和条码，配置纸张、数据源与样例数据，支持缩放排版、JSON 查看、预览和打印。

### 通用使用体验

- **偏好设置**：配置亮暗主题、主题色、布局、紧凑度、水印与交互偏好，支持个人偏好云端同步。
- **高级列表**：统一提供组合搜索、个人视图、列设置、多列排序、密度切换、树形展示、速览与导出能力。
- **内容编辑**：提供富文本、Markdown、代码与 JSON 编辑查看，以及 Cron 表达式可视化配置。
- **全局导航**：提供多标签页、收藏夹、全局搜索、通知与任务反馈，支持多语言、时区切换和锁屏。

## 技术栈

后端 .NET + XiHan.Framework（SqlSugar / Redis / SignalR / Serilog / Scalar 均由框架带入）；前端 Vue + TypeScript + Vite + XiHan.UI + Pinia + Tailwind CSS。

逐项清单见[后端工程说明](./backend/README_cn.md#依赖构成)与[前端工程说明](./frontend/README_cn.md#技术栈)。

## 架构

系统分为框架层、模块层与主应用层，每个模块内部遵循 DDD 分层（Domain / Application / Infrastructure）。

```text
┌─────────────────────────────────────────────────────────────┐
│                   XiHan.BasicApp.WebHost                    │
│                    (启动入口与模块聚合)                     │
├──────────┬──────────┬──────────┬──────────┬─────────────────┤
│ CodeGen  │    AI    │ Workflow │ Printing │      Chat       │
│(代码生成)│ (AI/RAG) │ (工作流) │(打印模板)│   (在线聊天)    │
├──────────┴──────────┴──────────┴──────────┴─────────────────┤
│                     XiHan.BasicApp.Saas                     │
│       (RBAC / 多租户 / 组织 / 审批 / 审计 / 消息中心)       │
├─────────────────────────────────────────────────────────────┤
│                   XiHan.BasicApp.Web.Core                   │
│          (Web 侧基座 / 动态 API / 文档 / 维护模式)          │
├─────────────────────────────────────────────────────────────┤
│                     XiHan.BasicApp.Core                     │
│                  (应用基座 / DDD / 模块化)                  │
├─────────────────────────────────────────────────────────────┤
│                      XiHan.Framework.*                      │
│   底层框架(认证 / 授权 / 数据 / 缓存 / 事件总线 / 多租户)   │
└─────────────────────────────────────────────────────────────┘
```

| 项目 | 说明 | 可卸载 |
| --- | --- | --- |
| `XiHan.BasicApp.Core` | 应用基座，聚合框架的非 Web 模块与全应用共享约定 | 否 |
| `XiHan.BasicApp.Web.Core` | Web 侧基座，聚合框架 Web 模块，提供维护模式中间件 | 否 |
| `XiHan.BasicApp.Saas` | 平台治理模块：用户 / 角色 / 权限 / 菜单 / 部门 / 租户 / 配置 / 字典 / 文件 / 通知 / 审批 / 日志 / 任务 | 否 |
| `XiHan.BasicApp.CodeGeneration` | 代码生成：数据源管理 / 表结构导入 / 模板配置 / 全栈生成 | 是 |
| `XiHan.BasicApp.AI` | AI 能力：提供商与密钥管理 / 提示词库 / 知识库 RAG / AI 技能（MCP 工具）/ 聊天 AI 助手 | 是（助手桥接依赖 Chat） |
| `XiHan.BasicApp.Workflow` | 工作流引擎落地：流程定义 / 实例 / 待办（框架引擎的持久化与 API） | 是 |
| `XiHan.BasicApp.Printing` | 打印模板：可视化设计 / 租户与平台双作用域 / 按编码解析 | 是 |
| `XiHan.BasicApp.Chat` | 在线聊天：单聊 / 群聊 / 部门群 / AI 助手会话 / 实时推送 / 合规审计 | 是（删除须连带处理 AI 的助手桥接） |
| `XiHan.BasicApp.WebHost` | 启动入口，聚合所有模块 | — |

```text
XiHan.BasicApp/
├── backend/                 # 后端（.NET）
│   ├── src/
│   │   ├── framework/       #   Core / Web.Core 基础能力
│   │   ├── modules/         #   Saas + 可选模块（CodeGen/AI/Workflow/Printing/Chat）
│   │   └── main/            #   WebHost 启动入口
│   ├── props/               #   共享 MSBuild 属性
│   ├── scripts/             #   版本号与清理脚本
│   └── test/                #   测试项目
├── frontend/                # 前端（Vue + XiHan.UI）
│   ├── src/                 #   应用源码（src/modules/ 与后端可选模块一一对应）
│   └── packages/            #   内部包
└── assets/                  # 品牌与 README 资源
    └── preview/             # 功能预览截图与索引
```

### 卸载可选模块

一个可选模块 = 后端一个工程 + 前端一个 `src/modules/<模块>` 目录。后端与前端各自的卸载步骤见[后端工程说明](./backend/README_cn.md#卸载可选模块)与[前端工程说明](./frontend/README_cn.md#卸载可选模块)。

⚠️ **卸载必须伴随重建数据库**：菜单、权限、角色授权、定时任务的种子行不会随模块删除自动回收。

## 快速开始

### 环境要求

| 依赖 | 版本 | 说明 |
| --- | --- | --- |
| .NET SDK | 10.0+ | 后端必需 |
| Node.js | 24.0+ | 前端必需 |
| pnpm | 11.0+ | 前端必需 |
| PostgreSQL | 14+ | 唯一硬依赖，也可用 MySQL / SQL Server / SQLite / Oracle |
| Redis | 6.0+ | 可选，关闭后退化为进程内内存缓存 |
| Qdrant | v1.15+ | 仅启用 AI 知识库时需要 |

### 容器一键起

仓库根自带 `docker-compose.yml`，包含 PostgreSQL、Redis、Qdrant、后端与前端五个服务：

```bash
cp .env.example .env
docker compose up -d
```

默认前端 `http://localhost:8080`、后端 `http://localhost:9708`，端口可在 `.env` 里改。

### 后端

```bash
git clone https://github.com/XiHanFun/XiHan.BasicApp.git
cd XiHan.BasicApp/backend

dotnet run --project src/main/XiHan.BasicApp.WebHost --launch-profile Development
```

启动后访问 `http://127.0.0.1:9708/scalar` 查看 API 文档。各环境端口：Development `9708`、Production `9709`。

连接串配在 `backend/src/main/XiHan.BasicApp.WebHost/appsettings.Development.json` 的 `XiHan:Data:SqlSugarCore:ConnectionConfigs`。首次启动会自动建库建表并播种。

框架默认走 NuGet 包，克隆本仓即可编译；连框架一起改的方式见[后端工程说明](./backend/README_cn.md#框架引用方式)。

### 前端

```bash
cd frontend
pnpm install
pnpm dev
```

> `@xihan-ui/*` 取 npm 上的正式版，单独 clone 本仓即可 `pnpm install`，不需要并列检出 `XiHan.UI`。要连组件库源码一起调试，临时在 `pnpm-workspace.yaml` 加 `overrides` 指到 `link:../../XiHan.UI/ui/packages/*`，调完删掉、不要提交。

### 默认账号

初始超级管理员账号为 `superadmin`，密码 `SuperAdmin@123`（写在种子里，账号标记为需要本人改密）。生产环境首次登录后请立即修改，并建议在参数「密码设置」里开启强制改密。

开发环境默认开启演示数据（`Saas:Seed:EnableDemoData`）：覆盖各种套餐、租户状态、成员类型、数据范围与账号状态的演示租户和账号，密码都是 `Demo@123`，清单见[框架简介：种子数据](docs/backend/introduction.md#演示数据)。

## 项目生态

- [XiHan.Framework](https://github.com/XiHanFun/XiHan.Framework) - 快速、轻量、高效、用心的 .NET 现代模块化开发框架
- [XiHan.UI](https://github.com/XiHanFun/XiHan.UI) - 快速、轻量、高效、用心的框架无关跨端组件库
- [XiHan.BasicApp](https://github.com/XiHanFun/XiHan.BasicApp) - 基于 .Net + Vue 的超高颜值中后台内核

## 诚挚致谢

排名不分先后。

| 项目                                                         | 致谢                                           |
| ------------------------------------------------------------ | ---------------------------------------------- |
| [XiHan.Framework](https://github.com/XiHanFun/XiHan.Framework) | 作为本项目的后端底层框架支持                   |
| [XiHan.UI](https://github.com/XiHanFun/XiHan.UI)             | 作为本项目的前端视图组件支持                   |
| [NaiveUI](https://github.com/tusen-ai/naive-ui)              | 作为本项目的前端视图组件支持（v4.0.0前）       |
| [Blog.Core](https://github.com/anjoy8/Blog.Core)             | 作为部分后端架构、逻辑功能灵感来源（启蒙项目） |
| [ Admin.Core.ZR](https://gitee.com/izory/ZrAdminNetCore)     | 作为部分后端功能灵感来源                       |
| [YuebonCore](https://gitee.com/yuebon/YuebonNetCore)         | 作为部分后端功能灵感来源                       |
| [VbenAdmin](https://github.com/vbenjs/vue-vben-admin)        | 作为部分前端架构、视觉功能灵感来源（启蒙项目） |
| [SoybeanAdmin](https://github.com/soybeanjs/soybean-admin)   | 作为部分前端视觉功能灵感来源                   |
| [LitheAdmin](https://github.com/tenianon/lithe-admin)        | 作为部分前端视觉功能灵感来源                   |
| 其他第三方依赖                                               | 作为项目功能丰富与拓展的基石                   |


## 支持&赞助

如果此项目对你的开发有助益，也欢迎请作者一杯咖啡。

官方赞助页 https://docs.xihanfun.com/cosmos/sponsor


## 版权&授权

Copyright (c) 2021-Present XiHanFun and contributors.

本项目采用 MIT 授权，详见 [License](./LICENSE)

XiHan.BasicApp Logo、XiHan.BasicApp名称、界面视觉设计与原创视觉表达归作者所有，第三方依赖和第三方服务分别遵循其各自授权与服务条款。

项目仅供学习参考，作者不承担任何软件的使用风险。
