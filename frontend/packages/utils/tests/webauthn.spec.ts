/**
 * 通行密钥的浏览器仪式
 *
 * 选项里的 base64url 换成字节交给 navigator.credentials，浏览器交回的字节再编回 base64url（不带填充）；
 * 取消与超时（NotAllowedError / AbortError）认作本人取消，其余错误照常抛出
 */
import { afterEach, describe, expect, it, vi } from 'vitest'
import { base64UrlToBytes, bytesToBase64Url, createPasskey, getPasskey, isPasskeyCancelled, passkeysSupported } from '~/utils/webauthn'

const bytes = (...values: number[]) => new Uint8Array(values)

function stubCredentials(credentials: Partial<CredentialsContainer>) {
  vi.stubGlobal('navigator', { ...navigator, credentials })
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('base64url', () => {
  it('不带填充、用 - 与 _ 替换 + 与 /，来回不丢字节', () => {
    const raw = bytes(0xFB, 0xFF, 0xBF, 0x00, 0x01)
    const encoded = bytesToBase64Url(raw)
    expect(encoded).toBe('-_-_AAE')
    expect([
      ...base64UrlToBytes(encoded),
    ]).toEqual([...raw])
  })

  it('认得 ArrayBuffer 与带偏移的视图', () => {
    const buffer = bytes(9, 1, 2, 3).buffer
    expect(bytesToBase64Url(buffer)).toBe('CQECAw')
    expect(bytesToBase64Url(new Uint8Array(buffer, 1, 2))).toBe('AQI')
  })
})

describe('取消', () => {
  it('notAllowedError 与 AbortError 认作取消，其余不是', () => {
    expect(isPasskeyCancelled(new DOMException('cancelled', 'NotAllowedError'))).toBe(true)
    expect(isPasskeyCancelled(new DOMException('aborted', 'AbortError'))).toBe(true)
    expect(isPasskeyCancelled(new DOMException('bad', 'SecurityError'))).toBe(false)
    expect(isPasskeyCancelled(new Error('NotAllowedError'))).toBe(false)
  })

  it('jsdom 没有 PublicKeyCredential：不支持', () => {
    expect(passkeysSupported()).toBe(false)
  })
})

describe('仪式', () => {
  it('登记：选项换成字节，凭据编回 JSON 并带上传输方式', async () => {
    const create = vi.fn().mockResolvedValue({
      id: 'AQI',
      rawId: bytes(1, 2).buffer,
      type: 'public-key',
      response: {
        clientDataJSON: bytes(3).buffer,
        attestationObject: bytes(4, 5).buffer,
        getTransports: () => ['internal', 'hybrid'],
      },
    })
    stubCredentials({ create })

    const credential = await createPasskey({
      rp: { id: 'localhost', name: 'XiHan BasicApp' },
      user: { id: 'AAE', name: 'ann', displayName: 'Ann' },
      challenge: 'CQECAw',
      pubKeyCredParams: [
        { type: 'public-key', alg: -7 },
      ],
      timeout: 300000,
      excludeCredentials: [
        {
          type: 'public-key',
          id: 'AQI',
          transports: ['internal'],
        },
      ],
      authenticatorSelection: { residentKey: 'required', requireResidentKey: true, userVerification: 'required' },
      attestation: 'none',
    })

    const publicKey = create.mock.calls[0]![0].publicKey as PublicKeyCredentialCreationOptions
    expect([...(publicKey.challenge as Uint8Array)]).toEqual([9, 1, 2, 3])
    expect([...(publicKey.user.id as Uint8Array)]).toEqual([0, 1])
    expect(publicKey.excludeCredentials).toEqual([
      {
        type: 'public-key',
        id: bytes(1, 2),
        transports: ['internal'],
      },
    ])
    expect(credential).toEqual({
      id: 'AQI',
      rawId: 'AQI',
      type: 'public-key',
      response: {
        clientDataJSON: 'Aw',
        attestationObject: 'BAU',
        transports: ['internal', 'hybrid'],
      },
    })
  })

  it('登录：不列允许的凭据，断言编回 JSON，没有用户句柄即 null', async () => {
    const get = vi.fn().mockResolvedValue({
      id: 'AQI',
      rawId: bytes(1, 2).buffer,
      type: 'public-key',
      response: {
        clientDataJSON: bytes(3).buffer,
        authenticatorData: bytes(6).buffer,
        signature: bytes(7, 8).buffer,
        userHandle: null,
      },
    })
    stubCredentials({ get })

    const assertion = await getPasskey({
      challenge: 'CQECAw',
      timeout: 300000,
      rpId: 'localhost',
      allowCredentials: [],
      userVerification: 'required',
    })

    const publicKey = get.mock.calls[0]![0].publicKey as PublicKeyCredentialRequestOptions
    expect(publicKey.allowCredentials).toEqual([])
    expect(publicKey.rpId).toBe('localhost')
    expect(assertion.response).toEqual({
      clientDataJSON: 'Aw',
      authenticatorData: 'Bg',
      signature: 'Bwg',
      userHandle: null,
    })
  })

  it('浏览器什么都没给：按取消抛出', async () => {
    stubCredentials({
      get: vi.fn().mockResolvedValue(null),
    })
    const failed = await getPasskey({
      challenge: 'AA',
      timeout: 1,
      rpId: 'localhost',
      allowCredentials: [],
      userVerification: 'required',
    }).catch((error: unknown) => error)
    expect(isPasskeyCancelled(failed)).toBe(true)
  })
})
