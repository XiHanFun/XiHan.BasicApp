/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * 成员资料卡的开关：卡片只放一张，由聊天面板（ChatPanel）提供；
 * 消息流里点头像、会话详情里点成员，都经 open 打开它
 */
import type { InjectionKey, Ref } from 'vue'
import { inject, provide, ref } from 'vue'

export interface ChatUserCard {
  /** 打开这个人的资料卡 */
  open: (userId: string) => void
}

export const CHAT_USER_CARD_KEY: InjectionKey<ChatUserCard> = Symbol('ChatUserCard')

/** 提供资料卡的开关；返回卡片要绑的状态 */
export function provideChatUserCard(): { show: Ref<boolean>, userId: Ref<null | string> } {
  const show = ref(false)
  const userId = ref<null | string>(null)
  provide(CHAT_USER_CARD_KEY, {
    open(target) {
      userId.value = target
      show.value = true
    },
  })
  return { show, userId }
}

/** 取聊天面板提供的资料卡开关；不在聊天面板里时为 null（那里没有卡片可开） */
export function useChatUserCard(): ChatUserCard | null {
  return inject(CHAT_USER_CARD_KEY, null)
}
