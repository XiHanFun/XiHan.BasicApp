// ==================== 路由路径常量（唯一定义，全局引用） ====================
//
// 下面这些都是 **packages 自己的核心路由**（定义在 ~/router/routes/core.ts），
// 底层包认识它们是应该的。

export const AUTH_PATH = '/auth'
export const LOGIN_PATH = '/auth/login'
export const CODE_LOGIN_PATH = '/auth/code-login'
export const EMAIL_LOGIN_PATH = '/auth/email-login'
export const QRCODE_LOGIN_PATH = '/auth/qrcode-login'
export const NOT_FOUND_PATH = '/404'
export const FORBIDDEN_PATH = '/403'
export const SERVER_ERROR_PATH = '/500'

/**
 * 首页兜底路径——**由应用声明**（.env 的 VITE_HOME_PATH），底层包不该写死 `/workbench/dashboard`。
 *
 * 它不只是一次跳转，而是 shell 的哨兵：固定首页页签、guard 重定向、tabbar 默认激活项都拿它做比较，
 * 且 tabbar store 初始化时就要用，早于 AppContext 注册——故走构建期 env 而非运行期上下文注入。
 * 运行时真实首页优先取后端菜单派生的 accessStore.homePath，这里只是它为空时的兜底。
 */
export const HOME_PATH = import.meta.env.VITE_HOME_PATH || '/'
