/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { ChatCallItem, ChatCallSignalPushPayload, ChatIceServer } from '../types'
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
import { callParticipantName, millisecondsBetween, peerOfCall } from './call-helpers'
import { acquireCallMedia, acquireCallTrack, stopStream } from './call-media'
import { ChatCallSession } from './call-session'
import { decodeCallSignal, encodeCallSignal } from './call-signal'

/**
 * 这个页签眼里的通话阶段
 * incoming：来电在响，还没接；outgoing：我打出去在响；connecting：接通了（或正在接），媒体还没连上；connected：通着
 */
export type ChatCallPhase = 'connected' | 'connecting' | 'idle' | 'incoming' | 'outgoing'

/** 一通电话收场时要告诉用户的事 */
export type ChatCallNoticeKind = 'cancelled' | 'connect-failed' | 'interrupted' | 'no-answer' | 'rejected'

/** 收场提示：界面取走提示后清掉 */
export interface ChatCallNotice {
  kind: ChatCallNoticeKind
  /** 对方的称呼 */
  peerName: string
}

/** 受邀一端比发起端晚这么久才替它挂：正常由发起端到点来挂，这里兜它挂不了的情形 */
const CALLEE_RING_GRACE_MS = 2000
/** 连接断开这么久没恢复即挂断 */
const DROP_GRACE_MS = 8000
/** 接通之后这么久媒体还没连上即挂断（两端之间不通，又没有可用的中继） */
const CONNECT_TIMEOUT_MS = 30000
/** 响铃的间隔：来电急一些，回铃缓一些 */
const RING_INTERVAL_MS = { incoming: 2200, outgoing: 3200 } as const

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
 * 通话的状态以服务端为准：发起、接听、拒绝、挂断走操作，结果经返回值与 ChatCallChanged 推送落地；
 * 媒体在两端的浏览器之间直连，信令经聊天 Hub 转发，只有占着这通电话的页签（发起了或接听了的那个）处理信令
 * 别的页签与别的端只跟着停铃：在别处接了、对方取消了，来电卡片随推送收起
 */
