/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { files as filesOps } from '~/contracts'
import { $t } from '~/locales'
import { endpointUrl, parseProblem } from '~/request'

/**
 * 下载前的探测：带 `Range: bytes=0-0` 只取第一个字节，读完响应头就中止，服务端不认 Range 时也不把整份下下来
 * 本地存储（宿主的 `/blobs/…`）出错回问题详情，按它的本地化正文报；S3 兼容存储回 XML、网络不通或跨域被拦时没有可读的原因，报「暂时无法下载」
 */
async function ensureDownloadable(url: string): Promise<void> {
  const controller = new AbortController()
  let response: Response
  try {
    response = await fetch(url, {
      headers: { Range: 'bytes=0-0' },
      signal: controller.signal,
    })
  }
  catch {
    throw new Error($t('files.download_unavailable'))
  }
  try {
    if (response.ok)
      return
    if (response.headers.get('Content-Type')?.includes('json'))
      throw await parseProblem(response, '')
    throw new Error($t('files.download_unavailable'))
  }
  finally {
    controller.abort()
  }
}

/**
 * 换取完整的签名下载地址，给图片、链接与新窗口用
 * 有效期内不再校验身份；地址带签名参数须原样使用（本地存储是宿主的 `/blobs/…`，S3 兼容存储直连）
 * `download` 为真时以附件下载（带原始文件名），否则按内容类型内联显示
 */
export async function fileLink(id: string, download = false): Promise<string> {
  const { url } = await filesOps.presign(download
    ? { id, download }
    : { id })
  return endpointUrl(url)
}

/** 读文件内容（预览用）：经签名地址取，不带令牌 */
export async function fetchFileBlob(id: string): Promise<Blob> {
  const response = await fetch(await fileLink(id))
  if (!response.ok)
    throw new Error(`HTTP ${response.status}`)
  return response.blob()
}

/**
 * 以附件下载：经签名地址让浏览器自己下载（带原始文件名），不把内容读进内存
 * 先只取第一个字节探一下：存储不可用、文件已删或地址失效时在当前页报错，不让浏览器跳到错误响应上
 */
export async function downloadFile(id: string): Promise<void> {
  const url = await fileLink(id, true)
  await ensureDownloadable(url)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.rel = 'noopener'
  document.body.append(anchor)
  anchor.click()
  anchor.remove()
}
