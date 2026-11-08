/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * localStorage 封装
 */
export const LocalStorage = {
  get<T = unknown>(key: string): T | null {
    try {
      const value = localStorage.getItem(key)
      return value
        ? (JSON.parse(value) as T)
        : null
    }
    catch {
      return null
    }
  },

  /**
   * 写入值；undefined 视为没有值，直接删键
   * 否则 setItem 会落盘字面量 'undefined'，has 判真而 get 返回 null，两个方法结论相反
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
   * 判断键是否存在，不看值能否解析
   * `set(key, null)` 是合法写入，键确实存在，不能改成 `get(key) !== null`
   * 只有绕过封装写入的空串或损坏 JSON 会 has 为真而 get 为 null：键在但内容读不出
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
      return value
        ? (JSON.parse(value) as T)
        : null
    }
    catch {
      return null
    }
  },

  /** 同 LocalStorage.set：undefined 视为没有值，直接删键 */
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
