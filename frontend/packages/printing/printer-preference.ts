/**
 * 打印机本地偏好存储。
 * 职责：按当前用户、租户和模板编码隔离浏览器本地打印机选择，不上传到服务端。
 */
import { useUserStore } from '~/stores'

const PREFERENCE_PREFIX = 'xihan:printing:preferred-printer'

/**
 * 读取当前用户、租户与模板的本地打印机偏好。
 * @param templateCode 模板编码。
 * @returns 打印机名称；未设置或非浏览器环境时返回 null。
 */
export function getPreferredPrinter(templateCode: string): string | null {
  if (typeof localStorage === 'undefined')
    return null
  const value = localStorage.getItem(createPreferenceKey(templateCode))?.trim()
  return value || null
}

/**
 * 保存或清除当前用户、租户与模板的本地打印机偏好。
 * @param templateCode 模板编码。
 * @param printerName 打印机名称；null/空白表示清除。
 * @returns 无返回值。
 * @throws 模板编码为空或浏览器存储不可用（非浏览器环境）。
 */
export function savePreferredPrinter(templateCode: string, printerName: null | string): void {
  const key = createPreferenceKey(templateCode)
  // 读侧无存储时静默返回 null（等价于「没有偏好」），写侧不能静默——用户选了打印机却没存下来必须让调用方知道；
  // 但要抛带业务语义的错误，而不是底层 TypeError，否则调用方无法与「编码为空」区分
  if (typeof localStorage === 'undefined')
    throw new Error('浏览器本地存储不可用，无法保存打印机偏好。')
  const normalized = printerName?.trim()
  if (normalized)
    localStorage.setItem(key, normalized)
  else
    localStorage.removeItem(key)
  return undefined
}

/** 创建不跨用户、租户和模板串用的存储键。 */
function createPreferenceKey(templateCode: string): string {
  const normalizedCode = templateCode?.trim()
  if (!normalizedCode)
    throw new Error('打印模板编码不能为空。')
  const user = useUserStore().userInfo
  const userId = user?.basicId || 'anonymous'
  const tenantId = user?.tenantId || 'platform'
  return `${PREFERENCE_PREFIX}:${encodeURIComponent(tenantId)}:${encodeURIComponent(userId)}:${encodeURIComponent(normalizedCode)}`
}
