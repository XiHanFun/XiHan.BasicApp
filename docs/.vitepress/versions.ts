/**
 * 版本与导航徽章的单一事实源
 * --------------------------------------------------------------------------
 * 主导航首项标题右上角的徽章（版本号 + 发布阶段）在这里维护：
 * 只声明「版本号」与「发布阶段」，徽章文案与配色都由阶段推导，不再手写。
 *
 * withNavBadge 把徽章拼进 nav 的 item.text，VitePress 以 v-html 渲染该字段，
 * 宽屏导航栏（VPNavBarMenuLink）与窄屏汉堡菜单（VPNavScreenMenuLink）读的是同一份 text，
 * 因此两处自动一致，无需分别写选择器。
 *
 * 外观见 theme/overrides.css 中的 .xh-nav-badge。
 */

/** 发布阶段 */
export enum ReleaseStage {
  /** 开发版：非常早期的开发阶段，可能非常不稳定，如 1.0.0-alpha、1.0.0-alpha.1 */
  Alpha = 1,
  /** 测试版：测试阶段，可能包含一些不稳定的功能，如 1.0.0-beta、1.0.0-beta.1 */
  Beta = 2,
  /** 预览版：测试阶段，可能包含一些不稳定的功能，如 1.0.0-preview、1.0.0-preview.1 */
  Preview = 3,
  /** 候选版：测试阶段，可能包含一些不稳定的功能，如 1.0.0-rc、1.0.0-rc.1 */
  Rc = 4,
  /** 稳定版：已经稳定，不包含任何不稳定的功能，如 1.0.0 */
  Release = 5,
}

/** 徽章配色。新增取值需在 overrides.css 补上对应的 .xh-nav-badge--* */
export type NavBadgeType = "tip" | "warning" | "danger";

export interface ProductRelease {
  /** 版本号，不含阶段后缀，如 "3.10.1" */
  version: string;
  /** 发布阶段，决定徽章后缀与配色 */
  stage: ReleaseStage;
}

export interface NavBadge {
  /** 徽章文案，如 "v3.10.1"、"v0.9.8-alpha" */
  text: string;
  /** 徽章配色 */
  type: NavBadgeType;
}

/**
 * 本仓库的发布状态。
 *
 * 版本号真源是 backend/props/version.props，改版本时同步这里。
 * 另外两个仓库（XiHan.Framework、XiHan.UI）独立发版，各自文档站维护各自的版本号。
 */
export const release: ProductRelease = {
  version: "3.10.1",
  stage: ReleaseStage.Release,
};

/** 阶段 → 版本后缀与徽章配色 */
const stageStyles: Record<ReleaseStage, { suffix: string; badge: NavBadgeType }> = {
  [ReleaseStage.Alpha]: { suffix: "-alpha", badge: "danger" },
  [ReleaseStage.Beta]: { suffix: "-beta", badge: "danger" },
  [ReleaseStage.Preview]: { suffix: "-preview", badge: "warning" },
  [ReleaseStage.Rc]: { suffix: "-rc", badge: "warning" },
  [ReleaseStage.Release]: { suffix: "", badge: "tip" },
};

/** 由发布状态推导徽章 */
export function toNavBadge(release: ProductRelease): NavBadge {
  const { suffix, badge } = stageStyles[release.stage];
  return { text: `v${release.version}${suffix}`, type: badge };
}

/** 把徽章拼到导航标题末尾 */
export function withNavBadge(text: string, release: ProductRelease): string {
  const badge = toNavBadge(release);
  return `${text}<span class="xh-nav-badge xh-nav-badge--${badge.type}">${badge.text}</span>`;
}
