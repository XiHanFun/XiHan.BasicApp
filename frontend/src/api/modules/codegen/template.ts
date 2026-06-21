import type { ApiId, PageResult } from '../../types'
import type {
  CodeGenTemplateCreateDto,
  CodeGenTemplateDetailDto,
  CodeGenTemplateListItemDto,
  CodeGenTemplatePageQueryDto,
  CodeGenTemplateStatusUpdateDto,
  CodeGenTemplateUpdateDto,
  CodeGenTemplateValidateDto,
  CodeGenTemplateValidateResultDto,
} from './template.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  formatDynamicApiRouteValue,
} from '../../base'

const command = createDynamicApiClient('CodeGenTemplate')
const query = createDynamicApiClient('CodeGenTemplateQuery')

export const codeGenTemplateApi = {
  create(input: CodeGenTemplateCreateDto) {
    return command.post<CodeGenTemplateDetailDto, CodeGenTemplateCreateDto>('Create', input)
  },
  update(input: CodeGenTemplateUpdateDto) {
    return command.put<CodeGenTemplateDetailDto, CodeGenTemplateUpdateDto>('Update', input)
  },
  updateStatus(input: CodeGenTemplateStatusUpdateDto) {
    return command.put<CodeGenTemplateDetailDto, CodeGenTemplateStatusUpdateDto>('Status', input)
  },
  delete(id: ApiId) {
    return command.delete(`Delete/${formatDynamicApiRouteValue(id)}`)
  },
  validate(input: CodeGenTemplateValidateDto) {
    return command.post<CodeGenTemplateValidateResultDto, CodeGenTemplateValidateDto>('Validate', input)
  },
  page(input: CodeGenTemplatePageQueryDto) {
    return query.get<PageResult<CodeGenTemplateListItemDto>>('Page', toPageParams(input))
  },
  detail(id: ApiId) {
    return query.get<CodeGenTemplateDetailDto | null>(`Detail/${formatDynamicApiRouteValue(id)}`)
  },
}

function toPageParams(input: CodeGenTemplatePageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'TemplateGroup', input.templateGroup)
  appendDynamicApiParam(params, 'TemplateType', input.templateType)
  appendDynamicApiParam(params, 'TemplateEngine', input.templateEngine)
  appendDynamicApiParam(params, 'IsBuiltIn', input.isBuiltIn)
  appendDynamicApiParam(params, 'IsEnabled', input.isEnabled)
  appendDynamicApiParam(params, 'Status', input.status)
  return params
}
