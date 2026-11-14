/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { ChatCallSignal } from './call-signal'

/** 一场通话的媒体连接要的东西 */
export interface ChatCallSessionOptions {
  /** 发起方出 offer，接听方等 offer 再回 answer */
  initiator: boolean
  /** 穿透用的 STUN 与 TURN；没配为空，只有能直连的两端打得通 */
  iceServers: RTCIceServer[]
  /** 本端的麦克风（与摄像头） */
  localStream: MediaStream
  /** 把信令交给对端（经聊天 Hub 转发） */
  send: (signal: ChatCallSignal) => void
  /** 对端的媒体到了 */
  onRemoteStream: (stream: MediaStream) => void
  /** 连接状态变了 */
  onStateChange: (state: RTCPeerConnectionState) => void
  /** 建连接的方法；测试注入替身，缺省用浏览器的 RTCPeerConnection */
  createPeer?: (configuration: RTCConfiguration) => RTCPeerConnection
}

/**
 * 一场一对一通话的媒体连接（浏览器原生 WebRTC）
 * 只管两端之间的连接：会话描述的一来一回、ICE 候选的交换与本端轨道的替换；通话的状态（谁叫了谁、接没接）不归它管
 * 信令一条一条顺序处理：对端的候选可能比会话描述先到，先存着，等远端描述设好再加
 */
export class ChatCallSession {
  private readonly peer: RTCPeerConnection
  private readonly options: ChatCallSessionOptions
  private readonly pendingCandidates: RTCIceCandidateInit[] = []
  private remoteDescribed = false
  private closed = false
  private queue: Promise<void> = Promise.resolve()

  constructor(options: ChatCallSessionOptions) {
    this.options = options
    const createPeer = options.createPeer ?? (configuration => new RTCPeerConnection(configuration))
    this.peer = createPeer({ iceServers: options.iceServers })
    for (const track of options.localStream.getTracks()) {
      this.peer.addTrack(track, options.localStream)
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
      if (!this.closed) {
        options.onRemoteStream(event.streams[0] ?? new MediaStream([event.track]))
      }
    }
    this.peer.onconnectionstatechange = () => {
      if (!this.closed) {
        options.onStateChange(this.peer.connectionState)
      }
    }
  }

  /** 发起方开始协商：出 offer 交给对端 */
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
            await this.peer.addIceCandidate(signal.candidate)
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
   * 换一条本端轨道（换麦克风或摄像头）：发送端原地替换，不重新协商
   * @returns 换上了为真；连接里没有这一类轨道的发送端为假
   */
  async replaceTrack(track: MediaStreamTrack): Promise<boolean> {
    const sender = this.peer.getSenders().find(candidate => candidate.track?.kind === track.kind)
    if (!sender || this.closed) {
      return false
    }
    await sender.replaceTrack(track)
    return true
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

  private async describeRemote(description: RTCSessionDescriptionInit): Promise<void> {
    await this.peer.setRemoteDescription(description)
    this.remoteDescribed = true
    for (const candidate of this.pendingCandidates.splice(0)) {
      await this.peer.addIceCandidate(candidate)
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
