-- 5.3.0
-- 一、权限目录新增作用侧 side：平台 = 1、租户 = 2、两侧 = 3。
-- 二、会话标识改为全局唯一（见文末）。
--
-- 建表只建缺失的表，存量库的 sys_permission 由本脚本补列；本脚本在建表之后、播种之前执行。
-- 存量行按改造前的语义（平台专属清单以外的权限两侧都生效）先补成两侧，
-- 随后权限种子把内置权限同步为各模块声明的作用侧；管理员自建的权限保持两侧，需要收窄的到权限管理页调整。
-- 列不留默认值：新行由应用必填写入，未声明的作用侧不会被静默补成某一侧。
--
-- 标识符一律小写不加引号：SqlSugar 建表未加引号，PostgreSQL 折叠为小写，库里实际是 sys_permission / side。
--
-- 幂等：新库上列已按实体建好、表里还没有行，整段空转；存量库跑过一次后同样空转。

ALTER TABLE sys_permission ADD COLUMN IF NOT EXISTS side int4 NULL;

UPDATE sys_permission
   SET side = 3
 WHERE side IS NULL OR side = 0;

ALTER TABLE sys_permission ALTER COLUMN side SET NOT NULL;

COMMENT ON COLUMN sys_permission.side IS '作用侧';

-- 会话标识改为全局唯一：令牌里的会话声明跨租户定位会话行，切换租户换新会话（不再复用标识），
-- 唯一索引去掉租户维度。存量标识均为随机 GUID，不会冲突。
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_user_session_usseid ON sys_user_session (user_session_id ASC, isdeleted ASC);

DROP INDEX IF EXISTS ux_sys_user_session_teid_usseid;
