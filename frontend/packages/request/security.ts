import type { AxiosResponse, InternalAxiosRequestConfig } from 'axios'

interface SecureEnvelope {
  alg?: string
  contentSign?: string
  data?: string
  iv?: string
}

export interface ApiSecurityRuntimeConfig {
  accessKey: string
  contentSignAlgorithm: string
  enabled: boolean
  encryptAlgorithm: string
  encryptKey: string
  encryptResponse: boolean
  signAlgorithm: string
  secretKey: string
}

const HeaderNames = {
  accessKey: 'X-Access-Key',
  contentSign: 'X-Content-Sign',
  contentSignAlgorithm: 'X-Content-Sign-Algorithm',
  encryptAlgorithm: 'X-Encrypt-Algorithm',
  encryptIv: 'X-Encrypt-Iv',
  encryptResponse: 'X-Encrypt-Response',
  nonce: 'X-Nonce',
  secureResponse: 'X-Secure-Response',
  signature: 'X-Signature',
  signatureAlgorithm: 'X-Sign-Algorithm',
  timestamp: 'X-Timestamp',
} as const

const textEncoder = new TextEncoder()
const textDecoder = new TextDecoder()

export function resolveApiSecurityRuntimeConfig(): ApiSecurityRuntimeConfig {
  const enabled = toBoolean(import.meta.env.VITE_API_SECURITY_ENABLED, false)

  return {
    enabled,
    accessKey: String(import.meta.env.VITE_API_SECURITY_ACCESS_KEY ?? '').trim(),
    secretKey: String(import.meta.env.VITE_API_SECURITY_SECRET_KEY ?? '').trim(),
    encryptKey: String(import.meta.env.VITE_API_SECURITY_ENCRYPT_KEY ?? '').trim(),
    signAlgorithm: normalizeAlgorithm(
      String(import.meta.env.VITE_API_SECURITY_SIGN_ALGORITHM ?? ''),
      'HMACSHA256',
    ),
    contentSignAlgorithm: normalizeAlgorithm(
      String(import.meta.env.VITE_API_SECURITY_CONTENT_SIGN_ALGORITHM ?? ''),
      'SHA256',
    ),
    encryptAlgorithm: normalizeAlgorithm(
      String(import.meta.env.VITE_API_SECURITY_ENCRYPT_ALGORITHM ?? ''),
      'AES-CBC',
    ),
    encryptResponse: toBoolean(import.meta.env.VITE_API_SECURITY_ENCRYPT_RESPONSE, true),
  }
}

export async function applyApiSecurityToRequest(
  config: InternalAxiosRequestConfig,
  requestUri: string,
  runtime: ApiSecurityRuntimeConfig,
): Promise<void> {
  if (!runtime.enabled || !runtime.accessKey || !runtime.secretKey) {
    return
  }

  if (!globalThis.crypto?.subtle) {
    return
  }

  const method = String(config.method ?? 'GET').toUpperCase()
  const nonce = createNonce()
  const timestamp = Math.floor(Date.now() / 1000).toString()
  const encryptAlgorithm = normalizeAlgorithm(runtime.encryptAlgorithm, 'AES-CBC')

  let plaintextBody = ''
  const shouldHandleBody = ['POST', 'PUT', 'PATCH', 'DELETE'].includes(method)
    && config.data !== undefined
    && config.data !== null
  if (shouldHandleBody) {
    const serialized = stringifyPayload(config.data)
    if (serialized === null) {
      return
    }

    plaintextBody = serialized
    if (!isNoEncryption(encryptAlgorithm)) {
      if (encryptAlgorithm !== 'AES' && encryptAlgorithm !== 'AES-CBC') {
        throw new Error(`不支持的前端请求加密算法: ${encryptAlgorithm}`)
      }

      const encrypted = await aesEncrypt(plaintextBody, runtime.encryptKey || runtime.secretKey)
      config.data = {
        alg: 'AES-CBC',
        data: encrypted.cipherText,
        iv: encrypted.iv,
      }
      setHeader(config, HeaderNames.encryptIv, encrypted.iv)
    }
  }

  const contentSign = await computeContentSign(runtime.contentSignAlgorithm, plaintextBody)
  const { path, query } = resolveCanonicalPathAndQuery(requestUri)
  const canonicalRequest = [method, path, query, contentSign, timestamp, nonce].join('\n')
  const signature = await computeRequestSignature(runtime.signAlgorithm, runtime.secretKey, canonicalRequest)

  setHeader(config, HeaderNames.accessKey, runtime.accessKey)
  setHeader(config, HeaderNames.timestamp, timestamp)
  setHeader(config, HeaderNames.nonce, nonce)
  setHeader(config, HeaderNames.signature, signature)
  setHeader(config, HeaderNames.contentSign, contentSign)
  setHeader(config, HeaderNames.signatureAlgorithm, runtime.signAlgorithm)
  setHeader(config, HeaderNames.contentSignAlgorithm, runtime.contentSignAlgorithm)
  setHeader(config, HeaderNames.encryptAlgorithm, encryptAlgorithm)
  if (runtime.encryptResponse) {
    setHeader(config, HeaderNames.encryptResponse, '1')
  }
}

