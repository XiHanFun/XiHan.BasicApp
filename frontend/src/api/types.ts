/**
 * 基础契约类型已下沉到 packages 底层（`~/types/contracts`），此处反向 re-export 以保持 `@/api` 入口不变。
 * 见架构审查报告 D1/H8：packages 不得反向依赖 src，契约类型属可复用基建故归属 packages。
 */
export * from '~/types/contracts'
