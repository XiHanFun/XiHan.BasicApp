/**
 * 聊天模块的协议常量与枚举契约
 *
 * 职责边界：锁定与后端对齐的字面量（Hub 路径、推送方法名、客户端 Hub 方法名、权限码、业务不变量），
 * 并保证同一组常量内部取值互不重复——重复会让「按同名字符串订阅」的实时链路静默串线
 * Hub 路径与方法名都取生成的 ~/contracts（后端导出），这里核对聊天用到的那一组与后端约定一致
 * 刻意不引组件，只按文件精确导入
 */
import { describe, expect, it } from 'vitest'
import {
  CHAT_DRAFTS_STORAGE_KEY,
  CHAT_PAGE_PATH,
  CHAT_PERMISSIONS,
  CHAT_SEND_KEY_STORAGE_KEY,
  CHAT_VOICE_PLAYED_CAP,
  CHAT_VOICE_PLAYED_STORAGE_KEY,
} from '@/modules/chat/constants'
import {
  CHAT_EDIT_WINDOW_MINUTES,
  CHAT_MAX_CONTENT_LENGTH,
  CHAT_MAX_GROUP_NAME_LENGTH,
  CHAT_MAX_MENTION_COUNT,
  CHAT_RECALL_WINDOW_MINUTES,
} from '@/modules/chat/types'
import {
  ChatConversationType,
  ChatMemberRole,
  ChatMessageType,
  operationPermission,
  permissionDefinitions,
  RealtimeHubs,
  RealtimeInvocations,
  RealtimeMethods,
} from '~/contracts'

/** 首字母大写驼峰（后端方法名形态） */
const PASCAL_CASE = /^[A-Z][A-Za-z0-9]*$/u

/** 聊天集成订阅的九个推送方法（use-chat-integration 逐个 on） */
const CHAT_PUSH_METHODS = [
  RealtimeMethods.ReceiveChatMessage,
  RealtimeMethods.ChatMessageRecalled,
  RealtimeMethods.ChatConversationChanged,
  RealtimeMethods.ChatTyping,
  RealtimeMethods.ChatMessageEdited,
  RealtimeMethods.ChatReactionChanged,
  RealtimeMethods.ChatReadPositionChanged,
  RealtimeMethods.ChatAssistantDelta,
  RealtimeMethods.ChatAssistantCompleted,
]

describe('实时推送方法名', () => {
  it('九个聊天推送方法名互不重复，避免同名订阅互相顶掉', () => {
    expect(CHAT_PUSH_METHODS).toHaveLength(9)
    expect(new Set(CHAT_PUSH_METHODS).size).toBe(CHAT_PUSH_METHODS.length)
  })

  it('生成的推送方法表里聊天相关的恰好是这九个，后端新增或改名时这里先报', () => {
    const chatMethods = Object.values(RealtimeMethods).filter(name => name.includes('Chat')).sort()

    expect(chatMethods).toStrictEqual([...CHAT_PUSH_METHODS].sort())
  })

  it('推送方法名全部是后端约定的 PascalCase，常量键与取值同名', () => {
    for (const [key, value] of Object.entries(RealtimeMethods)) {
      expect(value).toMatch(PASCAL_CASE)
      expect(value).toBe(key)
    }
  })

  it('助手增量与助手完成是两个独立方法名，不复用普通消息推送', () => {
    expect(RealtimeMethods.ChatAssistantDelta).toBe('ChatAssistantDelta')
    expect(RealtimeMethods.ChatAssistantCompleted).toBe('ChatAssistantCompleted')
    expect(RealtimeMethods.ReceiveChatMessage).toBe('ReceiveChatMessage')
  })
})

describe('客户端 Hub 方法与路径', () => {
  it('三个客户端方法名互不重复且为 PascalCase', () => {
    const values = Object.values(RealtimeInvocations)

    expect(values).toStrictEqual(['JoinConversation', 'LeaveConversation', 'Typing'])
    expect(new Set(values).size).toBe(values.length)
    for (const value of values) {
      expect(value).toMatch(PASCAL_CASE)
    }
  })

  it('聊天 Hub 路径与全屏页路径都以 / 开头且互不相同', () => {
    expect(RealtimeHubs.Chat.startsWith('/')).toBe(true)
    expect(CHAT_PAGE_PATH.startsWith('/')).toBe(true)
    expect(RealtimeHubs.Chat).not.toBe(CHAT_PAGE_PATH)
    expect(RealtimeHubs.Chat).toBe('/hubs/chat')
    // 后端 ChatPages 登记的 chat.conversations 页面
    expect(CHAT_PAGE_PATH).toBe('/chat/conversations')
  })

  it('客户端方法名与服务端推送方法名不重叠，两个方向的方法表互相隔离', () => {
    const inbound = new Set<string>(Object.values(RealtimeMethods))
    const overlapping = Object.values(RealtimeInvocations).filter(name => inbound.has(name))

    expect(overlapping).toStrictEqual([])
  })
})

