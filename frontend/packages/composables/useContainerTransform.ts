/**
 * 容器变形转场（Material Container Transform）—— 全局零侵入。
 *
 * 机制：
 * - 全局捕获 pointerdown，记录最近点击的「容器元素」（按钮 / 表格行 / 链接）矩形；
 * - MutationObserver 监听 NModal 挂载：若弹窗在点击后短窗口内出现，则生成一枚
 *   「幽灵容器」从点击源矩形飞向弹窗矩形（FLIP + WAAPI），末段淡出露出真实弹窗——
 *   视觉上点击的行/按钮「长大」成了弹窗；
 * - 弹窗卸载时反向播放：幽灵从弹窗矩形收回点击源位置。
 *
 * 弹窗在 Naive 进场缩放动画中测量会失真：用 offsetWidth/offsetHeight（布局尺寸，
 * 不受 transform 影响）+ 按计算样式的 transform 矩阵与 transform-origin 反解出布局位置。
 */

interface SourceCapture {
  rect: DOMRect
  time: number
}

/** 点击源识别选择器（就近匹配）：表格行优先于行内按钮之外的小元素 */
const SOURCE_SELECTOR = 'button, [role="button"], a, .n-data-table-tr, .n-card'

/** 点击 → 弹窗出现 的关联窗口（毫秒） */
const LINK_WINDOW = 800

const OPEN_DURATION = 300
const CLOSE_DURATION = 240
const EASING = 'cubic-bezier(0.3, 0.9, 0.3, 1)'

let installed = false
let lastSource: SourceCapture | null = null
/** 弹窗元素 → 关联的点击源/弹窗布局矩形（关闭反向动画用） */
const linked = new WeakMap<Element, { source: DOMRect, modal: DOMRect }>()

function reducedMotion(): boolean {
  return typeof window !== 'undefined' && window.matchMedia('(prefers-reduced-motion: reduce)').matches
}

/** 反解元素的「布局矩形」：消除进行中的 scale 变换（按 transform 矩阵与 origin 还原） */
function layoutRect(el: HTMLElement): DOMRect {
  const rect = el.getBoundingClientRect()
  const style = getComputedStyle(el)
  const matrix = new DOMMatrixReadOnly(style.transform === 'none' ? '' : style.transform)
  const sx = matrix.a || 1
  const sy = matrix.d || 1
  if (sx === 1 && sy === 1) {
    return rect
  }
  const [ox = 0, oy = 0] = style.transformOrigin.split(' ').map(Number.parseFloat)
  const left = rect.left + ox * sx - ox
  const top = rect.top + oy * sy - oy
  return new DOMRect(left, top, el.offsetWidth, el.offsetHeight)
}

/** 幽灵容器：定位在 to 矩形，从 from 矩形经 transform 飞入并末段淡出 */
function morph(from: DOMRect, to: DOMRect, duration: number): void {
  if (from.width <= 0 || to.width <= 0) {
    return
  }
  const ghost = document.createElement('div')
  Object.assign(ghost.style, {
    position: 'fixed',
    left: `${to.left}px`,
    top: `${to.top}px`,
    width: `${to.width}px`,
    height: `${to.height}px`,
    borderRadius: 'var(--radius-card, 10px)',
    background: 'hsl(var(--card))',
    border: '1px solid hsl(var(--border))',
    boxShadow: '0 18px 48px hsl(var(--foreground) / 18%)',
    zIndex: '9999',
    pointerEvents: 'none',
    willChange: 'transform, opacity',
  } as Partial<CSSStyleDeclaration>)
  document.body.appendChild(ghost)

  const dx = from.left - to.left
  const dy = from.top - to.top
  const sx = from.width / to.width
  const sy = from.height / to.height
  const animation = ghost.animate(
    [
      { transform: `translate(${dx}px, ${dy}px) scale(${sx}, ${sy})`, opacity: 0.96, transformOrigin: 'top left', offset: 0 },
      { opacity: 0.96, offset: 0.6 },
      { transform: 'translate(0, 0) scale(1, 1)', opacity: 0, transformOrigin: 'top left', offset: 1 },
    ],
    { duration, easing: EASING, fill: 'forwards' },
  )
  const cleanup = () => ghost.remove()
  animation.finished.then(cleanup).catch(cleanup)
}

function findModal(node: Node): HTMLElement | null {
  if (!(node instanceof HTMLElement)) {
    return null
  }
  if (node.classList.contains('n-modal')) {
    return node
  }
  return node.querySelector<HTMLElement>('.n-modal')
}

/**
 * 安装容器变形转场（幂等，应用内调用一次）。
 * options.enabled 为动态开关（如跟随「页面切换动画」偏好）。
 */
export function setupContainerTransform(options?: { enabled?: () => boolean }): void {
  if (installed || typeof document === 'undefined') {
    return
  }
  installed = true

  const enabled = () => (options?.enabled ? options.enabled() : true) && !reducedMotion()

  // 记录点击源（捕获期，任何 UI 框架处理前）
  document.addEventListener(
    'pointerdown',
    (e) => {
      const target = (e.target as HTMLElement | null)?.closest<HTMLElement>(SOURCE_SELECTOR)
      if (target) {
        lastSource = { rect: target.getBoundingClientRect(), time: Date.now() }
      }
    },
    true,
  )

  const onModalOpened = (modal: HTMLElement) => {
    if (!enabled() || !lastSource || Date.now() - lastSource.time > LINK_WINDOW) {
      return
    }
    const source = lastSource.rect
    // 双 rAF：等弹窗完成首帧布局后测量（transform 失真由 layoutRect 反解）
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        if (!modal.isConnected) {
          return
        }
        const modalRect = layoutRect(modal)
        if (modalRect.width <= 0) {
          return
        }
        linked.set(modal, { source, modal: modalRect })
        morph(source, modalRect, OPEN_DURATION)
      })
    })
  }

  const onModalClosed = (modal: HTMLElement) => {
    const link = linked.get(modal)
    if (!link || !enabled()) {
      return
    }
    linked.delete(modal)
    // 反向：从弹窗矩形收回点击源位置
    morph(link.modal, link.source, CLOSE_DURATION)
  }

  new MutationObserver((mutations) => {
    for (const mutation of mutations) {
      for (const node of mutation.addedNodes) {
        const modal = findModal(node)
        if (modal) {
          onModalOpened(modal)
        }
      }
      for (const node of mutation.removedNodes) {
        const modal = findModal(node)
        if (modal) {
          onModalClosed(modal)
        }
      }
    }
  }).observe(document.body, { childList: true })
}
