import { cacheApi } from '../cache'
import { configApi, dictApi } from '../configuration'
import { constraintRuleApi } from '../constraint-rule'
import { fileApi } from '../files'
import { menuApi } from '../navigation'
import { oauthAppApi } from '../oauth'
import { serverApi } from '../server'
import {
  tenantApi,
  tenantEditionApi,
  tenantEditionPermissionApi,
  tenantMemberApi,
} from '../tenant'
import { reviewApi, taskApi } from '../workflow'

export const appManagementApi = {
  ...oauthAppApi,
}

export const tenantManagementApi = {
  ...tenantApi,
  editionPermissions: tenantEditionPermissionApi,
  editions: tenantEditionApi,
  members: tenantMemberApi,
}

export const menuManagementApi = {
  ...menuApi,
}

export const configManagementApi = {
  ...configApi,
  constraints: constraintRuleApi,
}

export const dictManagementApi = {
  ...dictApi,
}

export const fileManagementApi = {
  ...fileApi,
}

export const jobManagementApi = {
  ...taskApi,
}

export const approvalManagementApi = {
  ...reviewApi,
}

export const serverManagementApi = {
  ...serverApi,
}

export const cacheManagementApi = {
  ...cacheApi,
}