describe('权限码与本地存储键', () => {
  it('三个权限码互不重复，都在生成的权限表里，且是 chat 包的三段码', () => {
    const values = Object.values(CHAT_PERMISSIONS)

    expect(new Set(values).size).toBe(values.length)
    for (const value of values) {
      expect(value.startsWith('chat.')).toBe(true)
      expect(permissionDefinitions[value]?.code).toBe(value)
    }
  })

  it('界面显隐用的码与后端操作要的码一致：会话读管打开会话、消息发送管发、会话管理管建群', () => {
    expect(operationPermission['chat-conversations.mine']).toBe(CHAT_PERMISSIONS.read)
    expect(operationPermission['chat-conversations.leave']).toBe(CHAT_PERMISSIONS.read)
    expect(operationPermission['chat-messages.send']).toBe(CHAT_PERMISSIONS.send)
    expect(operationPermission['chat-messages.forward']).toBe(CHAT_PERMISSIONS.send)
    expect(operationPermission['chat-conversations.create-group']).toBe(CHAT_PERMISSIONS.manage)
    expect(operationPermission['chat-conversations.silence-members']).toBe(CHAT_PERMISSIONS.manage)
  })

  it('三个 localStorage 键互不重复，草稿、发送键偏好、语音已听各占一格', () => {
    const keys = [CHAT_DRAFTS_STORAGE_KEY, CHAT_SEND_KEY_STORAGE_KEY, CHAT_VOICE_PLAYED_STORAGE_KEY]

    expect(new Set(keys).size).toBe(keys.length)
    for (const key of keys) {
      expect(key.trim()).toBe(key)
      expect(key.length).toBeGreaterThan(0)
    }
  })

  it('语音已听记录上限是正整数，保证裁剪逻辑 slice(0, cap) 不会清空全表', () => {
    expect(Number.isInteger(CHAT_VOICE_PLAYED_CAP)).toBe(true)
    expect(CHAT_VOICE_PLAYED_CAP).toBeGreaterThan(0)
    expect(CHAT_VOICE_PLAYED_CAP).toBe(500)
  })
})

describe('业务枚举与后端序列化值对齐', () => {
  it('会话类型、成员角色、消息类型的成员与后端一致（生成的契约字面量）', () => {
    expect([...ChatConversationType]).toStrictEqual([
      'Single',
      'Group',
      'Department',
      'Assistant',
    ])
    expect([...ChatMemberRole]).toStrictEqual(['Owner', 'Admin', 'Member'])
    expect([...ChatMessageType]).toStrictEqual([
      'Text',
      'Image',
      'Voice',
      'File',
      'Assistant',
      'System',
    ])
  })

  it('三个枚举各自取值互不重复', () => {
    for (const members of [ChatConversationType, ChatMemberRole, ChatMessageType]) {
      expect(new Set(members).size).toBe(members.length)
    }
  })

  it('会话类型与消息类型共用 Assistant 字面量，但分属两个枚举不可互换', () => {
    expect(ChatConversationType).toContain('Assistant')
    expect(ChatMessageType).toContain('Assistant')
    expect(ChatConversationType).not.toBe(ChatMessageType)
  })
})

describe('业务不变量', () => {
  it('撤回窗口短于编辑窗口，两者都是正整数分钟', () => {
    expect(CHAT_RECALL_WINDOW_MINUTES).toBe(2)
    expect(CHAT_EDIT_WINDOW_MINUTES).toBe(5)
    expect(CHAT_RECALL_WINDOW_MINUTES).toBeLessThan(CHAT_EDIT_WINDOW_MINUTES)
  })

  it('正文长度上限远大于群名上限，@ 人数上限为正整数', () => {
    expect(CHAT_MAX_CONTENT_LENGTH).toBe(4000)
    expect(CHAT_MAX_GROUP_NAME_LENGTH).toBe(100)
    expect(CHAT_MAX_CONTENT_LENGTH).toBeGreaterThan(CHAT_MAX_GROUP_NAME_LENGTH)
    expect(Number.isInteger(CHAT_MAX_MENTION_COUNT)).toBe(true)
    expect(CHAT_MAX_MENTION_COUNT).toBeGreaterThan(0)
  })
})
