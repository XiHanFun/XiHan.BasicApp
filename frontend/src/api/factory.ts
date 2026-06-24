import type { DynamicApiClient } from './base'
import type { ApiId, PageRequest, PageResult } from './types'
import {
  createDynamicApiClient,
  formatDynamicApiRouteValue,
} from './base'

/**
 * 资源工厂配置选项
 */
export interface ResourceOptions {
  /** 查询控制器名，如 'UserQuery' */
  query: string
  /** 命令控制器名，如 'User' */
  command: string
  /**
   * 资源段，用于拼接 action 名称（如 `${resource}Page`、`${resource}Detail`）。
   * 默认等于 command。
   */
  resource?: string
}

/**
 * defineResource 返回的标准资源操作对象
 *
 * @template TList     列表项 DTO
 * @template TDetail   详情 DTO
 * @template TCreate   创建输入 DTO
 * @template TUpdate   更新输入 DTO
 * @template TQuery    分页查询请求类型
 */
export interface ResourceApi<TList, TDetail, TCreate, TUpdate, TQuery extends PageRequest = PageRequest> {
  /** 底层查询控制器客户端，可用于扩展自定义查询 action */
  query: DynamicApiClient
  /** 底层命令控制器客户端，可用于扩展自定义命令 action */
  command: DynamicApiClient
  /** 分页查询列表 */
  page: (query: TQuery) => Promise<PageResult<TList>>
  /** 查询单条详情 */
  detail: (id: ApiId) => Promise<TDetail | null>
  /** 新增资源 */
  create: (input: TCreate) => Promise<TDetail>
  /** 更新资源 */
  update: (input: TUpdate) => Promise<TDetail>
  /** 删除资源 */
  remove: (id: ApiId) => Promise<void>
}

/**
 * 定义标准 CRUD 资源工厂。
 *
 * 将「查询控制器 + 命令控制器 + 分页参数 + 差异过滤字段」声明化，
 * 消除各模块手拼参数、重复创建 client 的样板代码。
 *
 * @example
 * ```ts
 * export const userApi = defineResource<UserListItemDto, UserDetailDto, UserCreateDto, UserUpdateDto, UserPageRequest>({
 *   query: 'UserQuery',
 *   command: 'User',
 * })
 * ```
 *
 * @template TList    列表项 DTO
 * @template TDetail  详情 DTO
 * @template TCreate  创建输入 DTO
 * @template TUpdate  更新输入 DTO
 * @template TQuery   分页查询请求类型，默认为 PageRequest
 */
export function defineResource<
  TList,
  TDetail,
  TCreate,
  TUpdate,
  TQuery extends PageRequest = PageRequest,
>(options: ResourceOptions): ResourceApi<TList, TDetail, TCreate, TUpdate, TQuery> {
  const queryClient = createDynamicApiClient(options.query)
  const commandClient = createDynamicApiClient(options.command)
  const resource = options.resource ?? options.command

  return {
    /** 底层查询控制器客户端，供子聚合扩展自定义查询 action 复用 */
    query: queryClient,

    /** 底层命令控制器客户端，供子聚合扩展自定义命令 action 复用 */
    command: commandClient,

    /**
     * 分页查询列表。
     * 自动合并公共分页参数与业务差异过滤字段。
     */
    page(query: TQuery): Promise<PageResult<TList>> {
      // 分页统一走 POST：整个查询对象作为 JSON body 上送（业务字段为对象自身属性，后端从 body 绑定）
      return queryClient.post<PageResult<TList>, TQuery>(`${resource}Page`, query)
    },

    /** 查询单条详情 */
    detail(id: ApiId): Promise<TDetail | null> {
      return queryClient.get<TDetail | null>(`${resource}Detail/${formatDynamicApiRouteValue(id)}`)
    },

    /** 新增资源 */
    create(input: TCreate): Promise<TDetail> {
      return commandClient.post<TDetail, TCreate>(resource, input)
    },

    /** 更新资源 */
    update(input: TUpdate): Promise<TDetail> {
      return commandClient.put<TDetail, TUpdate>(resource, input)
    },

    /** 删除资源 */
    remove(id: ApiId): Promise<void> {
      return commandClient.delete<void>(`${resource}/${formatDynamicApiRouteValue(id)}`)
    },
  }
}
