/**
 * 通用文件下载器：底层封装「拿到数据后触发浏览器下载」，业务处直接调用。
 * 不负责发请求（与 api 层解耦）——业务先经 api 取 Blob，再交给这里触发下载。
 */

/** 触发浏览器下载一个 Blob（如鉴权下载接口返回的文件流） */
export function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob)
  triggerAnchorDownload(url, filename)
  // 延迟释放，确保下载已开始（部分浏览器同步 revoke 会中断下载）
  setTimeout(() => URL.revokeObjectURL(url), 1000)
}

/** 触发浏览器下载一个 URL（静态资源 / 已有直链） */
export function downloadByUrl(url: string, filename = '') {
  triggerAnchorDownload(url, filename)
}

function triggerAnchorDownload(url: string, filename: string) {
  const anchor = document.createElement('a')
  anchor.href = url
  if (filename) {
    anchor.download = filename
  }
  anchor.style.display = 'none'
  document.body.appendChild(anchor)
  anchor.click()
  document.body.removeChild(anchor)
}
