declare const __APP_VERSION__: string
declare const __APP_BUILD_TIME__: string
declare const __APP_HOMEPAGE__: string
declare const __APP_NAME__: string
declare const __APP_AUTHOR_NAME__: string
declare const __APP_AUTHOR_URL__: string

/** 构建期解析出的前端生产依赖：包名 → 真实版本 */
declare const __APP_DEPENDENCIES__: Record<string, string>
/** 构建期解析出的前端开发依赖：包名 → 真实版本 */
declare const __APP_DEV_DEPENDENCIES__: Record<string, string>
