import type { ApiId, PageResult } from '../../types'
import type {
  PositionCreateDto,
  PositionDetailDto,
  PositionListItemDto,
  PositionPageQueryDto,
  PositionStatusUpdateDto,
  PositionUpdateDto,
} from './position.types'
import {
  createDynamicApiClient,
  createReadApi,
  formatDynamicApiRouteValue,
} from '../../base'

const positionQueryApi = createDynamicApiClient('PositionQuery')
const positionCommandApi = createDynamicApiClient('Position')
const positionReadApi = createReadApi<PositionListItemDto, PositionDetailDto, PositionPageQueryDto>(
  'PositionQuery',
  'Position',
)

export const positionApi = {
  create(input: PositionCreateDto) {
    return positionCommandApi.post<PositionDetailDto, PositionCreateDto>('Position', input)
  },
  delete(id: ApiId) {
    return positionCommandApi.delete(`Position/${formatDynamicApiRouteValue(id)}`)
  },
  detail(id: ApiId) {
    return positionReadApi.detail(id)
  },
  page(input: PositionPageQueryDto) {
    return positionQueryApi.post<PageResult<PositionListItemDto>>('PositionPage', input)
  },
  update(input: PositionUpdateDto) {
    return positionCommandApi.put<PositionDetailDto, PositionUpdateDto>('Position', input)
  },
  updateStatus(input: PositionStatusUpdateDto) {
    return positionCommandApi.put<PositionDetailDto, PositionStatusUpdateDto>('PositionStatus', input)
  },
}
