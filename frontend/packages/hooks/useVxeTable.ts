import type { VxeGridProps, VxeGridPropTypes } from 'vxe-table'
import dayjs from 'dayjs'
import { reactive } from 'vue'

// eslint-disable-next-line ts/no-explicit-any
export type VxeColumnOptions<D = any> = (VxeGridPropTypes.Columns<D> extends (infer U)[]
  ? U
  // eslint-disable-next-line ts/no-explicit-any
  : any) & {
    children?: VxeColumnOptions<D>[]
  }

interface UseVxeTableOptions {
  id?: string
  name?: string
  // eslint-disable-next-line ts/no-explicit-any
  columns: VxeColumnOptions<any>[]
  // eslint-disable-next-line ts/no-explicit-any
  data?: any[]
  showFooter?: boolean
  // eslint-disable-next-line ts/no-explicit-any
  footerData?: any
  // eslint-disable-next-line ts/no-explicit-any
  footerMethod?: any
}

// eslint-disable-next-line ts/no-explicit-any
export function useVxeTable<T = any>(opt: UseVxeTableOptions, extras?: VxeGridProps<T>) {
  const options = reactive<VxeGridProps>({
    stripe: true,
    border: false,
    id: opt.id || opt.name,
    height: '100%',
    autoResize: true,
    size: 'small',
    loading: false,
    align: 'left',
    columns: opt.columns,
    showFooter: opt.showFooter,
    footerData: opt.footerData,
    footerMethod: opt.footerMethod,
    toolbarConfig: {
      size: 'small',
      slots: { buttons: 'toolbar_buttons' },
      refresh: true,
      export: true,
      print: true,
      zoom: true,
      custom: true,
    },
    sortConfig: { remote: true },
    exportConfig: {
      remote: false,
      original: false,
      filename: `${opt.name || '列表'}导出_${dayjs().format('YYYYMMDDHHmmss')}`,
      modes: ['current', 'selected', 'all'],
    },
    pagerConfig: {
      enabled: true,
      size: 'small',
      pageSize: 20,
    },
    printConfig: { sheetName: '' },
    customConfig: {
      storage: {
        visible: true,
        resizable: true,
        sort: true,
        fixed: true,
      },
    },
  })

  if (opt.data) {
    options.data = opt.data
  }
  else {
    options.proxyConfig = {
      enabled: true,
      autoLoad: false,
      sort: true,
      response: {
        list: 'items',
        result: 'items',
        total: 'total',
      },
    }
  }

  if (extras) {
    const savedProxyConfig = options.proxyConfig ? { ...options.proxyConfig } : undefined
    const savedToolbarConfig = options.toolbarConfig ? { ...options.toolbarConfig } : undefined
    const savedPagerConfig = options.pagerConfig ? { ...options.pagerConfig } : undefined
    const savedExportConfig = options.exportConfig ? { ...options.exportConfig } : undefined
    const savedSortConfig = options.sortConfig ? { ...options.sortConfig } : undefined

    const { proxyConfig, toolbarConfig, pagerConfig, exportConfig, sortConfig, ...rest } = extras
    Object.assign(options, rest)

    if (proxyConfig && savedProxyConfig) {
      options.proxyConfig = {
        ...savedProxyConfig,
        ...proxyConfig,
        response: {
          // eslint-disable-next-line ts/no-explicit-any
          ...(savedProxyConfig as any)?.response,
          ...proxyConfig?.response,
        },
      }
    }
    else if (proxyConfig) {
      options.proxyConfig = proxyConfig
    }
    else if (savedProxyConfig) {
      options.proxyConfig = savedProxyConfig
    }

    if (toolbarConfig) {
      options.toolbarConfig = { ...savedToolbarConfig, ...toolbarConfig }
    }
    if (pagerConfig) {
      options.pagerConfig = { ...savedPagerConfig, ...pagerConfig }
    }
    if (exportConfig) {
      options.exportConfig = { ...savedExportConfig, ...exportConfig }
    }
    if (sortConfig) {
      options.sortConfig = { ...savedSortConfig, ...sortConfig }
    }
  }

  return options
}
