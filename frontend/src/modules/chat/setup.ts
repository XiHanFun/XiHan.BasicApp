/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { registerModuleLocales } from '~/locales'
import { registerShellExtension } from '~/stores'
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
}
