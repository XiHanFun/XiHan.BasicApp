/**
 * 安全开关接入拦截器的端到端单元测试。
 *
 * 职责边界：只验证 RequestClient 与 security.ts 的接线——开关关闭时完全不介入、
 * 开关打开时出站请求带齐签名头且签名覆盖真实的路径与查询串、成功响应先解密再拆信封、
 * 错误响应解密后能取到后端业务消息，以及解密失败时不得吞掉原始错误。
 * 签名与解密的算法细节在 security-request/security-response 两个测试文件里。
 */
import type { AxiosAdapter, AxiosResponse, InternalAxiosRequestConfig } from 'axios'
import type { Router } from 'vue-router'
import { AxiosError, AxiosHeaders } from 'axios'
import { beforeEach, expect, it, vi } from 'vitest'
import { bindLockHook, bindLogoutHook, bindRouter, RequestClient } from './index'

const SECRET = 'pipeline-secret'
const SHA256_OF_EMPTY = 'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855'
const encoder = new TextEncoder()

beforeEach(() => {
  bindRouter({ replace: () => Promise.resolve() } as unknown as Router)
  bindLogoutHook(() => {})
  bindLockHook(() => {})
})

it('安全开关关闭时出站请求不带任何签名头', async () => {
  const calls: InternalAxiosRequestConfig[] = []
  const client = new RequestClient({ adapter: capturingAdapter(calls) })

  await client.get('/Sys/Ping')

  expect(calls[0]?.headers.get('X-Access-Key')).toBeUndefined()
  expect(calls[0]?.headers.get('X-Signature')).toBeUndefined()
})

it('安全开关打开时出站请求带齐访问标识与签名头', async () => {
  enableSecurity()
  const calls: InternalAxiosRequestConfig[] = []
  const client = new RequestClient({ adapter: capturingAdapter(calls) })

  await client.get('/Sys/Ping')

  expect(calls[0]?.headers.get('X-Access-Key')).toBe('pipeline-access')
  expect(String(calls[0]?.headers.get('X-Signature'))).toMatch(/^[0-9a-f]{64}$/)
})

it('签名覆盖拼接后的完整路径与序列化后的查询参数', async () => {
  // 回归锚点：签名用的是 instance.getUri(config)，漏掉 params 会让带查询的请求整批验签失败
  enableSecurity()
  const calls: InternalAxiosRequestConfig[] = []
  const client = new RequestClient({ adapter: capturingAdapter(calls) })

  await client.get('/Sys/Page', { params: { b: 2, a: 1 } })

  const config = calls[0]
  const canonical = [
    'GET',
    '/api/Sys/Page',
    'a=1&b=2',
    SHA256_OF_EMPTY,
    String(config?.headers.get('X-Timestamp')),
    String(config?.headers.get('X-Nonce')),
  ].join('\n')
  expect(config?.headers.get('X-Signature')).toBe(await hmacHex(SECRET, canonical))
})

it('开关打开时成功响应先解密再拆业务信封', async () => {
  enableSecurity()
  const envelope = await encryptEnvelope(JSON.stringify({ isSuccess: true, code: 200, data: { id: 42 } }))
  const client = new RequestClient({
    adapter: ((config: InternalAxiosRequestConfig) => Promise.resolve({
      config,
      data: envelope,
      headers: { 'X-Secure-Response': '1' },
      status: 200,
      statusText: 'OK',
    })) as unknown as AxiosAdapter,
  })

  await expect(client.get('/Sys/Detail')).resolves.toEqual({ id: 42 })
})

it('开关打开时错误响应也先解密，业务错误消息因此能被读出来', async () => {
  enableSecurity()
  const envelope = await encryptEnvelope(JSON.stringify({ code: 400, message: '通用码描述', data: '库存不足' }))
  const client = new RequestClient({
    adapter: ((config: InternalAxiosRequestConfig) => Promise.reject(new AxiosError(
      'Request failed with status code 400',
      AxiosError.ERR_BAD_RESPONSE,
      config,
      null,
      {
        config,
        data: envelope,
        headers: new AxiosHeaders({ 'X-Secure-Response': '1' }),
        status: 400,
        statusText: '',
      },
    ))) as unknown as AxiosAdapter,
  })

  const { error } = await client.getFlat('/Order/Create')

  expect(error?.message).toBe('库存不足')
})

it('错误响应解密失败时不中断原始错误流程，且不把密文当成错误消息透出', async () => {
  // 回归锚点：错误分支里的解密被 try/catch 包住，抛错不得中断原始错误流程；
  // 解密失败后 response.data 仍是安全信封，其 data 是 base64 密文而不是后端业务消息，
  // extractBackendMessage 必须认出信封形态并跳过，让文案回落到状态码兜底
  enableSecurity()
  const client = new RequestClient({
    adapter: ((config: InternalAxiosRequestConfig) => Promise.reject(new AxiosError(
      'Request failed with status code 503',
      AxiosError.ERR_BAD_RESPONSE,
      config,
      null,
      {
        config,
        data: { alg: 'AES-CBC', data: 'ZmFrZQ==' },
        headers: new AxiosHeaders({ 'X-Secure-Response': '1' }),
        status: 503,
        statusText: '',
      },
    ))) as unknown as AxiosAdapter,
  })

  const { data, error } = await client.getFlat('/Sys/Any')

  expect(data).toBeNull()
  expect((error as AxiosError).response?.status).toBe(503)
  expect(error?.message).toBe('服务暂时不可用')
})