export const useChatCallStore = defineStore('chat-call', () => {
  /** 这个页签眼前的通话；没有为 null */
  const call = shallowRef<ChatCallItem | null>(null)
  /** 这个页签占着它（发起了，或点了接听） */
  const engaged = ref(false)
  const localStream = shallowRef<MediaStream | null>(null)
  const remoteStream = shallowRef<MediaStream | null>(null)
  const micOn = ref(true)
  const cameraOn = ref(false)
  const peerMicOn = ref(true)
  const peerCameraOn = ref(false)
  const connectionState = ref<RTCPeerConnectionState>('new')
  /** 媒体连上的时刻（本机毫秒）；没连上为 null */
  const connectedAt = ref<null | number>(null)
  /** 发起、接听、拒绝的请求在路上 */
  const working = ref(false)
  const notice = shallowRef<ChatCallNotice | null>(null)

  let session: ChatCallSession | null = null
  let sessionCallId: null | string = null
  const pendingSignals: ChatCallSignal[] = []
  /** 发起的请求还没回来时先到的推送（接听可能快过发起的响应） */
  let starting = false
  let stashed: ChatCallItem | null = null
  let ringTimer: null | ReturnType<typeof setTimeout> = null
  let ringInterval: null | ReturnType<typeof setInterval> = null
  let dropTimer: null | ReturnType<typeof setTimeout> = null
  let connectTimer: null | ReturnType<typeof setTimeout> = null

  function myUserId(): string {
    return useUserStore().userInfo?.basicId ?? ''
  }

  /** 有通话权限才出来电：没有的接不了也拒不了，那通电话到点记未接听 */
  function canAnswer(): boolean {
    return useUserStore().hasPermission(CHAT_PERMISSIONS.call)
  }

  const phase = computed<ChatCallPhase>(() => {
    const current = call.value
    if (!current) {
      return 'idle'
    }
    if (current.status === 'Ringing') {
      if (current.initiatorUserId === myUserId()) {
        return 'outgoing'
      }
      return engaged.value
        ? 'connecting'
        : 'incoming'
    }
    return connectionState.value === 'connected'
      ? 'connected'
      : 'connecting'
  })

  /** 通话里的另一个人 */
  const peer = computed(() => (call.value
    ? peerOfCall(call.value, myUserId())
    : null))

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

  function clearConnectionTimers(): void {
    if (dropTimer) {
      clearTimeout(dropTimer)
      dropTimer = null
    }
    if (connectTimer) {
      clearTimeout(connectTimer)
      connectTimer = null
    }
  }

  // ===== 收场 =====

  /** 放掉这通电话占着的东西：媒体连接、设备、计时器与刷新记号 */
  function teardown(): void {
    session?.close()
    session = null
    sessionCallId = null
    pendingSignals.length = 0
    stopStream(localStream.value)
    localStream.value = null
    remoteStream.value = null
    stopRing()
    clearRingTimer()
    clearConnectionTimers()
    engagedMarker.write(null)
  }

  function clear(): void {
    call.value = null
    engaged.value = false
    connectionState.value = 'new'
    connectedAt.value = null
    micOn.value = true
    cameraOn.value = false
    peerMicOn.value = true
    peerCameraOn.value = false
  }

  function noticeFor(kind: ChatCallNoticeKind, item: ChatCallItem): ChatCallNotice {
    return {
      kind,
      peerName: callParticipantName(peerOfCall(item, myUserId())),
    }
  }

  /** 这个页签自己判定打不下去了：收场、提示，并让服务端结束这通电话 */
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
        notice.value = outgoing
          ? noticeFor('rejected', item)
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

  // ===== 媒体连接 =====

  function api() {
    return getChatApi()
  }

  function sendSignal(signal: ChatCallSignal): void {
    const current = call.value
    if (!current) {
      return
    }
    // Hub 断着发不出去：候选丢了连不上时由连接超时收场
    useSignalR(RealtimeHubs.Chat).invoke(RealtimeInvocations.CallSignal, current.callId, encodeCallSignal(signal)).catch(() => {})
  }

  /** 把本端麦克风与摄像头的开关告诉对端 */
  function sendState(): void {
    if (session) {
      sendSignal({ type: 'state', micOn: micOn.value, cameraOn: cameraOn.value })
    }
  }

  function handleConnectionState(state: RTCPeerConnectionState): void {
    connectionState.value = state
    if (state === 'connected') {
      connectedAt.value ??= Date.now()
      clearConnectionTimers()
      return
    }
    if (state === 'failed') {
      fail(connectedAt.value
        ? 'interrupted'
        : 'connect-failed')
      return
    }
    if (state === 'disconnected' && !dropTimer) {
      // 网络抖一下常常自己恢复，留一段宽限
      dropTimer = setTimeout(() => {
        dropTimer = null
        fail('interrupted')
      }, DROP_GRACE_MS)
    }
  }

  /** 接通之后建媒体连接：发起方出 offer，接听方等 offer；重复调用只建一次 */
  async function ensureSession(item: ChatCallItem): Promise<void> {
    if (session || sessionCallId === item.callId) {
      return
    }
    sessionCallId = item.callId
    connectTimer = setTimeout(() => {
      connectTimer = null
      fail('connect-failed')
    }, CONNECT_TIMEOUT_MS)
    try {
      const servers = await api().callIceServers()
      const stream = localStream.value
      // 等的时候通话已经没了
      if (call.value?.callId !== item.callId || !engaged.value || !stream) {
        return
      }
      const created = new ChatCallSession({
        initiator: item.initiatorUserId === myUserId(),
        iceServers: servers.map(toRtcIceServer),
        localStream: stream,
        send: sendSignal,
        onRemoteStream: (remote) => {
          remoteStream.value = remote
        },
        onStateChange: handleConnectionState,
      })
      session = created
      // 存着的信令一次排进去再等：边等边排的话，这期间新到的会插到存着的前面
      const backlog = pendingSignals.splice(0).map(signal => created.handle(signal))
      if (item.initiatorUserId === myUserId()) {
        backlog.push(created.start())
      }
      await Promise.all(backlog)
      sendState()
    }
    catch {
      fail('connect-failed')
    }
  }

  // ===== 响铃到点 =====

  /**
   * 响铃到点由端上来挂：发起端准点挂，受邀端晚一点替它挂（发起端的页面可能已经没了）
   * 服务端把到点之后的挂断一律记未接听
   */
  function scheduleRingEnd(callId: string, remainingMs: number, outgoing: boolean): void {
    clearRingTimer()
    const delay = Math.max(0, remainingMs) + (outgoing
      ? 0
      : CALLEE_RING_GRACE_MS)
    ringTimer = setTimeout(() => {
      ringTimer = null
      const current = call.value
      // 正在接听的不动：接听的结果马上就到
      if (current?.callId !== callId || current.status !== 'Ringing' || (engaged.value && !outgoing)) {
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
    remoteStream.value = null
    micOn.value = true
    cameraOn.value = item.media === 'Video'
    peerMicOn.value = true
    peerCameraOn.value = item.media === 'Video'
    connectionState.value = 'new'
    connectedAt.value = null
    engagedMarker.write(item.callId)
  }

  /** 取走发起期间先到的那条推送 */
  function takeStashed(): ChatCallItem | null {
    const item = stashed
    stashed = null
    return item
  }

  // ===== 用户的动作 =====

  /**
   * 发起通话：先取设备（取不到就不打），再让服务端开始响铃
   * @throws ChatCallMediaError 设备取不到；ApiError 占线、对方不在线等
   */
  async function start(conversationId: string, media: ChatCallMedia): Promise<void> {
    if (call.value || working.value) {
      return
    }
    working.value = true
    starting = true
    stashed = null
    let stream: MediaStream | null = null
    try {
      stream = await acquireCallMedia(media)
      const started = await api().startCall(conversationId, media)
      adopt(started, stream)
      stream = null
      startRing('outgoing')
      scheduleRingEnd(started.callId, millisecondsBetween(started.startedAt, started.ringExpiresAt), true)
      const early = takeStashed()
      if (early && early.callId === started.callId && early.status !== 'Ringing') {
        applyCallChanged(early)
      }
    }
    finally {
      stopStream(stream)
      starting = false
      stashed = null
      working.value = false
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
      if (call.value?.callId !== incoming.callId) {
        return
      }
      // 先占下：接通的推送可能比接听的响应先到，不能当成在别处接了
      adopt(incoming, stream)
      stream = null
      const accepted = await api().acceptCall(incoming.callId)
      if (call.value?.callId === accepted.callId) {
        applyCallChanged(accepted)
      }
    }
    catch (error) {
      if (call.value?.callId === incoming.callId) {
        if (error instanceof ApiError) {
          // 接不了（已经被接听、已结束）：这通不再响
          teardown()
          clear()
        }
        else if (engaged.value) {
          // 请求没发到：放掉设备，来电照旧响着
          stopStream(localStream.value)
          localStream.value = null
          engaged.value = false
          engagedMarker.write(null)
        }
      }
      throw error
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
      // 服务端说拒不了即它已经不在响铃：卡片收起就是了；请求没发到的照常抛出
      if (!(error instanceof ApiError)) {
        throw error
      }
    }
    finally {
      working.value = false
    }
    if (call.value?.callId === incoming.callId) {
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
    await session?.replaceTrack(track)
    const previous = kind === 'audioinput'
      ? stream.getAudioTracks()
      : stream.getVideoTracks()
    for (const old of previous) {
      stream.removeTrack(old)
      old.stop()
    }
    stream.addTrack(track)
  }

  // ===== 推送回灌（由 use-chat-integration 订阅后调用） =====

  function applyCallChanged(item: ChatCallItem): void {
    const mine = myUserId()
    if (!item.participants.some(participant => participant.userId === mine)) {
      return
    }
    const current = call.value
    // 这个页签眼前已经有一通了：别的不打断它，没人理的那通到点记未接听
    if (current && current.callId !== item.callId) {
      return
    }
    if (!current) {
      if (item.status === 'Ringing' && item.initiatorUserId !== mine && canAnswer()) {
        showIncoming(item, millisecondsBetween(item.startedAt, item.ringExpiresAt))
      }
      else if (starting && item.initiatorUserId === mine) {
        stashed = item
      }
      // 其余的不归这个页签：自己在别处发起的，或已经在别处接通的
      return
    }
    if (item.status === 'Ended') {
      finish(item)
      return
    }
    call.value = item
    if (item.status !== 'Ongoing') {
      return
    }
    stopRing()
    clearRingTimer()
    if (engaged.value) {
      void ensureSession(item)
    }
    else {
      // 在别的页签或别的端接了：这里停铃收起
      teardown()
      clear()
    }
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
    if (signal.type === 'state') {
      peerMicOn.value = signal.micOn
      peerCameraOn.value = signal.cameraOn
      return
    }
    if (session) {
      session.handle(signal).catch(() => fail('connect-failed'))
    }
    else {
      // 媒体连接还在建（在取穿透服务器）：先存着
      pendingSignals.push(signal)
    }
  }

  /**
   * 连上或重连之后与服务端对一遍：断线期间结束了的收场，还在响的来电认回来，
   * 刷新之前这个页签占着的那一通替自己挂断（媒体已经随页面没了）
   */
  async function restore(): Promise<void> {
    const user = useUserStore()
    if (!user.hasPermission(CHAT_PERMISSIONS.callRead)) {
      return
    }
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
      applyCallChanged(active)
      return
    }
    if (engagedMarker.read() === active.callId) {
      engagedMarker.write(null)
      await api().hangupCall(active.callId)
      return
    }
    if (active.status === 'Ringing' && active.initiatorUserId !== myUserId() && canAnswer()) {
      showIncoming(active, millisecondsBetween(now, active.ringExpiresAt))
    }
  }

  /** 登出：放掉一切，不通知服务端（会话已经没了，离线处理会收尾） */
  function reset(): void {
    teardown()
    clear()
    notice.value = null
    working.value = false
  }

  return {
    call,
    engaged,
    phase,
    peer,
    localStream,
    remoteStream,
    micOn,
    cameraOn,
    peerMicOn,
    peerCameraOn,
    connectionState,
    connectedAt,
    working,
    notice,
    start,
    accept,
    reject,
    hangup,
    setMicOn,
    setCameraOn,
    switchDevice,
    applyCallChanged,
    applyCallSignal,
    restore,
    reset,
  }
})
