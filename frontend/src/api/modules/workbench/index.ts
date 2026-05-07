import type { WorkbenchDashboardSummaryDto } from './workbench.types'
import { createDynamicApiClient } from '../../base'
import { userStatisticsApi } from '../identity'
import { userInboxApi } from '../messaging'

const workbenchQueryApi = createDynamicApiClient('WorkbenchQuery')

export const workbenchApi = {
  dashboard: {
    summary() {
      return workbenchQueryApi.get<WorkbenchDashboardSummaryDto>('DashboardSummary')
    },
    statistics: userStatisticsApi,
  },
  inbox: userInboxApi,
}

export * from './workbench.types'
