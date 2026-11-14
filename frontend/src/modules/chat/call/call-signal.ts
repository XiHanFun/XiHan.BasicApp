/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * 通话信令：两端经聊天 Hub 互相转发的消息，服务端只转不解析
 * offer 与 answer 是会话描述，candidate 是 ICE 候选，这三种只在一对人之间走（多人通话里每两个人各自协商）
 * state 是麦克风、摄像头与屏幕共享的开关，发给通话里的所有人（对端据此显示静音、头像与共享的画面）
 */
export type ChatCallSignal
  = | { type: 'answer', sdp: string }
    | { type: 'candidate', candidate: RTCIceCandidateInit }
    | { type: 'offer', sdp: string }
    | { type: 'state', micOn: boolean, cameraOn: boolean, screenOn: boolean }

/** 编成 Hub 上传的字符串 */
export function encodeCallSignal(signal: ChatCallSignal): string {
  return JSON.stringify(signal)
}

/**
 * 解开对端转来的信令
 * @returns 认得的信令；不是 JSON、形状不对或种类不认识的为 null（对端可能是别的版本，不认识的不处理）
 */
export function decodeCallSignal(data: string): ChatCallSignal | null {
  let parsed: unknown
  try {
    parsed = JSON.parse(data)
  }
  catch {
    return null
  }
  if (typeof parsed !== 'object' || parsed === null) {
    return null
  }
  const signal = parsed as Record<string, unknown>
  switch (signal.type) {
    case 'offer':
    case 'answer':
      return typeof signal.sdp === 'string' && signal.sdp.length > 0
        ? { type: signal.type, sdp: signal.sdp }
        : null
    case 'candidate':
      return typeof signal.candidate === 'object' && signal.candidate !== null
        ? { type: 'candidate', candidate: signal.candidate as RTCIceCandidateInit }
        : null
    case 'state':
      return typeof signal.micOn === 'boolean' && typeof signal.cameraOn === 'boolean' && typeof signal.screenOn === 'boolean'
        ? {
            type: 'state',
            micOn: signal.micOn,
            cameraOn: signal.cameraOn,
            screenOn: signal.screenOn,
          }
        : null
    default:
      return null
  }
}
