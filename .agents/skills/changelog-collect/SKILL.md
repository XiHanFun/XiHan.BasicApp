---
name: xihan-basicapp-changelog-collect
description: 收集 XiHan.BasicApp 两个版本或引用之间的用户可感知变化，整理未发布内容或更新 docs/changelog.md 时使用。它不修改版本、不打标签，也不执行部署。
---

# XiHan.BasicApp 变更日志整理

## 事实源

- 用户指定的起止 tag/commit；未指定时列出最近正式 tag 和当前 HEAD。
- 对应范围的提交、实际 diff、合入 PR、操作与 DTO（`frontend/contracts/openapi.json` 的变化）、权限码与页面登记、前端页面和表结构脚本（`backend/src/XiHan.BasicApp.Host/Schema/<版本>/*.sql`）。
- `docs/changelog.md` 当前分类和升级须知格式。

## 收录规则

- 收录用户或运维可感知的新增、修复、优化、调整、升级和移除。
- 内部重构、纯测试、CI 和格式不单列，除非改变接口、部署、性能或安全结果。
- 同一功能的后端、前端、权限、菜单和文档合并成一个用户结果，不按文件拆分。
- 操作 ID 与路由、DTO、权限码、页面路由、配置键和模块开关的变化写清迁移方式。
- 有表结构脚本时在升级须知列出数据库、已有数据、启动时自动升级的语义（`BasicApp:Schema:AutoUpgrade`）和备份要求。
- 安全修复描述影响与升级必要性，不泄露可直接利用的生产细节。
- XiHan.Framework 或 XiHan.UI 版本变化只有在真正改变 BasicApp 行为时收录。

开发中内容先进入未发布区；正式版本使用 `## vX.Y.Z (YYYY-MM-DD)`。本技能不改版本号、不同步版本文件，也不打 tag。
