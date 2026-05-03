import type { PageResult } from '../../types'
import type { LoginLogListItemDto, LoginLogPageQueryDto } from './login-log.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
} from '../../base'

const loginLogQueryApi = createDynamicApiClient('LoginLogQuery')

export const loginLogApi = {
  page(input: LoginLogPageQueryDto) {
    return loginLogQueryApi.get<PageResult<LoginLogListItemDto>>(
      'LoginLogPage',
      toLoginLogPageParams(input),
    )
  },
}

function toLoginLogPageParams(input: LoginLogPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'LoginResult', input.loginResult)
  appendDynamicApiParam(params, 'LoginTimeEnd', input.loginTimeEnd)
  appendDynamicApiParam(params, 'LoginTimeStart', input.loginTimeStart)
  appendDynamicApiParam(params, 'LoginType', input.loginType)
  appendDynamicApiParam(params, 'UserId', input.userId)
  appendDynamicApiParam(params, 'UserName', input.userName)
  return params
}
