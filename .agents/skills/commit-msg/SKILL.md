---
name: xihan-basicapp-commit-msg
description: 根据 XiHan.BasicApp 当前暂存区生成一行提交信息时使用。适用于“提交信息”“commit msg”“msg”或提交前概括 staged changes；只生成消息，不负责暂存、解决冲突或执行提交。
---

# XiHan.BasicApp 提交信息

1. 检查 `git status --short --branch`；存在冲突、merge 或 rebase 时先报告。
2. 只读取 `git diff --cached --stat`、完整 staged diff 和最近 20 条提交。暂存区为空时不从未暂存内容猜测。
3. 将全部暂存变化归纳为一个意图，输出一行 Conventional Commit。

格式：`<type>(<scope>): <中文说明>`。type 使用 `feat`、`fix`、`refactor`、`perf`、`docs`、`style`、`test`、`build`、`ci`、`chore`、`revert`。

scope 优先使用 `backend`、`frontend`、`saas`、`ai`、`chat`、`codegen`、`printing`、`workflow`、`webhost`、`docs`；跨前后端同一功能可以使用业务模块 scope，不逐文件罗列。

改变 Dynamic API、DTO、权限码、菜单路径、数据库结构或公开配置时使用 `!`，除非仓库当前兼容策略明确证明非破坏。只要求消息时最终只输出一行标题。
