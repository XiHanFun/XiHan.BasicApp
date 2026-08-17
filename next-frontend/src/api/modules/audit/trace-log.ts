import type { TraceTimelineQueryDto, TraceTimelineResultDto } from './trace-log.types'
import { createDynamicApiClient } from '../../base'

const traceQueryApi = createDynamicApiClient('TraceQuery')

export const traceLogApi = {
  timeline(input: TraceTimelineQueryDto) {
    return traceQueryApi.post<TraceTimelineResultDto>('TraceTimeline', input)
  },
}
