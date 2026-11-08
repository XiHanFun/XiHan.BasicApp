/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * 页面切换动画（进场与离场）
 *
 * 为什么不交给 CSS 过渡类：Vue 的 <Transition> 靠在页面根元素上增删 enter-from、
 * enter-active、enter-to 三个类完成一次进场，一次导航就是五次 class 变更。而 XiHan.UI 的
 * Portal 会给每个浮层装一座「视觉环境桥」，桥在来源的整条祖先链上挂 MutationObserver，
 * 祖先只要改 class 就重算一次环境——重算要把来源、壳父节点与文档根三份计算样式里的
 * 自定义属性整张表读一遍。页面根正是页内所有浮层（下拉、气泡、提示、列设置…）的祖先，
 * 于是每加一次类就换来十来万次 getComputedStyle().getPropertyValue()
 * 实测（权限管理页，门户根上 61 个宿主）：往页面根上加一个 class 触发 66 次门户同步、
 * 121108 次读取、约 28ms 主线程；一次进场五次 class 变更，整次切页阻塞 217ms → 421ms
 *
 * （「改 class 就重算」是退化态：页面上有一张读不到规则的跨域样式表时，桥的样式索引整份作废、
 * class 判据随之失效。md-editor 的 CDN 样式表改为 CORS 加载后索引可用，无关的过渡类不再惊动桥。）
 *
 * WAAPI 不碰 class，桥一次都不会醒，不必依赖索引是否可用。离场要把旧页钉成绝对定位，写的是
 * 不含自定义属性的内联样式，桥同样不理会。动画效果与时长曲线不变——关键帧与这里的
 * 时长/曲线令牌一一对应原来 transition.css 里的那张表
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

/** 终止帧只归位起始帧改过的属性，免得压住页面自己的样式 */
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
  return raw.endsWith('ms')
    ? value
    : value * 1000
}

/**
 * 播一次进场动画，播完或不该播都调 done
 * 未知动画名、空名、减弱动效下 1ms 的时长、页面在后台（finished 未必结算）都直接收工
 * Vue 在 css=false 下等这个回调，不调页面会一直挂在离场队列里
 */
export function playPageEnter(el: Element, name: string, done: () => void): void {
  cancelPageLeave(el)
  const motion = PAGE_MOTIONS[name]
  if (!motion || typeof el.animate !== 'function' || document.hidden) {
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
  const animation = el.animate([
    motion.from,
    restFrameOf(motion.from),
  ], {
    duration,
    easing,
    // fill 留 none：动画结束即交还页面自身的样式，不在元素上留下任何形变
    fill: 'none',
  })
  animation.finished.then(done, done)
}

/** 正在离场的旧页：动画与「把钉住它的那几条 inline 样式还原」的收尾 */
const leaving = new WeakMap<Element, { animation: Animation, release: () => void }>()

/** 离场期间旧页脱离文档流叠在新页上面，不把新页往下挤；收尾时逐条还原成接管前的值 */
const PINNED_STYLES = {
  position: 'absolute',
  insetBlockStart: '0',
  insetInlineStart: '0',
  inlineSize: '100%',
  pointerEvents: 'none',
} as const

function pin(el: HTMLElement): () => void {
  const style = el.style
  const before = Object.fromEntries(Object.keys(PINNED_STYLES).map(key => [key, style[key as keyof typeof PINNED_STYLES]]))
  Object.assign(style, PINNED_STYLES)
  return () => {
    Object.assign(style, before)
  }
}

/**
 * 播一次离场：旧页原地淡出，新页同时在底下进场
 * 只淡不移，方向感由进场给；时长取更短的退场档，减弱动效下仍淡变
 * KeepAlive 缓存页在离场结束时被搬进缓存容器，钉住的样式须先还原，否则再激活仍是绝对定位、透明的
 */
export function playPageLeave(el: Element, name: string, done: () => void): void {
  // 后台标签页不出帧，动画的 finished 可能一直不结算，旧页就一直钉在新页上面：直接收尾
  if (!PAGE_MOTIONS[name] || typeof el.animate !== 'function' || !(el instanceof HTMLElement) || document.hidden) {
    done()
    return
  }
  const styles = getComputedStyle(el)
  const duration = durationOf(styles, '--xh-motion-duration-exit')
  if (duration <= 1) {
    done()
    return
  }
  const release = pin(el)
  const animation = el.animate([
    { opacity: 1 },
    { opacity: 0 },
  ], {
    duration,
    easing: styles.getPropertyValue('--xh-motion-ease-exit').trim() || 'ease-in',
    // 播完到 Vue 摘掉它之间不回弹成不透明
    fill: 'forwards',
  })
  leaving.set(el, { animation, release })
  animation.finished.then(() => {
    if (leaving.get(el)?.animation !== animation)
      return
    leaving.delete(el)
    // 还原与摘除同在这一个任务里，中间不出帧，看不到回弹
    release()
    animation.cancel()
    done()
  }, () => {})
}

/**
 * 离场没播完又被切回时撤掉淡出、还原钉住的样式；已播完的不处理
 * 同 key 缓存页由 Vue 经 after-leave 提前收掉离场，其余走 leave-cancelled，进场开头再兜一次
 */
export function cancelPageLeave(el: Element): void {
  const entry = leaving.get(el)
  if (!entry)
    return
  leaving.delete(el)
  entry.animation.cancel()
  entry.release()
}
