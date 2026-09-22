/**
 * 页面切换动画（进场）。
 *
 * 为什么不交给 CSS 过渡类：Vue 的 <Transition> 靠在页面根元素上增删 enter-from、
 * enter-active、enter-to 三个类完成一次进场，一次导航就是五次 class 变更。而 XiHan.UI 的
 * Portal 会给每个浮层装一座「视觉环境桥」，桥在来源的整条祖先链上挂 MutationObserver，
 * 祖先只要改 class 就重算一次环境——重算要把来源、壳父节点与文档根三份计算样式里的
 * 自定义属性整张表读一遍。页面根正是页内所有浮层（下拉、气泡、提示、列设置…）的祖先，
 * 于是每加一次类就换来十来万次 getComputedStyle().getPropertyValue()。
 * 实测（权限管理页，门户根上 61 个宿主）：往页面根上加一个 class 触发 66 次门户同步、
 * 121108 次读取、约 28ms 主线程；一次进场五次 class 变更，整次切页阻塞 217ms → 421ms。
 *
 * WAAPI 不碰 class、也不碰内联 style，桥一次都不会醒。动画效果与时长曲线不变——
 * 关键帧与这里的时长/曲线令牌一一对应原来 transition.css 里的那张表。
 */

/** 一种页面动画：进场起始帧，外加取哪一档时长与哪条曲线 */
interface PageMotion {
  /** 起始帧。终止帧由下面按键名反推（transform → none、filter → none、opacity → 1） */
  from: Keyframe
  /** 时长令牌：快档给纯淡入，常规档给带位移与形变的 */
  duration: '--transition-fast' | '--transition-normal'
  easing: string
}

/** 偏好设置里那十二种，名字与 preference.general.animation.* 一一对应 */
const PAGE_MOTIONS: Record<string, PageMotion> = {
  'fade': {
    from: { opacity: 0 },
    duration: '--transition-fast',
    easing: '--xh-motion-ease-enter',
  },
  'slide-left': {
    from: { opacity: 0, transform: 'translateX(30px)' },
    duration: '--transition-normal',
    easing: '--xh-motion-ease-slide',
  },
  'slide-right': {
    from: { opacity: 0, transform: 'translateX(-30px)' },
    duration: '--transition-normal',
    easing: '--xh-motion-ease-slide',
  },
  'slide-up': {
    from: { opacity: 0, transform: 'translateY(30px)' },
    duration: '--transition-normal',
    easing: '--xh-motion-ease-slide',
  },
  'slide-down': {
    from: { opacity: 0, transform: 'translateY(-30px)' },
    duration: '--transition-normal',
    easing: '--xh-motion-ease-slide',
  },
  'skew-slide': {
    from: { opacity: 0, transform: 'translateX(30px) skewX(-8deg)' },
    duration: '--transition-normal',
    easing: '--xh-motion-ease-slide',
  },
  'zoom-fade': {
    from: { opacity: 0, transform: 'scale(0.96)' },
    duration: '--transition-normal',
    easing: '--xh-motion-ease-enter-strong',
  },
  'scale-up': {
    from: { opacity: 0, transform: 'scale(0.92)' },
    duration: '--transition-normal',
    easing: '--xh-motion-ease-enter-strong',
  },
  'scale-down': {
    from: { opacity: 0, transform: 'scale(1.08)' },
    duration: '--transition-normal',
    easing: '--xh-motion-ease-enter-strong',
  },
  'blur-fade': {
    from: { opacity: 0, filter: 'blur(4px)' },
    duration: '--transition-normal',
    easing: '--xh-motion-ease-enter',
  },
  'rotate-fade': {
    from: { opacity: 0, transform: 'rotate(-6deg) scale(0.96)' },
    duration: '--transition-normal',
    easing: '--xh-motion-ease-enter',
  },
  'flip-fade': {
    from: { opacity: 0, transform: 'perspective(1200px) rotateY(-10deg)' },
    duration: '--transition-normal',
    easing: '--xh-motion-ease-enter',
  },
}

/** 终止帧：起始帧改过哪几路就把哪几路归位，别的属性不写进关键帧，免得压住页面自己的样式 */
function restFrameOf(from: Keyframe): Keyframe {
  const rest: Keyframe = {}
  if ('opacity' in from) {
    rest.opacity = 1
  }
  if ('transform' in from) {
    rest.transform = 'none'
  }
  if ('filter' in from) {
    rest.filter = 'none'
  }
  return rest
}

/** 令牌里的时长（"320ms" / "0.32s"）取成毫秒；取不到或非正数按不播处理 */
function durationOf(styles: CSSStyleDeclaration, token: string): number {
  const raw = styles.getPropertyValue(token).trim()
  if (!raw) {
    return 0
  }
  const value = Number.parseFloat(raw)
  if (!Number.isFinite(value)) {
    return 0
  }
  return raw.endsWith('ms') ? value : value * 1000
}

/**
 * 播一次进场动画，播完（或不该播）调 done。
 *
 * 不认得的动画名、动画关掉时传来的空名、以及「减少动态效果」下被令牌层压到 1ms 的时长，
 * 都直接收工：Vue 在 css=false 下等的就是这个回调，不调它页面会一直挂在离场队列里。
 */
export function playPageEnter(el: Element, name: string, done: () => void): void {
  const motion = PAGE_MOTIONS[name]
  if (!motion || typeof el.animate !== 'function') {
    done()
    return
  }
  const styles = getComputedStyle(el)
  const duration = durationOf(styles, motion.duration)
  if (duration <= 1) {
    done()
    return
  }
  const easing = styles.getPropertyValue(motion.easing).trim() || 'ease'
  const animation = el.animate([motion.from, restFrameOf(motion.from)], {
    duration,
    easing,
    // fill 留 none：动画结束即交还页面自身的样式，不在元素上留下任何形变
    fill: 'none',
  })
  animation.finished.then(done, done)
}
