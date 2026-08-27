/**
 * localStorage 封装
 */
export const LocalStorage = {
  get<T = unknown>(key: string): T | null {
    try {
      const value = localStorage.getItem(key)
      return value ? (JSON.parse(value) as T) : null
    }
    catch {
      return null
    }
  },

  /**
   * 写入 undefined 等同于「这个键没有值」，直接删键。
   *
   * JSON.stringify(undefined) 返回 undefined，setItem 会把它转成字面量字符串 'undefined' 落盘；
   * 之后 has 判真、get 解析失败返回 null，同一个键在两个方法上结论相反——
   * 以 has 做「是否已初始化」判断的调用方拿到 true，再 get 却是 null，走进未预期分支。
   */
  set(key: string, value: unknown): void {
    try {
      if (value === undefined) {
        localStorage.removeItem(key)
        return
      }
      localStorage.setItem(key, JSON.stringify(value))
    }
    catch {
      // storage full or unavailable
    }
  },

  remove(key: string): void {
    localStorage.removeItem(key)
  },

  clear(): void {
    localStorage.clear()
  },

  /**
   * 判断键是否存在。
   *
   * 有意保持「键存在性」而不是复用 get 的解析结果：`set(key, null)` 是一次合法写入，
   * 键确实存在，改成 `get(key) !== null` 会让它判假，反而制造新的不一致。
   * 经本封装写入的值，has 与 get 的结论一致；只有绕过封装直接 setItem 写入的空串 /
   * 损坏 JSON 才会出现 has 为真而 get 为 null——那是「键在，但内容读不出来」，两件事。
   */
  has(key: string): boolean {
    return localStorage.getItem(key) !== null
  },
}

/**
 * sessionStorage 封装
 */
export const SessionStorage = {
  get<T = unknown>(key: string): T | null {
    try {
      const value = sessionStorage.getItem(key)
      return value ? (JSON.parse(value) as T) : null
    }
    catch {
      return null
    }
  },

  /** 与 LocalStorage.set 同口径：undefined 视为「没有值」，删键而不是落盘字面量 'undefined' */
  set(key: string, value: unknown): void {
    try {
      if (value === undefined) {
        sessionStorage.removeItem(key)
        return
      }
      sessionStorage.setItem(key, JSON.stringify(value))
    }
    catch {
      // storage full or unavailable
    }
  },

  remove(key: string): void {
    sessionStorage.removeItem(key)
  },

  clear(): void {
    sessionStorage.clear()
  },
}

// 兼容旧代码命名（storage = localStorage 封装）
export const storage = LocalStorage
