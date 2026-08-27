/**
 * 接口安全响应解密单元测试。
 *
 * 职责边界：锁定 `tryDecryptSecureResponse` 的全部短路条件（开关、密钥、WebCrypto、
 * X-Secure-Response 标记、信封形态）、算法与 IV 的取值优先级、解密结果的 JSON 解析，
 * 以及内容签名校验失败时必须抛错而不是放行。出站签名在 security-request.test.ts。
 */
import type { AxiosResponse, InternalAxiosRequestConfig } from 'axios'
import type { ApiSecurityRuntimeConfig } from './security'
import { AxiosHeaders } from 'axios'
import { expect, it, vi } from 'vitest'
import { tryDecryptSecureResponse } from './security'

const encoder = new TextEncoder()
const SECRET = 'response-secret-1'

it('安全开关关闭时不碰响应体', async () => {
  const envelope = await encryptEnvelope('{"id":1}', SECRET)
  const response = makeResponse(envelope, { 'X-Secure-Response': '1' })

  await tryDecryptSecureResponse(response, runtime({ enabled: false }))

  expect(response.data).toBe(envelope)
})

it('未配置签名密钥时不碰响应体', async () => {
  const envelope = await encryptEnvelope('{"id":1}', SECRET)
  const response = makeResponse(envelope, { 'X-Secure-Response': '1' })

  await tryDecryptSecureResponse(response, runtime({ secretKey: '' }))

  expect(response.data).toBe(envelope)
})

it('运行环境没有 WebCrypto subtle 时不碰响应体也不抛错', async () => {
  const envelope = await encryptEnvelope('{"id":1}', SECRET)
  const response = makeResponse(envelope, { 'X-Secure-Response': '1' })
  vi.stubGlobal('crypto', { getRandomValues: (bytes: Uint8Array) => bytes })

  await tryDecryptSecureResponse(response, runtime())

  expect(response.data).toBe(envelope)
})

it('没有 X-Secure-Response 标记时按明文响应处理', async () => {
  const envelope = await encryptEnvelope('{"id":1}', SECRET)
  const response = makeResponse(envelope, {})

  await tryDecryptSecureResponse(response, runtime())

  expect(response.data).toBe(envelope)
})

it('加密标记头按 1/true/yes 判真，其余取值一律按未加密处理', async () => {
  const decrypted: Array<[string, boolean]> = []
  for (const flag of ['1', 'true', 'YES', '0', 'false', 'no', '', 'perhaps']) {
    const envelope = await encryptEnvelope('{"id":1}', SECRET)
    const response = makeResponse(envelope, { 'X-Secure-Response': flag })
    await tryDecryptSecureResponse(response, runtime())
    decrypted.push([flag, response.data !== envelope])
  }

  expect(decrypted).toEqual([
    ['1', true],
    ['true', true],
    ['YES', true],
    ['0', false],
    ['false', false],
    ['no', false],
    ['', false],
    ['perhaps', false],
  ])
})

it('响应头以普通对象小写键承载时同样能读到标记', async () => {
  const envelope = await encryptEnvelope('{"id":7}', SECRET)
  const response = makeResponse(envelope, { 'x-secure-response': '1' })

  await tryDecryptSecureResponse(response, runtime())

  expect(response.data).toEqual({ id: 7 })
})

it('响应头值为数组时取第一项', async () => {
  const envelope = await encryptEnvelope('{"id":8}', SECRET)
  const response = makeResponse(envelope, { 'X-Secure-Response': ['1', '0'] })

  await tryDecryptSecureResponse(response, runtime())

  expect(response.data).toEqual({ id: 8 })
})

it('响应头值为空数组时按无标记处理', async () => {
  const envelope = await encryptEnvelope('{"id":9}', SECRET)
  const response = makeResponse(envelope, { 'X-Secure-Response': [] })

  await tryDecryptSecureResponse(response, runtime())

  expect(response.data).toBe(envelope)
})

it('响应头整体缺失时按无标记处理', async () => {
  const envelope = await encryptEnvelope('{"id":10}', SECRET)
  const response = makeResponse(envelope, null)

  await tryDecryptSecureResponse(response, runtime())

  expect(response.data).toBe(envelope)
})

it('响应体不是信封形态时原样保留', async () => {
  const plainString = makeResponse('明文响应', { 'X-Secure-Response': '1' })
  const nullBody = makeResponse(null, { 'X-Secure-Response': '1' })
  const noData = makeResponse({ alg: 'AES-CBC', iv: 'x' }, { 'X-Secure-Response': '1' })
  const emptyData = makeResponse({ alg: 'AES-CBC', data: '', iv: 'x' }, { 'X-Secure-Response': '1' })

  for (const response of [plainString, nullBody, noData, emptyData]) {
    await tryDecryptSecureResponse(response, runtime())
  }

  expect(plainString.data).toBe('明文响应')
  expect(nullBody.data).toBeNull()
  expect(noData.data).toEqual({ alg: 'AES-CBC', iv: 'x' })
  expect(emptyData.data).toEqual({ alg: 'AES-CBC', data: '', iv: 'x' })
})

