import { ref } from 'vue'

/**
 * 「飞入收藏夹」动画。
 *
 * 收藏夹按钮（AppFavorites）与触发收藏的标签栏（AppTabbar）是布局内的兄弟组件，
 * 这里用模块级单例桥接：AppFavorites 挂载时注册按钮 DOM 锚点，AppTabbar 收藏时
 * 调用 flyToFavorites 从标签位置飞向锚点。动画走 Web Animations API，零额外依赖。
 */

/** 收藏夹按钮的 DOM 锚点（飞行终点） */
const anchorEl = ref<HTMLElement | null>(null)
/** 按钮高亮脉冲计数（AppFavorites 监听，飞行命中时 +1 触发一次抖动） */
const pulseTick = ref(0)

/** AppFavorites 挂载 / 卸载时注册 / 注销按钮锚点 */
export function registerFavoritesAnchor(el: HTMLElement | null): void {
  anchorEl.value = el
}

/** AppFavorites 订阅按钮脉冲信号 */
export function useFavoritesPulse() {
  return pulseTick
}

function triggerPulse(): void {
  pulseTick.value += 1
}

interface FlyPayload {
  /** 飞行药丸上的文字（已翻译的标题） */
  label: string
}

/**
 * 从源矩形飞一枚药丸到收藏夹按钮，命中后触发按钮脉冲。
 * 缺少源矩形 / 锚点 / 非浏览器环境时退化为仅脉冲。
 */
export function flyToFavorites(source: DOMRect | null, payload: FlyPayload): void {
  const target = anchorEl.value
  if (!source || !target || typeof document === 'undefined' || typeof source.left !== 'number') {
    triggerPulse()
    return
  }

  const targetRect = target.getBoundingClientRect()
  const sx = source.left + source.width / 2
  const sy = source.top + source.height / 2
  const tx = targetRect.left + targetRect.width / 2
  const ty = targetRect.top + targetRect.height / 2
  const dx = tx - sx
  const dy = ty - sy

  const ghost = document.createElement('div')
  ghost.className = 'xh-fav-fly'
  ghost.textContent = payload.label
  Object.assign(ghost.style, {
    position: 'fixed',
    left: `${sx}px`,
    top: `${sy}px`,
    zIndex: '3000',
    maxWidth: '160px',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
    padding: '4px 12px',
    borderRadius: '9999px',
    fontSize: '13px',
    fontWeight: '500',
    lineHeight: '20px',
    color: 'hsl(var(--primary-foreground))',
    background: 'hsl(var(--primary))',
    boxShadow: '0 8px 24px hsl(var(--primary) / 45%)',
    pointerEvents: 'none',
    willChange: 'transform, opacity',
  } as Partial<CSSStyleDeclaration>)
  document.body.appendChild(ghost)

  const animation = ghost.animate(
    [
      { transform: 'translate(-50%, -50%) translate(0px, 0px) scale(1)', opacity: 1, offset: 0 },
      {
        transform: `translate(-50%, -50%) translate(${dx * 0.5}px, ${dy * 0.5 - 40}px) scale(0.82)`,
        opacity: 0.95,
        offset: 0.55,
      },
      {
        transform: `translate(-50%, -50%) translate(${dx}px, ${dy}px) scale(0.18)`,
        opacity: 0.1,
        offset: 1,
      },
    ],
    { duration: 560, easing: 'cubic-bezier(0.22, 1, 0.36, 1)', fill: 'forwards' },
  )

  let pulsed = false
  const finish = () => {
    if (!pulsed) {
      pulsed = true
      triggerPulse()
    }
    ghost.remove()
  }
  animation.finished.then(finish).catch(finish)
}
