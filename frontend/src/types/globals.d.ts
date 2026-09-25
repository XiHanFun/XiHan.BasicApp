declare const __APP_VERSION__: string
declare const __APP_BUILD_TIME__: string
/** 本次构建的唯一标记：重复打包同一份源码也会换新值，发版检查据它比对线上版本 */
declare const __APP_BUILD_STAMP__: string
/** 仓库地址（package.json 的 repository），页脚应用名链到这里 */
declare const __APP_REPOSITORY__: string
declare const __APP_NAME__: string
declare const __APP_AUTHOR_NAME__: string
declare const __APP_AUTHOR_URL__: string

/** 构建期解析出的前端生产依赖：包名 → 真实版本 */
declare const __APP_DEPENDENCIES__: Record<string, string>
/** 构建期解析出的前端开发依赖：包名 → 真实版本 */
declare const __APP_DEV_DEPENDENCIES__: Record<string, string>