it('信封声明算法为 NONE 时不解密，直接保留原信封', async () => {
  const response = makeResponse({ alg: 'none', data: 'ZmFrZQ==' }, { 'X-Secure-Response': '1' })

  await tryDecryptSecureResponse(response, runtime())

  expect(response.data).toEqual({ alg: 'none', data: 'ZmFrZQ==' })
})

it('运行配置的加密算法为 NONE 且信封未声明算法时同样不解密', async () => {
  const response = makeResponse({ data: 'ZmFrZQ==' }, { 'X-Secure-Response': '1' })

  await tryDecryptSecureResponse(response, runtime({ encryptAlgorithm: 'NONE' }))

  expect(response.data).toEqual({ data: 'ZmFrZQ==' })
})

it('信封声明了不支持的加密算法时抛错而不是静默放行', async () => {
  const response = makeResponse({ alg: 'DES', data: 'ZmFrZQ==' }, { 'X-Secure-Response': '1' })

  await expect(tryDecryptSecureResponse(response, runtime()))
    .rejects
    .toThrow(/不支持的响应加密算法/)
})

it('信封未声明算法时从 X-Encrypt-Algorithm 响应头取算法', async () => {
  const response = makeResponse({ data: 'ZmFrZQ==' }, {
    'X-Secure-Response': '1',
    'X-Encrypt-Algorithm': 'DES',
  })

  await expect(tryDecryptSecureResponse(response, runtime()))
    .rejects
    .toThrow(/不支持的响应加密算法/)
})

it('缺少解密 IV 时抛错', async () => {
  const response = makeResponse({ alg: 'AES-CBC', data: 'ZmFrZQ==' }, { 'X-Secure-Response': '1' })

  await expect(tryDecryptSecureResponse(response, runtime()))
    .rejects
    .toThrow(/缺少响应解密 IV/)
})

it('信封没带 IV 时回落到 X-Encrypt-Iv 响应头', async () => {
  const envelope = await encryptEnvelope('{"from":"header-iv"}', SECRET)
  const response = makeResponse({ alg: 'AES-CBC', data: envelope.data }, {
    'X-Secure-Response': '1',
    'X-Encrypt-Iv': envelope.iv,
  })

  await tryDecryptSecureResponse(response, runtime())

  expect(response.data).toEqual({ from: 'header-iv' })
})

it('解密向量不是 16 字节时抛出明确的长度错误', async () => {
  const envelope = await encryptEnvelope('{"id":1}', SECRET)
  const response = makeResponse({ ...envelope, iv: btoa('short-iv') }, { 'X-Secure-Response': '1' })

  await expect(tryDecryptSecureResponse(response, runtime()))
    .rejects
    .toThrow(/IV 长度必须为 16 字节/)
})

it('解密成功后把响应体替换为解析出的 JSON 对象', async () => {
  const payload = { code: 200, isSuccess: true, data: { name: '曦寒🚀', list: [1, 2] } }
  const envelope = await encryptEnvelope(JSON.stringify(payload), SECRET)
  const response = makeResponse(envelope, { 'X-Secure-Response': '1' })

  await tryDecryptSecureResponse(response, runtime())

  expect(response.data).toEqual(payload)
})

it('解密结果不是合法 JSON 时保留为原始字符串', async () => {
  const envelope = await encryptEnvelope('不是 JSON 的纯文本', SECRET)
  const response = makeResponse(envelope, { 'X-Secure-Response': '1' })

  await tryDecryptSecureResponse(response, runtime())

  expect(response.data).toBe('不是 JSON 的纯文本')
})

it('优先使用独立的加密密钥解密', async () => {
  const envelope = await encryptEnvelope('{"by":"encrypt-key"}', 'dedicated-encrypt-key')
  const response = makeResponse(envelope, { 'X-Secure-Response': '1' })

  await tryDecryptSecureResponse(response, runtime({ encryptKey: 'dedicated-encrypt-key' }))

  expect(response.data).toEqual({ by: 'encrypt-key' })
})

it('未配置独立加密密钥时用签名密钥解密', async () => {
  const envelope = await encryptEnvelope('{"by":"secret-key"}', SECRET)
  const response = makeResponse(envelope, { 'X-Secure-Response': '1' })

  await tryDecryptSecureResponse(response, runtime({ encryptKey: '' }))

  expect(response.data).toEqual({ by: 'secret-key' })
})

it('密钥种子正好 32 字节时按原始字节使用而不是再做一次摘要', async () => {
  const seed = 'abcdefghijklmnopqrstuvwxyz012345'
  const envelope = await encryptEnvelope('{"raw":true}', seed)
  const response = makeResponse(envelope, { 'X-Secure-Response': '1' })

  await tryDecryptSecureResponse(response, runtime({ encryptKey: seed }))

  expect(response.data).toEqual({ raw: true })
})

it('内容签名一致时放行（大小写不敏感比较）', async () => {
  const plaintext = '{"verified":true}'
  const envelope = await encryptEnvelope(plaintext, SECRET)
  const contentSign = (await digestHex('SHA-256', plaintext)).toUpperCase()
  const response = makeResponse({ ...envelope, contentSign }, { 'X-Secure-Response': '1' })

  await tryDecryptSecureResponse(response, runtime())

  expect(response.data).toEqual({ verified: true })
})

