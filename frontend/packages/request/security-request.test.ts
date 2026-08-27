/**
 * 接口安全出站签名单元测试。
 *
 * 职责边界：锁定 `resolveApiSecurityRuntimeConfig` 的环境变量解析规则，
 * 以及 `applyApiSecurityToRequest` 的短路条件、随机 nonce、时间戳、
 * 规范化请求串（方法/路径/排序后的查询串/内容签名/时间戳/nonce）、
 * HMAC 签名算法选择、请求体 AES 加密与密钥派生。
 * 响应解密在 security-response.test.ts，拦截器接线在 interceptor.test.ts。
 */
import type { InternalAxiosRequestConfig } from 'axios'
import type { ApiSecurityRuntimeConfig } from './security'
import { AxiosHeaders } from 'axios'
import { afterEach, expect, it, vi } from 'vitest'
import { applyApiSecurityToRequest, resolveApiSecurityRuntimeConfig } from './security'

/** 空串的 SHA-256，用于校验无请求体时的内容签名 */
const SHA256_OF_EMPTY = 'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855'
const encoder = new TextEncoder()

afterEach(() => {
  vi.useRealTimers()
})

it('未配置任何环境变量时运行配置回落到关闭 + 默认算法', () => {
  const config = resolveApiSecurityRuntimeConfig()

  expect(config).toEqual({
    enabled: false,
    accessKey: '',
    secretKey: '',
    encryptKey: '',
    signAlgorithm: 'HMACSHA256',
    contentSignAlgorithm: 'SHA256',
    encryptAlgorithm: 'AES-CBC',
    encryptResponse: true,
  })
})

it('开关环境变量按 1/true/yes 与 0/false/no 双向解析，无法识别时取默认值', () => {
  const parsed: boolean[] = []
  for (const raw of ['1', 'true', 'TRUE', ' yes ', '0', 'false', 'no', 'maybe']) {
    vi.stubEnv('VITE_API_SECURITY_ENABLED', raw)
    parsed.push(resolveApiSecurityRuntimeConfig().enabled)
  }

  expect(parsed).toEqual([true, true, true, true, false, false, false, false])
})

it('响应加密开关无法识别时取默认值 true 而不是 false', () => {
  vi.stubEnv('VITE_API_SECURITY_ENCRYPT_RESPONSE', '随便写点什么')

  expect(resolveApiSecurityRuntimeConfig().encryptResponse).toBe(true)
})

it('密钥两端空白被裁剪，算法名统一大写', () => {
  vi.stubEnv('VITE_API_SECURITY_ACCESS_KEY', '  basicapp-frontend  ')
  vi.stubEnv('VITE_API_SECURITY_SECRET_KEY', '\tsign-secret\n')
  vi.stubEnv('VITE_API_SECURITY_ENCRYPT_KEY', ' encrypt-secret ')
  vi.stubEnv('VITE_API_SECURITY_SIGN_ALGORITHM', 'hmacsha512')
  vi.stubEnv('VITE_API_SECURITY_CONTENT_SIGN_ALGORITHM', ' sha512 ')
  vi.stubEnv('VITE_API_SECURITY_ENCRYPT_ALGORITHM', 'aes-cbc')

  expect(resolveApiSecurityRuntimeConfig()).toMatchObject({
    accessKey: 'basicapp-frontend',
    secretKey: 'sign-secret',
    encryptKey: 'encrypt-secret',
    signAlgorithm: 'HMACSHA512',
    contentSignAlgorithm: 'SHA512',
    encryptAlgorithm: 'AES-CBC',
  })
})

it('算法环境变量为空白串时回落到默认算法而不是空算法', () => {
  vi.stubEnv('VITE_API_SECURITY_SIGN_ALGORITHM', '   ')
  vi.stubEnv('VITE_API_SECURITY_ENCRYPT_ALGORITHM', '')

  expect(resolveApiSecurityRuntimeConfig()).toMatchObject({
    signAlgorithm: 'HMACSHA256',
    encryptAlgorithm: 'AES-CBC',
  })
})

