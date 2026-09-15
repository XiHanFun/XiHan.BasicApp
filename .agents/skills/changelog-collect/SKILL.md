---
name: xihan-basicapp-changelog-collect
description: 收集 XiHan.BasicApp 两个版本或引用之间的用户可感知变化，整理未发布内容或更新 docs/changelog.md 时使用。它不修改版本、不打标签，也不执行部署。
---

# XiHan.BasicApp 变更日志整理

## 事实源

- 用户指定的起止 tag/commit；未指定时列出最近正式 tag 和当前 HEAD。
- 对应范围的提交、实际 diff、合入 PR、Dynamic API/DTO、权限种子、前端调用和升级脚本。
- `docs/changelog.md` 当前分类和升级须知格式。

## 收录规则

- 收录用户或运维可感知的新增、修复、优化、调整、升级和移除。
- 内部重构、纯测试、CI 和格式不单列，除非改变接口、部署、性能或安全结果。
- 同一功能的后端、前端、权限、菜单和文档合并成一个用户结果，不按文件拆分。
- Dynamic API、DTO、权限码、配置键、模块开关和路由变化写清迁移方式。
- 有 `UpdateScripts/<version>/<version>.sql` 时在升级须知列出数据库、已有数据、自动执行语义和备份要求。
- 安全修复描述影响与升级必要性，不泄露可直接利用的生产细节。
- XiHan.Framework 或 XiHan.UI 版本变化只有在真正改变 BasicApp 行为时收录。

开发中内容先进入未发布区；正式版本使用 `## vX.Y.Z (YYYY-MM-DD)`。本技能不运行 VersionUpgrade，不同步版本文件，也不打 tag。
