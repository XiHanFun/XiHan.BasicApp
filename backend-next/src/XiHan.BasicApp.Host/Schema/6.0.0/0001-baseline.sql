-- XiHan.BasicApp 6.0.0 基线：由 SchemaBaselineTests 按 InitTables 建出的结构生成（XIHAN_UPDATE_SCHEMA_BASELINE=1），可重复执行。

CREATE TABLE IF NOT EXISTS "audit_record" (
    "id" uuid NOT NULL,
    "sequence" bigint DEFAULT '0'::bigint NOT NULL,
    "timestamp" timestamp with time zone NOT NULL,
    "action" character varying(256) NOT NULL,
    "succeeded" boolean DEFAULT false NOT NULL,
    "error_code" character varying(64) DEFAULT NULL::character varying NULL,
    "error_message_key" character varying(256) DEFAULT NULL::character varying NULL,
    "error_message" text NULL,
    "error_details_json" text NULL,
    "trace_id" character varying(64) NOT NULL,
    "resource" character varying(1024) DEFAULT NULL::character varying NULL,
    "principal_id" character varying(256) DEFAULT NULL::character varying NULL,
    "tenant_id" character varying(128) DEFAULT NULL::character varying NULL,
    "details_json" text NULL,
    CONSTRAINT "audit_record_pkey" PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS "job_entity" (
    "id" character varying(128) NOT NULL,
    "job_type_name" character varying(512) NOT NULL,
    "data" text NOT NULL,
    "allow_concurrent" boolean DEFAULT false NOT NULL,
    "run_on_every_instance" boolean DEFAULT false NOT NULL,
    "requests_recovery" boolean DEFAULT false NOT NULL,
    "description" character varying(1024) DEFAULT NULL::character varying NULL,
    "paused" boolean DEFAULT false NOT NULL,
    "active_run_id" character varying(128) DEFAULT NULL::character varying NULL,
    "source" integer DEFAULT 0 NOT NULL,
    "updated_at" timestamp with time zone NOT NULL,
    "code_fingerprint" character varying(64) DEFAULT NULL::character varying NULL,
    "retry_max_retries" integer NULL,
    "retry_delay_milliseconds" bigint NULL,
    "retry_backoff" integer NULL,
    "retry_max_delay_milliseconds" bigint NULL,
    "timeout_milliseconds" bigint NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "job_entity_pkey" PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS "job_run_entity" (
    "id" character varying(128) NOT NULL,
    "job" character varying(128) NOT NULL,
    "trigger_name" character varying(256) DEFAULT NULL::character varying NULL,
    "trigger" integer DEFAULT 0 NOT NULL,
    "scheduled_at" timestamp with time zone NOT NULL,
    "started_at" timestamp with time zone NOT NULL,
    "completed_at" timestamp with time zone NULL,
    "state" integer DEFAULT 0 NOT NULL,
    "error_code" character varying(64) DEFAULT NULL::character varying NULL,
    "error_message" text NULL,
    "node_id" character varying(256) NOT NULL,
    "cancel_requested" boolean DEFAULT false NOT NULL,
    "data" text NOT NULL,
    "attempt" integer NULL,
    "retry_of" character varying(128) DEFAULT NULL::character varying NULL,
    "sequence" bigint DEFAULT '0'::bigint NOT NULL,
    CONSTRAINT "job_run_entity_pkey" PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS "outbox_message" (
    "id" uuid NOT NULL,
    "topic" character varying(512) NOT NULL,
    "payload" bytea NOT NULL,
    "created_at" timestamp with time zone NOT NULL,
    "dispatched_at" timestamp with time zone NULL,
    "attempts" integer DEFAULT 0 NOT NULL,
    "last_error" character varying(512) DEFAULT NULL::character varying NULL,
    CONSTRAINT "outbox_message_pkey" PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS "permission_grant_record" (
    "id" uuid NOT NULL,
    "subject_kind" character varying(32) NOT NULL,
    "subject_id" character varying(256) NOT NULL,
    "permission" character varying(256) NOT NULL,
    "tenant_id" character varying(128) NOT NULL,
    "created_at" timestamp with time zone NOT NULL,
    CONSTRAINT "permission_grant_record_pkey" PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS "scheduler_lease_entity" (
    "id" character varying(128) NOT NULL,
    "holder" character varying(256) NOT NULL,
    "token" character varying(128) NOT NULL,
    "expires_at" timestamp with time zone NOT NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "scheduler_lease_entity_pkey" PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS "scheduler_node_entity" (
    "id" character varying(256) NOT NULL,
    "started_at" timestamp with time zone NOT NULL,
    "last_heartbeat_at" timestamp with time zone NOT NULL,
    CONSTRAINT "scheduler_node_entity_pkey" PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS "sys_access_policy" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "code" character varying(100) NOT NULL,
    "name" character varying(200) NOT NULL,
    "description" character varying(1000) DEFAULT NULL::character varying NULL,
    "action" integer DEFAULT 0 NOT NULL,
    "priority" integer DEFAULT 0 NOT NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "effective_at" timestamp with time zone NULL,
    "expires_at" timestamp with time zone NULL,
    "template_code" character varying(100) DEFAULT NULL::character varying NULL,
    "remark" character varying(500) DEFAULT NULL::character varying NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_access_policy_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_access_policy_owner_tenant_id_code ON public.sys_access_policy USING btree (owner_tenant_id, code) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_access_policy_condition" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "policy_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "ordinal" integer DEFAULT 0 NOT NULL,
    "group_no" integer DEFAULT 0 NOT NULL,
    "attribute_name" character varying(200) NOT NULL,
    "operator" integer DEFAULT 0 NOT NULL,
    "is_negated" boolean DEFAULT false NOT NULL,
    "value" character varying(2000) DEFAULT NULL::character varying NULL,
    "description" character varying(500) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_access_policy_condition_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_access_policy_condition_owner_tenant_id_policy_id ON public.sys_access_policy_condition USING btree (owner_tenant_id, policy_id);

CREATE TABLE IF NOT EXISTS "sys_bot_config" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "code" character varying(64) NOT NULL,
    "name" character varying(128) NOT NULL,
    "provider" integer DEFAULT 0 NOT NULL,
    "webhook_url" character varying(500) NOT NULL,
    "secret" character varying(1024) DEFAULT NULL::character varying NULL,
    "keyword" character varying(100) DEFAULT NULL::character varying NULL,
    "is_default" boolean DEFAULT false NOT NULL,
    "is_enabled" boolean DEFAULT false NOT NULL,
    "order" integer DEFAULT 0 NOT NULL,
    "remark" character varying(500) DEFAULT NULL::character varying NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_bot_config_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_bot_config_owner_tenant_id_code ON public.sys_bot_config USING btree (owner_tenant_id, code) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_bot_message" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "sender_id" uuid NULL,
    "channel" integer DEFAULT 0 NOT NULL,
    "chat_id" character varying(64) DEFAULT NULL::character varying NULL,
    "content" text NOT NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "attempts" integer DEFAULT 0 NOT NULL,
    "max_attempts" integer DEFAULT 0 NOT NULL,
    "next_attempt_at" timestamp with time zone NULL,
    "sent_at" timestamp with time zone NULL,
    "error_message" character varying(2000) DEFAULT NULL::character varying NULL,
    "config_id" uuid NULL,
    "business_type" character varying(100) DEFAULT NULL::character varying NULL,
    "business_id" character varying(100) DEFAULT NULL::character varying NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_bot_message_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_bot_message_status_created_at ON public.sys_bot_message USING btree (status, created_at);

CREATE TABLE IF NOT EXISTS "sys_config" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "key" character varying(512) NOT NULL,
    "name" character varying(512) NOT NULL,
    "group" character varying(512) DEFAULT NULL::character varying NULL,
    "value" text NULL,
    "default_value" text NULL,
    "data_type" integer DEFAULT 0 NOT NULL,
    "is_encrypted" boolean DEFAULT false NOT NULL,
    "is_built_in" boolean DEFAULT false NOT NULL,
    "allow_tenant_override" boolean DEFAULT false NOT NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "order" integer DEFAULT 0 NOT NULL,
    "description" character varying(512) DEFAULT NULL::character varying NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_config_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_config_owner_tenant_id_key ON public.sys_config USING btree (owner_tenant_id, key) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_constraint_rule" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "code" character varying(100) NOT NULL,
    "name" character varying(200) NOT NULL,
    "type" integer DEFAULT 0 NOT NULL,
    "target_type" integer DEFAULT 0 NOT NULL,
    "max_allowed" integer DEFAULT 0 NOT NULL,
    "conditions" text NULL,
    "violation_action" integer DEFAULT 0 NOT NULL,
    "priority" integer DEFAULT 0 NOT NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "effective_at" timestamp with time zone NULL,
    "expires_at" timestamp with time zone NULL,
    "description" character varying(1000) DEFAULT NULL::character varying NULL,
    "remark" character varying(500) DEFAULT NULL::character varying NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_constraint_rule_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_constraint_rule_owner_tenant_id_code ON public.sys_constraint_rule USING btree (owner_tenant_id, code) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_constraint_rule_item" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "rule_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "target_id" character varying(128) NOT NULL,
    "group_no" integer DEFAULT 0 NOT NULL,
    "remark" character varying(500) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_constraint_rule_item_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_constraint_rule_item_rule_id_target_id ON public.sys_constraint_rule_item USING btree (rule_id, target_id);

CREATE TABLE IF NOT EXISTS "sys_department" (
    "id" uuid NOT NULL,
    "code" character varying(512) NOT NULL,
    "name" character varying(512) NOT NULL,
    "parent_id" uuid NULL,
    "leader_user_id" uuid NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "order" integer DEFAULT 0 NOT NULL,
    "description" character varying(512) DEFAULT NULL::character varying NULL,
    "tenant_id" character varying(512) NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_department_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_department_tenant_id_code ON public.sys_department USING btree (tenant_id, code) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_department_hierarchy" (
    "id" uuid NOT NULL,
    "ancestor_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "descendant_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "depth" integer DEFAULT 0 NOT NULL,
    "tenant_id" character varying(512) NOT NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_department_hierarchy_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_department_hierarchy_tenant_id_descendant_id ON public.sys_department_hierarchy USING btree (tenant_id, descendant_id);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_department_hierarchy_tenant_id_ancestor_id_descendant_id ON public.sys_department_hierarchy USING btree (tenant_id, ancestor_id, descendant_id);

CREATE TABLE IF NOT EXISTS "sys_dict" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "code" character varying(512) NOT NULL,
    "name" character varying(512) NOT NULL,
    "category" character varying(512) DEFAULT NULL::character varying NULL,
    "description" character varying(512) DEFAULT NULL::character varying NULL,
    "is_built_in" boolean DEFAULT false NOT NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "order" integer DEFAULT 0 NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_dict_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_dict_owner_tenant_id_code ON public.sys_dict USING btree (owner_tenant_id, code) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_dict_item" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "dict_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "parent_id" uuid NULL,
    "code" character varying(512) NOT NULL,
    "name" character varying(512) NOT NULL,
    "value" character varying(512) DEFAULT NULL::character varying NULL,
    "description" character varying(512) DEFAULT NULL::character varying NULL,
    "metadata" text NULL,
    "is_default" boolean DEFAULT false NOT NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "order" integer DEFAULT 0 NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_dict_item_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_dict_item_parent_id ON public.sys_dict_item USING btree (parent_id);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_dict_item_dict_id_code ON public.sys_dict_item USING btree (dict_id, code) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_edition" (
    "id" uuid NOT NULL,
    "code" character varying(512) NOT NULL,
    "name" character varying(512) NOT NULL,
    "description" character varying(512) DEFAULT NULL::character varying NULL,
    "is_default" boolean DEFAULT false NOT NULL,
    "order" integer DEFAULT 0 NOT NULL,
    "user_limit" integer NULL,
    "storage_limit_mb" bigint NULL,
    "price" numeric NULL,
    "billing_period_months" integer NULL,
    "is_free" boolean DEFAULT false NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_edition_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_edition_code ON public.sys_edition USING btree (code) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_edition_grant" (
    "id" uuid NOT NULL,
    "edition_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "permission_code" character varying(512) NOT NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_edition_grant_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_edition_grant_edition_id_permission_code ON public.sys_edition_grant USING btree (edition_id, permission_code);

CREATE TABLE IF NOT EXISTS "sys_email" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "sender_id" uuid NULL,
    "receiver_id" uuid NULL,
    "to_email" character varying(256) NOT NULL,
    "subject" character varying(512) NOT NULL,
    "content" text NULL,
    "is_html" boolean DEFAULT false NOT NULL,
    "template_code" character varying(64) DEFAULT NULL::character varying NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "attempts" integer DEFAULT 0 NOT NULL,
    "max_attempts" integer DEFAULT 0 NOT NULL,
    "next_attempt_at" timestamp with time zone NULL,
    "sent_at" timestamp with time zone NULL,
    "error_message" character varying(2000) DEFAULT NULL::character varying NULL,
    "config_id" uuid NULL,
    "business_type" character varying(100) DEFAULT NULL::character varying NULL,
    "business_id" character varying(100) DEFAULT NULL::character varying NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_email_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_email_owner_tenant_id_created_at ON public.sys_email USING btree (owner_tenant_id, created_at);
CREATE INDEX IF NOT EXISTS ix_sys_email_status_created_at ON public.sys_email USING btree (status, created_at);

CREATE TABLE IF NOT EXISTS "sys_email_config" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "code" character varying(64) NOT NULL,
    "name" character varying(128) NOT NULL,
    "smtp_host" character varying(256) NOT NULL,
    "smtp_port" integer DEFAULT 0 NOT NULL,
    "use_ssl" boolean DEFAULT false NOT NULL,
    "accept_invalid_certificate" boolean DEFAULT false NOT NULL,
    "from_email" character varying(256) NOT NULL,
    "from_name" character varying(128) DEFAULT NULL::character varying NULL,
    "user_name" character varying(256) DEFAULT NULL::character varying NULL,
    "password" character varying(1024) DEFAULT NULL::character varying NULL,
    "is_default" boolean DEFAULT false NOT NULL,
    "is_enabled" boolean DEFAULT false NOT NULL,
    "order" integer DEFAULT 0 NOT NULL,
    "remark" character varying(500) DEFAULT NULL::character varying NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_email_config_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_email_config_owner_tenant_id_code ON public.sys_email_config USING btree (owner_tenant_id, code) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_export_task" (
    "id" uuid NOT NULL,
    "business_type" character varying(128) NOT NULL,
    "task_name" character varying(256) NOT NULL,
    "scope" integer DEFAULT 0 NOT NULL,
    "format" integer DEFAULT 0 NOT NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "progress" integer DEFAULT 0 NOT NULL,
    "total_count" bigint DEFAULT '0'::bigint NOT NULL,
    "processed_count" bigint DEFAULT '0'::bigint NOT NULL,
    "query_snapshot" text NULL,
    "fields_snapshot" text NOT NULL,
    "file_id" uuid NULL,
    "file_name" character varying(256) DEFAULT NULL::character varying NULL,
    "file_size" bigint NULL,
    "error_message" character varying(1000) DEFAULT NULL::character varying NULL,
    "started_at" timestamp with time zone NULL,
    "finished_at" timestamp with time zone NULL,
    "requester_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "tenant_id" character varying(512) NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_export_task_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_export_task_status ON public.sys_export_task USING btree (status);
CREATE INDEX IF NOT EXISTS ix_sys_export_task_tenant_id_requester_id_created_at ON public.sys_export_task USING btree (tenant_id, requester_id, created_at);

CREATE TABLE IF NOT EXISTS "sys_field_security_rule" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "target_kind" integer DEFAULT 0 NOT NULL,
    "target_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "entity" character varying(64) NOT NULL,
    "field" character varying(64) NOT NULL,
    "strategy" integer DEFAULT 0 NOT NULL,
    "keep_head" integer NULL,
    "keep_tail" integer NULL,
    "replacement" character varying(100) DEFAULT NULL::character varying NULL,
    "is_editable" boolean DEFAULT false NOT NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "remark" character varying(512) DEFAULT NULL::character varying NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_field_security_rule_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_field_security_rule_owner_tenant_id_entity_status ON public.sys_field_security_rule USING btree (owner_tenant_id, entity, status);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_field_security_rule_target_field ON public.sys_field_security_rule USING btree (owner_tenant_id, target_kind, target_id, entity, field) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_file" (
    "id" uuid NOT NULL,
    "name" character varying(200) NOT NULL,
    "original_name" character varying(255) NOT NULL,
    "extension" character varying(20) DEFAULT NULL::character varying NULL,
    "kind" integer DEFAULT 0 NOT NULL,
    "content_type" character varying(100) NOT NULL,
    "size" bigint DEFAULT '0'::bigint NOT NULL,
    "hash" character varying(64) NOT NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "access_level" integer DEFAULT 0 NOT NULL,
    "is_temporary" boolean DEFAULT false NOT NULL,
    "expires_at" timestamp with time zone NULL,
    "source" character varying(32) NOT NULL,
    "tags" character varying(500) DEFAULT NULL::character varying NULL,
    "remark" character varying(500) DEFAULT NULL::character varying NULL,
    "download_count" integer DEFAULT 0 NOT NULL,
    "last_downloaded_at" timestamp with time zone NULL,
    "tenant_id" character varying(512) NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_file_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_file_tenant_id_created_at ON public.sys_file USING btree (tenant_id, created_at);
CREATE INDEX IF NOT EXISTS ix_sys_file_tenant_id_hash_size ON public.sys_file USING btree (tenant_id, hash, size);

CREATE TABLE IF NOT EXISTS "sys_file_storage" (
    "id" uuid NOT NULL,
    "file_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "storage_config_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "storage_type" integer DEFAULT 0 NOT NULL,
    "bucket" character varying(63) NOT NULL,
    "key" character varying(1024) NOT NULL,
    "is_primary" boolean DEFAULT false NOT NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "verified_at" timestamp with time zone NULL,
    "remark" character varying(500) DEFAULT NULL::character varying NULL,
    "tenant_id" character varying(512) NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_file_storage_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_file_storage_storage_config_id_bucket_key ON public.sys_file_storage USING btree (storage_config_id, bucket, key);
CREATE INDEX IF NOT EXISTS ix_sys_file_storage_tenant_id_file_id ON public.sys_file_storage USING btree (tenant_id, file_id);

CREATE TABLE IF NOT EXISTS "sys_file_upload" (
    "id" uuid NOT NULL,
    "user_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "file_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "storage_config_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "storage_type" integer DEFAULT 0 NOT NULL,
    "bucket" character varying(63) NOT NULL,
    "key" character varying(1024) NOT NULL,
    "upload_id" character varying(1024) NOT NULL,
    "original_name" character varying(255) NOT NULL,
    "content_type" character varying(100) DEFAULT NULL::character varying NULL,
    "size" bigint DEFAULT '0'::bigint NOT NULL,
    "part_size" bigint DEFAULT '0'::bigint NOT NULL,
    "part_count" integer DEFAULT 0 NOT NULL,
    "access_level" integer DEFAULT 0 NOT NULL,
    "is_temporary" boolean DEFAULT false NOT NULL,
    "file_expires_at" timestamp with time zone NULL,
    "tags" character varying(500) DEFAULT NULL::character varying NULL,
    "remark" character varying(500) DEFAULT NULL::character varying NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "expires_at" timestamp with time zone NOT NULL,
    "ended_at" timestamp with time zone NULL,
    "failure_code" character varying(64) DEFAULT NULL::character varying NULL,
    "tenant_id" character varying(512) NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_file_upload_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_file_upload_tenant_id_status_expires_at ON public.sys_file_upload USING btree (tenant_id, status, expires_at);

CREATE TABLE IF NOT EXISTS "sys_import_history" (
    "id" uuid NOT NULL,
    "user_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "page_code" character varying(100) NOT NULL,
    "resource_code" character varying(100) DEFAULT NULL::character varying NULL,
    "file_name" character varying(256) NOT NULL,
    "total_count" integer DEFAULT 0 NOT NULL,
    "success_count" integer DEFAULT 0 NOT NULL,
    "fail_count" integer DEFAULT 0 NOT NULL,
    "error_summary" text NULL,
    "tenant_id" character varying(512) NOT NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_import_history_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_import_history_tenant_id_user_id_page_code_created_at ON public.sys_import_history USING btree (tenant_id, user_id, page_code, created_at);

CREATE TABLE IF NOT EXISTS "sys_menu" (
    "id" uuid NOT NULL,
    "code" character varying(512) NOT NULL,
    "kind" integer DEFAULT 0 NOT NULL,
    "package" character varying(512) NOT NULL,
    "parent_id" uuid NULL,
    "route" character varying(512) NOT NULL,
    "component" character varying(512) NOT NULL,
    "title_key" character varying(512) NOT NULL,
    "title" character varying(512) DEFAULT NULL::character varying NULL,
    "icon" character varying(512) DEFAULT NULL::character varying NULL,
    "order" integer DEFAULT 0 NOT NULL,
    "permission" character varying(512) DEFAULT NULL::character varying NULL,
    "external_url" character varying(512) DEFAULT NULL::character varying NULL,
    "is_visible" boolean DEFAULT false NOT NULL,
    "keep_alive" boolean DEFAULT false NOT NULL,
    "is_affix" boolean DEFAULT false NOT NULL,
    "is_customized" boolean DEFAULT false NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_menu_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_menu_code ON public.sys_menu USING btree (code) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_message_template" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "code" character varying(64) NOT NULL,
    "channel" integer DEFAULT 0 NOT NULL,
    "name" character varying(128) NOT NULL,
    "subject" character varying(256) DEFAULT NULL::character varying NULL,
    "content" text NOT NULL,
    "is_html" boolean DEFAULT false NOT NULL,
    "description" character varying(512) DEFAULT NULL::character varying NULL,
    "is_enabled" boolean DEFAULT false NOT NULL,
    "order" integer DEFAULT 0 NOT NULL,
    "remark" character varying(500) DEFAULT NULL::character varying NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_message_template_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_message_template_owner_tenant_id_channel_code ON public.sys_message_template USING btree (owner_tenant_id, channel, code) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_migration_history" (
    "id" uuid NOT NULL,
    "version" character varying(32) NOT NULL,
    "script_name" character varying(256) NOT NULL,
    "executed_at" timestamp with time zone NOT NULL,
    "duration_ms" bigint DEFAULT '0'::bigint NOT NULL,
    "success" boolean DEFAULT false NOT NULL,
    "node_name" character varying(128) DEFAULT NULL::character varying NULL,
    "error_message" text NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_migration_history_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_migration_history_executed_at ON public.sys_migration_history USING btree (executed_at);

CREATE TABLE IF NOT EXISTS "sys_notification" (
    "id" uuid NOT NULL,
    "title" character varying(200) NOT NULL,
    "content" text NULL,
    "content_format" integer DEFAULT 0 NOT NULL,
    "notification_type" integer DEFAULT 0 NOT NULL,
    "priority" integer DEFAULT 0 NOT NULL,
    "icon" character varying(100) DEFAULT NULL::character varying NULL,
    "link" character varying(500) DEFAULT NULL::character varying NULL,
    "business_type" character varying(100) DEFAULT NULL::character varying NULL,
    "business_id" character varying(100) DEFAULT NULL::character varying NULL,
    "delivery_channels" integer DEFAULT 0 NOT NULL,
    "target_type" integer DEFAULT 0 NOT NULL,
    "target_ids" text NULL,
    "need_confirm" boolean DEFAULT false NOT NULL,
    "is_mandatory" boolean DEFAULT false NOT NULL,
    "is_banner" boolean DEFAULT false NOT NULL,
    "is_popup" boolean DEFAULT false NOT NULL,
    "starts_at" timestamp with time zone NULL,
    "expires_at" timestamp with time zone NULL,
    "is_published" boolean DEFAULT false NOT NULL,
    "published_at" timestamp with time zone NULL,
    "publisher_id" uuid NULL,
    "recipient_count" integer DEFAULT 0 NOT NULL,
    "remark" character varying(500) DEFAULT NULL::character varying NULL,
    "tenant_id" character varying(512) NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_notification_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_notification_tenant_id_created_at ON public.sys_notification USING btree (tenant_id, created_at);

CREATE TABLE IF NOT EXISTS "sys_numbering_allocation" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "rule_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "rule_code" character varying(512) NOT NULL,
    "request_tenant_id" character varying(512) NOT NULL,
    "idempotency_key" character varying(512) NOT NULL,
    "fingerprint" character varying(512) NOT NULL,
    "count" integer DEFAULT 0 NOT NULL,
    "start_value" bigint DEFAULT '0'::bigint NOT NULL,
    "end_value" bigint DEFAULT '0'::bigint NOT NULL,
    "period_key" character varying(512) NOT NULL,
    "prefix_snapshot" character varying(512) DEFAULT NULL::character varying NULL,
    "separator_snapshot" character varying(512) NOT NULL,
    "date_text_snapshot" character varying(512) DEFAULT NULL::character varying NULL,
    "serial_length_snapshot" integer DEFAULT 0 NOT NULL,
    "generated_at" timestamp with time zone NOT NULL,
    "business_type" character varying(512) DEFAULT NULL::character varying NULL,
    "business_id" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_numbering_allocation_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_numbering_allocation_rule_id_generated_at ON public.sys_numbering_allocation USING btree (rule_id, generated_at);
CREATE INDEX IF NOT EXISTS ix_sys_numbering_allocation_rule_id_period_key_end_value ON public.sys_numbering_allocation USING btree (rule_id, period_key, end_value);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_numbering_allocation_idempotency ON public.sys_numbering_allocation USING btree (owner_tenant_id, rule_id, request_tenant_id, idempotency_key);

CREATE TABLE IF NOT EXISTS "sys_numbering_rule" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "code" character varying(512) NOT NULL,
    "name" character varying(512) NOT NULL,
    "prefix" character varying(512) DEFAULT NULL::character varying NULL,
    "separator" character varying(512) NOT NULL,
    "date_format" integer DEFAULT 0 NOT NULL,
    "serial_length" integer DEFAULT 0 NOT NULL,
    "reset_cycle" integer DEFAULT 0 NOT NULL,
    "time_zone_id" character varying(512) NOT NULL,
    "current_value" bigint DEFAULT '0'::bigint NOT NULL,
    "current_period" character varying(512) NOT NULL,
    "current_period_ordinal" bigint DEFAULT '0'::bigint NOT NULL,
    "has_allocated" boolean DEFAULT false NOT NULL,
    "allow_tenant_use" boolean DEFAULT false NOT NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "order" integer DEFAULT 0 NOT NULL,
    "description" character varying(512) DEFAULT NULL::character varying NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_numbering_rule_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_numbering_rule_owner_tenant_id_code ON public.sys_numbering_rule USING btree (owner_tenant_id, code) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_o_auth_app" (
    "id" uuid NOT NULL,
    "app_name" character varying(100) NOT NULL,
    "app_description" character varying(500) DEFAULT NULL::character varying NULL,
    "client_id" character varying(100) NOT NULL,
    "client_type" integer DEFAULT 0 NOT NULL,
    "client_secret_hash" character varying(64) DEFAULT NULL::character varying NULL,
    "app_type" integer DEFAULT 0 NOT NULL,
    "grant_types" character varying(200) NOT NULL,
    "redirect_uris" character varying(2048) NOT NULL,
    "scopes" character varying(200) NOT NULL,
    "access_token_lifetime" integer DEFAULT 0 NOT NULL,
    "refresh_token_lifetime" integer DEFAULT 0 NOT NULL,
    "authorization_code_lifetime" integer DEFAULT 0 NOT NULL,
    "logo" character varying(500) DEFAULT NULL::character varying NULL,
    "homepage" character varying(500) DEFAULT NULL::character varying NULL,
    "skip_consent" boolean DEFAULT false NOT NULL,
    "is_enabled" boolean DEFAULT false NOT NULL,
    "owner_user_id" uuid NULL,
    "owner_user_name" character varying(256) DEFAULT NULL::character varying NULL,
    "remark" character varying(500) DEFAULT NULL::character varying NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_o_auth_app_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_o_auth_app_owner_user_id ON public.sys_o_auth_app USING btree (owner_user_id);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_o_auth_app_client_id ON public.sys_o_auth_app USING btree (client_id) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_o_auth_code" (
    "id" uuid NOT NULL,
    "code_hash" character varying(64) NOT NULL,
    "client_id" character varying(100) NOT NULL,
    "user_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "redirect_uri" character varying(500) NOT NULL,
    "scopes" character varying(200) NOT NULL,
    "code_challenge" character varying(128) DEFAULT NULL::character varying NULL,
    "code_challenge_method" character varying(10) DEFAULT NULL::character varying NULL,
    "nonce" character varying(200) DEFAULT NULL::character varying NULL,
    "expires_at" timestamp with time zone NOT NULL,
    "is_consumed" boolean DEFAULT false NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_o_auth_code_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_o_auth_code_expires_at ON public.sys_o_auth_code USING btree (expires_at);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_o_auth_code_code_hash ON public.sys_o_auth_code USING btree (code_hash) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_o_auth_consent" (
    "id" uuid NOT NULL,
    "user_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "client_id" character varying(100) NOT NULL,
    "scopes" character varying(200) NOT NULL,
    "granted_at" timestamp with time zone NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_o_auth_consent_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_o_auth_consent_user_id_client_id ON public.sys_o_auth_consent USING btree (user_id, client_id) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_o_auth_signing_key" (
    "id" uuid NOT NULL,
    "key_id" character varying(64) NOT NULL,
    "private_key" text NOT NULL,
    "is_active" boolean DEFAULT false NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_o_auth_signing_key_pkey" PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS "sys_o_auth_token" (
    "id" uuid NOT NULL,
    "token_hash" character varying(64) NOT NULL,
    "kind" integer DEFAULT 0 NOT NULL,
    "family_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "client_id" character varying(100) NOT NULL,
    "user_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "scopes" character varying(200) NOT NULL,
    "expires_at" timestamp with time zone NOT NULL,
    "revoked_at" timestamp with time zone NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_o_auth_token_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_o_auth_token_expires_at ON public.sys_o_auth_token USING btree (expires_at);
CREATE INDEX IF NOT EXISTS ix_sys_o_auth_token_family_id ON public.sys_o_auth_token USING btree (family_id);
CREATE INDEX IF NOT EXISTS ix_sys_o_auth_token_user_id_client_id ON public.sys_o_auth_token USING btree (user_id, client_id);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_o_auth_token_token_hash ON public.sys_o_auth_token USING btree (token_hash) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_password_history" (
    "id" uuid NOT NULL,
    "user_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "password_hash" character varying(512) NOT NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_password_history_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_password_history_user_id_created_at ON public.sys_password_history USING btree (user_id, created_at);

CREATE TABLE IF NOT EXISTS "sys_permission_condition" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "subject_kind" integer DEFAULT 0 NOT NULL,
    "subject_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "permission" character varying(128) NOT NULL,
    "action" integer DEFAULT 0 NOT NULL,
    "group_no" integer DEFAULT 0 NOT NULL,
    "attribute_name" character varying(200) NOT NULL,
    "operator" integer DEFAULT 0 NOT NULL,
    "is_negated" boolean DEFAULT false NOT NULL,
    "value" character varying(2000) DEFAULT NULL::character varying NULL,
    "description" character varying(500) DEFAULT NULL::character varying NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_permission_condition_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_permission_condition_owner_subject ON public.sys_permission_condition USING btree (owner_tenant_id, subject_kind, subject_id);

CREATE TABLE IF NOT EXISTS "sys_permission_delegation" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "delegator_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "delegatee_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "permission" character varying(128) DEFAULT NULL::character varying NULL,
    "role_id" uuid NULL,
    "effective_at" timestamp with time zone NULL,
    "expires_at" timestamp with time zone NOT NULL,
    "reason" character varying(512) DEFAULT NULL::character varying NULL,
    "revoked_at" timestamp with time zone NULL,
    "revoked_by" uuid NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_permission_delegation_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_permission_delegation_owner_delegatee_revoked ON public.sys_permission_delegation USING btree (owner_tenant_id, delegatee_id, revoked_at);
CREATE INDEX IF NOT EXISTS ix_sys_permission_delegation_owner_delegator_delegatee ON public.sys_permission_delegation USING btree (owner_tenant_id, delegator_id, delegatee_id);
CREATE INDEX IF NOT EXISTS ix_sys_permission_delegation_owner_tenant_id_created_at ON public.sys_permission_delegation USING btree (owner_tenant_id, created_at);
CREATE INDEX IF NOT EXISTS ix_sys_permission_delegation_role_id ON public.sys_permission_delegation USING btree (role_id);

CREATE TABLE IF NOT EXISTS "sys_permission_rule" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "subject_kind" integer DEFAULT 0 NOT NULL,
    "subject_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "permission" character varying(128) NOT NULL,
    "action" integer DEFAULT 0 NOT NULL,
    "effective_at" timestamp with time zone NULL,
    "expires_at" timestamp with time zone NULL,
    "reason" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_permission_rule_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_permission_rule_owner_subject_permission ON public.sys_permission_rule USING btree (owner_tenant_id, subject_kind, subject_id, permission);

CREATE TABLE IF NOT EXISTS "sys_position" (
    "id" uuid NOT NULL,
    "code" character varying(512) NOT NULL,
    "name" character varying(512) NOT NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "order" integer DEFAULT 0 NOT NULL,
    "description" character varying(512) DEFAULT NULL::character varying NULL,
    "tenant_id" character varying(512) NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_position_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_position_tenant_id_code ON public.sys_position USING btree (tenant_id, code) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_role" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "code" character varying(512) NOT NULL,
    "name" character varying(512) NOT NULL,
    "description" character varying(512) DEFAULT NULL::character varying NULL,
    "order" integer DEFAULT 0 NOT NULL,
    "is_built_in" boolean DEFAULT false NOT NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "data_scope" integer DEFAULT 0 NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_role_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_role_owner_tenant_id_code ON public.sys_role USING btree (owner_tenant_id, code) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_role_data_scope" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "role_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "department_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "include_children" boolean DEFAULT false NOT NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_role_data_scope_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_role_data_scope_role_id_department_id ON public.sys_role_data_scope USING btree (role_id, department_id);

CREATE TABLE IF NOT EXISTS "sys_role_inheritance" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "role_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "parent_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_role_inheritance_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_role_inheritance_owner_tenant_id_role_id_parent_id ON public.sys_role_inheritance USING btree (owner_tenant_id, role_id, parent_id);

CREATE TABLE IF NOT EXISTS "sys_session_role" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "session_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "user_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "role_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "activated_at" timestamp with time zone NOT NULL,
    "deactivated_at" timestamp with time zone NULL,
    "reason" character varying(500) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_session_role_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_session_role_owner_tenant_id_activated_at ON public.sys_session_role USING btree (owner_tenant_id, activated_at);
CREATE INDEX IF NOT EXISTS ix_sys_session_role_owner_tenant_id_user_id ON public.sys_session_role USING btree (owner_tenant_id, user_id);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_session_role_session_id_owner_tenant_id_role_id ON public.sys_session_role USING btree (session_id, owner_tenant_id, role_id);

CREATE TABLE IF NOT EXISTS "sys_sms" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "sender_id" uuid NULL,
    "receiver_id" uuid NULL,
    "to_phone" character varying(32) NOT NULL,
    "content" character varying(1000) NOT NULL,
    "template_code" character varying(64) DEFAULT NULL::character varying NULL,
    "template_params" text NULL,
    "provider" integer NULL,
    "provider_message_id" character varying(128) DEFAULT NULL::character varying NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "attempts" integer DEFAULT 0 NOT NULL,
    "max_attempts" integer DEFAULT 0 NOT NULL,
    "next_attempt_at" timestamp with time zone NULL,
    "sent_at" timestamp with time zone NULL,
    "error_message" character varying(2000) DEFAULT NULL::character varying NULL,
    "config_id" uuid NULL,
    "business_type" character varying(100) DEFAULT NULL::character varying NULL,
    "business_id" character varying(100) DEFAULT NULL::character varying NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_sms_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_sms_owner_tenant_id_created_at ON public.sys_sms USING btree (owner_tenant_id, created_at);
CREATE INDEX IF NOT EXISTS ix_sys_sms_status_created_at ON public.sys_sms USING btree (status, created_at);

CREATE TABLE IF NOT EXISTS "sys_sms_config" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "code" character varying(64) NOT NULL,
    "name" character varying(128) NOT NULL,
    "provider" integer DEFAULT 0 NOT NULL,
    "access_key_id" character varying(256) NOT NULL,
    "access_key_secret" character varying(1024) DEFAULT NULL::character varying NULL,
    "sdk_app_id" character varying(64) DEFAULT NULL::character varying NULL,
    "sign_name" character varying(64) NOT NULL,
    "region" character varying(64) DEFAULT NULL::character varying NULL,
    "template_map" text NULL,
    "is_default" boolean DEFAULT false NOT NULL,
    "is_enabled" boolean DEFAULT false NOT NULL,
    "order" integer DEFAULT 0 NOT NULL,
    "remark" character varying(500) DEFAULT NULL::character varying NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_sms_config_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_sms_config_owner_tenant_id_code ON public.sys_sms_config USING btree (owner_tenant_id, code) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_storage_config" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "code" character varying(100) NOT NULL,
    "name" character varying(200) NOT NULL,
    "type" integer DEFAULT 0 NOT NULL,
    "endpoint" character varying(500) DEFAULT NULL::character varying NULL,
    "region" character varying(100) DEFAULT NULL::character varying NULL,
    "bucket" character varying(63) NOT NULL,
    "access_key_id" character varying(200) DEFAULT NULL::character varying NULL,
    "secret_access_key" character varying(1000) DEFAULT NULL::character varying NULL,
    "is_default" boolean DEFAULT false NOT NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "order" integer DEFAULT 0 NOT NULL,
    "remark" character varying(500) DEFAULT NULL::character varying NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_storage_config_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_storage_config_owner_tenant_id_code ON public.sys_storage_config USING btree (owner_tenant_id, code) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_telegram_bot" (
    "id" uuid NOT NULL,
    "name" character varying(64) NOT NULL,
    "token" character varying(1024) NOT NULL,
    "admin_users" character varying(500) DEFAULT NULL::character varying NULL,
    "allowed_group_chat_ids" character varying(500) DEFAULT NULL::character varying NULL,
    "allowed_commands" character varying(500) DEFAULT NULL::character varying NULL,
    "enable_fallback_reply" boolean DEFAULT false NOT NULL,
    "broadcast_chat_ids" character varying(500) DEFAULT NULL::character varying NULL,
    "is_enabled" boolean DEFAULT false NOT NULL,
    "order" integer DEFAULT 0 NOT NULL,
    "remark" character varying(500) DEFAULT NULL::character varying NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_telegram_bot_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_telegram_bot_name ON public.sys_telegram_bot USING btree (name) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_telegram_message" (
    "id" uuid NOT NULL,
    "bot_name" character varying(64) NOT NULL,
    "bot_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "chat_id" bigint DEFAULT '0'::bigint NOT NULL,
    "api_method" character varying(50) NOT NULL,
    "message_type" character varying(50) NOT NULL,
    "content" text NULL,
    "parse_mode" character varying(20) DEFAULT NULL::character varying NULL,
    "telegram_message_id" bigint NULL,
    "success" boolean DEFAULT false NOT NULL,
    "error_code" integer NULL,
    "error_message" character varying(1000) DEFAULT NULL::character varying NULL,
    "elapsed_ms" bigint DEFAULT '0'::bigint NOT NULL,
    "sent_at" timestamp with time zone NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_telegram_message_pkey" PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS "sys_tenant" (
    "id" uuid NOT NULL,
    "tenant_id" character varying(512) NOT NULL,
    "name" character varying(512) NOT NULL,
    "description" character varying(512) DEFAULT NULL::character varying NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "expires_at" timestamp with time zone NULL,
    "admin_user_id" uuid NULL,
    "order" integer DEFAULT 0 NOT NULL,
    "edition_id" uuid NULL,
    "user_limit" integer NULL,
    "storage_limit_mb" bigint NULL,
    "isolation_mode" integer DEFAULT 0 NOT NULL,
    "database_provider" character varying(32) DEFAULT NULL::character varying NULL,
    "connection_string" character varying(2048) DEFAULT NULL::character varying NULL,
    "database_status" integer DEFAULT 0 NOT NULL,
    "database_error" character varying(1024) DEFAULT NULL::character varying NULL,
    "database_initialized_at" timestamp with time zone NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_tenant_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_tenant_admin_user_id ON public.sys_tenant USING btree (admin_user_id);
CREATE INDEX IF NOT EXISTS ix_sys_tenant_edition_id ON public.sys_tenant USING btree (edition_id);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_tenant_tenant_id ON public.sys_tenant USING btree (tenant_id) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_tenant_member" (
    "id" uuid NOT NULL,
    "tenant_id" character varying(512) NOT NULL,
    "user_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "member_type" integer DEFAULT 0 NOT NULL,
    "effective_at" timestamp with time zone NULL,
    "expires_at" timestamp with time zone NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_tenant_member_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_tenant_member_user_id ON public.sys_tenant_member USING btree (user_id);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_tenant_member_tenant_id_user_id ON public.sys_tenant_member USING btree (tenant_id, user_id) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_user" (
    "id" uuid NOT NULL,
    "user_name" character varying(512) NOT NULL,
    "display_name" character varying(512) NOT NULL,
    "email" character varying(512) DEFAULT NULL::character varying NULL,
    "phone_number" character varying(512) DEFAULT NULL::character varying NULL,
    "email_verified" boolean DEFAULT false NOT NULL,
    "phone_verified" boolean DEFAULT false NOT NULL,
    "home_tenant_id" character varying(512) NOT NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "last_login_at" timestamp with time zone NULL,
    "real_name" character varying(50) DEFAULT NULL::character varying NULL,
    "gender" integer DEFAULT 0 NOT NULL,
    "birthday" timestamp with time zone NULL,
    "country" character varying(50) DEFAULT NULL::character varying NULL,
    "remark" character varying(500) DEFAULT NULL::character varying NULL,
    "avatar_file_id" uuid NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_user_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_user_home_tenant_id_created_at ON public.sys_user USING btree (home_tenant_id, created_at);
CREATE INDEX IF NOT EXISTS ix_sys_user_home_tenant_id_phone_number ON public.sys_user USING btree (home_tenant_id, phone_number);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_user_home_tenant_id_user_name ON public.sys_user USING btree (home_tenant_id, user_name) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_user_api_credential" (
    "id" uuid NOT NULL,
    "user_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "name" character varying(100) NOT NULL,
    "app_key" character varying(64) NOT NULL,
    "secret_cipher" character varying(1024) NOT NULL,
    "is_enabled" boolean DEFAULT false NOT NULL,
    "last_used_at" timestamp with time zone NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_user_api_credential_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_user_api_credential_user_id ON public.sys_user_api_credential USING btree (user_id);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_user_api_credential_app_key ON public.sys_user_api_credential USING btree (app_key) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_user_data_scope" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "user_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "data_scope" integer DEFAULT 0 NOT NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_user_data_scope_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_user_data_scope_owner_tenant_id_user_id ON public.sys_user_data_scope USING btree (owner_tenant_id, user_id);

CREATE TABLE IF NOT EXISTS "sys_user_data_scope_department" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "user_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "department_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "include_children" boolean DEFAULT false NOT NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_user_data_scope_department_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_user_data_scope_department_owner_user_department ON public.sys_user_data_scope_department USING btree (owner_tenant_id, user_id, department_id);

CREATE TABLE IF NOT EXISTS "sys_user_department" (
    "id" uuid NOT NULL,
    "user_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "department_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "position_id" uuid NULL,
    "is_primary" boolean DEFAULT false NOT NULL,
    "employee_number" character varying(512) DEFAULT NULL::character varying NULL,
    "job_grade" character varying(512) DEFAULT NULL::character varying NULL,
    "hired_at" timestamp with time zone NULL,
    "order" integer DEFAULT 0 NOT NULL,
    "tenant_id" character varying(512) NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_user_department_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_user_department_tenant_id_department_id ON public.sys_user_department USING btree (tenant_id, department_id);
CREATE INDEX IF NOT EXISTS ix_sys_user_department_tenant_id_position_id ON public.sys_user_department USING btree (tenant_id, position_id);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_user_department_tenant_id_user_id_department_id ON public.sys_user_department USING btree (tenant_id, user_id, department_id) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_user_external_login" (
    "id" uuid NOT NULL,
    "user_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "home_tenant_id" character varying(64) NOT NULL,
    "provider" character varying(32) NOT NULL,
    "provider_key" character varying(200) NOT NULL,
    "display_name" character varying(200) DEFAULT NULL::character varying NULL,
    "email" character varying(200) DEFAULT NULL::character varying NULL,
    "avatar_url" character varying(500) DEFAULT NULL::character varying NULL,
    "last_login_at" timestamp with time zone NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_user_external_login_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_user_external_login_home_tenant_id_provider_provider_key ON public.sys_user_external_login USING btree (home_tenant_id, provider, provider_key) WHERE (is_deleted = false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_user_external_login_user_id_provider ON public.sys_user_external_login USING btree (user_id, provider) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_user_notification" (
    "id" uuid NOT NULL,
    "notification_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "user_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "status" integer DEFAULT 0 NOT NULL,
    "read_at" timestamp with time zone NULL,
    "confirmed_at" timestamp with time zone NULL,
    "popup_shown_at" timestamp with time zone NULL,
    "tenant_id" character varying(512) NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_user_notification_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_user_notification_tenant_id_user_id_status ON public.sys_user_notification USING btree (tenant_id, user_id, status);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_user_notification_tenant_id_notification_id_user_id ON public.sys_user_notification USING btree (tenant_id, notification_id, user_id) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_user_notification_preference" (
    "id" uuid NOT NULL,
    "user_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "channel_in_app" boolean DEFAULT false NOT NULL,
    "channel_email" boolean DEFAULT false NOT NULL,
    "channel_sms" boolean DEFAULT false NOT NULL,
    "type_announcement" boolean DEFAULT false NOT NULL,
    "type_task" boolean DEFAULT false NOT NULL,
    "type_security" boolean DEFAULT false NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_user_notification_preference_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_user_notification_preference_user_id ON public.sys_user_notification_preference USING btree (user_id) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_user_role" (
    "id" uuid NOT NULL,
    "owner_tenant_id" character varying(512) NOT NULL,
    "user_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "role_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "effective_at" timestamp with time zone NULL,
    "expires_at" timestamp with time zone NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_user_role_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_user_role_owner_tenant_id_user_id ON public.sys_user_role USING btree (owner_tenant_id, user_id);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_user_role_role_id_user_id ON public.sys_user_role USING btree (role_id, user_id);

CREATE TABLE IF NOT EXISTS "sys_user_security" (
    "id" uuid NOT NULL,
    "user_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "password_hash" character varying(512) NOT NULL,
    "password_changed_at" timestamp with time zone NULL,
    "user_name_changed_at" timestamp with time zone NULL,
    "failed_access_count" integer DEFAULT 0 NOT NULL,
    "lockout_end_at" timestamp with time zone NULL,
    "security_stamp" character varying(512) NOT NULL,
    "must_change_password" boolean DEFAULT false NOT NULL,
    "two_factor_enabled" boolean DEFAULT false NOT NULL,
    "two_factor_methods" integer DEFAULT 0 NOT NULL,
    "two_factor_secret" character varying(1024) DEFAULT NULL::character varying NULL,
    "two_factor_last_step" bigint NULL,
    "api_signature_algorithm" character varying(32) DEFAULT NULL::character varying NULL,
    "api_ip_allow_list" character varying(2048) DEFAULT NULL::character varying NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_user_security_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_user_security_user_id ON public.sys_user_security USING btree (user_id) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_user_session" (
    "id" uuid NOT NULL,
    "user_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "active_tenant_id" character varying(512) DEFAULT NULL::character varying NULL,
    "refresh_token_hash" character varying(512) NOT NULL,
    "issued_at" timestamp with time zone NOT NULL,
    "refresh_expires_at" timestamp with time zone NOT NULL,
    "last_seen_at" timestamp with time zone NULL,
    "revoked_at" timestamp with time zone NULL,
    "revoke_reason" character varying(512) DEFAULT NULL::character varying NULL,
    "client_info" character varying(512) DEFAULT NULL::character varying NULL,
    "ip_address" character varying(512) DEFAULT NULL::character varying NULL,
    "browser" character varying(64) DEFAULT NULL::character varying NULL,
    "os" character varying(64) DEFAULT NULL::character varying NULL,
    "device" character varying(32) DEFAULT NULL::character varying NULL,
    "location" character varying(128) DEFAULT NULL::character varying NULL,
    "impersonator_user_id" uuid NULL,
    "impersonator_session_id" uuid NULL,
    "lock_reason" character varying(32) DEFAULT NULL::character varying NULL,
    "locked_at" timestamp with time zone NULL,
    "lock_password_hash" character varying(256) DEFAULT NULL::character varying NULL,
    "unlock_failed_attempts" integer DEFAULT 0 NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_user_session_pkey" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_sys_user_session_active_tenant_id_revoked_at_issued_at ON public.sys_user_session USING btree (active_tenant_id, revoked_at, issued_at);
CREATE INDEX IF NOT EXISTS ix_sys_user_session_impersonator_session_id ON public.sys_user_session USING btree (impersonator_session_id);
CREATE INDEX IF NOT EXISTS ix_sys_user_session_impersonator_user_id ON public.sys_user_session USING btree (impersonator_user_id);
CREATE INDEX IF NOT EXISTS ix_sys_user_session_user_id_revoked_at ON public.sys_user_session USING btree (user_id, revoked_at);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_user_session_refresh_token_hash ON public.sys_user_session USING btree (refresh_token_hash) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_user_setting" (
    "id" uuid NOT NULL,
    "user_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "scene" integer DEFAULT 0 NOT NULL,
    "key" character varying(512) NOT NULL,
    "value" text NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_user_setting_pkey" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_user_setting_user_id_scene_key ON public.sys_user_setting USING btree (user_id, scene, key) WHERE (is_deleted = false);

CREATE TABLE IF NOT EXISTS "sys_user_statistics" (
    "id" uuid NOT NULL,
    "user_id" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "login_count" integer DEFAULT 0 NOT NULL,
    "last_login_at" timestamp with time zone NULL,
    "tenant_id" character varying(512) NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "modified_by" character varying(512) DEFAULT NULL::character varying NULL,
    "is_deleted" boolean DEFAULT false NOT NULL,
    "deleted_at" timestamp with time zone NULL,
    "deleted_by" character varying(512) DEFAULT NULL::character varying NULL,
    "created_at" timestamp with time zone NOT NULL,
    "created_by" character varying(512) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_user_statistics_pkey" PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS "sys_version" (
    "id" uuid NOT NULL,
    "app_version" character varying(64) NOT NULL,
    "db_version" character varying(32) DEFAULT NULL::character varying NULL,
    "is_upgrading" boolean DEFAULT false NOT NULL,
    "upgrade_node" character varying(128) DEFAULT NULL::character varying NULL,
    "upgrade_started_at" timestamp with time zone NULL,
    "upgraded_at" timestamp with time zone NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "sys_version_pkey" PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS "tenant_record" (
    "id" character varying(128) NOT NULL,
    "display_name" character varying(256) DEFAULT NULL::character varying NULL,
    "is_enabled" boolean DEFAULT false NOT NULL,
    "properties_json" text NOT NULL,
    "created_at" timestamp with time zone NOT NULL,
    "modified_at" timestamp with time zone NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "tenant_record_pkey" PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS "trigger_entity" (
    "id" character varying(128) NOT NULL,
    "job" character varying(128) NOT NULL,
    "name" character varying(256) NOT NULL,
    "schedule" character varying(256) NOT NULL,
    "data" text NOT NULL,
    "priority" integer DEFAULT 0 NOT NULL,
    "misfire_policy" integer DEFAULT 0 NOT NULL,
    "description" character varying(1024) DEFAULT NULL::character varying NULL,
    "state" integer DEFAULT 0 NOT NULL,
    "next_fire_at" timestamp with time zone NULL,
    "previous_fire_at" timestamp with time zone NULL,
    "acquired_by" character varying(256) DEFAULT NULL::character varying NULL,
    "acquired_at" timestamp with time zone NULL,
    "fire_count" bigint DEFAULT '0'::bigint NOT NULL,
    "error" text NULL,
    "local" boolean DEFAULT false NOT NULL,
    "transient" boolean DEFAULT false NOT NULL,
    "run_as" integer DEFAULT 0 NOT NULL,
    "attempt" integer NULL,
    "retry_of" character varying(128) DEFAULT NULL::character varying NULL,
    "concurrency_stamp" character varying(32) NOT NULL,
    CONSTRAINT "trigger_entity_pkey" PRIMARY KEY (id)
);
