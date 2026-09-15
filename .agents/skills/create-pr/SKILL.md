---
name: xihan-basicapp-create-pr
description: 为 XiHan.BasicApp 准备、审查或创建 Pull Request 时使用。适用于生成 PR 标题与正文、比较分支、填写模板或执行 gh pr create；普通前后端代码审查不使用。
---

# XiHan.BasicApp 创建 PR

## 前置检查

- 检查分支、上游、工作区、merge/rebase 和冲突；异常状态不创建 PR。
- 根据用户意图和远端分支确定 base；普通开发通常面向 `dev`，不得无依据改成 `main`。
- 使用 `git log <base>..HEAD`、`git diff --stat <base>...HEAD` 和完整 diff 审查整条分支。
- 判断影响后端、前端、两端契约、数据库升级、权限/菜单种子或产品文档中的哪些部分。

## PR 内容

严格使用 `.github/PULL_REQUEST_TEMPLATE.md`，保留关联 Issue、变更类型、说明、影响范围、自测、破坏性变更和补充材料。

- 标题使用 Conventional Commit 形态，描述业务或使用者结果。
- Dynamic API、DTO、权限码、菜单 Component 路径和可选模块必须列出两端同步情况。
- 数据库变化列出 `UpdateScripts`、已有数据、幂等性、备份和回滚风险。
- 只有实际执行的后端构建/测试、前端 check/test/build 和文档构建才能勾选。
- Framework/UI 临时源码链接必须说明，不能把正式包验证与源码联调混为一谈。

只要求草稿时不写远程。明确要求创建 PR 时才允许推送当前分支并执行 `gh pr create`；禁止 force push、合并或改写其他远端分支。创建后返回 PR 链接。
