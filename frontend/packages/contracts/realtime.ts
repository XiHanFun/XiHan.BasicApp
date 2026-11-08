/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

// 由 scripts/gen-contracts.mjs 从 contracts/realtime.json 生成；不要手改，改后端契约后执行 pnpm gen:contracts

/** 实时 Hub 的路径（后端 RealtimeHubs） */
export const RealtimeHubs = {
  Chat: '/hubs/chat',
  Notification: '/hubs/notification',
} as const

/** 服务端推送的方法名（后端 RealtimeMethods）：订阅一律用这里的常量，后端改名时生成物漂移、类型检查报错 */
export const RealtimeMethods = {
  ChatAssistantCompleted: 'ChatAssistantCompleted',
  ChatAssistantDelta: 'ChatAssistantDelta',
  ChatConversationChanged: 'ChatConversationChanged',
  ChatMessageEdited: 'ChatMessageEdited',
  ChatMessageRecalled: 'ChatMessageRecalled',
  ChatReactionChanged: 'ChatReactionChanged',
  ChatReadPositionChanged: 'ChatReadPositionChanged',
  ChatTyping: 'ChatTyping',
  ForceLogout: 'ForceLogout',
  ReceiveChatMessage: 'ReceiveChatMessage',
  ReceiveNotification: 'ReceiveNotification',
  TaskProgress: 'TaskProgress',
  UserSettingChanged: 'UserSettingChanged',
} as const

export type RealtimeMethod = typeof RealtimeMethods[keyof typeof RealtimeMethods]

/** 客户端可调的 Hub 方法名（后端各 Hub 的客户端方法，如聊天的进入会话、正在输入） */
export const RealtimeInvocations = {
  JoinConversation: 'JoinConversation',
  LeaveConversation: 'LeaveConversation',
  Typing: 'Typing',
} as const

export type RealtimeInvocation = typeof RealtimeInvocations[keyof typeof RealtimeInvocations]