it('安全开关关闭时不写任何安全头', async () => {
  const config = makeConfig()

  await applyApiSecurityToRequest(config, '/api/Sys/Ping', runtime({ enabled: false }))

  expect(securityHeaderNames(config)).toEqual([])
})

it('缺 accessKey 或 secretKey 时不写任何安全头', async () => {
  const noAccess = makeConfig()
  const noSecret = makeConfig()

  await applyApiSecurityToRequest(noAccess, '/api/Sys/Ping', runtime({ accessKey: '' }))
  await applyApiSecurityToRequest(noSecret, '/api/Sys/Ping', runtime({ secretKey: '' }))

  expect(securityHeaderNames(noAccess)).toEqual([])
  expect(securityHeaderNames(noSecret)).toEqual([])
})

it('运行环境没有 WebCrypto subtle 时静默跳过签名而不是抛错', async () => {
  vi.stubGlobal('crypto', { getRandomValues: (bytes: Uint8Array) => bytes })
  const config = makeConfig()

  await applyApiSecurityToRequest(config, '/api/Sys/Ping', runtime())

  expect(securityHeaderNames(config)).toEqual([])
})

it('无请求体的 GET 写齐访问标识、时间戳、nonce、签名与算法声明', async () => {
  vi.useFakeTimers()
  vi.setSystemTime(new Date('2026-08-27T03:04:05.678Z'))
  const config = makeConfig()

  await applyApiSecurityToRequest(config, '/api/Sys/Ping', runtime())

  expect(headerOf(config, 'X-Access-Key')).toBe('ak-1')
  expect(headerOf(config, 'X-Timestamp')).toBe('1787799845')
  expect(headerOf(config, 'X-Sign-Algorithm')).toBe('HMACSHA256')
  expect(headerOf(config, 'X-Content-Sign-Algorithm')).toBe('SHA256')
  expect(headerOf(config, 'X-Encrypt-Algorithm')).toBe('NONE')
  expect(headerOf(config, 'X-Encrypt-Iv')).toBe('')
})

it('无请求体时内容签名固定为空串的 SHA-256', async () => {
  const config = makeConfig()

  await applyApiSecurityToRequest(config, '/api/Sys/Ping', runtime())

  expect(headerOf(config, 'X-Content-Sign')).toBe(SHA256_OF_EMPTY)
})

it('响应加密开关打开时写 X-Encrypt-Response，关闭时不写', async () => {
  const on = makeConfig()
  const off = makeConfig()

  await applyApiSecurityToRequest(on, '/api/Sys/Ping', runtime({ encryptResponse: true }))
  await applyApiSecurityToRequest(off, '/api/Sys/Ping', runtime({ encryptResponse: false }))

  expect(headerOf(on, 'X-Encrypt-Response')).toBe('1')
  expect(headerOf(off, 'X-Encrypt-Response')).toBe('')
})

it('nonce 是 16 字节随机数的十六进制串且每次请求都不同', async () => {
  const first = makeConfig()
  const second = makeConfig()

  await applyApiSecurityToRequest(first, '/api/Sys/Ping', runtime())
  await applyApiSecurityToRequest(second, '/api/Sys/Ping', runtime())

  expect(headerOf(first, 'X-Nonce')).toMatch(/^[0-9a-f]{32}$/)
  expect(headerOf(first, 'X-Nonce')).not.toBe(headerOf(second, 'X-Nonce'))
})

it('缺少 getRandomValues 时 nonce 退化为时间戳加随机后缀，仍然可用', async () => {
  vi.stubGlobal('crypto', { subtle: globalThis.crypto.subtle })
  const config = makeConfig()

  await applyApiSecurityToRequest(config, '/api/Sys/Ping', runtime())

  expect(headerOf(config, 'X-Nonce')).toMatch(/^\d+_[0-9a-z]+$/)
})

it('签名按「方法\\n路径\\n查询串\\n内容签名\\n时间戳\\nnonce」拼接后做 HMAC', async () => {
  const config = makeConfig({ method: 'get' })

  await applyApiSecurityToRequest(config, '/api/Sys/Ping', runtime())

  const canonical = [
    'GET',
    '/api/Sys/Ping',
    '',
    SHA256_OF_EMPTY,
    headerOf(config, 'X-Timestamp'),
    headerOf(config, 'X-Nonce'),
  ].join('\n')
  expect(headerOf(config, 'X-Signature')).toBe(await hmacHex('SHA-256', 'sk-1', canonical))
})

