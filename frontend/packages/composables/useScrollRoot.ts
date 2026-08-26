import { shallowRef } from 'vue'

/**
 * 真正在滚的那个元素。
 *
 * 本应用把滚动搬进了内容容器，body 自己不滚。组件库的滚动锁默认探 body，
 * 不告诉它就锁不住——模态浮层开着的时候，背后的内容照样能滚。
 * 布局挂载内容容器时登记，卸载时撤；登录等没有布局壳的页面取到 null，
 * 组件库按它自己的探测走。
 *
 * 存成 ref 而不是裸变量：要跟着滚动源换人重新接线的地方（回到顶部）直接 watch 它，
 * 不必再拿别的量当变化信号。
 */
const scrollRootEl = shallowRef<HTMLElement | null>(null)

/** 供 watch 的登记本体 */
export const scrollRootRef = scrollRootEl

export function setScrollRoot(el: HTMLElement | null): void {
  scrollRootEl.value = el
}

export function getScrollRoot(): HTMLElement | null {
  return scrollRootEl.value
}
