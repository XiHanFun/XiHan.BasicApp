/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/** 通话的展示与判定辅助：通话记录消息的解析与文案、时长格式、谁出 offer（纯函数，i18n 由调用方注入） */

import type { ChatCallItem, ChatCallParticipantItem } from '../types'
import type { ChatCallMediaFailure } from './call-media'
import type { ChatCallMedia } from '~/contracts'
import { ChatCallEndReason, ChatCallMedia as ChatCallMedias } from '~/contracts'

type Translate = (key: string, named?: Record<string, unknown>) => string

/** 通话记录消息的正文（后端 ChatCallRecord，通话结束时由服务端写进消息的 content） */
export interface ChatCallRecord {
  callId: string
  media: ChatCallMedia
  reason: ChatCallEndReason
  /** 通话时长（秒）；没接通为 0 */
  durationSeconds: number
  /** 没接到这通电话的人（服务端只给他们记未读） */
  missedUserIds: string[]
}

/** 通话记录是说给谁看的：同一条记录，发起人、没接到的人与别的成员看到的说法不同 */
export interface ChatCallRecordViewer {
  /** 这通电话是不是我发起的 */
  outgoing: boolean
  /** 是不是多人通话（群聊、部门群里的） */
  group: boolean
  /** 我的用户 ID */
  myUserId: string
}

/**
 * 解析通话记录消息的正文
 * @returns 通话记录；正文为空、不是 JSON 或字段不对的为 null
 */
export function parseCallRecord(content?: null | string): ChatCallRecord | null {
  if (!content) {
    return null
  }
  let parsed: unknown
  try {
    parsed = JSON.parse(content)
  }
  catch {
    return null
  }
  if (typeof parsed !== 'object' || parsed === null) {
    return null
  }
  const record = parsed as Record<string, unknown>
  const media = ChatCallMedias.find(value => value === record.media)
  const reason = ChatCallEndReason.find(value => value === record.reason)
  const missed = record.missedUserIds
  if (typeof record.callId !== 'string' || !media || !reason || typeof record.durationSeconds !== 'number'
    || !Array.isArray(missed) || !missed.every(id => typeof id === 'string')) {
    return null
  }
  return {
    callId: record.callId,
    media,
    reason,
    durationSeconds: Math.max(0, Math.floor(record.durationSeconds)),
    missedUserIds: missed,
  }
}

/**
 * 通话记录的简短标签（会话预览的本地回退、回复引用与置顶预览用）
 * 与同一处别的消息类型的标签一个写法；正文读不出记录时按语音通话说
 */
export function callRecordLabel(content?: null | string): string {
  return parseCallRecord(content)?.media === 'Video'
    ? '[视频通话]'
    : '[语音通话]'
}

/** 这条通话记录对我来说是不是「没接到」：服务端只给没接到的人记未读，提醒与未读都据此 */
export function isMissedCallRecord(content: null | string | undefined, myUserId: string): boolean {
  return parseCallRecord(content)?.missedUserIds.includes(myUserId) ?? false
}

/** 通话时长：不到一小时 mm:ss，到了 h:mm:ss */
export function formatCallDuration(seconds: number): string {
  const total = Number.isFinite(seconds)
    ? Math.max(0, Math.floor(seconds))
    : 0
  const pad = (value: number) => String(value).padStart(2, '0')
  const hours = Math.floor(total / 3600)
  const minutes = Math.floor((total % 3600) / 60)
  return hours > 0
    ? `${hours}:${pad(minutes)}:${pad(total % 60)}`
    : `${pad(minutes)}:${pad(total % 60)}`
}

/** 媒体的名字：语音通话或视频通话 */
export function callMediaLabel(t: Translate, media: ChatCallMedia): string {
  return media === 'Video'
    ? t('chat.call.video')
    : t('chat.call.voice')
}

/**
 * 通话记录在气泡里的一行字
 * 同一件事各人说法不同：一对一里我打出去没人接是「对方未接听」，打给我没接到是「未接听」；
 * 多人通话不说「对方」：受邀的都没来是「无人接听」，只有自己没接到的人看到「未接听」
 */
export function callRecordText(t: Translate, record: ChatCallRecord, viewer: ChatCallRecordViewer): string {
  const { outgoing, group } = viewer
  switch (record.reason) {
    case 'Completed':
      return t('chat.call.record_completed', {
        duration: formatCallDuration(record.durationSeconds),
      })
    case 'Interrupted':
      return t('chat.call.record_interrupted', {
        duration: formatCallDuration(record.durationSeconds),
      })
    case 'Cancelled':
      return outgoing || group
        ? t('chat.call.record_cancelled')
        : t('chat.call.record_cancelled_by_peer')
    case 'Rejected':
      if (group) {
        return t('chat.call.record_no_answer')
      }
      return outgoing
        ? t('chat.call.record_rejected_by_peer')
        : t('chat.call.record_rejected')
    case 'Missed':
      if (group) {
        return !outgoing && record.missedUserIds.includes(viewer.myUserId)
          ? t('chat.call.record_missed')
          : t('chat.call.record_no_answer')
      }
      return outgoing
        ? t('chat.call.record_missed_by_peer')
        : t('chat.call.record_missed')
  }
}

/** 取不到通话设备时说给用户的话 */
export function callMediaFailureText(t: Translate, failure: ChatCallMediaFailure): string {
  switch (failure) {
    case 'missing':
      return t('chat.call.media_missing')
    case 'busy':
      return t('chat.call.media_busy')
    case 'unsupported':
      return t('chat.call.media_unsupported')
    case 'denied':
      return t('chat.call.media_denied')
  }
}

/** 是不是多人通话：群聊与部门群里的通话，一对一的在单聊里 */
export function isGroupCall(call: Pick<ChatCallItem, 'conversationType'>): boolean {
  return call.conversationType !== 'Single'
}

/** 参与者这次加入通话的时刻（毫秒）；没加入过或读不出的为 0 */
export function callJoinedAt(participant: ChatCallParticipantItem): number {
  const joinedAt = Date.parse(participant.joinedAt ?? '')
  return Number.isFinite(joinedAt)
    ? joinedAt
    : 0
}

/**
 * 两个人之间由谁出 offer：后加入通话的一方出，同一毫秒加入的比用户 ID
 * 两端各自按同一份参与者算，结论一致，不会两边同时出 offer
 */
export function callOffersTo(me: ChatCallParticipantItem, peer: ChatCallParticipantItem): boolean {
  const [mine, theirs] = [
    callJoinedAt(me),
    callJoinedAt(peer),
  ]
  return mine === theirs
    ? me.userId > peer.userId
    : mine > theirs
}

/** 一对一通话里的另一个人；找不到（数据不全）为 null */
export function peerOfCall(call: ChatCallItem, myUserId: string): ChatCallParticipantItem | null {
  return call.participants.find(participant => participant.userId !== myUserId) ?? null
}

/** 参与者的称呼：与聊天里别处一致用用户名（会话标题、消息的发送人都是它），没有才取显示名 */
export function callParticipantName(participant: ChatCallParticipantItem | null): string {
  return participant?.userName?.trim() || participant?.displayName?.trim() || ''
}

/** 两个时刻之间的毫秒数；解析不出的按 0 算 */
export function millisecondsBetween(from: string, to: string): number {
  const span = Date.parse(to) - Date.parse(from)
  return Number.isFinite(span)
    ? span
    : 0
}
