---
name: xihan-basicapp-fullstack-contracts
description: 设计、变更、审查或诊断 XiHan.BasicApp 前后端契约时使用。覆盖操作 ID 与 HTTP 路由约定、请求响应与分页批量形态、错误码与 Problem Details、权限码与作用侧、页面登记与菜单组件路径、推送方法、contracts export 与 gen-contracts、框架锁定与快照构建、配置节与 Provider、表结构基线与部署边界。
---

# XiHan.BasicApp 全栈契约

用于 `backend/`、`frontend/` 之间的契约，以及 `docs/` 与 `plans/` 的同步。契约形态先读 `references/contracts.md`；涉及框架锁定、配置、Provider、表结构或部署时读 `references/operations.md`。方案 `plans/下一代总体方案.md` 的 D1–D12、5.4、5.13、5.14 与第 6.3 节是设计依据。

## 契约清单

改动任何一项都同步其余各项，并在同一个提交里落地：

1. 操作 ID、动词、`Expose`、`ReadOnly`、`Permission` / `Policy`（以及 `OperationExposureTests`、匿名白名单）。
2. 请求 / 响应 record、字段可空、枚举值集、分页与批量形态，登记进包的 `JsonContext`。
3. 错误码、消息键与 10 个区域文件；`<操作>.title` / `.description`、`openapi.tags.<资源>.name`。
4. 权限码、作用侧、`audited`、页面登记（页面码、路由、组件路径、读权限）与对应种子。
5. 推送方法与载荷（`realtime.json`）。
6. 后端 `contracts export` → `frontend/contracts/*` → `pnpm gen:contracts` → `packages/contracts/*` → 页面与测试。
7. 后端测试、前端门禁、docs 与方案修订。

## 不可变边界

- 一份 `openapi.json`、一个生成器：前端不手写路由、DTO 或权限码表；CI 的契约漂移门禁重新导出并比对，生成物与后端同一提交。
- 认证、授权、租户、数据范围与字段安全在后端执行；前端隐藏按钮只是可用性。
- 不保留双字段、双协议、伪空结果或静默兜底；破坏性变更明确迁移并一次收口（首个正式版发布前直接改，发布后按版本加升级脚本）。
- 框架问题回 XiHan.Framework 修，BasicApp 只升锁定提交并跟进，不写兼容；UI 问题回 XiHan.UI 修。
- 数据库是启动硬要求；Redis、RabbitMQ 按配置装，配了连不上即启动失败，不改失败等级掩盖不可用。
- 不手工对任何库执行升级脚本，不推送、不发布、不部署，除非用户明确要求。

跨端功能要验证实际 HTTP 路由、权限、租户、菜单、错误码与浏览器行为，不以两端分别编译成功代替契约验证。
