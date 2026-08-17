/** 扩散动画的起点（通常取点击位置；缺省时从视口中心扩散） */
export interface ThemeTransitionOrigin {
  clientX: number
  clientY: number
}

export interface ThemeTransitionOptions {
  /** 切换后是否为暗色，决定扩散方向 */
  toDark: boolean
  /** 落地主题变更；在 ViewTransition 的回调内执行 */
  commit: () => Promise<void> | void
  /** 扩散起点，缺省从视口中心 */
  origin?: ThemeTransitionOrigin
  /** 关闭动画或浏览器不支持时，直接切换并抑制一帧 CSS 过渡 */
  enabled?: boolean
}

/**
 * 明暗切换的圆形扩散动画。
 *
 * 本机点击切换与其它设备推送来的偏好变更共用同一实现：远端推送没有点击位置，
 * 省略 origin 即从视口中心扩散，观感与本机一致。
 */
export async function runThemeTransition(options: ThemeTransitionOptions): Promise<void> {
  const { toDark, commit, origin, enabled = true } = options
  const root = document.documentElement

  // 无动画 / 浏览器不支持 / 页面不可见时直接切换，抑制 CSS 过渡一帧。
  // 后台标签页必须绕开：此时 ViewTransition 的 ready 可能长期不结算，
  // 而其它设备推来的偏好正等着它落地，卡住会让同步回写门一直关着。
  if (!enabled || !('startViewTransition' in document) || document.hidden) {
    root.classList.add('theme-switching')
    await commit()
    requestAnimationFrame(() => root.classList.remove('theme-switching'))
    return
  }

  const vw = window.innerWidth
  const vh = window.innerHeight
  const x = origin?.clientX ?? vw / 2
  const y = origin?.clientY ?? vh / 2
  // 覆盖全屏所需半径：取起点到最远视口角的距离
  const endRadius = Math.hypot(Math.max(x, vw - x), Math.max(y, vh - y))

  // 一律用百分比而非 px：::view-transition-* 伪元素的几何空间不跟随浏览器页面缩放，
  // 写 px 会被按缩放比整体压缩（圆心偏向左上），百分比相对伪元素自身盒子解析，与缩放无关。
  const xPercent = (x / vw) * 100
  const yPercent = (y / vh) * 100
  // circle() 的百分比半径按规范以 √(w²+h²)/√2 为参照解析
  const radiusPercent = (endRadius / (Math.hypot(vw, vh) / Math.SQRT2)) * 100

  // clipPath 起止：从起点 0 → 全屏
  const clipPath = [
    `circle(0% at ${xPercent}% ${yPercent}%)`,
    `circle(${radiusPercent}% at ${xPercent}% ${yPercent}%)`,
  ]

  // 全程抑制 CSS transition，防止截图期间元素颜色渐变产生残影
  root.classList.add('theme-switching')

  let commitDone = () => {}
  const committed = new Promise<void>((resolve) => {
    commitDone = resolve
  })

  const transition = (
    document as Document & {
      startViewTransition: (cb: () => Promise<void>) => {
        ready: Promise<void>
        finished: Promise<void>
        skipTransition?: () => void
      }
    }
  ).startViewTransition(async () => {
    await commit()
    commitDone()
  })

  const playReveal = async () => {
    try {
      await transition.ready
      // 切暗色 → 旧层（亮）在上，全屏 → 0 收缩（z-index 由 html.dark CSS 类自动控制）
      // 切亮色 → 新层（亮）在上，0 → 全屏 扩散
      const anim = root.animate(
        { clipPath: toDark ? [...clipPath].reverse() : clipPath },
        {
          duration: 450,
          easing: 'ease-in',
          pseudoElement: toDark ? '::view-transition-old(root)' : '::view-transition-new(root)',
        } as KeyframeAnimationOptions,
      )
      anim.onfinish = () => {
        // 动画结束后立即跳过剩余 ViewTransition，消除尾帧闪烁
        transition.skipTransition?.()
        root.classList.remove('theme-switching')
      }
    }
    catch {
      root.classList.remove('theme-switching')
    }
  }

  // 调用方只等「值已落地」，不等动画播完：
  // 远端偏好应用若一路等到 450ms 动画结束才恢复回写门，期间的本机改动会丢失上行
  void playReveal()
  await committed
}