it('明文错误响应里的 data 仍然优先当作后端业务消息，信封判定不误伤', async () => {
  // 与上一条配对：跳过的只能是 { alg|iv, data: 密文 } 这种信封形态，
  // 普通错误体（后端把具体错误写在 data 里）必须照常透出
  enableSecurity()
  const client = new RequestClient({
    adapter: ((config: InternalAxiosRequestConfig) => Promise.reject(new AxiosError(
      'Request failed with status code 400',
      AxiosError.ERR_BAD_RESPONSE,
      config,
      null,
      {
        config,
        data: { code: 400, message: '通用码描述', data: '库存不足' },
        headers: new AxiosHeaders(),
        status: 400,
        statusText: '',
      },
    ))) as unknown as AxiosAdapter,
  })

  const { error } = await client.getFlat('/Order/Create')

  expect(error?.message).toBe('库存不足')
})

it('成功响应解密失败时错误向调用方暴露，不返回半解密的响应体', async () => {
  // 与错误分支相反：成功分支没有 try/catch，解密异常必须冒泡
  enableSecurity()
  const client = new RequestClient({
    adapter: ((config: InternalAxiosRequestConfig) => Promise.resolve({
      config,
      data: { alg: 'AES-CBC', data: 'ZmFrZQ==' },
      headers: { 'X-Secure-Response': '1' },
      status: 200,
      statusText: 'OK',
    })) as unknown as AxiosAdapter,
  })

  const { error } = await client.getFlat('/Sys/Any')

  expect(error?.message).toMatch(/缺少响应解密 IV/)
})

it('未标记 X-Secure-Response 的响应在开关打开时也按明文处理', async () => {
  enableSecurity()
  const client = new RequestClient({
    adapter: ((config: InternalAxiosRequestConfig) => Promise.resolve({
      config,
      data: { isSuccess: true, code: 200, data: '明文直出' },
      headers: {},
      status: 200,
      statusText: 'OK',
    })) as unknown as AxiosAdapter,
  })

  await expect(client.get('/Sys/Detail')).resolves.toBe('明文直出')
})

it('安全配置在客户端创建时固化，之后改环境变量不影响已创建的客户端', async () => {
  const calls: InternalAxiosRequestConfig[] = []
  const client = new RequestClient({ adapter: capturingAdapter(calls) })
  enableSecurity()

  await client.get('/Sys/Ping')

  expect(calls[0]?.headers.get('X-Access-Key')).toBeUndefined()
})

/** 打开安全开关并注入测试用密钥（unstubEnvs 负责用例后还原）。 */
function enableSecurity(): void {
  vi.stubEnv('VITE_API_SECURITY_ENABLED', 'true')
  vi.stubEnv('VITE_API_SECURITY_ACCESS_KEY', 'pipeline-access')
  vi.stubEnv('VITE_API_SECURITY_SECRET_KEY', SECRET)
  vi.stubEnv('VITE_API_SECURITY_ENCRYPT_KEY', '')
  vi.stubEnv('VITE_API_SECURITY_ENCRYPT_ALGORITHM', 'NONE')
  vi.stubEnv('VITE_API_SECURITY_ENCRYPT_RESPONSE', 'true')
}

/** 记录出站配置并返回成功信封的适配器。 */
function capturingAdapter(calls: InternalAxiosRequestConfig[]): AxiosAdapter {
  return ((config: InternalAxiosRequestConfig) => {
    calls.push(config)
    return Promise.resolve<AxiosResponse>({
      config,
      data: { isSuccess: true, code: 200, data: null },
      headers: new AxiosHeaders(),
      status: 200,
      statusText: 'OK',
    })
  }) as unknown as AxiosAdapter
}

/** 用签名密钥派生的 AES 密钥加密出一份响应信封。 */
async function encryptEnvelope(plaintext: string): Promise<{ alg: string, data: string, iv: string }> {
  const keyBytes = new Uint8Array(await crypto.subtle.digest('SHA-256', toBuffer(encoder.encode(SECRET))))
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

/** 计算 HMAC-SHA256 十六进制签名。 */
async function hmacHex(secret: string, message: string): Promise<string> {
  const key = await crypto.subtle.importKey(
    'raw',
    toBuffer(encoder.encode(secret)),
    { name: 'HMAC', hash: { name: 'SHA-256' } },
    false,
    ['sign'],
  )
  const signature = await crypto.subtle.sign('HMAC', key, toBuffer(encoder.encode(message)))
  return Array.from(new Uint8Array(signature)).map(byte => byte.toString(16).padStart(2, '0')).join('')
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
