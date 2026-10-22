import type { DynamicApiParams } from '../../base'
import type {
  PermissionConditionAttributeDto,
  PermissionConditionBatchTrialInputDto,
  PermissionConditionBatchTrialResultDto,
  PermissionConditionInventoryDto,
  PermissionConditionTrialInputDto,
  PermissionConditionTrialResultDto,
} from './permission-condition-insight.types'
import { appendDynamicApiParam, createDynamicApiClient } from '../../base'

const permissionConditionInsightQueryApi = createDynamicApiClient('PermissionConditionInsightQuery')

/** 权限条件的属性目录、盘点与试算 */
export const permissionConditionInsightApi = {
  /** 条件属性目录：给了权限码时带上它登记的数据行属性 */
  attributes(permissionCode?: string | null) {
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'permissionCode', permissionCode)
    return permissionConditionInsightQueryApi.get<PermissionConditionAttributeDto[]>('ConditionAttributes', params)
  },
  /** 条件盘点 */
  inventory() {
    return permissionConditionInsightQueryApi.get<PermissionConditionInventoryDto>('PermissionConditionInventory')
  },
  /** 单人试算 */
  trial(input: PermissionConditionTrialInputDto) {
    return permissionConditionInsightQueryApi.post<PermissionConditionTrialResultDto, PermissionConditionTrialInputDto>(
      'PermissionConditionTrial',
      input,
    )
  },
  /** 批量试算：按一条授权找出受影响的人各算一遍 */
  batchTrial(input: PermissionConditionBatchTrialInputDto) {
    return permissionConditionInsightQueryApi.post<PermissionConditionBatchTrialResultDto, PermissionConditionBatchTrialInputDto>(
      'PermissionConditionBatchTrial',
      input,
    )
  },
}