export async function tryDecryptSecureResponse<T>(
  response: AxiosResponse<T>,
  runtime: ApiSecurityRuntimeConfig,
): Promise<void> {
  if (!runtime.enabled || !runtime.secretKey || !globalThis.crypto?.subtle) {
    return
  }

  const secureResponseFlag = getHeaderValue(response.headers, HeaderNames.secureResponse)
  if (!toBoolean(secureResponseFlag, false)) {
    return
  }

  const envelope = response.data as SecureEnvelope | null | undefined
  if (!envelope || typeof envelope !== 'object' || !envelope.data) {
    return
  }

  const encryptAlgorithm = normalizeAlgorithm(
    envelope.alg || getHeaderValue(response.headers, HeaderNames.encryptAlgorithm),
    runtime.encryptAlgorithm,
  )
  if (isNoEncryption(encryptAlgorithm)) {
    return
  }

  if (encryptAlgorithm !== 'AES' && encryptAlgorithm !== 'AES-CBC') {
    throw new Error(`不支持的响应加密算法: ${encryptAlgorithm}`)
  }

  const iv = envelope.iv || getHeaderValue(response.headers, HeaderNames.encryptIv)
  if (!iv) {
    throw new Error('缺少响应解密 IV')
  }

  const plaintext = await aesDecrypt(envelope.data, runtime.encryptKey || runtime.secretKey, iv)
  if (envelope.contentSign) {
    const calculated = await computeContentSign(runtime.contentSignAlgorithm, plaintext)
    if (calculated.toLowerCase() !== String(envelope.contentSign).toLowerCase()) {
      throw new Error('响应内容签名校验失败')
    }
  }

  ; (response as any).data = tryParseJson(plaintext)
}

function createNonce(): string {
  if (!globalThis.crypto?.getRandomValues) {
    return `${Date.now()}_${Math.random().toString(16).slice(2, 14)}`
  }

  const bytes = new Uint8Array(16)
  globalThis.crypto.getRandomValues(bytes)
  return Array.from(bytes).map(byte => byte.toString(16).padStart(2, '0')).join('')
}

function stringifyPayload(payload: unknown): string | null {
  if (typeof FormData !== 'undefined' && payload instanceof FormData) {
    return null
  }

  if (payload instanceof ArrayBuffer) {
    return null
  }

  if (typeof Blob !== 'undefined' && payload instanceof Blob) {
    return null
  }

  if (typeof payload === 'string') {
    return payload
  }

  try {
    return JSON.stringify(payload ?? {})
  }
  catch {
    return null
  }
}

