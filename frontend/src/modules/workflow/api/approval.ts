import type {
  WorkflowApprovalDetailDto,
  WorkflowApprovalInstanceItemDto,
  WorkflowApprovalPageQueryDto,
  WorkflowApprovalRecordItemDto,
  WorkflowApprovalWithdrawDto,
} from './approval.types'
import type { ApiId, PageResult } from '@/api/types'
import { createDynamicApiClient } from '@/api/base'

const command = createDynamicApiClient('WorkflowApproval')
const query = createDynamicApiClient('WorkflowApprovalQuery')

export const workflowApprovalApi = {
  // Query（主体服务端锁定为当前用户；Get 前缀剥离，分页带查询对象走 POST）
  donePage(input: WorkflowApprovalPageQueryDto) {
    return query.post<PageResult<WorkflowApprovalRecordItemDto>>('DonePage', input)
  },
  ccPage(input: WorkflowApprovalPageQueryDto) {
    return query.post<PageResult<WorkflowApprovalRecordItemDto>>('CcPage', input)
  },
  startedPage(input: WorkflowApprovalPageQueryDto) {
    return query.post<PageResult<WorkflowApprovalInstanceItemDto>>('StartedPage', input)
  },
  /** 审批详情；从待办打开时带上 taskId，详情据此返回办理所需的任务信息 */
  detail(instanceId: ApiId, taskId?: string | null) {
    return query.get<WorkflowApprovalDetailDto>('Detail', taskId ? { instanceId, taskId } : { instanceId })
  },
  // Commands（Withdraw 不在动词剥离表内：路由保留完整方法名，POST）
  withdraw(input: WorkflowApprovalWithdrawDto) {
    return command.post<void, WorkflowApprovalWithdrawDto>('Withdraw', input)
  },
}
