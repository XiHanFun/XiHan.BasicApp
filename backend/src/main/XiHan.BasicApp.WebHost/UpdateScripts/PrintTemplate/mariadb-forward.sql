-- 打印模板模块 MariaDB 正向脚本。
-- MariaDB DDL 会隐式提交，无法依赖事务回滚；上线前应先在隔离库验证并保留备份。
-- 当前无历史数据迁移。Basic_Id 由应用的分布式 ID 生成器赋值，不使用 AUTO_INCREMENT。
CREATE TABLE IF NOT EXISTS `Sys_Print_Template`
(
    `Basic_Id`          bigint        NOT NULL,
    `Template_Code`     varchar(100)  NOT NULL,
    `Data_Source_Code`  varchar(100)  NULL COMMENT '可选代码数据源；NULL 表示自由模板',
    `Template_Name`     varchar(100)  NOT NULL,
    `Template_Json`     longtext      NOT NULL,
    `Engine_Version`    varchar(32)   NOT NULL,
    `Allow_Tenant_Use`  tinyint(1)    NOT NULL,
    `Status`            int           NOT NULL,
    `Sort`              int           NOT NULL,
    `Remark`            varchar(500)  NULL,
    `Tenant_Id`         bigint        NOT NULL,
    `Row_Version`       bigint        NOT NULL,
    `Created_Time`      datetime(6)   NOT NULL,
    `Created_Id`        bigint        NULL,
    `Created_By`        varchar(255)  NULL,
    `Modified_Time`     datetime(6)   NULL,
    `Modified_Id`       bigint        NULL,
    `Modified_By`       varchar(255)  NULL,
    `Is_Deleted`        tinyint(1)    NOT NULL,
    `Deleted_Time`      datetime(6)   NULL,
    `Deleted_Id`        bigint        NULL,
    `Deleted_By`        varchar(255)  NULL,
    PRIMARY KEY (`Basic_Id`),
    KEY `IX_Sys_Print_Template_TeId_CrTi` (`Tenant_Id`, `Created_Time`),
    KEY `IX_Sys_Print_Template_CrId` (`Created_Id`),
    KEY `IX_Sys_Print_Template_TeId_IsDe` (`Tenant_Id`, `Is_Deleted`),
    UNIQUE KEY `UX_Sys_Print_Template_TeId_TeCo` (`Tenant_Id`, `Template_Code`, `Is_Deleted`),
    KEY `IX_Sys_Print_Template_TeId_St_So` (`Tenant_Id`, `Status`, `Sort`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='hiprint 打印模板表；Tenant_Id=0 表示平台全局模板';
