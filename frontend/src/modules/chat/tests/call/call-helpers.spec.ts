/**
 * call-helpers.ts：通话记录消息的解析与文案、时长格式
 *
 * 职责边界：通话记录的正文是服务端写的 JSON，读不出来的一律当没有记录（不抛错）；
 * 同一件事两边说法不同（我打出去没人接是「对方未接听」，打给我没接到是「未接听」）；
 * 只有没接到的记录算未读。翻译函数用回显键的替身，断言落在键与参数上
 */
import type { ChatCallItem, ChatCallParticipantItem } from '@/modules/chat/types'
import { describe, expect, it } from 'vitest'
import {
  callMediaFailureText,
  callMediaLabel,
  callParticipantName,
  callRecordLabel,
  callRecordText,
  formatCallDuration,
  isMissedCallRecord,
  millisecondsBetween,
  parseCallRecord,
  peerOfCall,
} from '@/modules/chat/call/call-helpers'

/** 回显键与参数的翻译替身 */
function t(key: string, named?: Record<string, unknown>): string {
  return named
    ? `${key}${JSON.stringify(named)}`
    : key
}

function record(patch: Record<string, unknown> = {}): string {
  return JSON.stringify({
    callId: 'c-1',
    media: 'Voice',
    reason: 'Completed',
    durationSeconds: 135,
    ...patch,
  })
}

function participant(patch: Partial<ChatCallParticipantItem> & { userId: string }): ChatCallParticipantItem {
  return { status: 'Joined', ...patch }
}

describe('解析通话记录', () => {
  it('服务端写的正文解得出通话、媒体、原因与时长', () => {
    expect(parseCallRecord(record())).toStrictEqual({
      callId: 'c-1',
      media: 'Voice',
      reason: 'Completed',
      durationSeconds: 135,
    })
  })

  it('时长取整到秒、不为负', () => {
    expect(parseCallRecord(record({ durationSeconds: 12.9 }))?.durationSeconds).toBe(12)
    expect(parseCallRecord(record({ durationSeconds: -3 }))?.durationSeconds).toBe(0)
  })

  it.each([
    ['空', null],
    ['空串', ''],
    ['不是 JSON', '语音通话'],
    ['不是对象', '42'],
    [
      '媒体不认识',
      record({ media: 'Screen' }),
    ],
    [
      '原因不认识',
      record({ reason: 'Busy' }),
    ],
    [
      '没有通话标识',
      record({ callId: 7 }),
    ],
    [
      '时长不是数字',
      record({ durationSeconds: '135' }),
    ],
  ])('%s：读不出记录', (_, content) => {
    expect(parseCallRecord(content)).toBeNull()
  })
})

describe('通话时长', () => {
  it.each([
    [0, '00:00'],
    [9, '00:09'],
    [135, '02:15'],
    [3599, '59:59'],
    [3600, '1:00:00'],
    [3725, '1:02:05'],
    [135.9, '02:15'],
  ])('%d 秒写成 %s', (seconds, text) => {
    expect(formatCallDuration(seconds)).toBe(text)
  })

  it('负数与非数字按 0 算，不把 NaN 摆到界面上', () => {
    expect(formatCallDuration(-5)).toBe('00:00')
    expect(formatCallDuration(Number.NaN)).toBe('00:00')
    expect(formatCallDuration(Number.POSITIVE_INFINITY)).toBe('00:00')
  })
})

