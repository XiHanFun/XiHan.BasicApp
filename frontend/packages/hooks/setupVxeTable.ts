import type { App } from 'vue'
import { useOsTheme } from 'naive-ui'
import { watch } from 'vue'
import VxeUI from 'vxe-pc-ui'
import VxeUITable from 'vxe-table'
import { THEME_AUTO } from '~/constants'
import { i18n } from '~/locales'
import { useAppStore } from '~/stores'

import 'vxe-pc-ui/lib/style.css'
import 'vxe-table/lib/style.css'

export function setupVxeTable(app: App) {
  VxeUI.setI18n((key, args) => i18n.global.t(key, args))

  const appStore = useAppStore()
  const osTheme = useOsTheme()

  function syncTheme() {
    const mode = appStore.themeMode
    const dark = mode === THEME_AUTO ? osTheme.value === 'dark' : mode === 'dark'
    VxeUI.setTheme(dark ? 'dark' : 'light')
  }

  syncTheme()
  watch(() => appStore.themeMode, syncTheme)
  watch(osTheme, syncTheme)

  VxeUITable.setConfig({
    size: 'small',
    table: {
      border: false,
      stripe: true,
      showOverflow: true,
      autoResize: true,
      emptyText: '暂无数据',
      columnConfig: {
        isCurrent: false,
        isHover: false,
        resizable: true,
        minWidth: 70,
      },
      rowConfig: {
        isCurrent: true,
        isHover: true,
      },
      checkboxConfig: {
        strict: true,
        highlight: true,
        range: true,
        trigger: 'default',
      },
      tooltipConfig: {
        showAll: false,
        enterable: true,
        theme: 'dark',
      },
      customConfig: {
        storage: {
          visible: true,
          resizable: true,
        },
      },
      virtualXConfig: {
        enabled: true,
        gt: 30,
      },
      virtualYConfig: {
        scrollToTopOnChange: true,
        enabled: true,
        gt: 50,
      },
    },
    grid: {
      size: 'small',
      toolbarConfig: {
        size: 'small',
        perfect: false,
      },
      zoomConfig: { escRestore: true },
    },
    pager: {
      size: 'small',
      background: true,
      autoHidden: false,
      pageSize: 20,
      pageSizes: [10, 20, 50, 100, 200, 500],
      layouts: [
        'PrevJump',
        'PrevPage',
        'JumpNumber',
        'NextPage',
        'NextJump',
        'Sizes',
        'FullJump',
        'PageCount',
        'Total',
      ],
    },
  })

  app.use(VxeUI)
  app.use(VxeUITable)
}
