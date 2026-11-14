/**
 * integration/chat-alert.ts：推来的消息要不要提醒、桌面通知写什么
 * 锁定：自己发的、系统提示、不是自己没接到的通话记录不提醒，免打扰的会话不提醒；
 * 桌面通知在群里是「群名 / 谁: 内容」，单聊、助手会话与没有名字的群是「对方 / 内容」
 */
import type { ChatConversationSnapshot, ChatMessageItem } from '@/modules/chat/types'
import { describe, expect, it } from 'vitest'
import { chatDesktopNotificationText, shouldAlertChatMessage } from '@/modules/chat/integration/chat-alert'

function message(overrides: Partial<ChatMessageItem> = {}): ChatMessageItem {
  return {
    messageId: 'm-1',
    conversationId: 'c-1',
    seq: 1,
    senderUserId: 'u-2',
    senderUserName: 'bob',
    messageType: 'Text',
    content: 'hello',
    attachments: [],
    isRecalled: false,
    createdAt: '2026-11-14T08:00:00Z',
    mentionedUserIds: [],
    mentionAll: false,
    isPinned: false,
    reactions: [],
    ...overrides,
  }
}

function callRecord(missedUserIds: string[]): ChatMessageItem {
  return message({
    messageType: 'Call',
    content: JSON.stringify({
      callId: 'k-1',
      media: 'Voice',
      reason: 'Missed',
      durationSeconds: 0,
      missedUserIds,
    }),
  })
}

function snapshot(overrides: Partial<ChatConversationSnapshot> = {}): ChatConversationSnapshot {
  return {
    conversationId: 'c-1',
    conversationType: 'Single',
    ...overrides,
  }
}

describe('shouldAlertChatMessage', () => {
  it('别人发来的普通消息要提醒', () => {
    expect(shouldAlertChatMessage(message(), 'u-1', false)).toBe(true)
    expect(shouldAlertChatMessage(message({ messageType: 'Image', content: null }), 'u-1', false)).toBe(true)
  })

  it('自己发的、系统提示不提醒', () => {
    expect(shouldAlertChatMessage(message({ senderUserId: 'u-1' }), 'u-1', false)).toBe(false)
    expect(shouldAlertChatMessage(message({ messageType: 'System' }), 'u-1', false)).toBe(false)
  })

  it('通话记录只提醒没接到的人', () => {
    expect(shouldAlertChatMessage(callRecord(['u-1']), 'u-1', false)).toBe(true)
    expect(shouldAlertChatMessage(callRecord(['u-3']), 'u-1', false)).toBe(false)
    expect(shouldAlertChatMessage(callRecord([]), 'u-1', false)).toBe(false)
  })

  it('免打扰的会话不提醒，提到自己也一样', () => {
    expect(shouldAlertChatMessage(message(), 'u-1', true)).toBe(false)
    expect(shouldAlertChatMessage(message({
      mentionedUserIds: ['u-1'],
    }), 'u-1', true)).toBe(false)
    expect(shouldAlertChatMessage(callRecord(['u-1']), 'u-1', true)).toBe(false)
  })
})

describe('chatDesktopNotificationText', () => {
  it('群聊与部门群：标题是群名，正文带上是谁说的', () => {
    expect(chatDesktopNotificationText(snapshot({ conversationType: 'Group', conversationName: 'Team' }), 'bob', 'hello'))
      .toEqual({ title: 'Team', body: 'bob: hello' })
    expect(chatDesktopNotificationText(snapshot({ conversationType: 'Department', conversationName: 'R&D' }), 'bob', '[图片]'))
      .toEqual({ title: 'R&D', body: 'bob: [图片]' })
  })

  it('单聊与助手会话：标题是对方，正文就是内容', () => {
    expect(chatDesktopNotificationText(snapshot(), 'bob', 'hello')).toEqual({ title: 'bob', body: 'hello' })
    expect(chatDesktopNotificationText(snapshot({ conversationType: 'Assistant', conversationName: 'Helper' }), 'Helper', 'hi'))
      .toEqual({ title: 'Helper', body: 'hi' })
  })

  it('群没有名字时按单聊的写法', () => {
    expect(chatDesktopNotificationText(snapshot({ conversationType: 'Group', conversationName: null }), 'bob', 'hello'))
      .toEqual({ title: 'bob', body: 'hello' })
  })
})
