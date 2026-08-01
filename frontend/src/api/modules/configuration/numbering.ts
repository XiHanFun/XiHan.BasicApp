/**
 * 业务编号 Dynamic API 客户端。
 * 页面只调用规则管理、记录查询与格式预览；generate 方法供受权业务代码使用，不在管理页放置发号按钮。
 */
import type { ApiId, PageResult } from '../../types'
import type {
  NumberBatchGenerateDto,
  NumberGenerateDto,
  NumberGenerationResultDto,
  NumberingAllocationListItemDto,
  NumberingAllocationPageQueryDto,
  NumberingBatchPreviewDto,
  NumberingBatchPreviewResultDto,
  NumberingPreviewDto,
  NumberingPreviewResultDto,
  NumberingRuleCreateDto,
  NumberingRuleDetailDto,
  NumberingRuleListItemDto,
  NumberingRulePageQueryDto,
  NumberingRuleResetDto,
  NumberingRuleStatusUpdateDto,
  NumberingRuleUpdateDto,
  NumberingScope,
  NumberingTimeZoneOptionDto,
} from './numbering.types'
import { createDynamicApiClient } from '../../base'

const ruleQueryApi = createDynamicApiClient('NumberingRuleQuery')
const ruleCommandApi = createDynamicApiClient('NumberingRule')
const numberingCommandApi = createDynamicApiClient('Numbering')

/** 业务编号 API 集合。 */
export const numberingApi = {
  /**
   * 创建当前作用域规则。
   * @param input 规则配置，不包含租户 ID。
   * @returns 创建后的规则详情。
   * @throws 后端校验、权限或唯一性错误。
   */
  create(input: NumberingRuleCreateDto) {
    return ruleCommandApi.post<NumberingRuleDetailDto, NumberingRuleCreateDto>('NumberingRule', input)
  },

  /**
   * 更新规则。
   * @param input 更新配置；首次发号后格式字段不可变。
   * @returns 更新后的规则详情。
   * @throws 后端权限、并发或冻结策略错误。
   */
  update(input: NumberingRuleUpdateDto) {
    return ruleCommandApi.put<NumberingRuleDetailDto, NumberingRuleUpdateDto>('NumberingRule', input)
  },

  /**
   * 更新规则启停状态。
   * @param input 主键、作用域和目标状态。
   * @returns 更新后的规则详情。
   * @throws 后端权限或并发错误。
   */
  updateStatus(input: NumberingRuleStatusUpdateDto) {
    return ruleCommandApi.put<NumberingRuleDetailDto, NumberingRuleStatusUpdateDto>('NumberingRuleStatus', input)
  },

  /**
   * 安全设置下一流水值。
   * @param input 重置原因与全局规则可选二次确认编码。
   * @returns 更新后的规则详情。
   * @throws 下一值可能重复、权限不足或确认编码错误。
   */
  reset(input: NumberingRuleResetDto) {
    return ruleCommandApi.post<NumberingRuleDetailDto, NumberingRuleResetDto>('ResetNumberingRule', input)
  },

  /**
   * 删除从未发号的规则。
   * @param id 规则主键。
   * @param scope 规则作用域。
   * @returns 完成信号。
   * @throws 已发号规则、权限或并发错误。
   */
  delete(id: ApiId, scope: NumberingScope) {
    return ruleCommandApi.delete('NumberingRule', { id, scope })
  },

  /**
   * 查询指定作用域规则分页。
   * @param input 分页与筛选条件。
   * @returns 规则分页。
   * @throws 查询权限或字段安全错误。
   */
  page(input: NumberingRulePageQueryDto) {
    return ruleQueryApi.post<PageResult<NumberingRuleListItemDto>, NumberingRulePageQueryDto>('NumberingRulePage', input)
  },

  /**
   * 查询向租户开放的全局规则分页。
   * @param input 分页与筛选条件。
   * @returns 可用全局规则分页。
   * @throws 查询权限错误。
   */
  availableGlobalPage(input: NumberingRulePageQueryDto) {
    return ruleQueryApi.post<PageResult<NumberingRuleListItemDto>, NumberingRulePageQueryDto>('AvailableGlobalNumberingRulePage', input)
  },

  /**
   * 查询规则详情。
   * @param id 规则主键。
   * @param scope 规则作用域。
   * @returns 规则详情或 null。
   * @throws 规则未开放或查询权限错误。
   */
  detail(id: ApiId, scope: NumberingScope) {
    return ruleQueryApi.get<NumberingRuleDetailDto | null>(
      'NumberingRuleDetail',
      { scope, id },
    )
  },

  /**
   * 查询永久发号记录分页。
   * @param input 规则、作用域与分页条件。
   * @returns 发号记录分页。
   * @throws 发号记录查看权限或租户隔离错误。
   */
  allocationPage(input: NumberingAllocationPageQueryDto) {
    return ruleQueryApi.post<PageResult<NumberingAllocationListItemDto>, NumberingAllocationPageQueryDto>('NumberingAllocationPage', input)
  },

  /**
   * 查询当前后端实例实际支持的规则时区。
   * @returns 后端已验证可解析的时区选项。
   * @throws 查询权限或服务运行环境错误。
   */
  timeZoneOptions() {
    return ruleQueryApi.get<NumberingTimeZoneOptionDto[]>('NumberingTimeZoneOptions')
  },

  /**
   * 预览规则格式且不消耗流水。
   * @param input 格式参数与样例流水值。
   * @returns 预览编号、周期和规则本地时间。
   * @throws 日期/周期组合、时区或容量校验错误。
   */
  preview(input: NumberingPreviewDto) {
    return ruleQueryApi.post<NumberingPreviewResultDto, NumberingPreviewDto>('PreviewNumberingFormat', input)
  },

  /**
   * 从样例流水开始连续预览至多 50 个编号且不消耗真实流水。
   * @param input 格式参数、样例起始流水值和连续预览数量。
   * @returns 连续编号、流水区间、周期和规则本地时间。
   * @throws 数量、日期/周期组合、时区或流水容量校验错误。
   */
  previewBatch(input: NumberingBatchPreviewDto) {
    return ruleQueryApi.post<NumberingBatchPreviewResultDto, NumberingBatchPreviewDto>('PreviewNumberingBatch', input)
  },

  /**
   * 生成一个真实编号。
   * @param input 规则编码、作用域、幂等键和可选业务标识。
   * @returns 原子分配结果。
   * @throws 幂等冲突、流水耗尽、权限或并发错误。
   */
  generate(input: NumberGenerateDto) {
    return numberingCommandApi.post<NumberGenerationResultDto, NumberGenerateDto>('GenerateNumber', input)
  },

  /**
   * 批量生成至多 1000 个真实编号。
   * @param input 批量请求。
   * @returns 原子连续分配结果。
   * @throws 幂等冲突、流水耗尽、权限或并发错误。
   */
  generateBatch(input: NumberBatchGenerateDto) {
    return numberingCommandApi.post<NumberGenerationResultDto, NumberBatchGenerateDto>('GenerateNumberBatch', input)
  },
}