function resolveCanonicalPathAndQuery(requestUri: string): { path: string, query: string } {
  const base = typeof window !== 'undefined' ? window.location.origin : 'http://localhost'
  const url = new URL(requestUri, base)

  const entries: Array<[string, string]> = []
  url.searchParams.forEach((value, key) => {
    entries.push([key, value])
  })

  entries.sort((a, b) => {
    if (a[0] < b[0])
      return -1
    if (a[0] > b[0])
      return 1
    if (a[1] < b[1])
      return -1
    if (a[1] > b[1])
      return 1
    return 0
  })

  const query = entries
    .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(value)}`)
    .join('&')

  return {
    path: url.pathname || '/',
    query,
  }
}

async function computeContentSign(algorithm: string, text: string): Promise<string> {
  const normalized = normalizeAlgorithm(algorithm, 'SHA256')
  const digestAlgorithm = normalized === 'SHA512' ? 'SHA-512' : 'SHA-256'
  const digest = await globalThis.crypto.subtle.digest(digestAlgorithm, textEncoder.encode(text))
  return bytesToHex(new Uint8Array(digest))
}

async function computeRequestSignature(
  algorithm: string,
  secretKey: string,
  canonicalRequest: string,
): Promise<string> {
  const normalized = normalizeAlgorithm(algorithm, 'HMACSHA256')
  const hashName = normalized === 'HMACSHA1'
    ? 'SHA-1'
    : normalized === 'HMACSHA512'
      ? 'SHA-512'
      : 'SHA-256'

  const key = await globalThis.crypto.subtle.importKey(
    'raw',
    toBufferSource(textEncoder.encode(secretKey)),
    { name: 'HMAC', hash: { name: hashName } },
    false,
    ['sign'],
  )
  const signature = await globalThis.crypto.subtle.sign('HMAC', key, toBufferSource(textEncoder.encode(canonicalRequest)))
  return bytesToHex(new Uint8Array(signature))
}

async function aesEncrypt(
  plaintext: string,
  keySeed: string,
): Promise<{ cipherText: string, iv: string }> {
  const keyBytes = await resolveAesKeyBytes(keySeed)
  const ivBytes = new Uint8Array(16)
  globalThis.crypto.getRandomValues(ivBytes)

  const aesKey = await globalThis.crypto.subtle.importKey(
    'raw',
    toBufferSource(keyBytes),
    { name: 'AES-CBC' },
    false,
    ['encrypt'],
  )

  const encrypted = await globalThis.crypto.subtle.encrypt(
    { name: 'AES-CBC', iv: toBufferSource(ivBytes) },
    aesKey,
    toBufferSource(textEncoder.encode(plaintext)),
  )

  return {
    cipherText: bytesToBase64(new Uint8Array(encrypted)),
    iv: bytesToBase64(ivBytes),
  }
}

async function aesDecrypt(
  cipherTextBase64: string,
  keySeed: string,
  ivBase64: string,
): Promise<string> {
  const keyBytes = await resolveAesKeyBytes(keySeed)
  const cipherBytes = base64ToBytes(cipherTextBase64)
  const ivBytes = base64ToBytes(ivBase64)
  if (ivBytes.length !== 16) {
    throw new Error('IV 长度必须为 16 字节')
  }

  const aesKey = await globalThis.crypto.subtle.importKey(
    'raw',
    toBufferSource(keyBytes),
    { name: 'AES-CBC' },
    false,
    ['decrypt'],
  )

  const decrypted = await globalThis.crypto.subtle.decrypt(
    { name: 'AES-CBC', iv: toBufferSource(ivBytes) },
    aesKey,
    toBufferSource(cipherBytes),
  )

  return textDecoder.decode(new Uint8Array(decrypted))
}

async function resolveAesKeyBytes(seed: string): Promise<Uint8Array> {
  const raw = textEncoder.encode(seed)
  if (raw.length === 16 || raw.length === 24 || raw.length === 32) {
    return raw
  }

  const digest = await globalThis.crypto.subtle.digest('SHA-256', raw)
  return new Uint8Array(digest)
}

function setHeader(config: InternalAxiosRequestConfig, name: string, value: string) {
  const headers = config.headers as any
  if (!headers) {
    config.headers = { [name]: value } as any
    return
  }

  if (typeof headers.set === 'function') {
    headers.set(name, value)
    return
  }

  headers[name] = value
}

function getHeaderValue(headers: unknown, name: string): string {
  const rawHeaders = headers as any
  if (!rawHeaders) {
    return ''
  }

  if (typeof rawHeaders.get === 'function') {
    const value = rawHeaders.get(name) ?? rawHeaders.get(name.toLowerCase())
    return value ? String(value) : ''
  }

  const value = rawHeaders[name] ?? rawHeaders[name.toLowerCase()]
  if (Array.isArray(value)) {
    return value.length > 0 ? String(value[0]) : ''
  }

  return value ? String(value) : ''
}

function bytesToHex(bytes: Uint8Array): string {
  return Array.from(bytes).map(byte => byte.toString(16).padStart(2, '0')).join('')
}

function toBufferSource(bytes: Uint8Array): ArrayBuffer {
  const { byteOffset, byteLength, buffer } = bytes
  if (byteOffset === 0 && byteLength === buffer.byteLength) {
    return buffer as ArrayBuffer
  }

  return buffer.slice(byteOffset, byteOffset + byteLength) as ArrayBuffer
}

function bytesToBase64(bytes: Uint8Array): string {
  const chunkSize = 0x8000
  let binary = ''
  for (let index = 0; index < bytes.length; index += chunkSize) {
    const chunk = bytes.subarray(index, index + chunkSize)
    binary += String.fromCharCode(...chunk)
  }
  return btoa(binary)
}

function base64ToBytes(base64: string): Uint8Array {
  const binary = atob(base64)
  const bytes = new Uint8Array(binary.length)
  for (let index = 0; index < binary.length; index += 1) {
    bytes[index] = binary.charCodeAt(index)
  }
  return bytes
}

function normalizeAlgorithm(value: string, fallback = ''): string {
  const normalized = String(value ?? '').trim().toUpperCase()
  return normalized || fallback.toUpperCase()
}

function isNoEncryption(value: string): boolean {
  return normalizeAlgorithm(value, 'NONE') === 'NONE'
}

function toBoolean(raw: unknown, fallback: boolean): boolean {
  if (raw === undefined || raw === null) {
    return fallback
  }

  const value = String(raw).trim().toLowerCase()
  if (value === '1' || value === 'true' || value === 'yes') {
    return true
  }

  if (value === '0' || value === 'false' || value === 'no') {
    return false
  }

  return fallback
}

function tryParseJson(input: string): unknown {
  try {
    return JSON.parse(input)
  }
  catch {
    return input
  }
}
