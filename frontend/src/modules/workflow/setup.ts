/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { registerModuleLocales } from '~/locales'
import { registerWorkbenchWidget } from '~/stores'

/**
 * 登记本包文案与看板的「我的待办」小组件
 * 页面由后端菜单下发（BasicApp:Modules:Workflow:Enabled 关掉即不下发）；设计器与查看器打开对应页面时才注册
 * 小组件挂在审批中心页上，页面不下发时看板上随之隐藏
 */
export default function setup(): void {
  registerModuleLocales(import.meta.glob<{ default: Record<string, unknown> }>('./locales/*.ts'))
  registerWorkbenchWidget({
    key: 'workflow-todo',
    dataKey: 'workflow.todo',
    titleKey: 'workflow.todo_widget.title',
    descKey: 'workflow.todo_widget.desc',
    emptyKey: 'workflow.todo_widget.empty',
    icon: 'lucide:list-checks',
    defaultSpan: 4,
    page: 'workflow.approvals',
    moreLink: '/workflow/approvals?tab=todo',
  })
}