it('查询参数先按键再按值排序后逐项 URL 编码，中文键值不落原文', async () => {
  const config = makeConfig()

  await applyApiSecurityToRequest(config, '/api/Sys/Page?b=2&a=1&a=0&名称=曦寒', runtime())

  const canonical = [
    'GET',
    '/api/Sys/Page',
    'a=0&a=1&b=2&%E5%90%8D%E7%A7%B0=%E6%9B%A6%E5%AF%92',
    SHA256_OF_EMPTY,
    headerOf(config, 'X-Timestamp'),
    headerOf(config, 'X-Nonce'),
  ].join('\n')
  expect(headerOf(config, 'X-Signature')).toBe(await hmacHex('SHA-256', 'sk-1', canonical))
})

it('签名算法为 HMACSHA1 与 HMACSHA512 时切换对应摘要，未知算法退回 SHA-256', async () => {
  const results: Array<[string, string]> = []
  for (const [declared, hash] of [['HMACSHA1', 'SHA-1'], ['HMACSHA512', 'SHA-512'], ['HMACMD5', 'SHA-256']]) {
    const config = makeConfig()
    await applyApiSecurityToRequest(config, '/api/Sys/Ping', runtime({ signAlgorithm: declared! }))
    const canonical = [
      'GET',
      '/api/Sys/Ping',
      '',
      SHA256_OF_EMPTY,
      headerOf(config, 'X-Timestamp'),
      headerOf(config, 'X-Nonce'),
    ].join('\n')
    results.push([headerOf(config, 'X-Signature'), await hmacHex(hash!, 'sk-1', canonical)])
  }

  expect(results.map(([actual, expected]) => actual === expected)).toEqual([true, true, true])
  expect(results[0]?.[0]).toHaveLength(40)
  expect(results[1]?.[0]).toHaveLength(128)
  expect(results[2]?.[0]).toHaveLength(64)
})

it('内容签名算法声明 SHA512 时改用 SHA-512 摘要', async () => {
  const config = makeConfig({ data: { a: 1 }, method: 'POST' })

  await applyApiSecurityToRequest(config, '/api/Sys/Save', runtime({ contentSignAlgorithm: 'SHA512' }))

  expect(headerOf(config, 'X-Content-Sign')).toBe(await digestHex('SHA-512', '{"a":1}'))
})

it('带请求体的 POST 被 AES-CBC 加密成信封，且内容签名针对明文而不是密文', async () => {
  const config = makeConfig({ data: { orderNo: 'A-1', 备注: '中文🚀' }, method: 'POST' })
  const plaintext = JSON.stringify({ orderNo: 'A-1', 备注: '中文🚀' })

  await applyApiSecurityToRequest(config, '/api/Order/Create', runtime({ encryptAlgorithm: 'AES-CBC' }))

  const envelope = config.data as { alg: string, data: string, iv: string }
  expect(envelope.alg).toBe('AES-CBC')
  expect(headerOf(config, 'X-Encrypt-Iv')).toBe(envelope.iv)
  expect(headerOf(config, 'X-Content-Sign')).toBe(await digestHex('SHA-256', plaintext))
  expect(await aesDecryptWith(await deriveKeyBytes('encrypt-key-1'), envelope.data, envelope.iv)).toBe(plaintext)
})

it('每次加密使用新的 16 字节随机 IV', async () => {
  const first = makeConfig({ data: { a: 1 }, method: 'POST' })
  const second = makeConfig({ data: { a: 1 }, method: 'POST' })

  await applyApiSecurityToRequest(first, '/api/X', runtime({ encryptAlgorithm: 'AES-CBC' }))
  await applyApiSecurityToRequest(second, '/api/X', runtime({ encryptAlgorithm: 'AES-CBC' }))

  const firstIv = (first.data as { iv: string }).iv
  const secondIv = (second.data as { iv: string }).iv
  expect(base64ToBytes(firstIv)).toHaveLength(16)
  expect(firstIv).not.toBe(secondIv)
  expect((first.data as { data: string }).data).not.toBe((second.data as { data: string }).data)
})

