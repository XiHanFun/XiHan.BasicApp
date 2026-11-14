/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { ChatCallItem, ChatCallParticipantItem, ChatCallSettings, ChatCallSignalPushPayload, ChatIceServer } from '../types'
import type { ChatCallSlot, ChatCallTracks } from './call-session'
import type { ChatCallSignal } from './call-signal'
import type { ChatCallMedia } from '~/contracts'
import { defineStore } from 'pinia'
import { computed, ref, shallowRef } from 'vue'
import { playNotificationSound } from '~/composables/useNotificationSound'
import { useSignalR } from '~/composables/useSignalR'
import { RealtimeHubs, RealtimeInvocations } from '~/contracts'
import { ApiError } from '~/request'
import { useUserStore } from '~/stores'
import { getChatApi } from '../api-contract'
import { CHAT_CALL_SESSION_KEY, CHAT_PERMISSIONS } from '../constants'
import { callJoinedAt, callOffersTo, callParticipantName, isGroupCall, millisecondsBetween, peerOfCall } from './call-helpers'
import { acquireCallMedia, acquireCallTrack, acquireScreen, stopStream } from './call-media'
import { ChatCallSession } from './call-session'
import { decodeCallSignal, encodeCallSignal } from './call-signal'

/**
 * 这个页签眼里的通话阶段
 * incoming：来电在响，还没接；outgoing：我打出去在响；connecting：接通了（或正在接、正在加入），媒体还没连上；
 * waiting：接通了，但此刻里面只有我（别人还在响铃）；connected：通着
 */
export type ChatCallPhase = 'connected' | 'connecting' | 'idle' | 'incoming' | 'outgoing' | 'waiting'

/** 一通电话收场时要告诉用户的事 */
export type ChatCallNoticeKind = 'cancelled' | 'connect-failed' | 'interrupted' | 'no-answer' | 'rejected'

/** 收场提示：界面取走提示后清掉 */
export interface ChatCallNotice {
  kind: ChatCallNoticeKind
  /** 一对一通话是对方的称呼；多人通话是发起人的称呼 */
  peerName: string
  /** 多人通话：提示不点某一个人的名 */
  group: boolean
}

/** 通话里的另一个人此刻的样子：与他之间的媒体连接、他的开关与他的媒体 */
export interface ChatCallPeer {
  userId: string
  participant: ChatCallParticipantItem
  /** 与他之间的媒体连接：连接中（含断了在等恢复）、通着、连不上 */
  connection: 'connected' | 'connecting' | 'failed'
  micOn: boolean
  cameraOn: boolean
  screenOn: boolean
  /** 他的声音；轨道没到为 null */
  audio: MediaStream | null
  /** 他的摄像头画面；轨道没到为 null（到了也可能关着，看 cameraOn） */
  camera: MediaStream | null
  /** 他共享的屏幕；轨道没到为 null（到了也可能没在共享，看 screenOn） */
  screen: MediaStream | null
}

/** 与一个对端之间的连接（不进响应式：里面是连接对象与计时器） */
interface PeerLink {
  userId: string
  /** 对端这次加入通话的时刻（毫秒）：变了即他离开又回来过，旧连接作废 */
  joinedAt: number
  session: ChatCallSession | null
  connectTimer: null | ReturnType<typeof setTimeout>
  dropTimer: null | ReturnType<typeof setTimeout>
  failed: boolean
}

/** 受邀一端比发起端晚这么久才替它挂：正常由发起端到点来挂，这里兜它挂不了的情形 */
const CALLEE_RING_GRACE_MS = 2000
/** 连接断开这么久没恢复即当它断了 */
const DROP_GRACE_MS = 8000
/** 与一个人之间这么久媒体还没连上即当它连不上（两端之间不通，又没有可用的中继） */
const CONNECT_TIMEOUT_MS = 30000
/** 响铃的间隔：来电急一些，回铃缓一些 */
const RING_INTERVAL_MS = { incoming: 2200, outgoing: 3200 } as const
/** 一个人的连接建好之前先到的信令最多存这么多条（一次协商用不了这么多，多出来的是乱发的） */
const MAX_PENDING_SIGNALS = 200

/** 同一次加入的时刻前后两次读到的差不出这么多（库里存的精度比内存里低），超过才算重新加入过 */
const REJOIN_TOLERANCE_MS = 1

/** 对端某个轨道位的轨道到了：落到他的哪一路媒体上 */
function streamPatch(slot: ChatCallSlot, stream: MediaStream): Partial<ChatCallPeer> {
  switch (slot) {
    case 'mic':
      return { audio: stream }
    case 'camera':
      return { camera: stream }
    case 'screen':
      return { screen: stream }
  }
}

