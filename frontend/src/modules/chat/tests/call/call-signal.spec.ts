/**
 * call-signal.ts：两端经 Hub 互相转发的通话信令
 *
 * 职责边界：编出去的解得回来；对端发来的不是 JSON、形状不对或种类不认识的一律不认（返回 null），
 * 不抛错也不猜——服务端只转不解析，把关在这里
 */
import type { ChatCallSignal } from '@/modules/chat/call/call-signal'
import { describe, expect, it } from 'vitest'
import { decodeCallSignal, encodeCallSignal } from '@/modules/chat/call/call-signal'

describe('编与解', () => {
  it.each<ChatCallSignal>([
    { type: 'offer', sdp: 'v=0\r\no=- 1 2 IN IP4 127.0.0.1' },
    { type: 'answer', sdp: 'v=0' },
    {
      type: 'candidate',
      candidate: { candidate: 'candidate:1 1 udp 1 10.0.0.1 5000 typ host', sdpMid: '0', sdpMLineIndex: 0 },
    },
    { type: 'state', micOn: false, cameraOn: true },
  ])('$type 编成字符串再解开还是原样', (signal) => {
    expect(decodeCallSignal(encodeCallSignal(signal))).toStrictEqual(signal)
  })

  it('多出来的字段不带进结果：只取这一种信令该有的', () => {
    expect(decodeCallSignal('{"type":"state","micOn":true,"cameraOn":false,"extra":1}')).toStrictEqual({
      type: 'state',
      micOn: true,
      cameraOn: false,
    })
  })
})

describe('不认的信令', () => {
  it.each([
    ['不是 JSON', 'early'],
    ['JSON 但不是对象', '"offer"'],
    ['null', 'null'],
    ['种类不认识', '{"type":"renegotiate","sdp":"v=0"}'],
    ['没有种类', '{"sdp":"v=0"}'],
    ['offer 没有 sdp', '{"type":"offer"}'],
    ['answer 的 sdp 是空串', '{"type":"answer","sdp":""}'],
    ['candidate 不是对象', '{"type":"candidate","candidate":"candidate:1"}'],
    ['candidate 是 null', '{"type":"candidate","candidate":null}'],
    ['state 的开关不是布尔', '{"type":"state","micOn":"yes","cameraOn":true}'],
    ['state 少一个开关', '{"type":"state","micOn":true}'],
  ])('%s：返回 null', (_, data) => {
    expect(decodeCallSignal(data)).toBeNull()
  })
})
