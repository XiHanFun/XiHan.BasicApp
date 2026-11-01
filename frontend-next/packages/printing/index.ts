/**
 * XiHan 前端打印公共包入口。
 * 职责：集中导出运行配置、数据源注册、字段树、浏览器打印、本机客户端直打与打印机偏好。
 * 入口保持轻量：打印运行时与客户端在首次打印时按需加载；设计器经子路径 ~/printing/designer 取用。
 */
export * from './configuration'
export * from './data-source-registry'
export * from './document'
export * from './fields'
export * from './print-service'
export * from './printer-preference'
export * from './remote-data-sources'
export * from './sample-data'
export * from './types'
