/**
 * 真正在滚的那个元素。
 *
 * 本应用把滚动搬进了内容容器，body 自己不滚。组件库的滚动锁默认探 body，
 * 不告诉它就锁不住——模态浮层开着的时候，背后的内容照样能滚。
 * 布局挂载内容容器时登记，卸载时撤；登录等没有布局壳的页面取到 null，
 * 组件库按它自己的探测走。
 */
let scrollRootEl: HTMLElement | null = null

export function setScrollRoot(el: HTMLElement | null): void {
  scrollRootEl = el
}

export function getScrollRoot(): HTMLElement | null {
  return scrollRootEl
}
