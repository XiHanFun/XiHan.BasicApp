/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { registerModuleLocales } from '~/locales'
import { registerShellExtension, registerWelcomeHighlights } from '~/stores'
import { createChatApi } from './api'
import { setChatApi } from './api-contract'
import { chatShellExtension } from './integration'

/**
 * 登记文案、聊天 API 与壳层扩展（顶栏按钮、浮动面板、实时集成）
 * 只登记不发请求：按钮与实时链路按会话读权限（chat.chat-conversations.read）显隐启停
 * 后端关掉 BasicApp:Modules:Chat:Enabled 时权限与页面都不下发，界面随之不出现
 */
export default function setup(): void {
  registerModuleLocales(import.meta.glob<{ default: Record<string, unknown> }>('./locales/*.ts'))
  setChatApi(createChatApi())
  registerShellExtension(chatShellExtension)
  // 新版欢迎页里介绍聊天的一屏：挂在会话页上，模块关掉或没有权限时后端不下发这个页面，这一屏随之不出现
  registerWelcomeHighlights({
    release: '6.0',
    key: 'chat',
    order: 40,
    icon: 'lucide:messages-square',
    tone: 'danger',
    categoryKey: 'chat.welcome.category',
    titleKey: 'chat.welcome.title',
    descriptionKey: 'chat.welcome.description',
    pointKeys: ['chat.welcome.point_chat', 'chat.welcome.point_call', 'chat.welcome.point_share'],
    demo: () => import('./components/WelcomeDemo.vue'),
    page: 'chat.conversations',
    action: { labelKey: 'chat.welcome.action', to: '/chat/conversations' },
  })
}
