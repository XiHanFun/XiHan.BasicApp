/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { getCurrentScope, onScopeDispose } from 'vue'

/** 序列里的一步：等这么多毫秒再往下走；序列被停掉时不再往下 */
export type SequenceWait = (milliseconds: number) => Promise<void>

export interface Sequence {
  /** 从头开始（正在跑的那一遍作废） */
  start: () => void
  /** 停下：还没到点的等待全部作废 */
  stop: () => void
}

const STOPPED = Symbol('sequence-stopped')

/**
 * 可取消的循环序列：把「做一步、等一会儿、再做一步」的编排写成一个 async 函数，跑完一遍自动从头再来
 * 用在示意动画、轮播这类按时间走的编排上；所在的作用域销毁时自动停下
 *
 * 只想跑一遍就停在最后一步：在函数末尾 `await wait(Number.POSITIVE_INFINITY)`
 * @param run 一遍的编排；每一步之间经 wait 等待
 * @returns 开始与停下
 */
export function useSequence(run: (wait: SequenceWait) => Promise<void>): Sequence {
  let token = 0
  const timers = new Set<ReturnType<typeof setTimeout>>()

  function stop(): void {
    token++
    for (const timer of timers)
      clearTimeout(timer)
    timers.clear()
  }

  function start(): void {
    stop()
    const mine = token
    /** 这一遍还作不作数：停下或重新开始之后就不作数了 */
    const alive = (): boolean => mine === token
    const wait: SequenceWait = milliseconds => new Promise((resolve, reject) => {
      // 无限等待：不排计时器，停下时这一遍就此搁置
      if (!Number.isFinite(milliseconds))
        return
      const timer = setTimeout(() => {
        timers.delete(timer)
        if (alive())
          resolve()
        else reject(STOPPED)
      }, milliseconds)
      timers.add(timer)
    })
    void (async () => {
      try {
        while (alive())
          await run(wait)
      }
      catch (error) {
        if (error !== STOPPED)
          throw error
      }
    })()
  }

  if (getCurrentScope())
    onScopeDispose(stop)

  return { start, stop }
}
