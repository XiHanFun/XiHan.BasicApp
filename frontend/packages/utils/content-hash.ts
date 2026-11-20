/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/** 秒传探测的大小上限：整份读进内存算 SHA-256，更大的文件直接上传 */
export const FAST_UPLOAD_MAX_BYTES = 200 * 1024 * 1024

/**
 * 能不能为秒传算摘要
 * 空文件与超过 {@link FAST_UPLOAD_MAX_BYTES} 的不算；Web Crypto 只在安全上下文（HTTPS 或 localhost）里有
 */
export function canHashForFastUpload(blob: Blob): boolean {
  return blob.size > 0 && blob.size <= FAST_UPLOAD_MAX_BYTES && !!globalThis.crypto?.subtle
}

/** SHA-256（十六进制小写，与后端秒传同一算法） */
export async function sha256Hex(blob: Blob): Promise<string> {
  const digest = await crypto.subtle.digest('SHA-256', await blob.arrayBuffer())
  return [
    ...new Uint8Array(digest),
  ].map(b => b.toString(16).padStart(2, '0')).join('')
}
