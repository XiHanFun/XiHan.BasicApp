/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/** 统一管理所有 Pinia Store ID，避免字符串散落 */
export enum SetupStoreId {
  App = 'app',
  Access = 'access',
  Auth = 'auth',
  User = 'user',
  Tabbar = 'tabbar',
  Favorites = 'favorites',
  SplitView = 'split-view',
  LayoutState = 'layout-state',
  LayoutBridge = 'layout-bridge',
  LayoutPreferences = 'layout-preferences',
  Notification = 'notification',
  TabbarPreferences = 'tabbar-preferences',
}