describe('气泡里的一行字', () => {
  const of = (reason: string, outgoing: boolean, durationSeconds = 0) =>
    callRecordText(t, parseCallRecord(record({ reason, durationSeconds }))!, outgoing)

  it('接通过的带时长，两边说法一样', () => {
    expect(of('Completed', true, 135)).toBe('chat.call.record_completed{"duration":"02:15"}')
    expect(of('Completed', false, 135)).toBe('chat.call.record_completed{"duration":"02:15"}')
    expect(of('Interrupted', false, 61)).toBe('chat.call.record_interrupted{"duration":"01:01"}')
  })

  it('没接通的按谁打的换说法', () => {
    expect(of('Cancelled', true)).toBe('chat.call.record_cancelled')
    expect(of('Cancelled', false)).toBe('chat.call.record_cancelled_by_peer')
    expect(of('Rejected', true)).toBe('chat.call.record_rejected_by_peer')
    expect(of('Rejected', false)).toBe('chat.call.record_rejected')
    expect(of('Missed', true)).toBe('chat.call.record_missed_by_peer')
    expect(of('Missed', false)).toBe('chat.call.record_missed')
  })
})

describe('标签与未读', () => {
  it('简短标签按媒体取；读不出记录按语音通话说', () => {
    expect(callRecordLabel(record({ media: 'Video' }))).toBe('[视频通话]')
    expect(callRecordLabel(record())).toBe('[语音通话]')
    expect(callRecordLabel('坏掉的正文')).toBe('[语音通话]')
  })

  it('媒体的名字与设备失败的说法各有各的键', () => {
    expect(callMediaLabel(t, 'Voice')).toBe('chat.call.voice')
    expect(callMediaLabel(t, 'Video')).toBe('chat.call.video')
    expect(callMediaFailureText(t, 'denied')).toBe('chat.call.media_denied')
    expect(callMediaFailureText(t, 'missing')).toBe('chat.call.media_missing')
    expect(callMediaFailureText(t, 'busy')).toBe('chat.call.media_busy')
    expect(callMediaFailureText(t, 'unsupported')).toBe('chat.call.media_unsupported')
  })

  it('只有对方取消与没人接算没接到', () => {
    expect(isMissedCallRecord(record({ reason: 'Missed' }))).toBe(true)
    expect(isMissedCallRecord(record({ reason: 'Cancelled' }))).toBe(true)
    expect(isMissedCallRecord(record({ reason: 'Completed' }))).toBe(false)
    expect(isMissedCallRecord(record({ reason: 'Rejected' }))).toBe(false)
    expect(isMissedCallRecord(record({ reason: 'Interrupted' }))).toBe(false)
    expect(isMissedCallRecord(null)).toBe(false)
  })
})

describe('通话里的人与时刻', () => {
  const call = {
    callId: 'c-1',
    conversationId: 'conv',
    initiatorUserId: 'me',
    media: 'Voice',
    status: 'Ringing',
    startedAt: '2026-11-14T08:00:00Z',
    ringExpiresAt: '2026-11-14T08:01:00Z',
    participants: [
      participant({ userId: 'me', userName: 'me' }),
      participant({
        userId: 'bob',
        userName: 'bob',
        displayName: ' Bob ',
        status: 'Invited',
      }),
    ],
  } satisfies ChatCallItem

  it('另一个人是参与者里不是我的那个', () => {
    expect(peerOfCall(call, 'me')?.userId).toBe('bob')
    expect(peerOfCall(call, 'bob')?.userId).toBe('me')
    expect(peerOfCall({
      ...call,
      participants: [
        participant({ userId: 'me' }),
      ],
    }, 'me')).toBeNull()
  })

  it('称呼取用户名（与会话标题一致），没有取显示名，都没有为空', () => {
    expect(callParticipantName(participant({ userId: 'u', userName: ' bob ', displayName: 'Bob' }))).toBe('bob')
    expect(callParticipantName(participant({ userId: 'u', userName: '  ', displayName: ' Bob ' }))).toBe('Bob')
    expect(callParticipantName(participant({ userId: 'u' }))).toBe('')
    expect(callParticipantName(null)).toBe('')
  })

  it('两个时刻之间的毫秒数；解析不出的按 0 算', () => {
    expect(millisecondsBetween(call.startedAt, call.ringExpiresAt)).toBe(60000)
    expect(millisecondsBetween(call.ringExpiresAt, call.startedAt)).toBe(-60000)
    expect(millisecondsBetween('不是时间', call.ringExpiresAt)).toBe(0)
  })
})
