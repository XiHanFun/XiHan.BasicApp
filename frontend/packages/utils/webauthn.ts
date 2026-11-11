/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * 通行密钥的浏览器仪式：后端给 WebAuthn JSON 形态的选项，这里换成浏览器要的二进制，
 * 把浏览器交回的凭据再编成 JSON（二进制字段一律 base64url、不带填充），原样交回后端核对
 */

/** 凭据描述（排除表与允许表的一项） */
export interface PasskeyDescriptorJson {
  type: string
  id: string
  transports?: readonly string[] | null
}

/** 登记的创建选项（`navigator.credentials.create`） */
export interface PasskeyCreationOptionsJson {
  rp: { id: string, name: string }
  user: { id: string, name: string, displayName: string }
  challenge: string
  pubKeyCredParams: readonly { type: string, alg: number }[]
  timeout: number
  excludeCredentials: readonly PasskeyDescriptorJson[]
  authenticatorSelection: { residentKey: string, requireResidentKey: boolean, userVerification: string }
  attestation: string
}

/** 登录的请求选项（`navigator.credentials.get`） */
export interface PasskeyRequestOptionsJson {
  challenge: string
  timeout: number
  rpId: string
  allowCredentials: readonly PasskeyDescriptorJson[]
  userVerification: string
}

/** 浏览器创建的凭据 */
export interface PasskeyRegistrationJson {
  id: string
  rawId: string
  type: string
  response: { clientDataJSON: string, attestationObject: string, transports: string[] | null }
}

/** 浏览器给的断言 */
export interface PasskeyAssertionJson {
  id: string
  rawId: string
  type: string
  response: { clientDataJSON: string, authenticatorData: string, signature: string, userHandle: string | null }
}

/** 浏览器能不能用通行密钥（要安全上下文：https 或 localhost） */
export function passkeysSupported(): boolean {
  return typeof window !== 'undefined'
    && window.isSecureContext === true
    && typeof window.PublicKeyCredential === 'function'
    && typeof navigator.credentials?.create === 'function'
}

/**
 * 是不是本人取消或超时
 * 浏览器对取消、超时与「没有可用的通行密钥」一律报 NotAllowedError，不区分，调用方不当错误提示
 */
export function isPasskeyCancelled(error: unknown): boolean {
  return error instanceof DOMException && (error.name === 'NotAllowedError' || error.name === 'AbortError')
}

/** base64url（不带填充）转字节 */
export function base64UrlToBytes(value: string): Uint8Array<ArrayBuffer> {
  const base64 = value.replaceAll('-', '+').replaceAll('_', '/')
  const binary = atob(base64.padEnd(base64.length + (4 - base64.length % 4) % 4, '='))
  const bytes = new Uint8Array(binary.length)
  for (let i = 0; i < binary.length; i++)
    bytes[i] = binary.charCodeAt(i)
  return bytes
}

/** 字节转 base64url（不带填充） */
export function bytesToBase64Url(value: ArrayBuffer | ArrayBufferView): string {
  const bytes = value instanceof ArrayBuffer
    ? new Uint8Array(value)
    : new Uint8Array(value.buffer, value.byteOffset, value.byteLength)
  let binary = ''
  for (const byte of bytes)
    binary += String.fromCharCode(byte)
  return btoa(binary).replaceAll('+', '-').replaceAll('/', '_').replace(/=+$/, '')
}

function descriptorOf(descriptor: PasskeyDescriptorJson): PublicKeyCredentialDescriptor {
  return {
    type: descriptor.type as PublicKeyCredentialType,
    id: base64UrlToBytes(descriptor.id),
    ...(descriptor.transports
      ? {
          transports: [...descriptor.transports] as AuthenticatorTransport[],
        }
      : {}),
  }
}

/** 登记：弹出浏览器的通行密钥创建，返回要交给后端的凭据；本人取消时抛 NotAllowedError（见 isPasskeyCancelled） */
export async function createPasskey(options: PasskeyCreationOptionsJson): Promise<PasskeyRegistrationJson> {
  const credential = await navigator.credentials.create({
    publicKey: {
      rp: options.rp,
      user: {
        id: base64UrlToBytes(options.user.id),
        name: options.user.name,
        displayName: options.user.displayName,
      },
      challenge: base64UrlToBytes(options.challenge),
      pubKeyCredParams: options.pubKeyCredParams.map(param => ({ type: param.type as PublicKeyCredentialType, alg: param.alg })),
      timeout: options.timeout,
      excludeCredentials: options.excludeCredentials.map(descriptorOf),
      authenticatorSelection: {
        residentKey: options.authenticatorSelection.residentKey as ResidentKeyRequirement,
        requireResidentKey: options.authenticatorSelection.requireResidentKey,
        userVerification: options.authenticatorSelection.userVerification as UserVerificationRequirement,
      },
      attestation: options.attestation as AttestationConveyancePreference,
    },
  }) as PublicKeyCredential | null
  if (!credential)
    throw new DOMException('No passkey was created.', 'NotAllowedError')
  const response = credential.response as AuthenticatorAttestationResponse
  return {
    id: credential.id,
    rawId: bytesToBase64Url(credential.rawId),
    type: credential.type,
    response: {
      clientDataJSON: bytesToBase64Url(response.clientDataJSON),
      attestationObject: bytesToBase64Url(response.attestationObject),
      transports: typeof response.getTransports === 'function'
        ? response.getTransports()
        : null,
    },
  }
}

/** 登录：弹出浏览器的通行密钥选择，返回要交给后端的断言；本人取消时抛 NotAllowedError */
export async function getPasskey(options: PasskeyRequestOptionsJson): Promise<PasskeyAssertionJson> {
  const credential = await navigator.credentials.get({
    publicKey: {
      challenge: base64UrlToBytes(options.challenge),
      timeout: options.timeout,
      rpId: options.rpId,
      allowCredentials: options.allowCredentials.map(descriptorOf),
      userVerification: options.userVerification as UserVerificationRequirement,
    },
  }) as PublicKeyCredential | null
  if (!credential)
    throw new DOMException('No passkey was chosen.', 'NotAllowedError')
  const response = credential.response as AuthenticatorAssertionResponse
  return {
    id: credential.id,
    rawId: bytesToBase64Url(credential.rawId),
    type: credential.type,
    response: {
      clientDataJSON: bytesToBase64Url(response.clientDataJSON),
      authenticatorData: bytesToBase64Url(response.authenticatorData),
      signature: bytesToBase64Url(response.signature),
      userHandle: response.userHandle
        ? bytesToBase64Url(response.userHandle)
        : null,
    },
  }
}