it('内容签名不一致时抛错且不写回被篡改的响应体', async () => {
  const envelope = await encryptEnvelope('{"verified":true}', SECRET)
  const tampered = { ...envelope, contentSign: '0'.repeat(64) }
  const response = makeResponse(tampered, { 'X-Secure-Response': '1' })

  await expect(tryDecryptSecureResponse(response, runtime()))
    .rejects
    .toThrow(/响应内容签名校验失败/)
  expect(response.data).toBe(tampered)
})

it('内容签名算法声明 SHA512 时按 SHA-512 校验', async () => {
  const plaintext = '{"algo":"sha512"}'
  const envelope = await encryptEnvelope(plaintext, SECRET)
  const response = makeResponse(
    { ...envelope, contentSign: await digestHex('SHA-512', plaintext) },
    { 'X-Secure-Response': '1' },
  )

  await tryDecryptSecureResponse(response, runtime({ contentSignAlgorithm: 'SHA512' }))

  expect(response.data).toEqual({ algo: 'sha512' })
})

it('内容签名算法声明 SHA512 但签名按 SHA-256 计算时判定失败', async () => {
  const plaintext = '{"algo":"mismatch"}'
  const envelope = await encryptEnvelope(plaintext, SECRET)
  const response = makeResponse(
    { ...envelope, contentSign: await digestHex('SHA-256', plaintext) },
    { 'X-Secure-Response': '1' },
  )

  await expect(tryDecryptSecureResponse(response, runtime({ contentSignAlgorithm: 'SHA512' })))
    .rejects
    .toThrow(/响应内容签名校验失败/)
})

it('由 AxiosHeaders 承载的响应头同样能读出标记与向量', async () => {
  const envelope = await encryptEnvelope('{"carrier":"axios-headers"}', SECRET)
  const response = makeResponse({ alg: 'AES-CBC', data: envelope.data }, new AxiosHeaders({
    'X-Secure-Response': '1',
    'X-Encrypt-Iv': envelope.iv,
  }))

  await tryDecryptSecureResponse(response, runtime())

  expect(response.data).toEqual({ carrier: 'axios-headers' })
})

/** 构造一份最小可用的运行时安全配置。 */
function runtime(overrides: Partial<ApiSecurityRuntimeConfig> = {}): ApiSecurityRuntimeConfig {
  return {
    enabled: true,
    accessKey: 'ak-1',
    secretKey: SECRET,
    encryptKey: '',
    signAlgorithm: 'HMACSHA256',
    contentSignAlgorithm: 'SHA256',
    encryptAlgorithm: 'AES-CBC',
    encryptResponse: true,
    ...overrides,
  }
}

/** 构造一份最小可用的 axios 响应。 */
function makeResponse(data: unknown, headers: unknown): AxiosResponse<unknown> {
  return {
    config: { headers: new AxiosHeaders() } as InternalAxiosRequestConfig,
    data,
    headers: headers as AxiosResponse['headers'],
    status: 200,
    statusText: 'OK',
  }
}

/** 用与源码一致的密钥派生规则加密出一份响应信封。 */
async function encryptEnvelope(plaintext: string, keySeed: string): Promise<{ alg: string, data: string, iv: string }> {
  const keyBytes = await deriveKeyBytes(keySeed)
  const ivBytes = new Uint8Array(16)
  crypto.getRandomValues(ivBytes)
  const key = await crypto.subtle.importKey('raw', toBuffer(keyBytes), { name: 'AES-CBC' }, false, ['encrypt'])
  const cipher = await crypto.subtle.encrypt(
    { name: 'AES-CBC', iv: toBuffer(ivBytes) },
    key,
    toBuffer(encoder.encode(plaintext)),
  )
  return { alg: 'AES-CBC', data: bytesToBase64(new Uint8Array(cipher)), iv: bytesToBase64(ivBytes) }
}

/** 按源码同样的规则派生 AES 密钥字节。 */
async function deriveKeyBytes(seed: string): Promise<Uint8Array> {
  const raw = encoder.encode(seed)
  if (raw.length === 16 || raw.length === 24 || raw.length === 32) {
    return raw
  }
  return new Uint8Array(await crypto.subtle.digest('SHA-256', toBuffer(raw)))
}

/** 计算指定摘要算法的十六进制结果。 */
async function digestHex(algorithm: string, text: string): Promise<string> {
  const digest = await crypto.subtle.digest(algorithm, toBuffer(encoder.encode(text)))
  return Array.from(new Uint8Array(digest)).map(byte => byte.toString(16).padStart(2, '0')).join('')
}

/** 字节数组转 base64。 */
function bytesToBase64(bytes: Uint8Array): string {
  let binary = ''
  for (const byte of bytes) {
    binary += String.fromCharCode(byte)
  }
  return btoa(binary)
}

/** 取出独立的 ArrayBuffer，避免 TypedArray 视图偏移影响 WebCrypto。 */
function toBuffer(bytes: Uint8Array): ArrayBuffer {
  return bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength) as ArrayBuffer
}
