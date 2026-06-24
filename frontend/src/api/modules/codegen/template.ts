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
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

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
    return query.post<PageResult<CodeGenTemplateListItemDto>>('Page', input)
  },
  detail(id: ApiId) {
    return query.get<CodeGenTemplateDetailDto | null>(`Detail/${formatDynamicApiRouteValue(id)}`)
  },
}
