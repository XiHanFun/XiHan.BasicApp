import type { DropdownOption } from 'naive-ui'
import type { TabItem } from '~/types'
import { Icon } from '@iconify/vue'
import { NIcon } from 'naive-ui'
import { h } from 'vue'
import { HOME_PATH } from '~/constants'

export function createDropdownIcon(icon: string) {
  return () => h(NIcon, { size: 16 }, { default: () => h(Icon, { icon }) })
}

export function getTabByPath(tabs: TabItem[], path: string) {
  return tabs.find((item) => item.path === path)
}

export function getTabDisableState(tabs: TabItem[], path: string, closable: boolean) {
  const currentIndex = tabs.findIndex((item) => item.path === path)
  const leftTabs = tabs.slice(0, currentIndex)
  const rightTabs = tabs.slice(currentIndex + 1)
  const currentTab = getTabByPath(tabs, path)

  const hasLeftClosable = leftTabs.some((item) => item.closable)
  const hasRightClosable = rightTabs.some((item) => item.closable)
  const hasOtherClosable = tabs.some((item) => item.closable && item.path !== path)
  const hasAnyClosable = tabs.some((item) => item.closable)

  return {
    closeAllDisabled: !hasAnyClosable,
    closeCurrentDisabled: !closable,
    closeLeftDisabled: !hasLeftClosable,
    closeOthersDisabled: !hasOtherClosable,
    closeRightDisabled: !hasRightClosable,
    pinDisabled: currentTab?.path === HOME_PATH,
  }
}

export function buildTabContextOptions(params: {
  path: string
  closable: boolean
  pinned: boolean
  tabs: TabItem[]
  isContentMaximized: boolean
  t: (key: string) => string
}) {
  const { path, closable, pinned, tabs, isContentMaximized, t } = params
  const {
    closeAllDisabled,
    closeCurrentDisabled,
    closeLeftDisabled,
    closeOthersDisabled,
    closeRightDisabled,
    pinDisabled,
  } = getTabDisableState(tabs, path, closable)

  return [
    { key: 'reload', label: t('tabbar.reload'), icon: createDropdownIcon('lucide:refresh-cw') },
    { key: 'open', label: t('tabbar.open'), icon: createDropdownIcon('lucide:external-link') },
    {
      key: 'pin',
      label: pinned ? t('tabbar.unpin') : t('tabbar.pin'),
      disabled: pinDisabled,
      icon: createDropdownIcon(pinned ? 'lucide:pin-off' : 'lucide:pin'),
    },
    {
      key: 'maximize',
      label: isContentMaximized ? t('tabbar.unmaximize') : t('tabbar.maximize'),
      icon: createDropdownIcon(isContentMaximized ? 'lucide:minimize-2' : 'lucide:maximize-2'),
    },
    { key: 'divider-1', type: 'divider' },
    {
      key: 'close',
      label: t('tabbar.close'),
      disabled: closeCurrentDisabled,
      icon: createDropdownIcon('lucide:x'),
    },
    { key: 'divider-2', type: 'divider' },
    {
      key: 'closeLeft',
      label: t('tabbar.close_left'),
      disabled: closeLeftDisabled,
      icon: createDropdownIcon('lucide:panel-left-close'),
    },
    {
      key: 'closeRight',
      label: t('tabbar.close_right'),
      disabled: closeRightDisabled,
      icon: createDropdownIcon('lucide:panel-right-close'),
    },
    {
      key: 'closeOthers',
      label: t('tabbar.close_others'),
      disabled: closeOthersDisabled,
      icon: createDropdownIcon('lucide:circle-off'),
    },
    {
      key: 'closeAll',
      label: t('tabbar.close_all'),
      disabled: closeAllDisabled,
      icon: createDropdownIcon('lucide:rows-3'),
    },
  ] as DropdownOption[]
}

export function openTabInNewWindow(path: string) {
  window.open(
    import.meta.env.VITE_ROUTER_HISTORY === 'history'
      ? `${window.location.origin}${path}`
      : `${window.location.origin}${window.location.pathname}#${path}`,
    '_blank',
  )
}
