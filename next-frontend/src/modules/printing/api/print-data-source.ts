/**
 * 打印数据源目录 Dynamic API 客户端。
 * 职责：拉取后端数据源注册表（设计器素材、样例数据与模板绑定校验的单一事实源）。
 */
import type { RemotePrintDataSourceDto } from '~/printing'
import { createDynamicApiClient } from '@/api/base'

const printDataSourceQueryApi = createDynamicApiClient('PrintDataSourceQuery')

export const printDataSourceApi = {
  /** GetListAsync：Get 前缀剥离 → GET /PrintDataSourceQuery/List */
  list() {
    return printDataSourceQueryApi.get<RemotePrintDataSourceDto[]>('List')
  },
}
