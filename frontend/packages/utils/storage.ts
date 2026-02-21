/**
 * localStorage 封装
 */
export const storage = {
  get<T = any>(key: string): T | null {
    try {
      const value = localStorage.getItem(key)
      return value ? (JSON.parse(value) as T) : null
    } catch {
      return null
    }
  },

  set(key: string, value: any): void {
    try {
      localStorage.setItem(key, JSON.stringify(value))
    } catch {
      // storage full or unavailable
    }
  },

  remove(key: string): void {
    localStorage.removeItem(key)
  },

  clear(): void {
    localStorage.clear()
  },

  has(key: string): boolean {
    return localStorage.getItem(key) !== null
  },
}

/**
 * sessionStorage 封装
 */
export const sessionStorage_ = {
  get<T = any>(key: string): T | null {
    try {
      const value = sessionStorage.getItem(key)
      return value ? (JSON.parse(value) as T) : null
    } catch {
      return null
    }
  },

  set(key: string, value: any): void {
    try {
      sessionStorage.setItem(key, JSON.stringify(value))
    } catch {
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
