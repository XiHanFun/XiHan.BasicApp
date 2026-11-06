/**
 * 批量操作里有逐项失败就抛错，不当成功吞掉
 * `message` 已按请求语言本地化，可直接显示，相同的原因只留一条；要按原因分支的看 `code`
 */
export function throwOnFailed(result: { failed: readonly { message: string }[] }): void {
  if (result.failed.length > 0) {
    throw new Error([
      ...new Set(result.failed.map(failure => failure.message)),
    ].join('; '))
  }
}