function toRtcIceServer(server: ChatIceServer): RTCIceServer {
  return {
    urls: [...server.urls],
    ...(server.username
      ? { username: server.username }
      : {}),
    ...(server.credential
      ? { credential: server.credential }
      : {}),
  }
}

/** 会话存储读写：存储不可用（隐私模式等）时当作没有记号 */
const engagedMarker = {
  read(): null | string {
    try {
      return globalThis.sessionStorage?.getItem(CHAT_CALL_SESSION_KEY) ?? null
    }
    catch {
      return null
    }
  },
  write(callId: null | string): void {
    try {
      if (callId) {
        globalThis.sessionStorage?.setItem(CHAT_CALL_SESSION_KEY, callId)
      }
      else {
        globalThis.sessionStorage?.removeItem(CHAT_CALL_SESSION_KEY)
      }
    }
    catch {
      // 记不下来只影响刷新后的自动挂断，对端的断线检测与离线处理照样会收尾
    }
  },
}

/**
 * 通话状态 store：这个页签看到的那一通电话（来电在响，或自己正占着的）与它的媒体
 *
 * 通话的状态以服务端为准：发起、接听、拒绝、加入、挂断走操作，结果经返回值与 ChatCallChanged 推送落地；
 * 媒体在浏览器之间直连，多人通话里每两个人之间各一条连接；信令经聊天 Hub 转发，只有占着这通电话的页签（发起了、接听了或加入了的那个）处理信令
 * 别的页签与别的端只跟着停铃：在别处接了、对方取消了，来电卡片随推送收起
 * 群聊里还没结束的通话另记在会话名下：没被邀请、离开了的成员据此看得到有通话在进行、能加入
 */
