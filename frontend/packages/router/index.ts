/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

export { mapMenuToRoutes, menuComponents } from './dynamic'
export { setupRouterGuard } from './guard'
export { firstRoute } from './menus'
export { coreRoutes } from './routes/core'
export { prepareViewSources, registerViewSource, resolveView, viewKeys } from './views'
export type { ViewLoader, ViewSource } from './views'