it('未单独配置加密密钥时退回使用签名密钥派生 AES 密钥', async () => {
  const config = makeConfig({ data: { a: 1 }, method: 'POST' })

  await applyApiSecurityToRequest(config, '/api/X', runtime({ encryptAlgorithm: 'AES-CBC', encryptKey: '' }))

  const envelope = config.data as { data: string, iv: string }
  expect(await aesDecryptWith(await deriveKeyBytes('sk-1'), envelope.data, envelope.iv)).toBe('{"a":1}')
})

it('密钥长度正好 16/24/32 字节时原样使用，其它长度先做 SHA-256 派生', async () => {
  const raw16 = 'abcdefghijklmnop'
  const shortSeed = 'abc'
  const rawConfig = makeConfig({ data: { a: 1 }, method: 'POST' })
  const hashedConfig = makeConfig({ data: { a: 1 }, method: 'POST' })

  await applyApiSecurityToRequest(rawConfig, '/api/X', runtime({ encryptAlgorithm: 'AES-CBC', encryptKey: raw16 }))
  await applyApiSecurityToRequest(hashedConfig, '/api/X', runtime({ encryptAlgorithm: 'AES-CBC', encryptKey: shortSeed }))

  const rawEnvelope = rawConfig.data as { data: string, iv: string }
  const hashedEnvelope = hashedConfig.data as { data: string, iv: string }
  expect(await aesDecryptWith(encoder.encode(raw16), rawEnvelope.data, rawEnvelope.iv)).toBe('{"a":1}')
  expect(await aesDecryptWith(
    new Uint8Array(await crypto.subtle.digest('SHA-256', toBuffer(encoder.encode(shortSeed)))),
    hashedEnvelope.data,
    hashedEnvelope.iv,
  )).toBe('{"a":1}')
})

it('字符串请求体原样参与签名，不再被 JSON 序列化一次', async () => {
  const config = makeConfig({ data: 'raw=1&flag=true', method: 'PUT' })

  await applyApiSecurityToRequest(config, '/api/X', runtime())

  expect(headerOf(config, 'X-Content-Sign')).toBe(await digestHex('SHA-256', 'raw=1&flag=true'))
})

it('加密算法声明为 NONE 时请求体保持明文且不写 IV 头', async () => {
  const body = { a: 1 }
  const config = makeConfig({ data: body, method: 'POST' })

  await applyApiSecurityToRequest(config, '/api/X', runtime({ encryptAlgorithm: 'NONE' }))

  expect(config.data).toBe(body)
  expect(headerOf(config, 'X-Encrypt-Iv')).toBe('')
  expect(headerOf(config, 'X-Content-Sign')).toBe(await digestHex('SHA-256', '{"a":1}'))
})

it('带请求体时遇到不支持的加密算法立即抛错', async () => {
  const config = makeConfig({ data: { a: 1 }, method: 'POST' })

  await expect(applyApiSecurityToRequest(config, '/api/X', runtime({ encryptAlgorithm: 'DES' })))
    .rejects
    .toThrow(/不支持的前端请求加密算法/)
})

it('无请求体的 GET 即使声明了不支持的加密算法也照常签名', async () => {
  const config = makeConfig({ encryptAlgorithm: undefined })

  await applyApiSecurityToRequest(config, '/api/X', runtime({ encryptAlgorithm: 'DES' }))

  expect(headerOf(config, 'X-Encrypt-Algorithm')).toBe('DES')
  expect(headerOf(config, 'X-Signature')).toMatch(/^[0-9a-f]{64}$/)
})

it('写类动词 POST/PUT/PATCH/DELETE 都会加密请求体，GET 与 HEAD 不碰请求体', async () => {
  const encryptedMethods: string[] = []
  const untouchedMethods: string[] = []
  for (const method of ['POST', 'PUT', 'PATCH', 'DELETE', 'GET', 'HEAD']) {
    const config = makeConfig({ data: { a: 1 }, method })
    await applyApiSecurityToRequest(config, '/api/X', runtime({ encryptAlgorithm: 'AES-CBC' }))
    const handled = typeof (config.data as { iv?: string })?.iv === 'string'
    ;(handled ? encryptedMethods : untouchedMethods).push(method)
  }

  expect(encryptedMethods).toEqual(['POST', 'PUT', 'PATCH', 'DELETE'])
  expect(untouchedMethods).toEqual(['GET', 'HEAD'])
})