export const useChatCallStore = defineStore('chat-call', () => {
  /** 这个页签眼前的通话；没有为 null */
  const call = shallowRef<ChatCallItem | null>(null)
  /** 这个页签占着它（发起了，或点了接听、加入） */
  const engaged = ref(false)
  /** 本端的麦克风与摄像头 */
  const localStream = shallowRef<MediaStream | null>(null)
  /** 本端正在共享的屏幕；没在共享为 null */
  const screenStream = shallowRef<MediaStream | null>(null)
  const micOn = ref(true)
  const cameraOn = ref(false)
  /** 通话里已经加入的其他人，次序随通话的参与者 */
  const peers = shallowRef<ChatCallPeer[]>([])
  /** 第一次与人连上的时刻（本机毫秒）；没连上过为 null */
  const connectedAt = ref<null | number>(null)
  /** 发起、接听、拒绝、加入的请求在路上 */
  const working = ref(false)
  const notice = shallowRef<ChatCallNotice | null>(null)
  /** 各会话里还没结束的多人通话（会话 ID → 通话） */
  const ongoingCalls = shallowRef<Record<string, ChatCallItem>>({})
  /** 响铃秒数与人数上限；没取过为 null */
  const settings = shallowRef<ChatCallSettings | null>(null)

  const links = new Map<string, PeerLink>()
  const pendingSignals = new Map<string, ChatCallSignal[]>()
  /** 取穿透服务器的请求：同时建几条连接时合用一次，取完即弃（凭据会过期，后来的人重新取） */
  let iceRequest: null | Promise<RTCIceServer[]> = null
  /** 发起的请求还没回来时先到的推送（接听可能快过发起的响应） */
  let starting = false
  let stashed: ChatCallItem | null = null
  /** 接听或加入了、还没看到自己已加入的状态：这期间到的状态里我还不算已加入，不能当成被请出去了 */
  let entering = false
  /**
   * 推送到过几次：推送按服务端提交的先后到，响应与推送之间却没有先后
   * 请求在路上时到过推送的，以推送为准、不拿响应覆盖：响应可能比它旧，照着旧的对齐会把刚建好的连接关掉
   */
  let pushes = 0
  let sharing = false
  let ringTimer: null | ReturnType<typeof setTimeout> = null
  let ringInterval: null | ReturnType<typeof setInterval> = null
  /** 正开着的会话：重连之后把它里面的通话重取一遍 */
  let watchedConversationId: null | string = null
  const ongoingRevisions = new Map<string, number>()

  function myUserId(): string {
    return useUserStore().userInfo?.basicId ?? ''
  }

  /** 有通话权限才出来电：没有的接不了也拒不了，那通电话到点记未接听 */
  function canAnswer(): boolean {
    return useUserStore().hasPermission(CHAT_PERMISSIONS.call)
  }

  function mineIn(item: ChatCallItem): ChatCallParticipantItem | null {
    const mine = myUserId()
    return item.participants.find(participant => participant.userId === mine) ?? null
  }

  const phase = computed<ChatCallPhase>(() => {
    const current = call.value
    if (!current) {
      return 'idle'
    }
    if (!engaged.value) {
      return 'incoming'
    }
    if (current.status === 'Ringing') {
      return current.initiatorUserId === myUserId()
        ? 'outgoing'
        : 'connecting'
    }
    if (mineIn(current)?.status !== 'Joined') {
      return 'connecting'
    }
    if (peers.value.length === 0) {
      return 'waiting'
    }
    return peers.value.some(peer => peer.connection === 'connected')
      ? 'connected'
      : 'connecting'
  })

  /** 一对一通话里的另一个人；多人通话里是发起人（来电卡片上说是谁叫的） */
  const peer = computed(() => {
    const current = call.value
    if (!current) {
      return null
    }
    return isGroupCall(current)
      ? current.participants.find(participant => participant.userId === current.initiatorUserId) ?? null
      : peerOfCall(current, myUserId())
  })

  const screenOn = computed(() => screenStream.value !== null)

  // ===== 响铃 =====

  function stopRing(): void {
    if (ringInterval) {
      clearInterval(ringInterval)
      ringInterval = null
    }
  }

  function startRing(kind: 'incoming' | 'outgoing'): void {
    stopRing()
    const sound = kind === 'incoming'
      ? 'notification'
      : 'receive'
    playNotificationSound(sound)
    ringInterval = setInterval(playNotificationSound, RING_INTERVAL_MS[kind], sound)
  }

  function clearRingTimer(): void {
    if (ringTimer) {
      clearTimeout(ringTimer)
      ringTimer = null
    }
  }

  // ===== 与各人之间的连接 =====

  function api() {
    return getChatApi()
  }

  function patchPeer(userId: string, patch: Partial<ChatCallPeer>): void {
    peers.value = peers.value.map(existing => (existing.userId === userId
      ? {
          ...existing,
          ...patch,
        }
      : existing))
  }

  function clearLinkTimers(link: PeerLink): void {
    if (link.connectTimer) {
      clearTimeout(link.connectTimer)
      link.connectTimer = null
    }
    if (link.dropTimer) {
      clearTimeout(link.dropTimer)
      link.dropTimer = null
    }
  }

  function dropLink(link: PeerLink): void {
    clearLinkTimers(link)
    link.session?.close()
    link.session = null
  }

  /** 这个人不在通话里了：关掉与他之间的连接 */
  function closeLink(userId: string): void {
    const link = links.get(userId)
    if (link) {
      dropLink(link)
      links.delete(userId)
    }
    pendingSignals.delete(userId)
  }

  function sendSignal(toUserId: null | string, signal: ChatCallSignal): void {
    const current = call.value
    if (!current) {
      return
    }
    // Hub 断着发不出去：候选丢了连不上时由连接超时收场
    useSignalR(RealtimeHubs.Chat).invoke(RealtimeInvocations.CallSignal, current.callId, toUserId, encodeCallSignal(signal)).catch(() => {})
  }

  /** 把本端麦克风、摄像头与屏幕共享的开关告诉对端；不指人即告诉通话里的所有人 */
  function sendState(toUserId: null | string = null): void {
    if (links.size > 0) {
      sendSignal(toUserId, {
        type: 'state',
        micOn: micOn.value,
        cameraOn: cameraOn.value,
        screenOn: screenOn.value,
      })
    }
  }

  function localTracks(): ChatCallTracks {
    return {
      mic: localStream.value?.getAudioTracks()[0] ?? null,
      camera: localStream.value?.getVideoTracks()[0] ?? null,
      screen: screenStream.value?.getVideoTracks()[0] ?? null,
    }
  }

  /** 与这个人之间连不上或断了：这条连接作废；与所有人都这样了，这通电话就打不下去 */
  function failLink(link: PeerLink): void {
    if (links.get(link.userId) !== link || link.failed) {
      return
    }
    dropLink(link)
    link.failed = true
    pendingSignals.delete(link.userId)
    patchPeer(link.userId, {
      connection: 'failed',
      audio: null,
      camera: null,
      screen: null,
    })
    if ([
      ...links.values(),
    ].every(other => other.failed)) {
      fail(connectedAt.value
        ? 'interrupted'
        : 'connect-failed')
    }
  }

  function handleLinkState(link: PeerLink, state: RTCPeerConnectionState): void {
    if (links.get(link.userId) !== link || link.failed) {
      return
    }
    if (state === 'connected') {
      clearLinkTimers(link)
      connectedAt.value ??= Date.now()
      patchPeer(link.userId, { connection: 'connected' })
      return
    }
    if (state === 'failed') {
      failLink(link)
      return
    }
    if (state === 'disconnected' && !link.dropTimer) {
      // 网络抖一下常常自己恢复，留一段宽限
      patchPeer(link.userId, { connection: 'connecting' })
      link.dropTimer = setTimeout(() => {
        link.dropTimer = null
        failLink(link)
      }, DROP_GRACE_MS)
    }
  }

  function iceServers(): Promise<RTCIceServer[]> {
    iceRequest ??= api().callIceServers().then(servers => servers.map(toRtcIceServer)).finally(() => {
      iceRequest = null
    })
    return iceRequest
  }

  /** 与一个刚加入的人建连接：后加入的一方出 offer，另一方等 */
  async function openLink(item: ChatCallItem, participant: ChatCallParticipantItem): Promise<void> {
    const me = mineIn(item)
    const link: PeerLink = {
      userId: participant.userId,
      joinedAt: callJoinedAt(participant),
      session: null,
      connectTimer: null,
      dropTimer: null,
      failed: false,
    }
    links.set(link.userId, link)
    link.connectTimer = setTimeout(() => {
      link.connectTimer = null
      failLink(link)
    }, CONNECT_TIMEOUT_MS)
    try {
      const servers = await iceServers()
      // 等的时候通话没了，或这个人又走了
      if (call.value?.callId !== item.callId || links.get(link.userId) !== link || link.failed || !localStream.value) {
        return
      }
      const offerer = me !== null && callOffersTo(me, participant)
      const created = new ChatCallSession({
        offerer,
        iceServers: servers,
        tracks: localTracks(),
        send: signal => sendSignal(link.userId, signal),
        onRemoteTrack: (slot, track) => {
          if (links.get(link.userId) === link) {
            patchPeer(link.userId, streamPatch(slot, new MediaStream([track])))
          }
        },
        onStateChange: state => handleLinkState(link, state),
      })
      link.session = created
      // 存着的信令一次排进去再等：边等边排的话，这期间新到的会插到存着的前面
      const backlog = (pendingSignals.get(link.userId) ?? []).map(signal => created.handle(signal))
      pendingSignals.delete(link.userId)
      if (offerer) {
        backlog.push(created.start())
      }
      await Promise.all(backlog)
      sendState(link.userId)
    }
    catch {
      failLink(link)
    }
  }

  /** 按通话此刻的参与者对齐连接与对端列表：走了的关掉，新来的建上 */
  function syncPeers(item: ChatCallItem): void {
    const mine = myUserId()
    const joined = item.participants.filter(participant => participant.userId !== mine && participant.status === 'Joined')
    // 边走边删是安全的：Map 的遍历不受删除当前项影响
    for (const link of links.values()) {
      const participant = joined.find(candidate => candidate.userId === link.userId)
      if (!participant || Math.abs(callJoinedAt(participant) - link.joinedAt) > REJOIN_TOLERANCE_MS) {
        closeLink(link.userId)
      }
    }
    const known = new Map(peers.value.map(existing => [existing.userId, existing]))
    peers.value = joined.map((participant): ChatCallPeer => {
      const existing = links.has(participant.userId)
        ? known.get(participant.userId)
        : undefined
      return existing
        ? { ...existing, participant }
        : {
            userId: participant.userId,
            participant,
            connection: 'connecting',
            micOn: true,
            cameraOn: item.media === 'Video',
            screenOn: false,
            audio: null,
            camera: null,
            screen: null,
          }
    })
    for (const participant of joined) {
      if (!links.has(participant.userId)) {
        void openLink(item, participant)
      }
    }
  }

  // ===== 收场 =====

  /** 放掉这通电话占着的东西：媒体连接、设备、计时器与刷新记号 */
  function teardown(): void {
    for (const link of links.values()) {
      dropLink(link)
    }
    links.clear()
    pendingSignals.clear()
    stopStream(localStream.value)
    localStream.value = null
    stopStream(screenStream.value)
    screenStream.value = null
    stopRing()
    clearRingTimer()
    engagedMarker.write(null)
  }

  function clear(): void {
    entering = false
    call.value = null
    engaged.value = false
    peers.value = []
    connectedAt.value = null
    micOn.value = true
    cameraOn.value = false
  }

  function noticeFor(kind: ChatCallNoticeKind, item: ChatCallItem): ChatCallNotice {
    const group = isGroupCall(item)
    const named = group
      ? item.participants.find(participant => participant.userId === item.initiatorUserId) ?? null
      : peerOfCall(item, myUserId())
    return {
      kind,
      peerName: callParticipantName(named),
      group,
    }
  }

  /** 这个页签自己判定打不下去了：收场、提示，并让服务端把自己从这通电话里拿出来 */
  function fail(kind: ChatCallNoticeKind): void {
    const current = call.value
    if (!current || !engaged.value) {
      return
    }
    teardown()
    clear()
    notice.value = noticeFor(kind, current)
    // 挂断失败不再提示：这通电话由对端的断线检测或服务端的收尾作业结束
    api().hangupCall(current.callId).catch(() => {})
  }

  /** 服务端说这通电话结束了 */
  function finish(item: ChatCallItem): void {
    const wasEngaged = engaged.value
    const outgoing = item.initiatorUserId === myUserId()
    teardown()
    clear()
    switch (item.endReason) {
      case 'Rejected':
        // 多人通话里不点谁拒绝了：受邀的都没来，说没人接
        notice.value = outgoing
          ? noticeFor(isGroupCall(item)
              ? 'no-answer'
              : 'rejected', item)
          : null
        break
      case 'Missed':
        notice.value = outgoing
          ? noticeFor('no-answer', item)
          : null
        break
      case 'Cancelled':
        notice.value = outgoing
          ? null
          : noticeFor('cancelled', item)
        break
      case 'Interrupted':
        notice.value = wasEngaged
          ? noticeFor('interrupted', item)
          : null
        break
      default:
        notice.value = null
    }
  }

  // ===== 响铃到点 =====

  /**
   * 响铃到点由端上来挂：发起端准点挂，受邀端晚一点替它挂（发起端的页面可能已经没了）
   * 服务端把到点之后的挂断一律记未接听；多人通话已经有人接了的，受邀端到点挂的只是自己那份邀请
   */
  function scheduleRingEnd(callId: string, remainingMs: number, outgoing: boolean): void {
    clearRingTimer()
    const delay = Math.max(0, remainingMs) + (outgoing
      ? 0
      : CALLEE_RING_GRACE_MS)
    ringTimer = setTimeout(() => {
      ringTimer = null
      const current = call.value
      if (current?.callId !== callId) {
        return
      }
      // 发起端：有人接了就不归它挂；受邀端：正在接听的不动，接听的结果马上就到
      if (outgoing
        ? current.status !== 'Ringing'
        : engaged.value) {
        return
      }
      teardown()
      clear()
      notice.value = outgoing
        ? noticeFor('no-answer', current)
        : null
      api().hangupCall(callId).catch(() => {})
    }, delay)
  }

  function showIncoming(item: ChatCallItem, remainingMs: number): void {
    call.value = item
    engaged.value = false
    startRing('incoming')
    scheduleRingEnd(item.callId, remainingMs, false)
  }

  /** 这个页签占下这通电话：设备归它，刷新后凭记号替自己挂断 */
  function adopt(item: ChatCallItem, stream: MediaStream): void {
    call.value = item
    engaged.value = true
    localStream.value = stream
    peers.value = []
    micOn.value = true
    cameraOn.value = item.media === 'Video'
    connectedAt.value = null
    engagedMarker.write(item.callId)
  }

  /** 取走发起期间先到的那条推送 */
  function takeStashed(): ChatCallItem | null {
    const item = stashed
    stashed = null
    return item
  }

  // ===== 会话里还没结束的通话 =====

  function setOngoing(conversationId: string, item: ChatCallItem | null): void {
    const current = ongoingCalls.value[conversationId]
    if (!item && !current) {
      return
    }
    const next = { ...ongoingCalls.value }
    if (item) {
      next[conversationId] = item
    }
    else {
      delete next[conversationId]
    }
    ongoingCalls.value = next
  }

  function noteOngoing(item: ChatCallItem): void {
    if (!isGroupCall(item)) {
      return
    }
    ongoingRevisions.set(item.conversationId, (ongoingRevisions.get(item.conversationId) ?? 0) + 1)
    setOngoing(item.conversationId, item.status === 'Ended'
      ? null
      : item)
  }

  /** 取一个会话里还没结束的通话；取的时候推送先到了的，以推送为准 */
  async function loadOngoing(conversationId: string): Promise<void> {
    if (!useUserStore().hasPermission(CHAT_PERMISSIONS.callRead)) {
      return
    }
    const revision = ongoingRevisions.get(conversationId) ?? 0
    const { call: found } = await api().ongoingCall(conversationId)
    if ((ongoingRevisions.get(conversationId) ?? 0) === revision) {
      setOngoing(conversationId, found ?? null)
    }
  }

  /**
   * 正开着哪个群会话：它里面的通话取一遍（之后的变化经会话组推来），重连之后再取
   * 离开的那个会话不再有推送：我不在它的通话里的，记着的那份随手丢掉，免得下次打开先闪一下旧的
   */
  async function watchConversation(conversationId: null | string): Promise<void> {
    const previous = watchedConversationId
    watchedConversationId = conversationId
    if (previous && previous !== conversationId) {
      const kept = ongoingCalls.value[previous]
      if (kept && !mineIn(kept)) {
        setOngoing(previous, null)
      }
    }
    if (conversationId) {
      await loadOngoing(conversationId)
    }
  }

  /** 取通话的设置（响铃秒数与人数上限）；发起多人通话选人时用 */
  async function loadSettings(): Promise<ChatCallSettings> {
    const loaded = await api().callSettings()
    settings.value = loaded
    return loaded
  }

  // ===== 用户的动作 =====

  /**
   * 发起通话：先取设备（取不到就不打），再让服务端开始响铃
   * @param conversationId 会话
   * @param media 语音还是视频
   * @param inviteeUserIds 群聊与部门群里要邀请的成员；单聊不用给
   * @throws ChatCallMediaError 设备取不到；ApiError 占线、对方不在线等
   */
  async function start(conversationId: string, media: ChatCallMedia, inviteeUserIds?: string[]): Promise<void> {
    if (call.value || working.value) {
      return
    }
    working.value = true
    starting = true
    stashed = null
    let stream: MediaStream | null = null
    try {
      stream = await acquireCallMedia(media)
      const started = await api().startCall(conversationId, media, inviteeUserIds)
      adopt(started, stream)
      stream = null
      noteOngoing(started)
      startRing('outgoing')
      scheduleRingEnd(started.callId, millisecondsBetween(started.startedAt, started.ringExpiresAt), true)
      // 发起期间到过这通电话的推送：它不比响应旧，照它落地
      const early = takeStashed()
      if (early && early.callId === started.callId) {
        apply(early)
      }
    }
    finally {
      stopStream(stream)
      starting = false
      stashed = null
      working.value = false
    }
  }

  /** 接听与加入共用的后半段：占下、问服务端、按结果落地；问不成的放掉 */
  async function enter(target: ChatCallItem, stream: MediaStream, request: (callId: string) => Promise<ChatCallItem>, wasIncoming: boolean): Promise<void> {
    // 先占下：接通的推送可能比响应先到，不能当成在别处接了
    adopt(target, stream)
    entering = true
    const seen = pushes
    try {
      const entered = await request(target.callId)
      // 请求在路上时到过推送的不用响应：自己已加入的那条推送要么已经到了，要么马上跟来
      if (pushes === seen && call.value?.callId === entered.callId) {
        apply(entered)
      }
    }
    catch (error) {
      entering = false
      if (call.value?.callId === target.callId && engaged.value) {
        if (wasIncoming && !(error instanceof ApiError)) {
          // 请求没发到：放掉设备，来电照旧响着
          stopStream(localStream.value)
          localStream.value = null
          engaged.value = false
          engagedMarker.write(null)
        }
        else {
          // 进不去（已经被接听、已结束、人数满了）：这通不再占着
          teardown()
          clear()
        }
      }
      throw error
    }
  }

  /**
   * 接听来电：先取设备，再告诉服务端
   * @throws ChatCallMediaError 设备取不到；ApiError 已经被接听或已结束等
   */
  async function accept(): Promise<void> {
    const incoming = call.value
    if (!incoming || phase.value !== 'incoming' || working.value) {
      return
    }
    working.value = true
    let stream: MediaStream | null = null
    try {
      stream = await acquireCallMedia(incoming.media)
      // 取设备的时候来电已经没了（对方取消或到点）
      if (call.value?.callId !== incoming.callId || engaged.value) {
        return
      }
      const acquired = stream
      stream = null
      await enter(call.value, acquired, callId => api().acceptCall(callId), true)
    }
    finally {
      stopStream(stream)
      working.value = false
    }
  }

  /**
   * 加入会话里一通还没结束的多人通话：先取设备，再告诉服务端；它正是眼前在响的来电时等同接听
   * @throws ChatCallMediaError 设备取不到；ApiError 已经结束、人数满了、自己正在别的通话里等
   */
  async function join(target: ChatCallItem): Promise<void> {
    if (call.value?.callId === target.callId && phase.value === 'incoming') {
      await accept()
      return
    }
    if (call.value || working.value) {
      return
    }
    working.value = true
    let stream: MediaStream | null = null
    try {
      stream = await acquireCallMedia(target.media)
      // 取设备的时候眼前有了别的通话（来电响了）
      if (call.value) {
        return
      }
      const acquired = stream
      stream = null
      await enter(target, acquired, callId => api().joinCall(callId), false)
    }
    finally {
      stopStream(stream)
      working.value = false
    }
  }

  /** 拒绝来电 */
  async function reject(): Promise<void> {
    const incoming = call.value
    if (!incoming || phase.value !== 'incoming' || working.value) {
      return
    }
    working.value = true
    try {
      await api().rejectCall(incoming.callId)
    }
    catch (error) {
      // 服务端说拒不了即它已经不在等我答复：卡片收起就是了；请求没发到的照常抛出
      if (!(error instanceof ApiError)) {
        throw error
      }
    }
    finally {
      working.value = false
    }
    if (call.value?.callId === incoming.callId && !engaged.value) {
      teardown()
      clear()
    }
  }

  /** 挂断自己占着的通话：先放掉设备，再告诉服务端 */
  async function hangup(): Promise<void> {
    const current = call.value
    if (!current || !engaged.value) {
      return
    }
    teardown()
    clear()
    await api().hangupCall(current.callId)
  }

  function setMicOn(on: boolean): void {
    micOn.value = on
    for (const track of localStream.value?.getAudioTracks() ?? []) {
      track.enabled = on
    }
    sendState()
  }

  function setCameraOn(on: boolean): void {
    cameraOn.value = on
    for (const track of localStream.value?.getVideoTracks() ?? []) {
      track.enabled = on
    }
    sendState()
  }

  /**
   * 通话中换麦克风或摄像头：新轨道原地替换，不重新协商
   * @throws ChatCallMediaError 新设备取不到（旧的照用）
   */
  async function switchDevice(kind: 'audioinput' | 'videoinput', deviceId: string): Promise<void> {
    const stream = localStream.value
    if (!stream) {
      return
    }
    const track = await acquireCallTrack(kind, deviceId)
    if (localStream.value !== stream) {
      // 取设备的时候通话已经结束
      track.stop()
      return
    }
    track.enabled = kind === 'audioinput'
      ? micOn.value
      : cameraOn.value
    const slot: ChatCallSlot = kind === 'audioinput'
      ? 'mic'
      : 'camera'
    await Promise.all([
      ...links.values(),
    ].map(link => link.session?.setTrack(slot, track).catch(() => {})))
    const previous = kind === 'audioinput'
      ? stream.getAudioTracks()
      : stream.getVideoTracks()
    for (const old of previous) {
      stream.removeTrack(old)
      old.stop()
    }
    stream.addTrack(track)
  }

  /** 停止共享屏幕 */
  function stopScreenShare(): void {
    const stream = screenStream.value
    if (!stream) {
      return
    }
    screenStream.value = null
    for (const link of links.values()) {
      link.session?.setTrack('screen', null).catch(() => {})
    }
    stopStream(stream)
    sendState()
  }

  /**
   * 开始共享屏幕：浏览器让用户挑要共享的屏幕、窗口或标签页，挑好即发给通话里的每个人；用户取消了就什么都不做
   * @throws ChatCallMediaError 浏览器不支持，或屏幕取不到
   */
  async function startScreenShare(): Promise<void> {
    const current = call.value
    if (!current || !engaged.value || screenStream.value || sharing) {
      return
    }
    sharing = true
    try {
      const stream = await acquireScreen()
      if (!stream) {
        return
      }
      const [track] = stream.getVideoTracks()
      // 挑的时候通话已经结束
      if (!track || call.value?.callId !== current.callId || !engaged.value) {
        stopStream(stream)
        return
      }
      // 用户从浏览器自己的「停止共享」上停掉
      track.addEventListener('ended', () => {
        if (screenStream.value === stream) {
          stopScreenShare()
        }
      })
      screenStream.value = stream
      for (const link of links.values()) {
        link.session?.setTrack('screen', track).catch(() => {})
      }
      sendState()
    }
    finally {
      sharing = false
    }
  }

  // ===== 状态落地：推送回灌（由 use-chat-integration 订阅后调用）与响应 =====

  /** 推来一条通话的状态 */
  function applyCallChanged(item: ChatCallItem): void {
    pushes += 1
    apply(item)
  }

  /** 按一份通话的状态落地（推送，或没有更新的推送可用时的响应） */
  function apply(item: ChatCallItem): void {
    noteOngoing(item)
    const me = mineIn(item)
    const current = call.value
    // 这个页签眼前已经有一通了：别的不打断它，没人理的那通到点记未接听
    if (current && current.callId !== item.callId) {
      return
    }
    if (!current) {
      if (!me) {
        return
      }
      if (item.status !== 'Ended' && me.status === 'Invited' && canAnswer()) {
        showIncoming(item, millisecondsBetween(item.startedAt, item.ringExpiresAt))
      }
      else if (starting && item.initiatorUserId === me.userId) {
        stashed = item
      }
      // 其余的不归这个页签：自己在别处发起的、已经在别处接通的，或自己已经不在里面的
      return
    }
    if (item.status === 'Ended') {
      finish(item)
      return
    }
    if (!engaged.value) {
      // 来电卡片：还在等我答复就接着响（别人先接了也一样）；在别处接了、拒了，或到点记了没接到，这里停铃收起
      if (me?.status === 'Invited') {
        call.value = item
      }
      else {
        teardown()
        clear()
      }
      return
    }
    if (me?.status !== 'Joined') {
      if (entering) {
        // 接听或加入的结果还没到：这条状态比它早
        call.value = item
        return
      }
      // 服务端已经把我记成离开（它以为我掉线了）：这个页签退出来
      teardown()
      clear()
      notice.value = noticeFor('interrupted', item)
      return
    }
    entering = false
    call.value = item
    if (item.status !== 'Ongoing') {
      return
    }
    stopRing()
    clearRingTimer()
    syncPeers(item)
  }

  function applyCallSignal(payload: ChatCallSignalPushPayload): void {
    const current = call.value
    if (!current || !engaged.value || current.callId !== payload.callId || payload.fromUserId === myUserId()) {
      return
    }
    const signal = decodeCallSignal(payload.data)
    if (!signal) {
      return
    }
    const from = payload.fromUserId
    if (signal.type === 'state') {
      patchPeer(from, { micOn: signal.micOn, cameraOn: signal.cameraOn, screenOn: signal.screenOn })
      return
    }
    const link = links.get(from)
    if (link?.failed) {
      return
    }
    if (link?.session) {
      link.session.handle(signal).catch(() => failLink(link))
      return
    }
    // 与他的连接还在建（在取穿透服务器），或他加入的推送还没到：先存着
    const waiting = pendingSignals.get(from) ?? []
    if (waiting.length < MAX_PENDING_SIGNALS) {
      waiting.push(signal)
      pendingSignals.set(from, waiting)
    }
  }

  /**
   * 连上或重连之后与服务端对一遍：断线期间结束了的收场，还在等我答复的来电认回来，
   * 刷新之前这个页签占着的那一通替自己挂断（媒体已经随页面没了）
   */
  async function restore(): Promise<void> {
    const user = useUserStore()
    if (!user.hasPermission(CHAT_PERMISSIONS.callRead)) {
      return
    }
    if (watchedConversationId) {
      loadOngoing(watchedConversationId).catch(() => {})
    }
    const seen = pushes
    const { call: active, now } = await api().activeCall()
    const current = call.value
    if (current && (!active || active.callId !== current.callId)) {
      teardown()
      clear()
    }
    if (!active) {
      engagedMarker.write(null)
      return
    }
    if (call.value?.callId === active.callId) {
      // 取的时候到过推送的以推送为准
      if (pushes === seen) {
        apply(active)
      }
      return
    }
    if (engagedMarker.read() === active.callId) {
      engagedMarker.write(null)
      await api().hangupCall(active.callId)
      return
    }
    if (mineIn(active)?.status === 'Invited' && canAnswer()) {
      noteOngoing(active)
      showIncoming(active, millisecondsBetween(now, active.ringExpiresAt))
    }
  }

  /** 登出：放掉一切，不通知服务端（会话已经没了，离线处理会收尾） */
  function reset(): void {
    teardown()
    clear()
    notice.value = null
    working.value = false
    ongoingCalls.value = {}
    ongoingRevisions.clear()
    watchedConversationId = null
    settings.value = null
  }

  return {
    call,
    engaged,
    phase,
    peer,
    peers,
    localStream,
    screenStream,
    micOn,
    cameraOn,
    screenOn,
    connectedAt,
    working,
    notice,
    ongoingCalls,
    settings,
    start,
    accept,
    join,
    reject,
    hangup,
    setMicOn,
    setCameraOn,
    switchDevice,
    startScreenShare,
    stopScreenShare,
    loadOngoing,
    watchConversation,
    loadSettings,
    applyCallChanged,
    applyCallSignal,
    restore,
    reset,
  }
})
