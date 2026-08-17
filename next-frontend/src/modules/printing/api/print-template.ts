import type {
  PrintTemplateCreateDto,
  PrintTemplateDeleteDto,
  PrintTemplateDetailDto,
  PrintTemplateListItemDto,
  PrintTemplatePageQueryDto,
  PrintTemplateScope,
  PrintTemplateStatusUpdateDto,
  PrintTemplateUpdateDto,
  ResolvedPrintTemplateDto,
} from './print-template.types'
/**
 * 打印模板 Dynamic API 客户端。
 * 职责：提供管理端 CRUD、可用全局模板查询和公共打印按编码解析入口。
 */
import type { PageResult } from '@/api/types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '@/api/base'

const queryApi = createDynamicApiClient('PrintTemplateQuery')
const commandApi = createDynamicApiClient('PrintTemplate')

/** 打印模板 API 集合。 */
export const printTemplateApi = {
  /**
   * 创建打印模板。
   * @param input 模板元数据与设计 JSON。
   * @returns 创建后的详情。
   * @throws 权限、唯一性或 JSON 结构错误。
   */
  create(input: PrintTemplateCreateDto) {
    return commandApi.post<PrintTemplateDetailDto, PrintTemplateCreateDto>('PrintTemplate', input)
  },

  /**
   * 使用行版本更新打印模板。
   * @param input 可编辑字段和最后读取的行版本。
   * @returns 更新后的详情。
   * @throws 权限、JSON 或乐观并发错误。
   */
  update(input: PrintTemplateUpdateDto) {
    return commandApi.put<PrintTemplateDetailDto, PrintTemplateUpdateDto>('PrintTemplate', input)
  },

  /**
   * 启用或停用打印模板。
   * @param input 目标状态与行版本。
   * @returns 更新后的详情。
   * @throws 权限或乐观并发错误。
   */
  updateStatus(input: PrintTemplateStatusUpdateDto) {
    return commandApi.put<PrintTemplateDetailDto, PrintTemplateStatusUpdateDto>('PrintTemplateStatus', input)
  },

  /**
   * 删除已经停用的打印模板。
   * @param input 主键、作用域和行版本。
   * @returns 完成信号。
   * @throws 模板仍启用、权限或乐观并发错误。
   */
  delete(input: PrintTemplateDeleteDto) {
    // Dynamic API 的 DELETE 不发送请求体；显式展开 DTO，既满足简单查询参数约束，也保持后端复杂对象模型绑定。
    return commandApi.delete('PrintTemplate', {
      basicId: input.basicId,
      rowVersion: input.rowVersion,
      scope: input.scope,
    })
  },

  /**
   * 查询指定作用域模板分页。
   * @param input 分页与筛选条件。
   * @returns 模板分页。
   */
  page(input: PrintTemplatePageQueryDto) {
    return queryApi.post<PageResult<PrintTemplateListItemDto>, PrintTemplatePageQueryDto>('PrintTemplatePage', input)
  },

  /**
   * 查询租户可使用的全局模板。
   * @param input 分页与筛选条件。
   * @returns 已启用且开放的全局模板分页。
   */
  availableGlobalPage(input: PrintTemplatePageQueryDto) {
    return queryApi.post<PageResult<PrintTemplateListItemDto>, PrintTemplatePageQueryDto>('AvailableGlobalPrintTemplatePage', input)
  },

  /**
   * 查询模板详情。
   * @param id 模板主键。
   * @param scope 查询作用域。
   * @returns 模板详情或 null。
   */
  detail(id: string, scope: PrintTemplateScope) {
    return queryApi.get<PrintTemplateDetailDto | null>(
      `PrintTemplateDetail/${formatDynamicApiRouteValue(id)}`,
      { scope },
    )
  },

  /**
   * 按编码解析启用模板。
   * @param templateCode 模板编码。
   * @param scope 解析作用域，默认由调用方传 Auto。
   * @returns 模板设计 JSON 与实际作用域。
   * @throws 模板不存在、停用、未开放或无 use 权限。
   */
  resolveByCode(templateCode: string, scope: PrintTemplateScope) {
    return queryApi.get<ResolvedPrintTemplateDto>(
      'ResolvedPrintTemplateByCode',
      { scope, templateCode },
    )
  },
}
