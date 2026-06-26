import { ref } from 'vue'

/**
 * 拼音搜索索引（命令面板等用）。
 *
 * - 懒加载：词典较大，首次调用 ensurePinyin()（如命令面板打开时）才动态加载 pinyin-pro，
 *   不影响首屏体积；加载完成前匹配静默退化为原样（仅标题/关键词匹配）。
 * - 索引缓存：按文本缓存 { 全拼, 全拼→字符位置映射, 首字母 }，高亮可精确映射回原字符。
 */

export interface PinyinIndex {
  /** 全拼（小写、无声调、无分隔），如 仪表盘 → "yibiaopan" */
  full: string
  /** full[i] 对应原文本的字符下标（高亮映射用） */
  fullMap: number[]
  /** 首字母串（每个字符一位），如 仪表盘 → "ybp" */
  initials: string
}

type PinyinFn = (text: string, options?: { toneType?: 'none' }) => string

const pinyinReady = ref(false)
let pinyinFn: PinyinFn | null = null
let loading: Promise<void> | null = null

/** CJK 统一表意文字基本区 */
const HAN_RE = /[\u4E00-\u9FFF]/
const cache = new Map<string, PinyinIndex | null>()

/** 懒加载 pinyin-pro（幂等；加载完成后翻转 pinyinReady 触发依赖方重算） */
export function ensurePinyin(): Promise<void> {
  if (pinyinFn) {
    return Promise.resolve()
  }
  loading ??= import('pinyin-pro')
    .then((mod) => {
      pinyinFn = mod.pinyin as PinyinFn
      cache.clear()
      pinyinReady.value = true
    })
    .catch(() => {
      loading = null
    }) as Promise<void>
  return loading
}

/**
 * 取文本的拼音索引；词典未就绪 / 文本不含汉字时返回 null。
 * 逐字符建索引以保证 全拼下标 → 原字符下标 的精确映射（结果按文本缓存）。
 */
export function getPinyinIndex(text: string): PinyinIndex | null {
  if (!pinyinFn || !HAN_RE.test(text)) {
    return null
  }
  let index = cache.get(text)
  if (index === undefined) {
    let full = ''
    const fullMap: number[] = []
    let initials = ''
    for (let i = 0; i < text.length; i++) {
      const char = text[i]!
      const syllable = HAN_RE.test(char) ? pinyinFn(char, { toneType: 'none' }).toLowerCase() : char.toLowerCase()
      initials += syllable[0] ?? ''
      for (const c of syllable) {
        full += c
        fullMap.push(i)
      }
    }
    index = { full, fullMap, initials }
    cache.set(text, index)
  }
  return index
}

/** 订阅词典就绪状态（computed 中 touch 此 ref 可在词典加载后自动重算） */
export function usePinyinReady() {
  return pinyinReady
}
