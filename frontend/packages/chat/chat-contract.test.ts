/**
 * packages/chat 的协议常量与枚举契约。
 *
 * 职责边界：锁定与后端对齐的字面量（Hub 方法名、推送方法名、权限码、业务不变量），
 * 并保证同一组常量内部取值互不重复——重复会让「按同名字符串订阅」的实时链路静默串线。
 * 这里刻意不引 ./index（它会连带加载 .vue 组件），只按文件精确导入。
 */
import { describe, expect, it } from 'vitest'
import {
  CHAT_DRAFTS_STORAGE_KEY,
  CHAT_HUB_METHODS,
  CHAT_HUB_PATH,
  CHAT_PAGE_PATH,
  CHAT_PERMISSIONS,
  CHAT_REALTIME_METHODS,
  CHAT_SEND_KEY_STORAGE_KEY,
  CHAT_VOICE_PLAYED_CAP,
  CHAT_VOICE_PLAYED_STORAGE_KEY,
} from './constants'
import { ChatConversationType, ChatMemberRole, ChatMessageType } from './enums'
import {
  CHAT_EDIT_WINDOW_MINUTES,
  CHAT_MAX_CONTENT_LENGTH,
  CHAT_MAX_GROUP_NAME_LENGTH,
  CHAT_MAX_MENTION_COUNT,
  CHAT_RECALL_WINDOW_MINUTES,
} from './types'

/** 首字母大写驼峰（后端方法名形态） */
const PASCAL_CASE = /^[A-Z][A-Za-z0-9]*$/u

describe('实时推送方法名', () => {
  it('九个服务端推送方法名互不重复，避免同名订阅互相顶掉', () => {
    const values = Object.values(CHAT_REALTIME_METHODS)

    expect(values).toHaveLength(9)
    expect(new Set(values).size).toBe(values.length)
  })

  it('推送方法名全部是后端约定的 PascalCase，且与本地 camelCase 键一一对应', () => {
    for (const [key, value] of Object.entries(CHAT_REALTIME_METHODS)) {
      expect(value).toMatch(PASCAL_CASE)
      expect(value[0]?.toLowerCase() + value.slice(1)).toBe(key)
    }
  })

  it('助手增量与助手完成是两个独立方法名，不复用普通消息推送', () => {
    expect(CHAT_REALTIME_METHODS.chatAssistantDelta).toBe('ChatAssistantDelta')
    expect(CHAT_REALTIME_METHODS.chatAssistantCompleted).toBe('ChatAssistantCompleted')
    expect(CHAT_REALTIME_METHODS.receiveChatMessage).toBe('ReceiveChatMessage')
  })
})

describe('客户端 Hub 方法与路径', () => {
  it('三个客户端方法名互不重复且为 PascalCase', () => {
    const values = Object.values(CHAT_HUB_METHODS)

    expect(values).toStrictEqual(['JoinConversation', 'LeaveConversation', 'Typing'])
    expect(new Set(values).size).toBe(values.length)
    for (const value of values) {
      expect(value).toMatch(PASCAL_CASE)
    }
  })

  it('聊天 Hub 路径与全屏页路径都以 / 开头且互不相同', () => {
    expect(CHAT_HUB_PATH.startsWith('/')).toBe(true)
    expect(CHAT_PAGE_PATH.startsWith('/')).toBe(true)
    expect(CHAT_HUB_PATH).not.toBe(CHAT_PAGE_PATH)
    expect(CHAT_HUB_PATH).toBe('/hubs/chat')
    expect(CHAT_PAGE_PATH).toBe('/message/chat')
  })

  it('客户端方法名与服务端推送方法名不重叠，两个方向的方法表互相隔离', () => {
    const inbound = new Set<string>(Object.values(CHAT_REALTIME_METHODS))
    const overlapping = Object.values(CHAT_HUB_METHODS).filter(name => inbound.has(name))

    expect(overlapping).toStrictEqual([])
  })
})

describe('权限码与本地存储键', () => {
  it('三个权限码互不重复且统一 chat: 前缀', () => {
    const values = Object.values(CHAT_PERMISSIONS)

    expect(new Set(values).size).toBe(values.length)
    for (const value of values) {
      expect(value.startsWith('chat:')).toBe(true)
    }
    expect(values).toStrictEqual(['chat:read', 'chat:send', 'chat:manage'])
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
  it('会话类型、成员角色、消息类型的枚举值全部等于成员名（JsonStringEnumConverter 口径）', () => {
    for (const enumObject of [ChatConversationType, ChatMemberRole, ChatMessageType]) {
      for (const [name, value] of Object.entries(enumObject)) {
        expect(value).toBe(name)
      }
    }
  })

  it('三个枚举各自取值互不重复且成员数固定', () => {
    expect(Object.values(ChatConversationType)).toStrictEqual(['Single', 'Group', 'Department', 'Assistant'])
    expect(Object.values(ChatMemberRole)).toStrictEqual(['Owner', 'Admin', 'Member'])
    expect(Object.values(ChatMessageType)).toStrictEqual(['Text', 'Image', 'Voice', 'File', 'Assistant', 'System'])
  })

  it('会话类型与消息类型共用 Assistant 字面量，但分属两个枚举不可互换', () => {
    expect(ChatConversationType.Assistant).toBe('Assistant')
    expect(ChatMessageType.Assistant).toBe('Assistant')
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