it('请求体为 null 或 undefined 时按无请求体处理', async () => {
  const nullBody = makeConfig({ data: null, method: 'POST' })
  const noBody = makeConfig({ method: 'POST' })

  await applyApiSecurityToRequest(nullBody, '/api/X', runtime({ encryptAlgorithm: 'AES-CBC' }))
  await applyApiSecurityToRequest(noBody, '/api/X', runtime({ encryptAlgorithm: 'AES-CBC' }))

  expect(nullBody.data).toBeNull()
  expect(headerOf(nullBody, 'X-Content-Sign')).toBe(SHA256_OF_EMPTY)
  expect(headerOf(noBody, 'X-Content-Sign')).toBe(SHA256_OF_EMPTY)
})

it('表单 FormData 上传被整体跳过，连访问标识与签名头都不会写', async () => {
  // 锁定当前真实行为：二进制请求体无法序列化签名，整条请求以未签名状态发出。
  // 已上报为缺陷但**不能**只改前端：后端按 UTF-8 读原始请求体算 contentSign，
  // 且要求「X-Content-Sign 一旦出现就必须与服务端自算值一致」，multipart 边界前端无从复算，
  // 单方面补签名头只会把未签名上传变成必定 401。改法需前后端同版本约定二进制体的 contentSign 口径。
  const form = new FormData()
  form.append('file', new Blob(['bytes']), 'a.txt')
  const config = makeConfig({ data: form, method: 'POST' })

  await applyApiSecurityToRequest(config, '/api/File/Upload', runtime({ encryptAlgorithm: 'AES-CBC' }))

  expect(securityHeaderNames(config)).toEqual([])
  expect(config.data).toBe(form)
})

it('二进制 Blob 与 ArrayBuffer 请求体同样整体跳过签名', async () => {
  const blobConfig = makeConfig({ data: new Blob(['x']), method: 'POST' })
  const bufferConfig = makeConfig({ data: new ArrayBuffer(8), method: 'POST' })

  await applyApiSecurityToRequest(blobConfig, '/api/X', runtime({ encryptAlgorithm: 'AES-CBC' }))
  await applyApiSecurityToRequest(bufferConfig, '/api/X', runtime({ encryptAlgorithm: 'AES-CBC' }))

  expect(securityHeaderNames(blobConfig)).toEqual([])
  expect(securityHeaderNames(bufferConfig)).toEqual([])
})

it('无法 JSON 序列化的循环引用请求体被跳过而不是抛错', async () => {
  const cyclic: Record<string, unknown> = { name: '循环' }
  cyclic.self = cyclic
  const config = makeConfig({ data: cyclic, method: 'POST' })

  await applyApiSecurityToRequest(config, '/api/X', runtime({ encryptAlgorithm: 'AES-CBC' }))

  expect(securityHeaderNames(config)).toEqual([])
})

it('配置上没有 headers 对象时新建一个并写齐全部安全头', async () => {
  const config = { method: 'GET', url: '/api/X' } as unknown as InternalAxiosRequestConfig

  await applyApiSecurityToRequest(config, '/api/X', runtime({ encryptResponse: true }))

  expect(Object.keys(config.headers as unknown as Record<string, string>).sort()).toEqual([
    'X-Access-Key',
    'X-Content-Sign',
    'X-Content-Sign-Algorithm',
    'X-Encrypt-Algorithm',
    'X-Encrypt-Response',
    'X-Nonce',
    'X-Sign-Algorithm',
    'X-Signature',
    'X-Timestamp',
  ])
})

