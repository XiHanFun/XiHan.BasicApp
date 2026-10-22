import type { PageResult } from '../../types'
import type { AccessPolicyHitListItemDto, AccessPolicyHitPageQueryDto } from './access-policy-hit.types'
import { createDynamicApiClient } from '../../base'

const accessPolicyHitQueryApi = createDynamicApiClient('AccessPolicyHitQuery')

/** 条件命中记录：访问策略与权限条件 */
export const accessPolicyHitApi = {
  page(input: AccessPolicyHitPageQueryDto) {
    return accessPolicyHitQueryApi.post<PageResult<AccessPolicyHitListItemDto>, AccessPolicyHitPageQueryDto>('AccessPolicyHitPage', input)
  },
}
