import hljs from 'highlight.js/lib/common'

/**
 * highlight.js「常用语言」实例（common 集包含约 40 种主流语言，足够文件预览 / 代码展示）。
 *
 * 作为底层单例导出：应用层不直接依赖 highlight.js，需要 hljs 实例的场景
 * （如传给 naive-ui NCode 的 :hljs）统一经 ~/utils 取用。
 */
export { hljs }