/** 构造一份最小可用的运行时安全配置。 */
function runtime(overrides: Partial<ApiSecurityRuntimeConfig> = {}): ApiSecurityRuntimeConfig {
  return {
    enabled: true,
    accessKey: 'ak-1',
    secretKey: 'sk-1',
    encryptKey: 'encrypt-key-1',
    signAlgorithm: 'HMACSHA256',
    contentSignAlgorithm: 'SHA256',
    encryptAlgorithm: 'NONE',
    encryptResponse: false,
    ...overrides,
  }
}

/** 构造一份带 AxiosHeaders 的请求配置。 */
function makeConfig(overrides: Record<string, unknown> = {}): InternalAxiosRequestConfig {
  return {
    headers: new AxiosHeaders(),
    method: 'GET',
    url: '/api/X',
    ...overrides,
  } as unknown as InternalAxiosRequestConfig
}

/** 读取请求头，兼容 AxiosHeaders 与普通对象两种载体。 */
function headerOf(config: InternalAxiosRequestConfig, name: string): string {
  const headers = config.headers as unknown as {
    get?: (key: string) => unknown
  } & Record<string, unknown>
  const value = typeof headers.get === 'function' ? headers.get(name) : headers[name]
  return value === undefined || value === null ? '' : String(value)
}

/** 列出已写入的安全头名称（排序后便于断言）。 */
function securityHeaderNames(config: InternalAxiosRequestConfig): string[] {
  const names = [
    'X-Access-Key',
    'X-Content-Sign',
    'X-Content-Sign-Algorithm',
    'X-Encrypt-Algorithm',
    'X-Encrypt-Iv',
    'X-Encrypt-Response',
    'X-Nonce',
    'X-Sign-Algorithm',
    'X-Signature',
    'X-Timestamp',
  ]
  return names.filter(name => headerOf(config, name) !== '')
}

/** 计算指定摘要算法的十六进制结果。 */
async function digestHex(algorithm: string, text: string): Promise<string> {
  const digest = await crypto.subtle.digest(algorithm, toBuffer(encoder.encode(text)))
  return toHex(new Uint8Array(digest))
}

/** 计算 HMAC 十六进制签名。 */
async function hmacHex(hash: string, secret: string, message: string): Promise<string> {
  const key = await crypto.subtle.importKey(
    'raw',
    toBuffer(encoder.encode(secret)),
    { name: 'HMAC', hash: { name: hash } },
    false,
    ['sign'],
  )
  const signature = await crypto.subtle.sign('HMAC', key, toBuffer(encoder.encode(message)))
  return toHex(new Uint8Array(signature))
}

/** 按源码同样的规则派生 AES 密钥字节。 */
async function deriveKeyBytes(seed: string): Promise<Uint8Array> {
  const raw = encoder.encode(seed)
  if (raw.length === 16 || raw.length === 24 || raw.length === 32) {
    return raw
  }
  return new Uint8Array(await crypto.subtle.digest('SHA-256', toBuffer(raw)))
}

/** 用指定密钥字节解开 AES-CBC 密文。 */
async function aesDecryptWith(keyBytes: Uint8Array, cipherBase64: string, ivBase64: string): Promise<string> {
  const key = await crypto.subtle.importKey('raw', toBuffer(keyBytes), { name: 'AES-CBC' }, false, ['decrypt'])
  const plain = await crypto.subtle.decrypt(
    { name: 'AES-CBC', iv: toBuffer(base64ToBytes(ivBase64)) },
    key,
    toBuffer(base64ToBytes(cipherBase64)),
  )
  return new TextDecoder().decode(new Uint8Array(plain))
}

/** base64 转字节数组。 */
function base64ToBytes(base64: string): Uint8Array {
  const binary = atob(base64)
  const bytes = new Uint8Array(binary.length)
  for (let index = 0; index < binary.length; index += 1) {
    bytes[index] = binary.charCodeAt(index)
  }
  return bytes
}

/** 字节数组转十六进制串。 */
function toHex(bytes: Uint8Array): string {
  return Array.from(bytes).map(byte => byte.toString(16).padStart(2, '0')).join('')
}

/** 取出独立的 ArrayBuffer，避免 TypedArray 视图偏移影响 WebCrypto。 */
function toBuffer(bytes: Uint8Array): ArrayBuffer {
  return bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength) as ArrayBuffer
}
