/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { ChatCallSignal } from './call-signal'

/** 一条连接上的轨道位：麦克风、摄像头与共享的屏幕 */
export type ChatCallSlot = 'camera' | 'mic' | 'screen'

/** 轨道位的固定次序：两端按它对齐，对端的轨道凭位置认出是哪一路 */
export const CHAT_CALL_SLOTS: readonly ChatCallSlot[] = ['mic', 'camera', 'screen']

const SLOT_KIND: Record<ChatCallSlot, 'audio' | 'video'> = {
  mic: 'audio',
  camera: 'video',
  screen: 'video',
}

/** 各轨道位上本端此刻在发的轨道；没有（没开摄像头、没在共享）为 null 或不写 */
export type ChatCallTracks = Partial<Record<ChatCallSlot, MediaStreamTrack | null>>

/** 与一个对端之间的媒体连接要的东西 */
export interface ChatCallSessionOptions {
  /** 出 offer 的一方；另一方等 offer 再回 answer */
  offerer: boolean
  /** 穿透用的 STUN 与 TURN；没配为空，只有能直连的两端打得通 */
  iceServers: RTCIceServer[]
  /** 建连接时本端各轨道位上的轨道 */
  tracks: ChatCallTracks
  /** 把信令交给对端（经聊天 Hub 转发） */
  send: (signal: ChatCallSignal) => void
  /** 对端某个轨道位的轨道到了（轨道在不在出画面、出声音，看对端发来的开关状态） */
  onRemoteTrack: (slot: ChatCallSlot, track: MediaStreamTrack) => void
  /** 连接状态变了 */
  onStateChange: (state: RTCPeerConnectionState) => void
  /** 建连接的方法；测试注入替身，缺省用浏览器的 RTCPeerConnection */
  createPeer?: (configuration: RTCConfiguration) => RTCPeerConnection
}

/**
 * 与一个对端之间的媒体连接（浏览器原生 WebRTC）；多人通话里每两个人之间各有一条
 * 只管两端之间的连接：会话描述的一来一回、ICE 候选的交换与本端轨道的替换；通话的状态（谁叫了谁、接没接）不归它管
 * 三个轨道位在协商时一次留好：之后开关摄像头、开始与停止共享屏幕都只是往位上换轨道，不重新协商
 * 信令一条一条顺序处理：对端的候选可能比会话描述先到，先存着，等远端描述设好再加
 */
export class ChatCallSession {
  private readonly peer: RTCPeerConnection
  private readonly options: ChatCallSessionOptions
  private readonly tracks: ChatCallTracks
  private readonly pendingCandidates: RTCIceCandidateInit[] = []
  private remoteDescribed = false
  private closed = false
  private queue: Promise<void> = Promise.resolve()

  constructor(options: ChatCallSessionOptions) {
    this.options = options
    this.tracks = { ...options.tracks }
    const createPeer = options.createPeer ?? (configuration => new RTCPeerConnection(configuration))
    this.peer = createPeer({ iceServers: options.iceServers })
    if (options.offerer) {
      // 出 offer 的一方按固定次序留位；另一方的位由对端的 offer 带出来
      for (const slot of CHAT_CALL_SLOTS) {
        this.peer.addTransceiver(this.tracks[slot] ?? SLOT_KIND[slot], { direction: 'sendrecv' })
      }
    }
    this.peer.onicecandidate = (event) => {
      if (event.candidate && !this.closed) {
        options.send({
          type: 'candidate',
          candidate: event.candidate.toJSON(),
        })
      }
    }
    this.peer.ontrack = (event) => {
      const slot = CHAT_CALL_SLOTS[this.peer.getTransceivers().indexOf(event.transceiver)]
      if (slot && !this.closed) {
        options.onRemoteTrack(slot, event.track)
      }
    }
    this.peer.onconnectionstatechange = () => {
      if (!this.closed) {
        options.onStateChange(this.peer.connectionState)
      }
    }
  }

  /** 出 offer 的一方开始协商：把 offer 交给对端 */
  start(): Promise<void> {
    return this.enqueue(async () => {
      const offer = await this.peer.createOffer()
      await this.peer.setLocalDescription(offer)
      this.options.send({ type: 'offer', sdp: offer.sdp ?? '' })
    })
  }

  /** 处理对端转来的一条信令（开关状态不归这里，由调用方直接用） */
  handle(signal: ChatCallSignal): Promise<void> {
    return this.enqueue(async () => {
      switch (signal.type) {
        case 'offer': {
          await this.describeRemote({ type: 'offer', sdp: signal.sdp })
          await this.occupySlots()
          const answer = await this.peer.createAnswer()
          await this.peer.setLocalDescription(answer)
          this.options.send({ type: 'answer', sdp: answer.sdp ?? '' })
          break
        }
        case 'answer':
          await this.describeRemote({ type: 'answer', sdp: signal.sdp })
          break
        case 'candidate':
          if (this.remoteDescribed) {
            await this.addCandidate(signal.candidate)
          }
          else {
            this.pendingCandidates.push(signal.candidate)
          }
          break
        case 'state':
          break
      }
    })
  }

  /**
   * 往一个轨道位上换轨道：换设备、开始共享屏幕给新轨道，停止共享给 null；发送端原地替换，不重新协商
   * 应答的一方在对端的 offer 到之前还没有位：先记着，应答时一并挂上
   */
  setTrack(slot: ChatCallSlot, track: MediaStreamTrack | null): Promise<void> {
    this.tracks[slot] = track
    return this.enqueue(async () => {
      const transceiver = this.peer.getTransceivers()[CHAT_CALL_SLOTS.indexOf(slot)]
      if (transceiver) {
        await transceiver.sender.replaceTrack(this.tracks[slot] ?? null)
      }
    })
  }

  /** 关掉连接；之后到的信令不再处理 */
  close(): void {
    if (this.closed) {
      return
    }
    this.closed = true
    this.peer.onicecandidate = null
    this.peer.ontrack = null
    this.peer.onconnectionstatechange = null
    this.peer.close()
  }

  /** 应答的一方：对端的 offer 带出了三个位，各位改成双向并挂上本端的轨道 */
  private async occupySlots(): Promise<void> {
    const transceivers = this.peer.getTransceivers()
    for (const [index, slot] of CHAT_CALL_SLOTS.entries()) {
      const transceiver = transceivers[index]
      if (transceiver) {
        transceiver.direction = 'sendrecv'
        await transceiver.sender.replaceTrack(this.tracks[slot] ?? null)
      }
    }
  }

  private async describeRemote(description: RTCSessionDescriptionInit): Promise<void> {
    await this.peer.setRemoteDescription(description)
    this.remoteDescribed = true
    for (const candidate of this.pendingCandidates.splice(0)) {
      await this.addCandidate(candidate)
    }
  }

  /** 一条候选加不上不算连接失败：别的候选照样能连，连不上的由连接状态说 */
  private async addCandidate(candidate: RTCIceCandidateInit): Promise<void> {
    try {
      await this.peer.addIceCandidate(candidate)
    }
    catch {
      // 忽略这一条
    }
  }

  /** 排到前一条之后做；关了之后的不做。一条失败不拦后面的，失败原样交给调用方 */
  private enqueue(step: () => Promise<void>): Promise<void> {
    const run = this.queue.then(async () => {
      if (!this.closed) {
        await step()
      }
    })
    this.queue = run.catch(() => {})
    return run
  }
}
