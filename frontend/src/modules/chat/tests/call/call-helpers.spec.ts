/**
 * call-helpers.ts：通话记录消息的解析与文案、时长格式、谁出 offer
 *
 * 职责边界：通话记录的正文是服务端写的 JSON，读不出来的一律当没有记录（不抛错）；
 * 同一件事各人说法不同（一对一里我打出去没人接是「对方未接听」，打给我没接到是「未接听」，多人通话不说「对方」）；
 * 只有自己没接到的记录算未读；两个人之间后加入的一方出 offer。翻译函数用回显键的替身，断言落在键与参数上
 */
import type { ChatCallItem, ChatCallParticipantItem } from '@/modules/chat/types'
import { describe, expect, it } from 'vitest'
import {
  callJoinedAt,
  callMediaFailureText,
  callMediaIcon,
  callMediaLabel,
  callOffersTo,
  callParticipantName,
  callRecordLabel,
  callRecordText,
  formatCallDuration,
  isGroupCall,
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
    missedUserIds: [],
    ...patch,
  })
}

function participant(patch: Partial<ChatCallParticipantItem> & { userId: string }): ChatCallParticipantItem {
  return { status: 'Joined', ...patch }
}

describe('解析通话记录', () => {
  it('服务端写的正文解得出通话、媒体、原因、时长与没接到的人', () => {
    expect(parseCallRecord(record({
      missedUserIds: ['bob', 'carol'],
    }))).toStrictEqual({
      callId: 'c-1',
      media: 'Voice',
      reason: 'Completed',
      durationSeconds: 135,
      missedUserIds: ['bob', 'carol'],
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
      record({ media: 'Hologram' }),
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
    [
      '没接到的人不是数组',
      record({ missedUserIds: 'bob' }),
    ],
    [
      '没接到的人里有不是字符串的',
      record({
        missedUserIds: ['bob', 7],
      }),
    ],
    [
      '没有没接到的人这一项',
      record({ missedUserIds: undefined }),
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
    callRecordText(t, parseCallRecord(record({ reason, durationSeconds }))!, { outgoing, group: false, myUserId: 'me' })
  const inGroup = (reason: string, outgoing: boolean, missedUserIds: string[] = []) =>
    callRecordText(t, parseCallRecord(record({ reason, durationSeconds: 0, missedUserIds }))!, { outgoing, group: true, myUserId: 'me' })

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

  it('多人通话不说「对方」：受邀的都没来是无人接听，只有自己没接到的人看到未接听', () => {
    expect(inGroup('Completed', false)).toBe('chat.call.record_completed{"duration":"00:00"}')
    expect(inGroup('Cancelled', true)).toBe('chat.call.record_cancelled')
    expect(inGroup('Cancelled', false, ['me'])).toBe('chat.call.record_cancelled')
    expect(inGroup('Rejected', true)).toBe('chat.call.record_no_answer')
    expect(inGroup('Rejected', false)).toBe('chat.call.record_no_answer')
    expect(inGroup('Missed', true, ['bob'])).toBe('chat.call.record_no_answer')
    expect(inGroup('Missed', false, ['bob'])).toBe('chat.call.record_no_answer')
    expect(inGroup('Missed', false, ['bob', 'me'])).toBe('chat.call.record_missed')
  })
})

describe('标签与未读', () => {
  it('简短标签按媒体取；读不出记录按语音通话说', () => {
    expect(callRecordLabel(record({ media: 'Video' }))).toBe('[视频通话]')
    expect(callRecordLabel(record({ media: 'Screen' }))).toBe('[屏幕共享]')
    expect(callRecordLabel(record())).toBe('[语音通话]')
    expect(callRecordLabel('坏掉的正文')).toBe('[语音通话]')
  })

  it('媒体的名字与设备失败的说法各有各的键', () => {
    expect(callMediaLabel(t, 'Voice')).toBe('chat.call.voice')
    expect(callMediaLabel(t, 'Video')).toBe('chat.call.video')
    expect(callMediaLabel(t, 'Screen')).toBe('chat.call.screen')
    expect(callMediaFailureText(t, 'denied')).toBe('chat.call.media_denied')
    expect(callMediaFailureText(t, 'missing')).toBe('chat.call.media_missing')
    expect(callMediaFailureText(t, 'busy')).toBe('chat.call.media_busy')
    expect(callMediaFailureText(t, 'unsupported')).toBe('chat.call.media_unsupported')
  })

  it('没接到看的是记录里点了谁的名，不看结果：通话接通了，没接到的人照样算没接到', () => {
    expect(isMissedCallRecord(record({
      reason: 'Missed',
      missedUserIds: ['me'],
    }), 'me')).toBe(true)
    expect(isMissedCallRecord(record({
      reason: 'Completed',
      missedUserIds: ['bob', 'me'],
    }), 'me')).toBe(true)
    expect(isMissedCallRecord(record({
      reason: 'Missed',
      missedUserIds: ['bob'],
    }), 'me')).toBe(false)
    expect(isMissedCallRecord(record({ reason: 'Cancelled' }), 'me')).toBe(false)
    expect(isMissedCallRecord('坏掉的正文', 'me')).toBe(false)
    expect(isMissedCallRecord(null, 'me')).toBe(false)
  })
})

describe('媒体的图标', () => {
  it('语音是电话、视频是摄像机、屏幕共享是屏幕；不知道是哪种按语音', () => {
    expect([
      callMediaIcon('Voice'),
      callMediaIcon('Video'),
      callMediaIcon('Screen'),
      callMediaIcon(null),
      callMediaIcon(undefined),
    ]).toStrictEqual([
      'lucide:phone',
      'lucide:video',
      'lucide:screen-share',
      'lucide:phone',
      'lucide:phone',
    ])
  })
})

describe('通话里的人与时刻', () => {
  const call = {
    callId: 'c-1',
    conversationId: 'conv',
    conversationType: 'Single',
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

  it('单聊里的是一对一通话，群聊与部门群里的是多人通话', () => {
    expect(isGroupCall(call)).toBe(false)
    expect(isGroupCall({ conversationType: 'Group' })).toBe(true)
    expect(isGroupCall({ conversationType: 'Department' })).toBe(true)
  })

  it('两个人之间后加入的一方出 offer：两端各算各的，结论相反', () => {
    const early = participant({ userId: 'b', joinedAt: '2026-11-14T08:00:05.100Z' })
    const late = participant({ userId: 'a', joinedAt: '2026-11-14T08:00:09.000Z' })

    expect(callOffersTo(late, early)).toBe(true)
    expect(callOffersTo(early, late)).toBe(false)
    expect(callJoinedAt(early)).toBe(Date.parse('2026-11-14T08:00:05.100Z'))
  })

  it('同一毫秒加入的比用户 ID；加入时刻读不出的按 0 算', () => {
    const one = participant({ userId: 'a', joinedAt: '2026-11-14T08:00:05.1234567Z' })
    const other = participant({ userId: 'b', joinedAt: '2026-11-14T08:00:05.123Z' })

    expect(callOffersTo(other, one)).toBe(true)
    expect(callOffersTo(one, other)).toBe(false)
    expect(callJoinedAt(participant({ userId: 'u' }))).toBe(0)
    expect(callJoinedAt(participant({ userId: 'u', joinedAt: '不是时间' }))).toBe(0)
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
