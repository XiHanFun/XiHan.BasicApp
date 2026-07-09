import type { ComponentPublicInstance, CSSProperties } from 'vue'
import type { HeaderMode } from '../contracts'
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useContentMaximize } from '~/hooks'
import { useAppStore, useLayoutBridgeStore, useLayoutPreferences } from '~/stores'
import { useLayout } from './use-layout'

export function useLayoutShellAdapter() {
  const SIDEBAR_COLLAPSE_WIDTH = 60
  const SIDEBAR_MIXED_WIDTH = 80
  const HEADER_TRIGGER_DISTANCE = 12
  const Z_INDEX_BASE = 200

  const appStore = useAppStore()
  const layoutBridgeStore = useLayoutBridgeStore()
  const layoutPreferences = useLayoutPreferences()
  const route = useRoute()
  const { contentIsMaximize: contentMaximized } = useContentMaximize()

  const viewportWidth = ref(typeof window !== 'undefined' ? window.innerWidth : 1200)
  const isMobile = computed(() => viewportWidth.value < 768)
  const isNarrowScreen = computed(() => viewportWidth.value < 960)

  const {
    currentLayout,
    isFullContent,
    isHeaderNav,
    isMixedNav,
    isSideMixedNav,
    isHeaderMixedNav,
    isHeaderSidebarNav,
    isSideMode,
    isDualColumnMode,
    showHeaderNav,
  } = useLayout(() => (isMobile.value ? 'side' : layoutPreferences.layoutMode.value))

  const sidebarCollapse = ref(appStore.sidebarCollapsed)
  const sidebarExpandOnHovering = ref(false)
  const headerIsHidden = ref(false)
  const mobileSidebarOpen = ref(false)
  const scrollY = ref(0)
  const contentScrollEl = ref<HTMLElement | null>(null)
  const mouseY = ref(0)

  const sidebarExtraVisible = ref(false)
  const sidebarExtraCollapse = ref(false)

  const headerMode = computed<HeaderMode>(() => appStore.headerMode as HeaderMode)
  const isHeaderAutoMode = computed(() => headerMode.value === 'auto')
  const isHeaderAutoScrollMode = computed(() => headerMode.value === 'auto-scroll')

  const headerHeight = computed(() => 50)
  const tabbarHeight = computed(() => 40)
  const footerHeight = computed(() => {
    if (isNarrowScreen.value) {
      return appStore.footerShowDevInfo ? 36 : 24
    }
    return 32
  })

  const headerWrapperHeight = computed(() => {
    let height = 0
    if (appStore.headerShow && !isFullContent.value && !contentMaximized.value) {
      height += headerHeight.value
    }
    if (appStore.tabbarEnabled && !isFullContent.value) {
      height += tabbarHeight.value
    }
    return height
  })

  const sidebarWidth = computed(() => appStore.sidebarWidth)

  const getSideCollapseWidth = computed(() => {
    return SIDEBAR_MIXED_WIDTH
  })

  const sidebarEnableState = computed(() => {
    if (isMobile.value)
      return true
    return !isHeaderNav.value && appStore.sidebarShow
  })

  const sidebarMarginTop = computed(() => {
    return isMixedNav.value && !isMobile.value ? headerHeight.value : 0
  })

  const getSidebarWidth = computed(() => {
    if (contentMaximized.value)
      return 0
    if (!appStore.sidebarShow)
      return 0
    if (!sidebarEnableState.value)
      return 0

    if ((isHeaderMixedNav.value || isSideMixedNav.value) && !isMobile.value) {
      return SIDEBAR_MIXED_WIDTH
    }
    if (sidebarCollapse.value) {
      return isMobile.value ? 0 : getSideCollapseWidth.value
    }
    return sidebarWidth.value
  })

  const sidebarExtraWidth = computed(() => {
    return sidebarExtraCollapse.value ? SIDEBAR_COLLAPSE_WIDTH : sidebarWidth.value
  })

  const headerFixed = computed(() => {
    return (
      isMixedNav.value
      || headerMode.value === 'fixed'
      || isHeaderAutoScrollMode.value
      || isHeaderAutoMode.value
    )
  })

  const showSidebar = computed(() => {
    return isSideMode.value && sidebarEnableState.value && !appStore.sidebarShow === false
  })

  const maskVisible = computed(() => mobileSidebarOpen.value && isMobile.value)

  const mainStyle = computed(() => {
    let width = '100%'
    let sidebarAndExtraWidth = '0'

    if (
      headerFixed.value
      && currentLayout.value !== 'top'
      && currentLayout.value !== 'mix'
      && currentLayout.value !== 'header-sidebar'
      && showSidebar.value
      && !isMobile.value
    ) {
      const isSideNavEffective
        = (isSideMixedNav.value || isHeaderMixedNav.value)
          && appStore.sidebarExpandOnHover
          && sidebarExtraVisible.value

      if (isSideNavEffective) {
        const sideCollapseW = sidebarCollapse.value
          ? getSideCollapseWidth.value
          : SIDEBAR_MIXED_WIDTH
        const sideW = sidebarExtraCollapse.value ? SIDEBAR_COLLAPSE_WIDTH : sidebarWidth.value
        sidebarAndExtraWidth = `${sideCollapseW + sideW}px`
        width = `calc(100% - ${sidebarAndExtraWidth})`
      }
      else {
        sidebarAndExtraWidth
          = sidebarExpandOnHovering.value && !appStore.sidebarExpandOnHover
            ? `${getSideCollapseWidth.value}px`
            : `${getSidebarWidth.value}px`
        width = `calc(100% - ${sidebarAndExtraWidth})`
      }
    }
    return { sidebarAndExtraWidth, width }
  })

  const tabbarStyle = computed((): CSSProperties => {
    let width = ''
    let marginLeft = 0
    const maximized = contentMaximized.value

    if (maximized || !isMixedNav.value || !appStore.sidebarShow) {
      width = '100%'
    }
    else if (sidebarEnableState.value) {
      const onHoveringWidth = appStore.sidebarExpandOnHover
        ? sidebarWidth.value
        : getSideCollapseWidth.value

      marginLeft = sidebarCollapse.value ? getSideCollapseWidth.value : onHoveringWidth

      width = `calc(100% - ${sidebarCollapse.value ? getSidebarWidth.value : onHoveringWidth}px)`
    }
    else {
      width = '100%'
    }

    return { marginLeft: `${marginLeft}px`, width }
  })

  const contentStyle = computed((): CSSProperties => {
    const fixed = headerFixed.value
    const maximized = contentMaximized.value
    const marginTop = maximized
      ? (appStore.tabbarEnabled && !isFullContent.value ? `${tabbarHeight.value}px` : '0')
      : (
          fixed
          && !isFullContent.value
          && !headerIsHidden.value
          && (!isHeaderAutoMode.value || scrollY.value < headerWrapperHeight.value)
            ? `${headerWrapperHeight.value}px`
            : '0'
        )
    return {
      marginTop,
      paddingBottom: `${appStore.footerEnable && appStore.footerFixed && !maximized ? footerHeight.value : 0}px`,
    }
  })

  const headerZIndex = computed(() => {
    const offset = isMixedNav.value ? 1 : 0
    return Z_INDEX_BASE + offset
  })

  const headerWrapperStyle = computed((): CSSProperties => {
    const fixed = headerFixed.value
    const maximized = contentMaximized.value
    return {
      height: isFullContent.value ? '0' : `${headerWrapperHeight.value}px`,
      left: maximized ? '0' : (isMixedNav.value ? '0' : mainStyle.value.sidebarAndExtraWidth),
      position: fixed || maximized ? 'fixed' : 'static',
      top: maximized
        ? '0'
        : (headerIsHidden.value || isFullContent.value ? `-${headerWrapperHeight.value}px` : '0'),
      transition: maximized ? 'none' : undefined,
      width: maximized ? '100%' : mainStyle.value.width,
      zIndex: maximized ? Z_INDEX_BASE + 10 : headerZIndex.value,
    }
  })

  const sidebarZIndex = computed(() => {
    let offset = isMobile.value || isSideMode.value ? 1 : -1
    if (isMixedNav.value)
      offset += 1
    return Z_INDEX_BASE + offset
  })

  const footerWidth = computed(() => {
    if (!appStore.footerFixed)
      return '100%'
    return mainStyle.value.width
  })

  const showHeaderToggleButton = computed(() => {
    return (
      isMobile.value
      || (appStore.widgetSidebarToggle
        && isSideMode.value
        && !isSideMixedNav.value
        && !isMixedNav.value
        && !isMobile.value)
    )
  })

  const showHeaderLogo = computed(() => {
    return !isSideMode.value || isMixedNav.value || isMobile.value
  })

  const headerHasShadow = computed(() => scrollY.value > 20)
  const transitionName = computed(() => (appStore.transitionEnable ? appStore.transitionName : ''))

  const effectiveCollapsed = computed(() => {
    if (isMobile.value || isDualColumnMode.value)
      return false
    if (!appStore.sidebarExpandOnHover && sidebarCollapse.value) {
      return !sidebarExpandOnHovering.value
    }
    return sidebarCollapse.value
  })

  const floatingSidebarMode = computed(() => {
    return (
      !appStore.sidebarExpandOnHover
      && sidebarCollapse.value
      && !isMobile.value
      && !isDualColumnMode.value
    )
  })

  const floatingSidebarExpand = computed(() => {
    return floatingSidebarMode.value && sidebarExpandOnHovering.value
  })

  const expandedSidebarWidth = computed(() => {
    return currentLayout.value === 'mix' ? 224 : sidebarWidth.value
  })

  const siderWidth = computed(() => {
    if (isMobile.value)
      return expandedSidebarWidth.value
    if (isDualColumnMode.value)
      return SIDEBAR_MIXED_WIDTH + appStore.sidebarWidth
    if (floatingSidebarMode.value && floatingSidebarExpand.value)
      return expandedSidebarWidth.value
    if (floatingSidebarMode.value)
      return getSideCollapseWidth.value
    if (effectiveCollapsed.value)
      return getSideCollapseWidth.value
    return currentLayout.value === 'mix' ? 224 : sidebarWidth.value
  })

  const showSider = computed(() => {
    if (isMobile.value)
      return mobileSidebarOpen.value
    return (
      !contentMaximized.value
      && !isFullContent.value
      && currentLayout.value !== 'top'
      && appStore.sidebarShow
    )
  })

  // --- Header auto-hide ---
  function handleAutoHideHeader() {
    if (!isHeaderAutoMode.value || isMixedNav.value || isFullContent.value) {
      if (headerMode.value !== 'auto-scroll') {
        headerIsHidden.value = false
      }
      return
    }
    const isInTriggerZone = mouseY.value <= HEADER_TRIGGER_DISTANCE
    const isInHeaderZone = !headerIsHidden.value && mouseY.value <= headerWrapperHeight.value
    headerIsHidden.value = !(isInTriggerZone || isInHeaderZone)
  }

  let lastScrollDirection: 'down' | 'up' | null = null
  let lastScrollY = 0

  function handleAutoScrollHeader() {
    if (headerMode.value !== 'auto-scroll' || isMixedNav.value || isFullContent.value)
      return

    if (scrollY.value < headerWrapperHeight.value) {
      headerIsHidden.value = false
      return
    }

    const direction = scrollY.value > lastScrollY ? 'down' : 'up'
    lastScrollY = scrollY.value

    if (direction !== lastScrollDirection) {
      lastScrollDirection = direction
      headerIsHidden.value = direction === 'down'
    }
  }

  // --- Event handlers ---
  function handleSidebarMouseEnter(e: MouseEvent) {
    if (e?.offsetX < 10)
      return
    if (appStore.sidebarExpandOnHover)
      return
    if (!sidebarExpandOnHovering.value) {
      sidebarCollapse.value = false
    }
    sidebarExpandOnHovering.value = true
  }

  function handleSidebarMouseLeave() {
    if (appStore.sidebarExpandOnHover)
      return
    sidebarExpandOnHovering.value = false
    sidebarCollapse.value = true
    sidebarExtraVisible.value = false
  }

  function handleClickMask() {
    sidebarCollapse.value = true
    mobileSidebarOpen.value = false
  }

  function handleHeaderToggle() {
    if (isMobile.value) {
      mobileSidebarOpen.value = !mobileSidebarOpen.value
    }
    else {
      appStore.setSidebarShow(!appStore.sidebarShow)
    }
  }

  function handleSidebarToggleRequest() {
    if (isNarrowScreen.value) {
      mobileSidebarOpen.value = !mobileSidebarOpen.value
      return
    }
    sidebarCollapse.value = !sidebarCollapse.value
    appStore.setSidebarCollapsed(sidebarCollapse.value)
  }

  function closeMobileSidebar() {
    mobileSidebarOpen.value = false
  }

  function updateViewportWidth() {
    viewportWidth.value = window.innerWidth
    if (!isNarrowScreen.value) {
      mobileSidebarOpen.value = false
    }
  }

  function handleScroll() {
    scrollY.value = contentScrollEl.value?.scrollTop ?? 0
    handleAutoScrollHeader()
  }

  // 内容滚动容器由布局通过 :ref 注入；滚动搬入容器后，滚动源改读容器 scrollTop（back-top/顶栏阴影/自动隐藏都依赖它）
  function setContentScrollEl(el: ComponentPublicInstance | Element | null) {
    const next = (el as HTMLElement) ?? null
    if (next === contentScrollEl.value)
      return
    contentScrollEl.value?.removeEventListener('scroll', handleScroll)
    contentScrollEl.value = next
    next?.addEventListener('scroll', handleScroll, { passive: true })
    handleScroll()
  }

  function scrollContentToTop() {
    contentScrollEl.value?.scrollTo({ top: 0, behavior: 'smooth' })
  }

  function handleMouseMove(e: MouseEvent) {
    mouseY.value = e.clientY
    handleAutoHideHeader()
  }

  // --- Watchers ---
  watch(
    () => appStore.sidebarCollapsed,
    (v) => {
      sidebarCollapse.value = v
    },
  )

  watch(sidebarCollapse, (v) => {
    // 移动端强制折叠不写回持久化偏好，否则会覆盖桌面端的展开/折叠选择
    if (!isMobile.value && v !== appStore.sidebarCollapsed) {
      appStore.setSidebarCollapsed(v)
    }
    if (v) {
      sidebarExpandOnHovering.value = false
    }
  })

  // 进入移动端前的桌面折叠态快照，回到桌面端时据此恢复
  let desktopSidebarCollapse = appStore.sidebarCollapsed

  watch(
    () => isMobile.value,
    (val, oldVal) => {
      if (val) {
        // 进入移动端：先记录桌面折叠态，再强制折叠（仅本地状态，不持久化）
        if (oldVal === false)
          desktopSidebarCollapse = sidebarCollapse.value
        sidebarCollapse.value = true
      }
      else {
        // 回到桌面端：恢复进入移动端前的折叠态
        sidebarCollapse.value = desktopSidebarCollapse
      }
    },
    { immediate: true },
  )

  watch(
    () => route.fullPath,
    () => {
      if (isNarrowScreen.value)
        mobileSidebarOpen.value = false
      // 滚动搬入内容容器后，路由切换手动重置到顶（原 router window scrollBehavior 已失效）
      contentScrollEl.value?.scrollTo({ top: 0 })
    },
  )

  watch(
    () => layoutBridgeStore.sidebarToggleVersion,
    () => {
      handleSidebarToggleRequest()
    },
  )

  onMounted(() => {
    updateViewportWidth()
    window.addEventListener('resize', updateViewportWidth)
    window.addEventListener('mousemove', handleMouseMove, { passive: true })
  })

  onBeforeUnmount(() => {
    window.removeEventListener('resize', updateViewportWidth)
    contentScrollEl.value?.removeEventListener('scroll', handleScroll)
    window.removeEventListener('mousemove', handleMouseMove)
  })

  return {
    appStore,
    contentMaximized,
    currentLayout,
    isFullContent,
    isHeaderNav,
    isMixedNav,
    isSideMixedNav,
    isHeaderMixedNav,
    isHeaderSidebarNav,
    isSideMode,
    isDualColumnMode,
    showHeaderNav,

    isMobile,
    isNarrowScreen,
    mobileSidebarOpen,
    sidebarCollapse,
    sidebarExpandOnHovering,
    sidebarExtraVisible,
    sidebarExtraCollapse,
    headerIsHidden,
    scrollY,
    setContentScrollEl,
    scrollContentToTop,

    headerHeight,
    tabbarHeight,
    footerHeight,
    headerWrapperHeight,
    headerFixed,
    headerZIndex,
    headerWrapperStyle,
    headerHasShadow,
    showHeaderToggleButton,
    showHeaderLogo,

    getSideCollapseWidth,
    getSidebarWidth,
    sidebarMarginTop,
    sidebarZIndex,
    sidebarExtraWidth,
    showSidebar,
    showSider,
    siderWidth,
    effectiveCollapsed,
    floatingSidebarMode,
    floatingSidebarExpand,
    expandedSidebarWidth,
    maskVisible,

    mainStyle,
    tabbarStyle,
    contentStyle,
    footerWidth,
    transitionName,

    handleSidebarMouseEnter,
    handleSidebarMouseLeave,
    handleClickMask,
    handleHeaderToggle,
    closeMobileSidebar,
  }
}
