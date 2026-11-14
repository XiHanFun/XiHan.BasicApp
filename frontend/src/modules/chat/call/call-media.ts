/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { ChatCallMedia } from '~/contracts'

/**
 * 取不到通话设备的原因
 * denied：没给权限，或页面不在安全上下文里（浏览器只在 HTTPS 与 localhost 下给设备）
 * missing：没有这种设备；busy：设备被别的程序占着；unsupported：浏览器不支持
 */
export type ChatCallMediaFailure = 'busy' | 'denied' | 'missing' | 'unsupported'

/** 取通话设备失败 */
export class ChatCallMediaError extends Error {
  readonly failure: ChatCallMediaFailure
  readonly media: ChatCallMedia

  constructor(failure: ChatCallMediaFailure, media: ChatCallMedia, cause?: unknown) {
    super(`Call media is unavailable (${failure}).`, { cause })
    this.name = 'ChatCallMediaError'
    this.failure = failure
    this.media = media
  }
}

/** 可选的通话设备 */
export interface ChatCallDevices {
  microphones: MediaDeviceInfo[]
  cameras: MediaDeviceInfo[]
  /** 扬声器；浏览器不支持选输出设备时为空 */
  speakers: MediaDeviceInfo[]
}

function failureOf(error: unknown): ChatCallMediaFailure {
  const name = error instanceof Error
    ? error.name
    : ''
  switch (name) {
    case 'NotFoundError':
    case 'OverconstrainedError':
      return 'missing'
    case 'NotReadableError':
    case 'AbortError':
      return 'busy'
    default:
      // NotAllowedError、SecurityError 以及别的拒绝都按没给权限说
      return 'denied'
  }
}

function mediaDevices(media: ChatCallMedia): MediaDevices {
  const devices = globalThis.navigator?.mediaDevices
  if (!devices?.getUserMedia) {
    throw new ChatCallMediaError('unsupported', media)
  }
  return devices
}

/**
 * 取通话要的本端媒体：语音只要麦克风，视频另要摄像头；屏幕共享也只要麦克风，屏幕另取（acquireScreen）
 * 设备取不全即失败：视频通话没有摄像头不悄悄降成语音
 * @throws ChatCallMediaError 带原因
 */
export async function acquireCallMedia(media: ChatCallMedia): Promise<MediaStream> {
  const devices = mediaDevices(media)
  try {
    return await devices.getUserMedia({ audio: true, video: media === 'Video' })
  }
  catch (error) {
    throw new ChatCallMediaError(failureOf(error), media, error)
  }
}

/**
 * 按设备取一条轨道（通话中换麦克风或摄像头）
 * @throws ChatCallMediaError 带原因
 */
export async function acquireCallTrack(kind: 'audioinput' | 'videoinput', deviceId: string): Promise<MediaStreamTrack> {
  const media: ChatCallMedia = kind === 'videoinput'
    ? 'Video'
    : 'Voice'
  const devices = mediaDevices(media)
  try {
    const stream = await devices.getUserMedia(kind === 'videoinput'
      ? {
          video: {
            deviceId: { exact: deviceId },
          },
        }
      : {
          audio: {
            deviceId: { exact: deviceId },
          },
        })
    const [track] = stream.getTracks()
    if (!track) {
      throw new ChatCallMediaError('missing', media)
    }
    return track
  }
  catch (error) {
    throw error instanceof ChatCallMediaError
      ? error
      : new ChatCallMediaError(failureOf(error), media, error)
  }
}

/** 这个浏览器能不能共享屏幕（手机浏览器多半不能） */
export function canShareScreen(): boolean {
  return typeof globalThis.navigator?.mediaDevices?.getDisplayMedia === 'function'
}

/**
 * 取要共享的屏幕：浏览器弹出选择框，由用户挑整个屏幕、一个窗口或一个标签页；只要画面，不带系统声音
 * @returns 屏幕的画面；用户在选择框里取消了为 null
 * @throws ChatCallMediaError 浏览器不支持，或取不到（被系统拦下等）
 */
export async function acquireScreen(): Promise<MediaStream | null> {
  if (!canShareScreen()) {
    throw new ChatCallMediaError('unsupported', 'Video')
  }
  try {
    const stream = await globalThis.navigator.mediaDevices.getDisplayMedia({ video: true, audio: false })
    for (const track of stream.getVideoTracks()) {
      // 屏幕上多是文字与界面：告诉编码器保清晰，不保帧率
      track.contentHint = 'detail'
    }
    return stream
  }
  catch (error) {
    // 取消选择框与没给权限是同一个错误，分不出来：都当作用户不想共享
    if (error instanceof Error && error.name === 'NotAllowedError') {
      return null
    }
    throw new ChatCallMediaError(failureOf(error), 'Video', error)
  }
}

/** 停掉一路媒体的全部轨道（麦克风与摄像头的占用灯随之熄灭） */
export function stopStream(stream: MediaStream | null | undefined): void {
  for (const track of stream?.getTracks() ?? []) {
    track.stop()
  }
}

/** 列出可选的通话设备（设备名要在取过一次媒体之后才有） */
export async function listCallDevices(): Promise<ChatCallDevices> {
  const all = await globalThis.navigator?.mediaDevices?.enumerateDevices?.() ?? []
  const canPickSpeaker = typeof HTMLMediaElement !== 'undefined' && 'setSinkId' in HTMLMediaElement.prototype
  return {
    microphones: all.filter(device => device.kind === 'audioinput'),
    cameras: all.filter(device => device.kind === 'videoinput'),
    speakers: canPickSpeaker
      ? all.filter(device => device.kind === 'audiooutput')
      : [],
  }
}
