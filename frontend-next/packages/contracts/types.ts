// 由 scripts/gen-contracts.mjs 从 contracts/openapi.json 生成；不要手改，改后端契约后执行 pnpm gen:contracts。
/* eslint-disable */
export interface paths {
    "/api/access-logs/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get an access log of the caller’s side. Requires auditing.access-logs.read. */
        get: operations["access-logs.get"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/access-logs:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List access logs page by page: every operation call of the caller’s side with its result, duration and client; filter by time range, trace, user, session, operation, result and status code. Requires auditing.access-logs.read. */
        post: operations["access-logs.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth:activate-roles": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Change the roles the current session has activated (all held right now and not violating dynamic separation of duty); returns new tokens. */
        post: operations["auth.activate-roles"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth:captcha": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Issue a captcha (4 digits, valid for one use within 5 minutes). */
        post: operations["auth.captcha"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth:external-login": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Third-party sign-in: exchanges the one-time ticket from the callback for the account bound in the chosen home tenant; unbound identities are rejected (sign in with a password and bind it in your profile first). Account status, lockout and two-factor apply as for password sign-in. */
        post: operations["auth.external-login"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth:lock": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Lock the current session (screen lock) with a one-off lock password; afterwards every request of this session except unlock and logout is rejected with IDENTITY_DENIED_002. Requires sign-in only. */
        post: operations["auth.lock"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth:login": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Sign in with user name and password (tenant optional; platform accounts leave it empty; when the captcha is on, include the id and code from auth.captcha). Returns a second-step ticket when the account has two-factor on, otherwise access and refresh tokens. */
        post: operations["auth.login"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth:login-config": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Sign-in page settings: available sign-in methods, third-party providers and whether a captcha is required. */
        get: operations["auth.login-config"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth:logout": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Revoke the session of the current token. */
        post: operations["auth.logout"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth:me": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Return the current user, tenant, session and roles. */
        get: operations["auth.me"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth:permissions": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Return the current principal's effective permissions, roles, menu tree and page codes. */
        get: operations["auth.permissions"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth:refresh": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Exchange a refresh token for new tokens; the refresh token rotates and the old one stops working. */
        post: operations["auth.refresh"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth:session-roles": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** The roles the current session holds right now and which of them are activated. Requires signing in only. */
        get: operations["auth.session-roles"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth:switch-tenant": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Switch the current session to another tenant (home tenant or a tenant the user is a member of) or back to the platform (platform accounts only); returns new tokens. */
        post: operations["auth.switch-tenant"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth:unlock": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Unlock the current session with its lock password; five wrong attempts in a row revoke the session (a forced sign-out). Requires sign-in only and works while locked. */
        post: operations["auth.unlock"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth:verify-two-factor": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Second step of two-factor sign-in: submit the code with the ticket from auth.login; returns access and refresh tokens on success. */
        post: operations["auth.verify-two-factor"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/bot-configs": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create bot configurations on the caller's side; codes are unique per side. Requires notification.bot-configs.create. */
        post: operations["bot-configs.create"];
        /** Delete bot configurations in bulk. Requires notification.bot-configs.delete. */
        delete: operations["bot-configs.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/bot-configs/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get one of the bot configurations. Requires notification.bot-configs.read. */
        get: operations["bot-configs.get"];
        /** Edit bot configurations; the code does not change. Requires notification.bot-configs.update. */
        put: operations["bot-configs.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/bot-configs/{id}:set-default": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Make an enabled entry of the bot configurations the caller's side default used for sending. Requires notification.bot-configs.update. */
        post: operations["bot-configs.set-default"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/bot-configs:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Page through the bot configurations of the caller's side. Requires notification.bot-configs.read. */
        post: operations["bot-configs.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/condition-hits/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a condition hit of the caller’s side. Requires auditing.condition-hits.read. */
        get: operations["condition-hits.get"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/condition-hits:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List condition hits page by page: access policy and permission condition hits of the caller’s side. Requires auditing.condition-hits.read. */
        post: operations["condition-hits.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/configs": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create a configuration entry on the caller’s side: the key is unique per side and the value is checked against the data type; a tenant entry with a platform key overrides it (rejected when the platform entry forbids overrides). Requires settings.configs.create. */
        post: operations["configs.create"];
        /** Delete configuration entries on the caller’s side in bulk; built-in entries are rejected one by one. Requires settings.configs.delete. */
        delete: operations["configs.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/configs/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a configuration entry (encrypted values are never returned). Requires settings.configs.read. */
        get: operations["configs.get"];
        /** Update a configuration entry on the caller’s side: name, group, value, default, data type, encryption, order, description and status (the key cannot change; an encrypted entry keeps its value when none is given). Requires settings.configs.update. */
        put: operations["configs.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/configs:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List configuration entries page by page: platform principals see platform entries, tenant principals see platform entries plus their own; encrypted values are never returned. Requires settings.configs.read. */
        post: operations["configs.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/constraint-rules": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create a constraint rule. Requires access.constraint-rules.create. */
        post: operations["constraint-rules.create"];
        /** Delete constraint rules together with their items. Requires access.constraint-rules.delete. */
        delete: operations["constraint-rules.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/constraint-rules/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a constraint rule by ID. Requires access.constraint-rules.read. */
        get: operations["constraint-rules.get"];
        /** Replace a constraint rule, including its status and items. Requires access.constraint-rules.update. */
        put: operations["constraint-rules.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/constraint-rules/{id}:set-status": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Enable or disable a constraint rule. Requires access.constraint-rules.update. */
        post: operations["constraint-rules.set-status"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/constraint-rules:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List the constraint rules of the current side, by evaluation order by default. Requires access.constraint-rules.read. */
        post: operations["constraint-rules.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/delegations": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create a delegation: the delegator hands one permission or one role to the delegatee for a limited time, and must hold it now. Requires access.delegations.create. */
        post: operations["delegations.create"];
        /** Delete delegation records; an active one stops taking effect. Requires access.delegations.delete. */
        delete: operations["delegations.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/delegations/{id}:revoke": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Revoke a pending or active delegation, keeping the record. Requires access.delegations.revoke. */
        post: operations["delegations.revoke"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/delegations:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List the delegations of the current side with paging; their status follows from revocation and the validity window. Requires access.delegations.read. */
        post: operations["delegations.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/departments": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create a department anywhere in the tree (code unique within the tenant; parent must exist; leader must be a user of this tenant). Tenant side only; requires organization.departments.create. */
        post: operations["departments.create"];
        /** Delete departments in batch; departments that still have children or members are rejected item by item. Requires organization.departments.delete. */
        delete: operations["departments.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/departments/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a department with its ancestors, leader user name, child count and member count. Requires organization.departments.read. */
        get: operations["departments.get"];
        /** Update a department's name, leader, order, description and status (the parent is changed with departments.move). Requires organization.departments.update. */
        put: operations["departments.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/departments/{id}:move": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Move a department under another parent or to the top level; moving under itself or a descendant is rejected. Requires organization.departments.update. */
        post: operations["departments.move"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/departments:tree": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** The whole department tree of the current tenant with direct member counts. Requires organization.departments.read. */
        get: operations["departments.tree"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/dict-items": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List all items of a dictionary (depth-first, with depth). Requires settings.dict-items.read. */
        get: operations["dict-items.list"];
        put?: never;
        /** Create an item in an enabled dictionary on the caller’s side: the code is unique in the dictionary, the parent must be in the same dictionary, making it the default clears the other default. Requires settings.dict-items.create. */
        post: operations["dict-items.create"];
        /** Delete dictionary items in bulk; items that still have children are rejected one by one. Requires settings.dict-items.delete. */
        delete: operations["dict-items.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/dict-items/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a dictionary item. Requires settings.dict-items.read. */
        get: operations["dict-items.get"];
        /** Update a dictionary item: parent, name, value, description, metadata, default, order and status (the code cannot change; an item cannot move under itself or its descendants). Requires settings.dict-items.update. */
        put: operations["dict-items.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/dicts": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create a dictionary on the caller’s side; codes are unique across platform and tenants (dictionaries are never overridden). Requires settings.dicts.create. */
        post: operations["dicts.create"];
        /** Delete dictionaries on the caller’s side in bulk; built-in dictionaries and those that still have items are rejected one by one. Requires settings.dicts.delete. */
        delete: operations["dicts.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/dicts/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a dictionary. Requires settings.dicts.read. */
        get: operations["dicts.get"];
        /** Update a dictionary on the caller’s side: name, category, description, order and status (the code cannot change). Requires settings.dicts.update. */
        put: operations["dicts.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/dicts:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List dictionaries page by page: platform dictionaries plus the caller’s tenant dictionaries. Requires settings.dicts.read. */
        post: operations["dicts.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/dicts:options": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get the options of a dictionary by code: tree dictionaries are flattened depth-first, disabled items are flagged, a disabled dictionary has no options. Any signed-in user. */
        get: operations["dicts.options"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/diff-logs/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a data change log of the caller’s side with field-level before and after values. Requires auditing.diff-logs.read. */
        get: operations["diff-logs.get"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/diff-logs:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List data change logs page by page: entity inserts, updates and deletes of the caller’s side. Requires auditing.diff-logs.read. */
        post: operations["diff-logs.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/editions": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List every edition (by order, then code) with its whitelist size and the number of tenants on it. Platform side only; requires tenancy.editions.read. */
        get: operations["editions.list"];
        put?: never;
        /** Create an edition with an initial whitelist (tenant-side permissions only); it can become the default edition for new tenants. Requires tenancy.editions.create. */
        post: operations["editions.create"];
        /** Delete editions and their whitelists in batch; editions still used by tenants are rejected item by item. Requires tenancy.editions.delete. */
        delete: operations["editions.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/editions/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get an edition with its permission whitelist. Requires tenancy.editions.read. */
        get: operations["editions.get"];
        /** Update an edition's name, description, default flag and order; the code cannot change. Requires tenancy.editions.update. */
        put: operations["editions.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/editions/{id}:set-permissions": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Replace an edition's permission whitelist; the effective permissions of tenants on it narrow or widen immediately. Requires tenancy.editions.update. */
        post: operations["editions.set-permissions"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/email-configs": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create email configurations on the caller's side; codes are unique per side. Requires notification.email-configs.create. */
        post: operations["email-configs.create"];
        /** Delete email configurations in bulk. Requires notification.email-configs.delete. */
        delete: operations["email-configs.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/email-configs/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get one of the email configurations. Requires notification.email-configs.read. */
        get: operations["email-configs.get"];
        /** Edit email configurations; the code does not change. Requires notification.email-configs.update. */
        put: operations["email-configs.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/email-configs/{id}:set-default": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Make an enabled entry of the email configurations the caller's side default used for sending. Requires notification.email-configs.update. */
        post: operations["email-configs.set-default"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/email-configs:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Page through the email configurations of the caller's side. Requires notification.email-configs.read. */
        post: operations["email-configs.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/emails": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        post?: never;
        /** Delete email send records in bulk. Requires notification.emails.delete. */
        delete: operations["emails.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/emails/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get one of the email send records. Requires notification.emails.read. */
        get: operations["emails.get"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/emails:cancel": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Cancel queued email send records. Requires notification.emails.resend. */
        post: operations["emails.cancel"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/emails:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Page through the email send records of the caller's side. Requires notification.emails.read. */
        post: operations["emails.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/emails:resend": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Resend email send records: back to queued with the attempt count reset; queued and sending ones are rejected. Requires notification.emails.resend. */
        post: operations["emails.resend"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/exception-logs/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get an exception log of the caller’s side, including the full stack trace. Requires auditing.exception-logs.read. */
        get: operations["exception-logs.get"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/exception-logs:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List exception logs page by page: unhandled exceptions of the caller’s side with type, message and location. Requires auditing.exception-logs.read. */
        post: operations["exception-logs.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/export-tasks": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        post?: never;
        /** Delete your export tasks together with their result files; running ones are rejected one by one. Requires sign-in. */
        delete: operations["export-tasks.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/export-tasks/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get one of your export tasks. Requires sign-in. */
        get: operations["export-tasks.get"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/export-tasks/{id}:cancel": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Cancel one of your queued export tasks. Requires sign-in. */
        post: operations["export-tasks.cancel"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/export-tasks:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Page through your own export tasks. Requires sign-in. */
        post: operations["export-tasks.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/export-tasks:submit": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Submit an export: run a list operation's query as yourself in the background and write CSV or XLSX, kept as a temporary file for 7 days. Requires sign-in; the list operation checks its own permission as usual. */
        post: operations["export-tasks.submit"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/field-security": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create a field security rule: how a role, user or department reads a field and whether it may change it. Requires access.field-security.create. */
        post: operations["field-security.create"];
        /** Delete field security rules. Requires access.field-security.delete. */
        delete: operations["field-security.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/field-security/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a field security rule. Requires access.field-security.read. */
        get: operations["field-security.get"];
        /** Edit a field security rule, including enabling and disabling it. Requires access.field-security.update. */
        put: operations["field-security.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/field-security:entities": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List the entities and fields that field security can protect (registered by each package). Requires access.field-security.read. */
        get: operations["field-security.entities"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/field-security:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List the field security rules of the current side with paging. Requires access.field-security.read. */
        post: operations["field-security.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/files": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        post?: never;
        /** Delete files in a batch; stored content goes once no other file uses it. Requires files.files.delete. */
        delete: operations["files.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/files/content/{token}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Read a file's content through a signed link issued by files.presign. */
        get: operations["files.signed-content"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/files/upload": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Upload one file as multipart form data (field `file`). */
        post: operations["files.upload"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/files/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a file. Requires files.files.read. */
        get: operations["files.get"];
        /** Edit a file's access level, temporary flag and expiry, tags and remark. Requires files.files.update. */
        put: operations["files.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/files/{id}/content": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Read a file's content on the caller's side, subject to its access level. */
        get: operations["files.content"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/files/{id}:presign": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Obtain a signed download link: files open to signed-in users need only a sign-in, files that need authorization require files.files.read; the link is not checked against an identity while it is valid. */
        get: operations["files.presign"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/files/{id}:set-primary-storage": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Make a normal, verified copy the primary copy; a corrupted file becomes normal again. Requires files.files.update. */
        post: operations["files.set-primary-storage"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/files/{id}:set-storage-status": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Enable or disable a copy; the primary copy cannot be disabled and a failed copy must be verified again. Requires files.files.update. */
        post: operations["files.set-storage-status"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/files/{id}:storages": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List a file's stored copies, primary copy first. Requires files.files.read. */
        get: operations["files.storages"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/files/{id}:verify-storage": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Verify a copy by reading it and comparing its SHA-256; a mismatch or missing content marks it failed, and a failed primary copy marks the file corrupted. Requires files.files.update. */
        post: operations["files.verify-storage"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/files:fast-upload": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Instant upload: when a normal file with the same content already exists on the caller's side, a new record is created at once; a miss returns nothing. Requires files.files.create. */
        post: operations["files.fast-upload"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/files:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Page through the caller's side's files; the keyword matches the original name, stored name, extension, content type and tags. Requires files.files.read. */
        post: operations["files.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/files:set-status": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Archive or restore files in a batch; corrupted files are rejected one by one. Requires files.files.update. */
        post: operations["files.set-status"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/impersonation:candidates": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List up to 20 active users that can be impersonated in a tenant (or the platform), matched by keyword. Requires identity.impersonation.start. */
        get: operations["impersonation.candidates"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/impersonation:start": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Sign in as another user: opens a separate session for the target and returns tokens carrying the actor claim. Tenant subjects stay in their own tenant. Requires identity.impersonation.start. */
        post: operations["impersonation.start"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/impersonation:stop": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** End impersonation: revokes the impersonation session and returns tokens for the actor's original session. */
        post: operations["impersonation.stop"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/import-histories": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Record the result of an import (the page creates the rows one by one). Requires sign-in. */
        post: operations["import-histories.create"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/import-histories:recent": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List your most recent imports on a page. Requires sign-in. */
        get: operations["import-histories.recent"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/inbox": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        post?: never;
        /** Delete entries from your own inbox; the notifications themselves are unaffected. Any signed-in user. */
        delete: operations["inbox.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/inbox/{id}:confirm": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Confirm a notification in your inbox that asks for confirmation; it is marked read too. Any signed-in user. */
        post: operations["inbox.confirm"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/inbox/{id}:mark-popup-shown": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Record that one of your popup notifications has been shown so it does not pop up again. Any signed-in user. */
        post: operations["inbox.mark-popup-shown"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/inbox:banners": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Your unread banner notifications, shown at the top of the page. Any signed-in user. */
        get: operations["inbox.banners"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/inbox:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Page through your own inbox on the current side (published and within their visibility window): the keyword matches title and content; filter by read state and type or show only pending items. Any signed-in user. */
        post: operations["inbox.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/inbox:mandatory": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Your unread mandatory notifications; pages are blocked until they are read. Any signed-in user. */
        get: operations["inbox.mandatory"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/inbox:mark-all-read": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Mark every visible unread entry of your inbox on the current side as read. Any signed-in user. */
        post: operations["inbox.mark-all-read"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/inbox:mark-read": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Mark entries of your own inbox as read. Any signed-in user. */
        post: operations["inbox.mark-read"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/inbox:popups": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Your popup notifications not shown yet; each pops up once after sign-in. Any signed-in user. */
        get: operations["inbox.popups"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/login-bans": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** The login auto-ban settings in effect and the addresses banned right now (platform only). Requires access.login-bans.read. */
        get: operations["login-bans.list"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/login-bans:lift": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Lift the ban on an address together with its failure count in the current window (platform only). Requires access.login-bans.lift. */
        post: operations["login-bans.lift"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/login-logs/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a sign-in log of the caller’s side. Requires auditing.login-logs.read. */
        get: operations["login-logs.get"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/login-logs:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List sign-in logs page by page: sign-in, refresh, sign-out, tenant switch, impersonation and password change events of the caller’s side, successful or not. Requires auditing.login-logs.read. */
        post: operations["login-logs.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/memberships": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Add a user of this tenant to a department, optionally with a position; the first membership of a user becomes the primary one. Requires organization.memberships.create. */
        post: operations["memberships.create"];
        /** Remove users from departments in batch; when a primary membership is removed the next one becomes primary. Requires organization.memberships.delete. */
        delete: operations["memberships.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/memberships/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        /** Update a membership's position, primary flag, employee number, grade, hire date and order. Requires organization.memberships.update. */
        put: operations["memberships.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/memberships:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List memberships filtered by department, user, position or primary flag, with user, department and position names. Requires organization.memberships.read. */
        post: operations["memberships.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/menus": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List the whole menu tree (hidden nodes, operator-created directories and links, and the customization flag of page nodes). Platform side only; requires access.menus.read. */
        get: operations["menus.list"];
        put?: never;
        /** Create an operator node: a directory (top level only) or an external link (http / https, may sit in a directory); page nodes come from page registrations and cannot be created. Requires access.menus.create. */
        post: operations["menus.create"];
        /** Delete operator nodes in batch; page nodes and directories that still have children are rejected item by item. Requires access.menus.delete. */
        delete: operations["menus.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/menus/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get one menu node. Requires access.menus.read. */
        get: operations["menus.get"];
        /** Update a menu node: page nodes only change presentation (title, icon, order, directory, visibility, caching, pinned tab) and become customized; operator nodes may also change their link and permission. Requires access.menus.update. */
        put: operations["menus.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/menus/{id}:reset": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Restore a page node's presentation to its registration and clear the customization flag. Requires access.menus.update. */
        post: operations["menus.reset"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/message-templates": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create message templates on the caller's side; codes are unique per side. Requires notification.message-templates.create. */
        post: operations["message-templates.create"];
        /** Delete message templates in bulk. Requires notification.message-templates.delete. */
        delete: operations["message-templates.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/message-templates/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get one of the message templates. Requires notification.message-templates.read. */
        get: operations["message-templates.get"];
        /** Edit message templates; the code does not change. Requires notification.message-templates.update. */
        put: operations["message-templates.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/message-templates:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Page through the message templates of the caller's side. Requires notification.message-templates.read. */
        post: operations["message-templates.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/my-activity:summary": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Your activity: sign-ins, accesses and operations today, this week and this month, the latest of each, and daily operations and accesses over the last 371 days, split by the requested time zone. Requires sign-in only. */
        get: operations["my-activity.summary"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/my-api-credentials": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List your own open API credentials (secrets are never shown). Requires sign-in only. */
        get: operations["my-api-credentials.list"];
        put?: never;
        /** Create an open API credential (at most 5 per person); the secret is returned this once only. Not available in impersonation sessions. */
        post: operations["my-api-credentials.create"];
        /** Delete your open API credentials; their AppKeys stop working at once. Not available in impersonation sessions. */
        delete: operations["my-api-credentials.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/my-api-credentials/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        /** Enable or disable one of your open API credentials. Not available in impersonation sessions. */
        put: operations["my-api-credentials.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/my-api-credentials/{id}:rotate-secret": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Rotate the secret of one of your credentials: the old secret stops working at once and the new one is returned this once only. Not available in impersonation sessions. */
        post: operations["my-api-credentials.rotate-secret"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/my-api-credentials:settings": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Your call security settings: the signature algorithm and IP allow-list, applied to all your credentials. Requires sign-in only. */
        get: operations["my-api-credentials.settings"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/my-api-credentials:update-settings": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Save your call security settings (replaced as a whole and applied to all your credentials at once). Not available in impersonation sessions. */
        post: operations["my-api-credentials.update-settings"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/my-login-logs": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List your own sign-in logs page by page (sign-in, refresh, sign-out, tenant switch, impersonation, password change and more, successful or not), newest first, up to 50 per page. Requires sign-in only. */
        get: operations["my-login-logs.list"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/my-oauth-apps": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List your own OAuth apps. Requires sign-in only. */
        get: operations["my-oauth-apps.list"];
        put?: never;
        /** Create one of your OAuth apps (authorization code and refresh token, scopes openid profile email, consent required); a confidential client’s secret is returned this once only. Not available in impersonation sessions. */
        post: operations["my-oauth-apps.create"];
        /** Delete your OAuth apps. Not available in impersonation sessions. */
        delete: operations["my-oauth-apps.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/my-oauth-apps/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get one of your OAuth apps. Requires sign-in only. */
        get: operations["my-oauth-apps.get"];
        /** Update one of your OAuth apps: name, homepage, description, redirect URIs, logo and enabled. Not available in impersonation sessions. */
        put: operations["my-oauth-apps.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/my-oauth-apps/{id}:reset-secret": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Reset the secret of one of your OAuth apps (public clients have none). Not available in impersonation sessions. */
        post: operations["my-oauth-apps.reset-secret"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/notifications": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create a notification draft, optionally publishing it at once: role, department and user targets need at least one ID, and departments exist only on the tenant side. Requires notification.notifications.create, plus notification.notifications.publish to publish at once. */
        post: operations["notifications.create"];
        /** Delete notifications in bulk; published ones also leave their recipients' inboxes. Requires notification.notifications.delete. */
        delete: operations["notifications.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/notifications/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a notification with its delivery targets. Requires notification.notifications.read. */
        get: operations["notifications.get"];
        /** Replace a notification draft, including its targets; published notifications cannot be changed. Requires notification.notifications.update. */
        put: operations["notifications.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/notifications/{id}:publish": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Publish a draft: resolve the active recipients of the caller's side from its targets, write their inbox entries and push them in real time after commit; a notification is published only once. Requires notification.notifications.publish. */
        post: operations["notifications.publish"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/notifications/{id}:read-stats": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Read statistics: recipients, read, unread and confirmed counts. Requires notification.notifications.read. */
        get: operations["notifications.read-stats"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/notifications/{id}:recipients": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Page through the recipients of a published notification with their read and confirmation state; optionally only unread or unconfirmed ones. Requires notification.notifications.read. */
        post: operations["notifications.recipients"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/notifications/{id}:remind": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Remind: push a published notification again to recipients who have not read it, or not confirmed it when confirmation is required. Requires notification.notifications.publish. */
        post: operations["notifications.remind"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/notifications:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Page through the caller's side's notifications: the keyword matches title and content; filter by type, priority, target and published state. Requires notification.notifications.read. */
        post: operations["notifications.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/numbering-rules": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create a numbering rule on the caller’s side: the date format must distinguish the reset cycle and the time zone is an IANA id. Requires settings.numbering-rules.create. */
        post: operations["numbering-rules.create"];
        /** Delete numbering rules on the caller’s side in bulk; rules that have issued numbers are rejected one by one. Requires settings.numbering-rules.delete. */
        delete: operations["numbering-rules.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/numbering-rules/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a numbering rule, including its current period and last issued sequence value. Requires settings.numbering-rules.read. */
        get: operations["numbering-rules.get"];
        /** Update a numbering rule on the caller’s side; once it has issued numbers the format is frozen and only name, description, order, status and tenant sharing can change. Requires settings.numbering-rules.update. */
        put: operations["numbering-rules.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/numbering-rules/{id}:allocations": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List a rule’s issue records page by page (a tenant only sees the ones it requested). Requires settings.numbering-rules.read. */
        post: operations["numbering-rules.allocations"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/numbering-rules/{id}:reset": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Safely reset the sequence: the next value must exceed the highest value already issued in the current period (jumping forward is allowed), a reason is required and platform rules need the rule code typed back as confirmation. Requires settings.numbering-rules.reset. */
        post: operations["numbering-rules.reset"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/numbering-rules:generate": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Issue numbers from a rule: committed in its own transaction, numbers are consumed permanently; the same idempotency key replays with identical parameters and conflicts otherwise. Requires settings.numbering-rules.generate. */
        post: operations["numbering-rules.generate"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/numbering-rules:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List numbering rules page by page: platform principals see platform rules, tenant principals see their own rules plus platform rules shared with tenants. Requires settings.numbering-rules.read. */
        post: operations["numbering-rules.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/numbering-rules:preview": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Preview numbers for a given format (pure computation, nothing is issued, up to 50). Requires settings.numbering-rules.read. */
        get: operations["numbering-rules.preview"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/oauth-apps": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create an OAuth app: the ClientId is generated when omitted and is globally unique; a confidential client’s secret is returned this once only, a public client has none (its authorization codes need PKCE). Requires openplatform.oauth-apps.create. */
        post: operations["oauth-apps.create"];
        /** Delete OAuth apps in bulk (soft delete; tokens issued to them stop working with them). Requires openplatform.oauth-apps.delete. */
        delete: operations["oauth-apps.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/oauth-apps/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get one OAuth app (the secret is never shown). Requires openplatform.oauth-apps.read. */
        get: operations["oauth-apps.get"];
        /** Update an OAuth app (replaced as a whole, including enabled; the ClientId, client type and secret stay). Requires openplatform.oauth-apps.update. */
        put: operations["oauth-apps.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/oauth-apps/{id}:reset-secret": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Reset an OAuth app’s secret: the old secret stops working at once and the new one is returned this once only; public clients have no secret. Requires openplatform.oauth-apps.update. */
        post: operations["oauth-apps.reset-secret"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/oauth-apps:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List OAuth apps (created by the platform or by users under My apps) page by page; the keyword matches name, description, ClientId, homepage and remark, with filters for app type, client type, enabled, skip consent and personal apps. Requires openplatform.oauth-apps.read. */
        post: operations["oauth-apps.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/oauth-consents": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List the apps you have authorized and the scopes you granted. Requires sign-in only. */
        get: operations["oauth-consents.list"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/oauth-consents:approve": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Approve the authorization: issues an authorization code, records the consent and returns the redirect URI with code and state. Not available in impersonation sessions. */
        post: operations["oauth-consents.approve"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/oauth-consents:deny": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Deny the authorization: returns the redirect URI with error=access_denied and state, only when the client and redirect URI are valid. Requires sign-in only. */
        post: operations["oauth-consents.deny"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/oauth-consents:resolve": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Preview for the consent page: checks the authorization request (client, redirect URI, response_type, PKCE, scopes) and returns the app, the scopes and whether consent can be skipped. Requires sign-in only; not available in impersonation sessions. */
        get: operations["oauth-consents.resolve"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/oauth-consents:revoke": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Revoke your authorization of an app: deletes the consent and revokes all of your tokens it holds. Not available in impersonation sessions. */
        post: operations["oauth-consents.revoke"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/open-api-logs/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get an open API log of the caller’s side. Requires auditing.open-api-logs.read. */
        get: operations["open-api-logs.get"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/open-api-logs:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List open API logs page by page: signed open API calls of the caller’s side with their signature checks. Requires auditing.open-api-logs.read. */
        post: operations["open-api-logs.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/open:echo": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Echo: returns the request body as received, to check the content signature and encryption (the body may be AES-CBC encrypted, and the response is then encrypted too). Signed calls only. */
        post: operations["open.echo"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/open:ping": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Signature self-check: once the signature verifies, returns the AppKey, the credential owner and the server time. Callable only with requests signed by an open API credential (X-Access-Key, X-Timestamp, X-Nonce, X-Content-Sign, X-Signature). */
        get: operations["open.ping"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/operation-logs/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get an operation log of the caller’s side, including its details. Requires auditing.operation-logs.read. */
        get: operations["operation-logs.get"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/operation-logs:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List operation logs page by page: audited business operations of the caller’s side with their results and durations. Requires auditing.operation-logs.read. */
        post: operations["operation-logs.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/permission-change-logs/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a permission change log of the caller’s side. Requires auditing.permission-change-logs.read. */
        get: operations["permission-change-logs.get"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/permission-change-logs:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List permission change logs page by page: grants, revocations, role membership, data scope and edition changes of the caller’s side. Requires auditing.permission-change-logs.read. */
        post: operations["permission-change-logs.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/permission-conditions": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List the grant bindings of a role or user and the condition clauses on them. Requires access.permission-conditions.read. */
        get: operations["permission-conditions.list"];
        put?: never;
        /** Add a condition clause to a grant or denial: met to grant on a grant, met to deny on a denial. Requires access.permission-conditions.create. */
        post: operations["permission-conditions.create"];
        /** Delete condition clauses. Requires access.permission-conditions.delete. */
        delete: operations["permission-conditions.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/permission-conditions/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        /** Edit a condition clause, including enabling and disabling it. Requires access.permission-conditions.update. */
        put: operations["permission-conditions.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/permission-conditions:attributes": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List the condition attribute catalog (shared by permission conditions and access policies), with the operators each allows and whether it is known before authentication. Requires access.permission-conditions.read. */
        get: operations["permission-conditions.attributes"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/permission-conditions:batch-trial": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Impact trial: the current verdict for everyone a grant affects, up to 200 people. Requires access.permission-conditions.trial. */
        get: operations["permission-conditions.batch-trial"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/permission-conditions:inventory": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Condition inventory: diagnostics for every condition on the current side and the conclusion of its group. Requires access.permission-conditions.trial. */
        get: operations["permission-conditions.inventory"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/permission-conditions:trial": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Condition trial: decide one permission for one user under the given request, explained group by group and clause by clause. Requires access.permission-conditions.trial. */
        get: operations["permission-conditions.trial"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/permissions": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List the grantable permission catalog: the caller's side by default; platform principals may pass side=tenant for the tenant side (used to edit edition whitelists); for tenant principals the catalog is narrowed by their edition. Requires access.permissions.read. */
        get: operations["permissions.list"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/policies": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create an access policy; it always starts as log only and enabled. Requires access.policies.create. */
        post: operations["policies.create"];
        /** Delete access policies. Requires access.policies.delete. */
        delete: operations["policies.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/policies/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get an access policy by ID. Requires access.policies.read. */
        get: operations["policies.get"];
        /** Replace the content and conditions of an access policy; its action and status stay. Requires access.policies.update. */
        put: operations["policies.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/policies/{id}:set-action": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Change the action of an access policy: log only, deny or allow. Requires access.policies.update. */
        post: operations["policies.set-action"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/policies/{id}:set-status": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Enable or disable an access policy. Requires access.policies.update. */
        post: operations["policies.set-status"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/policies:attributes": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** The condition attributes available to access policies, with the operators each allows and whether it is known before authentication. Requires access.policies.read. */
        get: operations["policies.attributes"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/policies:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List the access policies of the current side, by evaluation order by default. Requires access.policies.read. */
        post: operations["policies.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/policies:templates": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List the preset access policy templates. Requires access.policies.read. */
        get: operations["policies.templates"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/policies:trial": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Trial an access policy: evaluate a saved policy or draft conditions under the given request, explained group by group and condition by condition. Requires access.policies.trial. */
        post: operations["policies.trial"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/positions": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create a position (code unique within the tenant). Requires organization.positions.create. */
        post: operations["positions.create"];
        /** Delete positions in batch; positions still referenced by memberships are rejected item by item. Requires organization.positions.delete. */
        delete: operations["positions.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/positions/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a position. Requires organization.positions.read. */
        get: operations["positions.get"];
        /** Update a position's name, order, description and status. Requires organization.positions.update. */
        put: operations["positions.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/positions:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List positions with paging, keyword, filters and sorting. Requires organization.positions.read. */
        post: operations["positions.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/profile": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Your profile and account security summary (two-factor, last password change). Any signed-in user. */
        get: operations["profile.get"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/profile:begin-external-bind": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Start a binding: issues a binding intent for the signed-in user (10 minutes, single use) and returns the URL to the third-party authorization; only that user can confirm the resulting ticket. Not available in impersonation sessions. */
        post: operations["profile.begin-external-bind"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/profile:change-password": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Change your own password; requires the current password and revokes your other sessions. */
        post: operations["profile.change-password"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/profile:disable-two-factor": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Turn off a two-factor method: confirm with a current authenticator code. Not available while impersonating. */
        post: operations["profile.disable-two-factor"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/profile:enable-two-factor": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Turn on a two-factor method: confirm the authenticator with a code from the newly generated key. Not available while impersonating. */
        post: operations["profile.enable-two-factor"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/profile:external-logins": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List the third-party accounts bound to you. Requires sign-in only. */
        get: operations["profile.external-logins"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/profile:link-external": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Confirm a binding: binds the third-party account from the one-time ticket to the signed-in user; only the user who started the binding can confirm it, and it is refused when someone else has bound the account. Not available in impersonation sessions. */
        post: operations["profile.link-external"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/profile:notification-preference": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Your notification preferences: in-app, email and SMS channels and announcement, task and security topics; everything is received until saved. Mandatory and emergency notifications ignore them. Requires sign-in only. */
        get: operations["profile.notification-preference"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/profile:revoke-other-sessions": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Sign out all your devices except the current one, including impersonation sessions you started that are still open. Requires sign-in only. */
        post: operations["profile.revoke-other-sessions"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/profile:revoke-session": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Sign out one of your devices; use sign-out for the current one. Requires sign-in only. */
        post: operations["profile.revoke-session"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/profile:sessions": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List your signed-in devices: all your active sessions (platform and tenants alike), the current one first. Requires sign-in only. */
        get: operations["profile.sessions"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/profile:setup-totp": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Start binding an authenticator: generate a new key (confirm with an authenticator code within 10 minutes). Not available while impersonating. */
        post: operations["profile.setup-totp"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/profile:unlink-external": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Unbind one of your third-party accounts. Not available in impersonation sessions. */
        post: operations["profile.unlink-external"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/profile:update-notification-preference": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Save your notification preferences (replaces them as a whole). Requires sign-in only. */
        post: operations["profile.update-notification-preference"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create a role on the current side (platform or tenant); the code is unique per side and cannot be changed. Requires access.roles.create. */
        post: operations["roles.create"];
        /** Delete roles in batch together with their members and permissions; built-in roles cannot be deleted. Requires access.roles.delete. */
        delete: operations["roles.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a role with its granted permissions and member count. Requires access.roles.read. */
        get: operations["roles.get"];
        /** Update a role's name, description, order and status. Requires access.roles.update. */
        put: operations["roles.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles/{id}:assign": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Assign users to a role; users must belong to the role's side. Takes effect at the user's next token refresh. Requires access.roles.assign. */
        post: operations["roles.assign"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles/{id}:deny": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Deny permissions on a role: revokes the same grants and writes Deny rules (optional validity window and reason); a role deny only removes that role's own grants. Requires access.roles.grant. */
        post: operations["roles.deny"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles/{id}:grant": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Grant permissions to a role; codes must exist in the catalog and match the role's side. Requires access.roles.grant. */
        post: operations["roles.grant"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles/{id}:hierarchy": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get the inheritance of a role: all its parents, all its children, and the grants and denials it inherits. Requires access.roles.read. */
        get: operations["roles.hierarchy"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles/{id}:members": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List the members of a role. Requires access.roles.read. */
        get: operations["roles.members"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles/{id}:revoke": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Revoke permissions from a role. Requires access.roles.grant. */
        post: operations["roles.revoke"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles/{id}:set-parents": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Set the direct parents of a role to the given set (empty removes all inheritance): its holders get the parents' grants right away, and the parents' denials apply to it too. Requires access.roles.set-parents. */
        post: operations["roles.set-parents"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles/{id}:unassign": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Remove users from a role. Takes effect at the user's next token refresh. Requires access.roles.assign. */
        post: operations["roles.unassign"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles/{id}:update-member": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Change the validity window of a member assignment; expired or future assignments are dropped from decisions at once. Requires access.roles.assign. */
        post: operations["roles.update-member"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles:by-user": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List the roles a user holds on the current side (platform or tenant). Requires access.roles.read. */
        get: operations["roles.by-user"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List roles of the current side with paging, filters, keyword and sorting. Requires access.roles.read. */
        post: operations["roles.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/session-roles/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get one session role record by ID. Requires access.session-roles.read. */
        get: operations["session-roles.get"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/session-roles:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List the roles each session of the current side has activated. Requires access.session-roles.read. */
        post: operations["session-roles.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/sessions:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Page through the online users of the current tenant: one row per active session with device, browser, masked IP, sign-in location, sign-in and last activity time and realtime presence; narrowed by data scope. Requires identity.sessions.read. */
        post: operations["sessions.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/sessions:revoke": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Force sign-out: revoke active sessions of the current tenant; the clients are notified in real time and sign out. Your own current session cannot be revoked here. Requires identity.sessions.revoke. */
        post: operations["sessions.revoke"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/sessions:summary": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Online overview of the current tenant: active sessions, users with an active session and users connected in real time. Requires identity.sessions.read. */
        get: operations["sessions.summary"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/sms": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        post?: never;
        /** Delete SMS send records in bulk. Requires notification.sms.delete. */
        delete: operations["sms.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/sms-configs": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create SMS configurations on the caller's side; codes are unique per side. Requires notification.sms-configs.create. */
        post: operations["sms-configs.create"];
        /** Delete SMS configurations in bulk. Requires notification.sms-configs.delete. */
        delete: operations["sms-configs.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/sms-configs/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get one of the SMS configurations. Requires notification.sms-configs.read. */
        get: operations["sms-configs.get"];
        /** Edit SMS configurations; the code does not change. Requires notification.sms-configs.update. */
        put: operations["sms-configs.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/sms-configs/{id}:set-default": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Make an enabled entry of the SMS configurations the caller's side default used for sending. Requires notification.sms-configs.update. */
        post: operations["sms-configs.set-default"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/sms-configs:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Page through the SMS configurations of the caller's side. Requires notification.sms-configs.read. */
        post: operations["sms-configs.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/sms/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get one of the SMS send records. Requires notification.sms.read. */
        get: operations["sms.get"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/sms:cancel": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Cancel queued SMS send records. Requires notification.sms.resend. */
        post: operations["sms.cancel"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/sms:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Page through the SMS send records of the caller's side. Requires notification.sms.read. */
        post: operations["sms.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/sms:resend": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Resend SMS send records: back to queued with the attempt count reset; queued and sending ones are rejected. Requires notification.sms.resend. */
        post: operations["sms.resend"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/storage-configs": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create a local or S3-compatible storage configuration on the caller's side; the code is unique on the side and S3-compatible storage needs an endpoint and access keys. Requires files.storage-configs.create. */
        post: operations["storage-configs.create"];
        /** Delete storage configurations in a batch; the default one and those holding copies are rejected one by one. Requires files.storage-configs.delete. */
        delete: operations["storage-configs.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/storage-configs/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a storage configuration (the access key is never shown). Requires files.storage-configs.read. */
        get: operations["storage-configs.get"];
        /** Edit a storage configuration (the code stays; an empty access key keeps the current one). Requires files.storage-configs.update. */
        put: operations["storage-configs.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/storage-configs/{id}:set-default": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Make an enabled storage configuration the caller's side's default; later uploads go there. Requires files.storage-configs.update. */
        post: operations["storage-configs.set-default"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/storage-configs/{id}:set-status": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Enable or disable a storage configuration; the default one cannot be disabled. Requires files.storage-configs.update. */
        post: operations["storage-configs.set-status"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/storage-configs:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Page through the caller's side's storage configurations, default first; access keys are never shown. Requires files.storage-configs.read. */
        post: operations["storage-configs.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/subscription": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get the tenant's own subscription: edition, expiry, effective seat and storage limits and usage. Tenant side only; requires tenancy.subscription.read. */
        get: operations["subscription.get"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/telegram-bots": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create a Telegram bot: names are unique and the token is stored encrypted; admins, the group allow-list and broadcast chats are comma-separated integer IDs, the command allow-list is comma-separated command names. Requires notification.telegram-bots.create. */
        post: operations["telegram-bots.create"];
        /** Delete Telegram bots in bulk. Requires notification.telegram-bots.delete. */
        delete: operations["telegram-bots.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/telegram-bots/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a Telegram bot. Requires notification.telegram-bots.read. */
        get: operations["telegram-bots.get"];
        /** Edit a Telegram bot; leaving the token empty keeps it. Requires notification.telegram-bots.update. */
        put: operations["telegram-bots.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/telegram-bots:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Page through the platform-hosted Telegram bots; tokens are never shown. Requires notification.telegram-bots.read. */
        post: operations["telegram-bots.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/tenant-members": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List a tenant's members (users whose home tenant is elsewhere and who enter it by membership) with user name and display name. Platform side only; requires tenancy.tenant-members.read. */
        get: operations["tenant-members.list"];
        put?: never;
        /** Add a user as a tenant member; rejected when the user is already a member or the tenant is the user's home. Platform side only; requires tenancy.tenant-members.create. */
        post: operations["tenant-members.create"];
        /** Remove tenant members in batch; they can no longer switch into the tenant. Platform side only; requires tenancy.tenant-members.delete. */
        delete: operations["tenant-members.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/tenants": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Provision a tenant: write the tenant and register it (enabled) in the tenant directory; the administrator is created with tenants.initialize-admin. Platform side only; requires tenancy.tenants.create. Without an edition the default edition applies (an error if there is none). */
        post: operations["tenants.create"];
        /** Delete tenants in batch and remove them from the directory; tenants that still have users are rejected item by item. Requires tenancy.tenants.delete. */
        delete: operations["tenants.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/tenants/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a tenant with its administrator's user name and user count. Requires tenancy.tenants.read. */
        get: operations["tenants.get"];
        /** Update a tenant's name, description, expiry and order; the name is mirrored to the directory. Requires tenancy.tenants.update. */
        put: operations["tenants.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/tenants/{id}:change-edition": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Move a tenant to another edition, or lift edition limits; its users' effective permissions follow the new whitelist immediately. Requires tenancy.tenants.update. */
        post: operations["tenants.change-edition"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/tenants/{id}:initialize-admin": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create the first administrator of a tenant with every tenant-side permission; the password must be changed at first sign-in. Once per tenant. Requires tenancy.tenants.initialize-admin. */
        post: operations["tenants.initialize-admin"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/tenants:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List tenants with paging, filters, keyword and sorting. Requires tenancy.tenants.read. */
        post: operations["tenants.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/tenants:mine": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Tenants the current user can enter (home tenant and memberships, active only) and whether the platform is available; the control center switches with auth.switch-tenant. */
        get: operations["tenants.mine"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/tenants:quotas": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List every tenant's effective quotas and usage (seats, storage), optionally only those over quota. Platform side only; requires tenancy.tenants.read. */
        get: operations["tenants.quotas"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/tenants:resume": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Resume suspended tenants in batch. Requires tenancy.tenants.suspend. */
        post: operations["tenants.resume"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/tenants:suspend": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Suspend tenants in batch: the directory marks them disabled, their users cannot sign in and calls carrying the tenant are rejected. Requires tenancy.tenants.suspend. */
        post: operations["tenants.suspend"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/traces:timeline": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Aggregate the logs of the caller’s side by trace, user, session or client address into one timeline, oldest first, with counts per log type. Requires auditing.traces.read. */
        get: operations["traces.timeline"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/user-data-scopes": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Read a user's data scope override (tenant side only); without one the roles decide. Requires access.user-data-scopes.read. */
        get: operations["user-data-scopes.get"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/user-data-scopes:set": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Set or clear a user's data scope override (tenant side only); the override replaces the roles' scopes entirely. Requires access.user-data-scopes.update. */
        post: operations["user-data-scopes.set"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/user-grants": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List the permissions granted or denied directly to a user on the current side, with windows and reasons. Requires access.user-grants.read. */
        get: operations["user-grants.list"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/user-grants:deny": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Deny permissions directly on a user; a user deny overrides every grant. Requires access.user-grants.grant. */
        post: operations["user-grants.deny"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/user-grants:grant": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Grant permissions directly to a user (optional validity window and reason). Requires access.user-grants.grant. */
        post: operations["user-grants.grant"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/user-grants:revoke": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Revoke direct grants or denies of a user. Requires access.user-grants.grant. */
        post: operations["user-grants.revoke"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/user-settings": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Read one of your own settings by scene and key; the value is empty until saved. Any signed-in user. */
        get: operations["user-settings.get"];
        /** Write one of your own settings by scene and key, replacing it whole or creating it; an empty value clears it. Any signed-in user. */
        put: operations["user-settings.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/users": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create a user: a tenant principal creates one in its own tenant; a platform principal may name the home tenant or create a platform account. Requires identity.users.create. */
        post: operations["users.create"];
        /** Delete users in batch (soft delete) and revoke their sessions; you cannot delete yourself. Requires identity.users.delete. */
        delete: operations["users.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/users/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a user by ID; tenant principals only see their own tenant. Requires identity.users.read. */
        get: operations["users.get"];
        /** Update a user's display name, email and phone number. Requires identity.users.update. */
        put: operations["users.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/users/{id}:reset-password": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Set a new password for a user, revoke their sessions and require a change at next login. Requires identity.users.reset-password. */
        post: operations["users.reset-password"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/users:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List users with paging, filters, keyword and sorting; tenant principals only see their own tenant. Requires identity.users.read. */
        post: operations["users.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/users:lock": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Lock users in batch and revoke their sessions; you cannot lock yourself. Requires identity.users.lock. */
        post: operations["users.lock"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/users:reset-two-factor": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Reset two-factor for users in bulk (clears all methods and authenticator keys); you cannot reset yourself. Requires identity.users.reset-two-factor. */
        post: operations["users.reset-two-factor"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/users:unlock": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Unlock users in batch. Requires identity.users.lock. */
        post: operations["users.unlock"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
}
export type webhooks = Record<string, never>;
export interface components {
    schemas: {
        ProblemDetails: {
            type: string;
            title: string;
            detail?: string;
            instance?: string;
            code: string;
            traceId?: string;
            status: number;
            errors?: {
                [key: string]: string[];
            };
            details?: {
                [key: string]: string | number | boolean | null;
            };
        };
        AccessLogResponse: {
            /** Format: uuid */
            id: string;
            /** Format: date-time */
            occurredAt: string;
            traceId: string;
            /** Format: uuid */
            userId?: string | null;
            userName?: string | null;
            /** Format: uuid */
            sessionId?: string | null;
            clientIp?: string | null;
            userAgent?: string | null;
            operationId: string;
            protocol?: string | null;
            method?: string | null;
            path?: string | null;
            result: components["schemas"]["AccessResult"];
            /** Format: int32 */
            statusCode?: number | null;
            errorCode?: string | null;
            errorMessage?: string | null;
            /** Format: int64 */
            durationMs: number;
            browser?: string | null;
            os?: string | null;
            device?: string | null;
            referer?: string | null;
        };
        /** @enum {string} */
        AccessPolicyAction: "LogOnly" | "Deny" | "Allow";
        AccessPolicyConditionRequest: {
            attributeName: string;
            operator: components["schemas"]["ConditionOperator"];
            /** Format: int32 */
            groupNo?: number;
            isNegated?: boolean;
            value?: string | null;
            description?: string | null;
        };
        AccessPolicyConditionResponse: {
            /** Format: int32 */
            groupNo: number;
            attributeName: string;
            operator: components["schemas"]["ConditionOperator"];
            isNegated: boolean;
            value?: string | null;
            description?: string | null;
        };
        AccessPolicyResponse: {
            /** Format: uuid */
            id: string;
            code: string;
            name: string;
            description?: string | null;
            action: components["schemas"]["AccessPolicyAction"];
            /** Format: int32 */
            priority: number;
            status: components["schemas"]["AccessPolicyStatus"];
            /** Format: date-time */
            effectiveAt?: string | null;
            /** Format: date-time */
            expiresAt?: string | null;
            isEffective: boolean;
            phase: components["schemas"]["ConditionHitPhase"];
            templateCode?: string | null;
            remark?: string | null;
            conditions: components["schemas"]["AccessPolicyConditionResponse"][];
            /** Format: date-time */
            createdAt: string;
            /** Format: date-time */
            modifiedAt?: string | null;
        };
        /** @enum {string} */
        AccessPolicyStatus: "Active" | "Disabled";
        AccessPolicyTemplateResponse: {
            code: string;
            name: string;
            description: string;
            action: components["schemas"]["AccessPolicyAction"];
            status: components["schemas"]["AccessPolicyStatus"];
            /** Format: int32 */
            priority: number;
            phase: components["schemas"]["ConditionHitPhase"];
            conditions: components["schemas"]["AccessPolicyConditionResponse"][];
        };
        /** @enum {string} */
        AccessResult: "Success" | "Failed" | "Forbidden" | "Unauthorized" | "NotFound" | "ServerError";
        ActivityCountsResponse: {
            /** Format: int32 */
            logins: number;
            /** Format: int32 */
            accesses: number;
            /** Format: int32 */
            operations: number;
        };
        ActivityDayResponse: {
            /** Format: date */
            date: string;
            /** Format: int32 */
            operations: number;
            /** Format: int32 */
            accesses: number;
        };
        AvailableTenantResponse: {
            tenantId: string;
            name: string;
            isHome: boolean;
            memberType?: components["schemas"]["TenantMemberType"] | null;
            isCurrent: boolean;
            /** Format: date-time */
            joinedAt: string;
        };
        BatchFailure: {
            /** Format: uuid */
            id: string;
            code: string;
        };
        BotConfigResponse: {
            /** Format: uuid */
            id: string;
            code: string;
            name: string;
            provider: components["schemas"]["BotProvider"];
            webhookUrl: string;
            hasSecret: boolean;
            keyword?: string | null;
            isDefault: boolean;
            isEnabled: boolean;
            /** Format: int32 */
            order: number;
            remark?: string | null;
            /** Format: date-time */
            createdAt: string;
            /** Format: date-time */
            modifiedAt?: string | null;
        };
        /** @enum {string} */
        BotProvider: "DingTalk" | "Lark" | "WeCom";
        ConditionAttributeResponse: {
            name: string;
            kind: components["schemas"]["ConditionValueKind"];
            isMultiValued: boolean;
            isFamily: boolean;
            aliases: string[];
            operators: components["schemas"]["ConditionOperator"][];
            isBeforeAuthentication: boolean;
        };
        ConditionBatchTrialItemResponse: {
            /** Format: uuid */
            userId: string;
            userName: string;
            verdict: components["schemas"]["ConditionVerdict"];
        };
        ConditionBindingResponse: {
            permission: string;
            action: components["schemas"]["PermissionAction"];
            /** Format: date-time */
            effectiveAt?: string | null;
            /** Format: date-time */
            expiresAt?: string | null;
        };
        /** @enum {string} */
        ConditionDiagnostic: "UnregisteredAttribute" | "LegacyName" | "InvalidCondition" | "BindingInactive";
        /** @enum {string} */
        ConditionGroupConclusion: "Evaluable" | "NeverMatches" | "BindingInactive" | "Disabled";
        /** @enum {string} */
        ConditionHitAction: "LogOnly" | "Deny" | "Allow";
        /** @enum {string} */
        ConditionHitPhase: "BeforeAuthentication" | "AfterAuthentication" | "Login" | "Authorization";
        ConditionHitResponse: {
            /** Format: uuid */
            id: string;
            /** Format: date-time */
            occurredAt: string;
            traceId: string;
            /** Format: uuid */
            userId?: string | null;
            userName?: string | null;
            /** Format: uuid */
            sessionId?: string | null;
            clientIp?: string | null;
            userAgent?: string | null;
            source: components["schemas"]["ConditionHitSource"];
            /** Format: uuid */
            policyId?: string | null;
            policyCode: string;
            policyName?: string | null;
            action: components["schemas"]["ConditionHitAction"];
            phase: components["schemas"]["ConditionHitPhase"];
            isBlocked: boolean;
            operationId?: string | null;
            method?: string | null;
            path?: string | null;
            permissionCodes?: string | null;
            /** Format: int32 */
            hitCount: number;
        };
        /** @enum {string} */
        ConditionHitSource: "AccessPolicy" | "PermissionCondition";
        ConditionInventoryItemResponse: {
            /** Format: uuid */
            conditionId: string;
            subjectKind: components["schemas"]["GrantSubjectKind"];
            /** Format: uuid */
            subjectId: string;
            subjectName?: string | null;
            permission: string;
            action: components["schemas"]["PermissionAction"];
            /** Format: int32 */
            groupNo: number;
            attributeName: string;
            operator: components["schemas"]["ConditionOperator"];
            isNegated: boolean;
            value?: string | null;
            status: components["schemas"]["ConditionStatus"];
            diagnostics: components["schemas"]["ConditionDiagnostic"][];
            message?: string | null;
            groupConclusion: components["schemas"]["ConditionGroupConclusion"];
        };
        /** @enum {string} */
        ConditionOperator: "Equals" | "NotEquals" | "GreaterThan" | "GreaterThanOrEquals" | "LessThan" | "LessThanOrEquals" | "Contains" | "NotContains" | "In" | "NotIn" | "Between" | "StartsWith" | "EndsWith" | "IsNull" | "IsNotNull";
        /** @enum {string} */
        ConditionOutcome: "Matched" | "NotMatched" | "Indeterminate" | "Pending";
        /** @enum {string} */
        ConditionStatus: "Active" | "Disabled";
        ConditionTrialClauseResponse: {
            key?: string | null;
            attributeName: string;
            operator: components["schemas"]["ConditionOperator"];
            isNegated: boolean;
            value?: string | null;
            outcome: components["schemas"]["ConditionOutcome"];
            attributeValues: string[];
            reason?: string | null;
        };
        ConditionTrialGroupResponse: {
            key?: string | null;
            outcome: components["schemas"]["ConditionOutcome"];
            clauses: components["schemas"]["ConditionTrialClauseResponse"][];
        };
        ConditionTrialPathResponse: {
            source: string;
            /** Format: uuid */
            roleId?: string | null;
            roleName?: string | null;
            hasUnconditionalGrant: boolean;
            grantSets: components["schemas"]["ConditionTrialSetResponse"][];
            denySets: components["schemas"]["ConditionTrialSetResponse"][];
            holds: boolean;
        };
        ConditionTrialSetResponse: {
            subjectKind: components["schemas"]["GrantSubjectKind"];
            /** Format: uuid */
            subjectId: string;
            subjectName?: string | null;
            binding: string;
            action: components["schemas"]["PermissionAction"];
            applies: boolean;
            outcome?: components["schemas"]["ConditionOutcome"] | null;
            groups: components["schemas"]["ConditionTrialGroupResponse"][];
            errors: string[];
        };
        /** @enum {string} */
        ConditionValueKind: "String" | "Number" | "Boolean" | "DateTime" | "Date" | "TimeOfDay" | "IpAddress";
        /** @enum {string} */
        ConditionVerdict: "NotGranted" | "Unconditional" | "Granted" | "Denied";
        /** @enum {string} */
        ConfigDataType: "Text" | "Number" | "Boolean" | "Json" | "Array";
        ConfigResponse: {
            /** Format: uuid */
            id: string;
            ownerTenantId: string;
            isPlatform: boolean;
            key: string;
            name: string;
            group?: string | null;
            dataType: components["schemas"]["ConfigDataType"];
            value?: string | null;
            defaultValue?: string | null;
            hasValue: boolean;
            isEncrypted: boolean;
            isBuiltIn: boolean;
            allowTenantOverride: boolean;
            status: components["schemas"]["SettingStatus"];
            /** Format: int32 */
            order: number;
            description?: string | null;
            /** Format: date-time */
            createdAt: string;
            /** Format: date-time */
            modifiedAt?: string | null;
        };
        ConstraintItemRequest: {
            targetId: string;
            /** Format: int32 */
            groupNo?: number;
            remark?: string | null;
        };
        ConstraintItemResponse: {
            targetId: string;
            targetName?: string | null;
            targetCode?: string | null;
            /** Format: int32 */
            groupNo: number;
            remark?: string | null;
        };
        ConstraintRuleResponse: {
            /** Format: uuid */
            id: string;
            code: string;
            name: string;
            type: components["schemas"]["ConstraintType"];
            targetType: components["schemas"]["ConstraintTargetType"];
            /** Format: int32 */
            maxAllowed: number;
            conditions: components["schemas"]["AccessPolicyConditionResponse"][];
            violationAction: components["schemas"]["ConstraintViolationAction"];
            /** Format: int32 */
            priority: number;
            status: components["schemas"]["ConstraintRuleStatus"];
            /** Format: date-time */
            effectiveAt?: string | null;
            /** Format: date-time */
            expiresAt?: string | null;
            isEffective: boolean;
            description?: string | null;
            remark?: string | null;
            items: components["schemas"]["ConstraintItemResponse"][];
            /** Format: date-time */
            createdAt: string;
            /** Format: date-time */
            modifiedAt?: string | null;
        };
        /** @enum {string} */
        ConstraintRuleStatus: "Active" | "Disabled";
        /** @enum {string} */
        ConstraintTargetType: "Role" | "Permission" | "User";
        /** @enum {string} */
        ConstraintType: "Ssd" | "Dsd" | "MutualExclusion" | "Cardinality" | "Condition";
        /** @enum {string} */
        ConstraintViolationAction: "Deny" | "Warning" | "Log" | "RequireApproval";
        /** @enum {string} */
        DataScope: "SelfOnly" | "DepartmentOnly" | "DepartmentAndChildren" | "All" | "Custom";
        DataScopeDepartmentRequest: {
            /** Format: uuid */
            departmentId: string;
            includeChildren?: boolean;
        };
        DelegationResponse: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            delegatorId: string;
            delegatorName?: string | null;
            /** Format: uuid */
            delegateeId: string;
            delegateeName?: string | null;
            permission?: string | null;
            /** Format: uuid */
            roleId?: string | null;
            roleName?: string | null;
            /** Format: date-time */
            effectiveAt?: string | null;
            /** Format: date-time */
            expiresAt: string;
            reason?: string | null;
            status: components["schemas"]["DelegationStatus"];
            /** Format: date-time */
            revokedAt?: string | null;
            /** Format: date-time */
            createdAt: string;
        };
        /** @enum {string} */
        DelegationStatus: "Pending" | "Active" | "Expired" | "Revoked";
        /** @enum {string} */
        DeliveryChannel: "InApp" | "Email" | "Sms" | "Bot";
        DepartmentNode: {
            department: components["schemas"]["DepartmentResponse"];
            /** Format: int64 */
            memberCount: number;
            children: components["schemas"]["DepartmentNode"][];
        };
        DepartmentResponse: {
            /** Format: uuid */
            id: string;
            code: string;
            name: string;
            /** Format: uuid */
            parentId?: string | null;
            /** Format: uuid */
            leaderUserId?: string | null;
            status: components["schemas"]["OrganizationStatus"];
            /** Format: int32 */
            order: number;
            description?: string | null;
            /** Format: date-time */
            createdAt: string;
        };
        DictItemResponse: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            dictId: string;
            /** Format: uuid */
            parentId?: string | null;
            code: string;
            name: string;
            value?: string | null;
            description?: string | null;
            metadata?: string | null;
            isDefault: boolean;
            status: components["schemas"]["SettingStatus"];
            /** Format: int32 */
            order: number;
            /** Format: int32 */
            depth: number;
            /** Format: date-time */
            createdAt: string;
        };
        DictOption: {
            value: string;
            label: string;
            parentValue?: string | null;
            isDefault: boolean;
            disabled: boolean;
        };
        DictResponse: {
            /** Format: uuid */
            id: string;
            ownerTenantId: string;
            isPlatform: boolean;
            code: string;
            name: string;
            category?: string | null;
            description?: string | null;
            isBuiltIn: boolean;
            status: components["schemas"]["SettingStatus"];
            /** Format: int32 */
            order: number;
            /** Format: date-time */
            createdAt: string;
        };
        /** @enum {string} */
        DiffKind: "Insert" | "Update" | "Delete";
        DiffLogResponse: {
            /** Format: uuid */
            id: string;
            /** Format: date-time */
            occurredAt: string;
            traceId: string;
            /** Format: uuid */
            userId?: string | null;
            userName?: string | null;
            /** Format: uuid */
            sessionId?: string | null;
            clientIp?: string | null;
            userAgent?: string | null;
            operationId?: string | null;
            kind: components["schemas"]["DiffKind"];
            entityType: string;
            tableName?: string | null;
            entityId?: string | null;
            changedFields?: string | null;
        };
        EditionResponse: {
            /** Format: uuid */
            id: string;
            code: string;
            name: string;
            description?: string | null;
            isDefault: boolean;
            /** Format: int32 */
            order: number;
            /** Format: int32 */
            permissionCount: number;
            /** Format: int32 */
            tenantCount: number;
            /** Format: date-time */
            createdAt: string;
            /** Format: int32 */
            userLimit?: number | null;
            /** Format: int64 */
            storageLimitMb?: number | null;
            /** Format: decimal */
            price?: number | null;
            /** Format: int32 */
            billingPeriodMonths?: number | null;
            isFree: boolean;
        };
        EmailConfigResponse: {
            /** Format: uuid */
            id: string;
            code: string;
            name: string;
            smtpHost: string;
            /** Format: int32 */
            smtpPort: number;
            useSsl: boolean;
            acceptInvalidCertificate: boolean;
            fromEmail: string;
            fromName?: string | null;
            userName?: string | null;
            hasPassword: boolean;
            isDefault: boolean;
            isEnabled: boolean;
            /** Format: int32 */
            order: number;
            remark?: string | null;
            /** Format: date-time */
            createdAt: string;
            /** Format: date-time */
            modifiedAt?: string | null;
        };
        EmailRecordResponse: {
            /** Format: uuid */
            id: string;
            toEmail: string;
            subject: string;
            content?: string | null;
            isHtml: boolean;
            templateCode?: string | null;
            status: components["schemas"]["MessageSendStatus"];
            /** Format: int32 */
            attempts: number;
            /** Format: int32 */
            maxAttempts: number;
            /** Format: date-time */
            nextAttemptAt?: string | null;
            /** Format: date-time */
            sentAt?: string | null;
            errorMessage?: string | null;
            /** Format: uuid */
            configId?: string | null;
            /** Format: uuid */
            senderId?: string | null;
            /** Format: uuid */
            receiverId?: string | null;
            businessType?: string | null;
            businessId?: string | null;
            /** Format: date-time */
            createdAt: string;
        };
        ExceptionLogResponse: {
            /** Format: uuid */
            id: string;
            /** Format: date-time */
            occurredAt: string;
            traceId: string;
            /** Format: uuid */
            userId?: string | null;
            userName?: string | null;
            /** Format: uuid */
            sessionId?: string | null;
            clientIp?: string | null;
            userAgent?: string | null;
            operationId?: string | null;
            protocol?: string | null;
            method?: string | null;
            path?: string | null;
            exceptionType: string;
            message: string;
            source?: string | null;
            location?: string | null;
            machineName?: string | null;
            environmentName?: string | null;
            appVersion?: string | null;
        };
        ExportColumnRequest: {
            key: string;
            title: string;
            valueMap?: {
                [key: string]: string;
            } | null;
        };
        /** @enum {string} */
        ExportFormat: "Csv" | "Xlsx";
        /** @enum {string} */
        ExportScope: "CurrentPage" | "SearchResult" | "All";
        ExportTaskResponse: {
            /** Format: uuid */
            id: string;
            businessType: string;
            taskName: string;
            scope: components["schemas"]["ExportScope"];
            format: components["schemas"]["ExportFormat"];
            status: components["schemas"]["ExportTaskStatus"];
            /** Format: int32 */
            progress: number;
            /** Format: int64 */
            totalCount: number;
            /** Format: int64 */
            processedCount: number;
            /** Format: uuid */
            fileId?: string | null;
            fileName?: string | null;
            /** Format: int64 */
            fileSize?: number | null;
            errorMessage?: string | null;
            /** Format: date-time */
            startedAt?: string | null;
            /** Format: date-time */
            finishedAt?: string | null;
            /** Format: date-time */
            createdAt: string;
        };
        /** @enum {string} */
        ExportTaskStatus: "Pending" | "Processing" | "Success" | "Failed";
        ExternalLoginResponse: {
            provider: string;
            displayName?: string | null;
            email?: string | null;
            avatarUrl?: string | null;
            /** Format: date-time */
            boundAt: string;
            /** Format: date-time */
            lastLoginAt?: string | null;
        };
        /** @enum {string} */
        FieldMaskStrategy: "None" | "Hidden" | "FullMask" | "PartialMask" | "Hash" | "Redact";
        /** @enum {string} */
        FieldRuleStatus: "Active" | "Disabled";
        FieldSecurityEntityResponse: {
            code: string;
            titleKey: string;
            fields: components["schemas"]["FieldSecurityFieldResponse"][];
        };
        FieldSecurityFieldResponse: {
            name: string;
            titleKey: string;
            isText: boolean;
            isNullable: boolean;
        };
        FieldSecurityRuleResponse: {
            /** Format: uuid */
            id: string;
            targetKind: components["schemas"]["FieldSecurityTargetKind"];
            /** Format: uuid */
            targetId: string;
            targetName?: string | null;
            entity: string;
            field: string;
            strategy: components["schemas"]["FieldMaskStrategy"];
            /** Format: int32 */
            keepHead?: number | null;
            /** Format: int32 */
            keepTail?: number | null;
            replacement?: string | null;
            isEditable: boolean;
            status: components["schemas"]["FieldRuleStatus"];
            remark?: string | null;
            /** Format: date-time */
            createdAt: string;
        };
        /** @enum {string} */
        FieldSecurityTargetKind: "Role" | "User" | "Department";
        /** @enum {string} */
        FileAccessLevel: "Authenticated" | "Authorized";
        /** @enum {string} */
        FileKind: "Image" | "Document" | "Video" | "Audio" | "Archive" | "Other";
        /** FileResponse */
        FileResponse: {
            /** Format: uuid */
            id: string;
            name: string;
            originalName: string;
            extension?: string | null;
            kind: components["schemas"]["FileKind"];
            contentType: string;
            /** Format: int64 */
            size: number;
            hash: string;
            status: components["schemas"]["FileStatus"];
            accessLevel: components["schemas"]["FileAccessLevel"];
            isTemporary: boolean;
            /** Format: date-time */
            expiresAt?: string | null;
            source: string;
            tags?: string | null;
            remark?: string | null;
            /** Format: int32 */
            downloadCount: number;
            /** Format: date-time */
            lastDownloadedAt?: string | null;
            /** Format: date-time */
            createdAt: string;
            createdBy?: string | null;
            /** Format: date-time */
            modifiedAt?: string | null;
        };
        /** @enum {string} */
        FileStatus: "Normal" | "Archived" | "Corrupted";
        FileStorageResponse: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            storageConfigId: string;
            storageConfigCode?: string | null;
            storageConfigName?: string | null;
            storageType: components["schemas"]["StorageType"];
            bucket: string;
            key: string;
            isPrimary: boolean;
            status: components["schemas"]["FileStorageStatus"];
            /** Format: date-time */
            verifiedAt?: string | null;
            remark?: string | null;
            /** Format: date-time */
            createdAt: string;
        };
        /** @enum {string} */
        FileStorageStatus: "Normal" | "VerificationFailed" | "Unavailable";
        /** @enum {string} */
        GrantSubjectKind: "Role" | "User";
        ImpersonationCandidateResponse: {
            /** Format: uuid */
            id: string;
            userName: string;
            displayName: string;
            homeTenantId: string;
        };
        ImportHistoryResponse: {
            /** Format: uuid */
            id: string;
            pageCode: string;
            resourceCode?: string | null;
            fileName: string;
            /** Format: int32 */
            totalCount: number;
            /** Format: int32 */
            successCount: number;
            /** Format: int32 */
            failCount: number;
            errorSummary?: string | null;
            /** Format: date-time */
            createdAt: string;
        };
        InboxItemResponse: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            notificationId: string;
            title: string;
            content?: string | null;
            contentFormat: components["schemas"]["NotificationContentFormat"];
            notificationType: components["schemas"]["NotificationType"];
            priority: components["schemas"]["NotificationPriority"];
            status: components["schemas"]["UserNotificationStatus"];
            /** Format: date-time */
            publishedAt: string;
            /** Format: date-time */
            readAt?: string | null;
            /** Format: date-time */
            confirmedAt?: string | null;
            isGlobal: boolean;
            needConfirm: boolean;
            isMandatory: boolean;
            isBanner: boolean;
            isPopup: boolean;
            icon?: string | null;
            link?: string | null;
            /** Format: date-time */
            expiresAt?: string | null;
        };
        InheritedPermissionResponse: {
            permission: string;
            action: components["schemas"]["PermissionAction"];
            /** Format: uuid */
            sourceRoleId: string;
            sourceRoleName: string;
            /** Format: int32 */
            depth: number;
        };
        LoginAutoBanSettingsResponse: {
            configKey: string;
            isEnabled: boolean;
            /** Format: int32 */
            windowMinutes: number;
            /** Format: int32 */
            maxFailures: number;
            /** Format: int32 */
            banMinutes: number;
            /** Format: int32 */
            maxBanMinutes: number;
            error?: string | null;
        };
        LoginBanResponse: {
            ip: string;
            /** Format: date-time */
            until: string;
            /** Format: int32 */
            remainingSeconds: number;
        };
        /** @enum {string} */
        LoginEvent: "Login" | "Refresh" | "Logout" | "SwitchTenant" | "ImpersonationStart" | "ImpersonationStop" | "ChangePassword" | "TwoFactorChallenge" | "TwoFactorEnable" | "TwoFactorDisable" | "TwoFactorReset" | "RoleActivation";
        LoginLogResponse: {
            /** Format: uuid */
            id: string;
            /** Format: date-time */
            occurredAt: string;
            traceId: string;
            /** Format: uuid */
            userId?: string | null;
            userName?: string | null;
            /** Format: uuid */
            sessionId?: string | null;
            clientIp?: string | null;
            userAgent?: string | null;
            event: components["schemas"]["LoginEvent"];
            succeeded: boolean;
            errorCode?: string | null;
            errorMessage?: string | null;
            accountTenantId?: string | null;
            /** Format: uuid */
            impersonatorUserId?: string | null;
            detail?: string | null;
            browser?: string | null;
            os?: string | null;
            device?: string | null;
        };
        LoginProviderResponse: {
            name: string;
            displayName: string;
            type: string;
        };
        MembershipResponse: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            userId: string;
            userName: string;
            displayName: string;
            /** Format: uuid */
            departmentId: string;
            departmentName: string;
            /** Format: uuid */
            positionId?: string | null;
            positionName?: string | null;
            isPrimary: boolean;
            employeeNumber?: string | null;
            jobGrade?: string | null;
            /** Format: date-time */
            hiredAt?: string | null;
            /** Format: int32 */
            order: number;
            /** Format: date-time */
            createdAt: string;
        };
        /** @enum {string} */
        MenuKind: "Page" | "Directory" | "Link";
        MenuNode: {
            code: string;
            kind: components["schemas"]["MenuKind"];
            route: string;
            component: string;
            titleKey: string;
            title?: string | null;
            icon?: string | null;
            /** Format: int32 */
            order: number;
            permission?: string | null;
            externalUrl?: string | null;
            hidden: boolean;
            keepAlive: boolean;
            affix: boolean;
            children: components["schemas"]["MenuNode"][];
        };
        MenuResponse: {
            /** Format: uuid */
            id: string;
            code: string;
            kind: components["schemas"]["MenuKind"];
            package: string;
            /** Format: uuid */
            parentId?: string | null;
            route: string;
            component: string;
            titleKey: string;
            title?: string | null;
            icon?: string | null;
            /** Format: int32 */
            order: number;
            permission?: string | null;
            externalUrl?: string | null;
            isVisible: boolean;
            keepAlive: boolean;
            isAffix: boolean;
            isCustomized: boolean;
            children: components["schemas"]["MenuResponse"][];
        };
        /** @enum {string} */
        MessageSendStatus: "Pending" | "Sending" | "Success" | "Failed" | "Cancelled";
        MessageTemplateResponse: {
            /** Format: uuid */
            id: string;
            code: string;
            channel: components["schemas"]["TemplateChannel"];
            name: string;
            subject?: string | null;
            content: string;
            isHtml: boolean;
            description?: string | null;
            isEnabled: boolean;
            /** Format: int32 */
            order: number;
            remark?: string | null;
            /** Format: date-time */
            createdAt: string;
            /** Format: date-time */
            modifiedAt?: string | null;
        };
        MyApiCredentialResponse: {
            /** Format: uuid */
            id: string;
            name: string;
            appKey: string;
            isEnabled: boolean;
            /** Format: date-time */
            lastUsedAt?: string | null;
            /** Format: date-time */
            createdAt: string;
        };
        MyLoginLogResponse: {
            /** Format: uuid */
            id: string;
            /** Format: date-time */
            occurredAt: string;
            event: components["schemas"]["LoginEvent"];
            succeeded: boolean;
            errorCode?: string | null;
            errorMessage?: string | null;
            clientIp?: string | null;
            browser?: string | null;
            os?: string | null;
            device?: string | null;
            impersonated: boolean;
        };
        MySessionResponse: {
            /** Format: uuid */
            id: string;
            device?: string | null;
            browser?: string | null;
            os?: string | null;
            ipAddress?: string | null;
            location?: string | null;
            /** Format: date-time */
            loginAt: string;
            /** Format: date-time */
            lastActiveAt: string;
            isCurrent: boolean;
        };
        /** @enum {string} */
        NotificationContentFormat: "Text" | "Markdown" | "Html";
        /** @enum {string} */
        NotificationPriority: "Low" | "Normal" | "High" | "Urgent";
        /** @enum {string} */
        NotificationRecipientFilter: "All" | "Unread" | "Read" | "Unconfirmed";
        NotificationRecipientResponse: {
            /** Format: uuid */
            userId: string;
            userName: string;
            displayName: string;
            status: components["schemas"]["UserNotificationStatus"];
            /** Format: date-time */
            readAt?: string | null;
            /** Format: date-time */
            confirmedAt?: string | null;
            /** Format: date-time */
            receivedAt: string;
        };
        NotificationResponse: {
            /** Format: uuid */
            id: string;
            title: string;
            content?: string | null;
            contentFormat: components["schemas"]["NotificationContentFormat"];
            notificationType: components["schemas"]["NotificationType"];
            priority: components["schemas"]["NotificationPriority"];
            icon?: string | null;
            link?: string | null;
            businessType?: string | null;
            businessId?: string | null;
            deliveryChannels: components["schemas"]["DeliveryChannel"][];
            targetType: components["schemas"]["NotificationTargetType"];
            targetIds: string[];
            needConfirm: boolean;
            isMandatory: boolean;
            isBanner: boolean;
            isPopup: boolean;
            /** Format: date-time */
            startsAt?: string | null;
            /** Format: date-time */
            expiresAt?: string | null;
            isPublished: boolean;
            /** Format: date-time */
            publishedAt?: string | null;
            /** Format: uuid */
            publisherId?: string | null;
            /** Format: int32 */
            recipientCount: number;
            remark?: string | null;
            /** Format: date-time */
            createdAt: string;
            /** Format: date-time */
            modifiedAt?: string | null;
        };
        /** @enum {string} */
        NotificationTargetType: "All" | "Role" | "Department" | "User";
        /** @enum {string} */
        NotificationType: "System" | "Security" | "Business" | "Todo" | "Emergency";
        NumberingAllocationResponse: {
            /** Format: uuid */
            id: string;
            requestTenantId: string;
            idempotencyKey: string;
            /** Format: int32 */
            count: number;
            /** Format: int64 */
            startValue: number;
            /** Format: int64 */
            endValue: number;
            periodKey: string;
            firstNumber: string;
            lastNumber: string;
            businessType?: string | null;
            businessId?: string | null;
            /** Format: date-time */
            generatedAt: string;
        };
        /** @enum {string} */
        NumberingDateFormat: "None" | "Yyyy" | "YyyyMM" | "YyyyMMdd" | "YyMM" | "YyMMdd" | "MMdd";
        /** @enum {string} */
        NumberingResetCycle: "Never" | "Daily" | "Monthly" | "Yearly";
        NumberingRuleResponse: {
            /** Format: uuid */
            id: string;
            ownerTenantId: string;
            isPlatform: boolean;
            code: string;
            name: string;
            prefix?: string | null;
            separator: string;
            dateFormat: components["schemas"]["NumberingDateFormat"];
            /** Format: int32 */
            serialLength: number;
            resetCycle: components["schemas"]["NumberingResetCycle"];
            timeZoneId: string;
            template: string;
            /** Format: int64 */
            currentValue: number;
            currentPeriod: string;
            hasAllocated: boolean;
            allowTenantUse: boolean;
            status: components["schemas"]["SettingStatus"];
            /** Format: int32 */
            order: number;
            description?: string | null;
            /** Format: date-time */
            createdAt: string;
            /** Format: date-time */
            modifiedAt?: string | null;
        };
        /** @enum {string} */
        NumberingScope: "Auto" | "Tenant" | "Platform";
        OAuthAppResponse: {
            /** Format: uuid */
            id: string;
            appName: string;
            appDescription?: string | null;
            clientId: string;
            clientType: components["schemas"]["OAuthClientType"];
            appType: components["schemas"]["OAuthAppType"];
            grantTypes: string[];
            redirectUris: string[];
            scopes: string[];
            /** Format: int32 */
            accessTokenLifetime: number;
            /** Format: int32 */
            refreshTokenLifetime: number;
            /** Format: int32 */
            authorizationCodeLifetime: number;
            logo?: string | null;
            homepage?: string | null;
            skipConsent: boolean;
            isEnabled: boolean;
            /** Format: uuid */
            ownerUserId?: string | null;
            ownerUserName?: string | null;
            remark?: string | null;
            /** Format: date-time */
            createdAt: string;
            /** Format: date-time */
            modifiedAt?: string | null;
        };
        /** @enum {string} */
        OAuthAppType: "Web" | "Mobile" | "Desktop" | "Service";
        /** @enum {string} */
        OAuthClientType: "Confidential" | "Public";
        OAuthConsentResponse: {
            clientId: string;
            appName?: string | null;
            logo?: string | null;
            homepage?: string | null;
            scopes: string[];
            /** Format: date-time */
            grantedAt: string;
        };
        OnlineSessionResponse: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            userId: string;
            userName: string;
            displayName: string;
            device?: string | null;
            browser?: string | null;
            os?: string | null;
            ipAddressMasked?: string | null;
            location?: string | null;
            /** Format: date-time */
            loginAt: string;
            /** Format: date-time */
            lastActiveAt: string;
            isRealtimeOnline: boolean;
            isCurrent: boolean;
            /** Format: uuid */
            impersonatorUserId?: string | null;
        };
        OpenApiLogResponse: {
            /** Format: uuid */
            id: string;
            /** Format: date-time */
            occurredAt: string;
            traceId: string;
            /** Format: uuid */
            userId?: string | null;
            userName?: string | null;
            /** Format: uuid */
            sessionId?: string | null;
            clientIp?: string | null;
            userAgent?: string | null;
            clientId?: string | null;
            appName?: string | null;
            operationId?: string | null;
            method: string;
            path: string;
            signatureAlgorithm?: string | null;
            signatureValid: boolean;
            /** Format: int32 */
            statusCode: number;
            succeeded: boolean;
            errorCode?: string | null;
            errorMessage?: string | null;
            /** Format: int64 */
            durationMs: number;
            /** Format: int64 */
            requestSize: number;
            /** Format: int64 */
            responseSize: number;
        };
        OpenEchoRequest: {
            message?: string | null;
            /** Format: int32 */
            number?: number | null;
        };
        OperationLogResponse: {
            /** Format: uuid */
            id: string;
            /** Format: date-time */
            occurredAt: string;
            traceId: string;
            /** Format: uuid */
            userId?: string | null;
            userName?: string | null;
            /** Format: uuid */
            sessionId?: string | null;
            operationId: string;
            succeeded: boolean;
            errorCode?: string | null;
            errorMessage?: string | null;
            resource?: string | null;
            /** Format: int64 */
            durationMs?: number | null;
        };
        /** @enum {string} */
        OrganizationStatus: "Active" | "Disabled";
        PageFilter: {
            field: string;
            operator: string;
            value?: string | null;
        };
        PageInfo: {
            /** Format: int32 */
            index?: number;
            /** Format: int32 */
            size?: number;
        };
        PageSort: {
            field: string;
            descending?: boolean;
        };
        /** @enum {string} */
        PermissionAction: "Grant" | "Deny";
        PermissionChangeLogResponse: {
            /** Format: uuid */
            id: string;
            /** Format: date-time */
            occurredAt: string;
            traceId: string;
            /** Format: uuid */
            userId?: string | null;
            userName?: string | null;
            /** Format: uuid */
            sessionId?: string | null;
            clientIp?: string | null;
            userAgent?: string | null;
            changeType: components["schemas"]["PermissionChangeType"];
            /** Format: uuid */
            roleId?: string | null;
            roleName?: string | null;
            /** Format: uuid */
            targetUserId?: string | null;
            targetUserName?: string | null;
            /** Format: uuid */
            editionId?: string | null;
            editionCode?: string | null;
            targetTenantId?: string | null;
            permissionCodes?: string | null;
            detail?: string | null;
        };
        /** @enum {string} */
        PermissionChangeType: "RoleGrant" | "RoleRevoke" | "RoleAssign" | "RoleUnassign" | "RoleDataScope" | "RoleDelete" | "EditionPermissions" | "TenantEdition" | "RoleDeny" | "RoleAssignmentWindow" | "UserGrant" | "UserDeny" | "UserRevoke" | "UserDataScope" | "FieldSecurity" | "RoleInheritance" | "Delegation" | "ConditionChange" | "ConstraintViolation";
        PermissionConditionResponse: {
            /** Format: uuid */
            id: string;
            permission: string;
            action: components["schemas"]["PermissionAction"];
            /** Format: int32 */
            groupNo: number;
            attributeName: string;
            operator: components["schemas"]["ConditionOperator"];
            isNegated: boolean;
            value?: string | null;
            description?: string | null;
            status: components["schemas"]["ConditionStatus"];
            /** Format: date-time */
            createdAt: string;
        };
        PermissionFailure: {
            permission: string;
            code: string;
        };
        PermissionResponse: {
            code: string;
            displayName: string;
            description?: string | null;
            package: string;
            resource: string;
            action: string;
            scope: components["schemas"]["PermissionScope"];
            audited: boolean;
        };
        PermissionRuleResponse: {
            permission: string;
            action: components["schemas"]["PermissionAction"];
            /** Format: date-time */
            effectiveAt?: string | null;
            /** Format: date-time */
            expiresAt?: string | null;
            reason?: string | null;
        };
        /** @enum {string} */
        PermissionScope: "Platform" | "Tenant" | "Both";
        PositionResponse: {
            /** Format: uuid */
            id: string;
            code: string;
            name: string;
            status: components["schemas"]["OrganizationStatus"];
            /** Format: int32 */
            order: number;
            description?: string | null;
            /** Format: date-time */
            createdAt: string;
        };
        RoleMemberResponse: {
            /** Format: uuid */
            userId: string;
            userName: string;
            displayName: string;
            status: components["schemas"]["UserStatus"];
            /** Format: date-time */
            assignedAt: string;
            /** Format: date-time */
            effectiveAt?: string | null;
            /** Format: date-time */
            expiresAt?: string | null;
        };
        RoleRelativeResponse: {
            /** Format: uuid */
            roleId: string;
            code: string;
            name: string;
            status: components["schemas"]["RoleStatus"];
            /** Format: int32 */
            depth: number;
            path: string[];
            isEffective: boolean;
        };
        RoleResponse: {
            /** Format: uuid */
            id: string;
            ownerTenantId: string;
            code: string;
            name: string;
            description?: string | null;
            /** Format: int32 */
            order: number;
            isBuiltIn: boolean;
            status: components["schemas"]["RoleStatus"];
            dataScope: components["schemas"]["DataScope"];
            /** Format: date-time */
            createdAt: string;
        };
        /** @enum {string} */
        RoleStatus: "Active" | "Disabled";
        RoleSummary: {
            /** Format: uuid */
            id: string;
            code: string;
            name: string;
        };
        SessionRoleItemResponse: {
            /** Format: uuid */
            roleId: string;
            code: string;
            name: string;
            isActive: boolean;
        };
        SessionRoleResponse: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            sessionId: string;
            /** Format: uuid */
            userId: string;
            userName?: string | null;
            /** Format: uuid */
            roleId: string;
            roleName?: string | null;
            roleCode?: string | null;
            status: components["schemas"]["SessionRoleStatus"];
            /** Format: date-time */
            activatedAt: string;
            /** Format: date-time */
            deactivatedAt?: string | null;
            reason?: string | null;
        };
        /** @enum {string} */
        SessionRoleStatus: "Active" | "Inactive" | "Expired";
        /** @enum {string} */
        SettingStatus: "Active" | "Disabled";
        SmsConfigResponse: {
            /** Format: uuid */
            id: string;
            code: string;
            name: string;
            provider: components["schemas"]["SmsProvider"];
            accessKeyId: string;
            hasAccessKeySecret: boolean;
            sdkAppId?: string | null;
            signName: string;
            region?: string | null;
            templateMap?: string | null;
            isDefault: boolean;
            isEnabled: boolean;
            /** Format: int32 */
            order: number;
            remark?: string | null;
            /** Format: date-time */
            createdAt: string;
            /** Format: date-time */
            modifiedAt?: string | null;
        };
        /** @enum {string} */
        SmsProvider: "Aliyun" | "Tencent";
        SmsRecordResponse: {
            /** Format: uuid */
            id: string;
            toPhone: string;
            content: string;
            templateCode?: string | null;
            templateParams?: string | null;
            provider?: components["schemas"]["SmsProvider"] | null;
            providerMessageId?: string | null;
            status: components["schemas"]["MessageSendStatus"];
            /** Format: int32 */
            attempts: number;
            /** Format: int32 */
            maxAttempts: number;
            /** Format: date-time */
            nextAttemptAt?: string | null;
            /** Format: date-time */
            sentAt?: string | null;
            errorMessage?: string | null;
            /** Format: uuid */
            configId?: string | null;
            /** Format: uuid */
            senderId?: string | null;
            /** Format: uuid */
            receiverId?: string | null;
            businessType?: string | null;
            businessId?: string | null;
            /** Format: date-time */
            createdAt: string;
        };
        StorageConfigResponse: {
            /** Format: uuid */
            id: string;
            code: string;
            name: string;
            type: components["schemas"]["StorageType"];
            endpoint?: string | null;
            region?: string | null;
            bucket: string;
            accessKeyId?: string | null;
            hasSecretAccessKey: boolean;
            isDefault: boolean;
            status: components["schemas"]["StorageConfigStatus"];
            /** Format: int32 */
            order: number;
            remark?: string | null;
            /** Format: date-time */
            createdAt: string;
            /** Format: date-time */
            modifiedAt?: string | null;
        };
        /** @enum {string} */
        StorageConfigStatus: "Active" | "Disabled";
        /** @enum {string} */
        StorageType: "Local" | "S3" | "Oss" | "Cos" | "MinIO";
        TelegramBotResponse: {
            /** Format: uuid */
            id: string;
            name: string;
            hasToken: boolean;
            adminUsers?: string | null;
            allowedGroupChatIds?: string | null;
            allowedCommands?: string | null;
            enableFallbackReply: boolean;
            broadcastChatIds?: string | null;
            isEnabled: boolean;
            /** Format: int32 */
            order: number;
            remark?: string | null;
            /** Format: date-time */
            createdAt: string;
            /** Format: date-time */
            modifiedAt?: string | null;
        };
        /** @enum {string} */
        TemplateChannel: "Email" | "Sms";
        TenantMemberResponse: {
            /** Format: uuid */
            id: string;
            tenantId: string;
            /** Format: uuid */
            userId: string;
            userName: string;
            displayName: string;
            homeTenantId: string;
            memberType: components["schemas"]["TenantMemberType"];
            /** Format: date-time */
            createdAt: string;
        };
        /** @enum {string} */
        TenantMemberType: "Member" | "Owner";
        TenantQuotaResponse: {
            tenantId: string;
            name: string;
            editionCode?: string | null;
            editionName?: string | null;
            /** Format: int32 */
            userLimit?: number | null;
            /** Format: int64 */
            usedSeats: number;
            /** Format: int64 */
            storageLimitMb?: number | null;
            /** Format: int64 */
            usedStorageBytes: number;
            isOverSeats: boolean;
            isOverStorage: boolean;
        };
        TenantResponse: {
            /** Format: uuid */
            id: string;
            tenantId: string;
            name: string;
            description?: string | null;
            status: components["schemas"]["TenantStatus"];
            /** Format: date-time */
            expiresAt?: string | null;
            /** Format: uuid */
            adminUserId?: string | null;
            /** Format: uuid */
            editionId?: string | null;
            /** Format: int32 */
            order: number;
            /** Format: date-time */
            createdAt: string;
            /** Format: int32 */
            userLimit?: number | null;
            /** Format: int64 */
            storageLimitMb?: number | null;
        };
        /** @enum {string} */
        TenantStatus: "Active" | "Suspended";
        TokenResponse: {
            accessToken: string;
            /** Format: date-time */
            accessExpiresAt: string;
            refreshToken: string;
            /** Format: date-time */
            refreshExpiresAt: string;
            /** Format: uuid */
            sessionId: string;
            tenantId?: string | null;
            mustChangePassword: boolean;
            user: components["schemas"]["UserResponse"];
            tenantName?: string | null;
            impersonatorUserName?: string | null;
        };
        /** @enum {string} */
        TraceDimension: "TraceId" | "UserId" | "UserName" | "SessionId" | "ClientIp";
        /** @enum {string} */
        TraceItemStatus: "Success" | "Info" | "Warning" | "Failure";
        /** @enum {string} */
        TraceLogType: "Access" | "OpenApi" | "Operation" | "Login" | "Exception" | "Diff" | "PermissionChange" | "ConditionHit";
        TraceTimelineItem: {
            logType: components["schemas"]["TraceLogType"];
            /** Format: uuid */
            id: string;
            /** Format: date-time */
            occurredAt: string;
            title: string;
            summary?: string | null;
            status: components["schemas"]["TraceItemStatus"];
            traceId: string;
            /** Format: uuid */
            userId?: string | null;
            userName?: string | null;
            /** Format: uuid */
            sessionId?: string | null;
            clientIp?: string | null;
            method?: string | null;
            path?: string | null;
            /** Format: int32 */
            statusCode?: number | null;
            /** Format: int64 */
            durationMs?: number | null;
        };
        TraceTypeCount: {
            logType: components["schemas"]["TraceLogType"];
            /** Format: int32 */
            count: number;
        };
        TwoFactorChallengeResponse: {
            ticket: string;
            methods: components["schemas"]["TwoFactorMethod"][];
            method?: components["schemas"]["TwoFactorMethod"] | null;
            codeSent: boolean;
            /** Format: date-time */
            expiresAt: string;
        };
        /** @enum {string} */
        TwoFactorMethod: "Totp" | "Email" | "Phone";
        UserGrantResponse: {
            permission: string;
            action: components["schemas"]["PermissionAction"];
            /** Format: date-time */
            effectiveAt?: string | null;
            /** Format: date-time */
            expiresAt?: string | null;
            reason?: string | null;
        };
        /** @enum {string} */
        UserNotificationStatus: "Unread" | "Read";
        UserResponse: {
            /** Format: uuid */
            id: string;
            userName: string;
            displayName: string;
            email?: string | null;
            phoneNumber?: string | null;
            homeTenantId: string;
            status: components["schemas"]["UserStatus"];
            /** Format: date-time */
            lastLoginAt?: string | null;
            /** Format: date-time */
            createdAt: string;
            createdBy?: string | null;
        };
        /** @enum {string} */
        UserSettingScene: "Preference" | "Page";
        /** @enum {string} */
        UserStatus: "Active" | "Disabled" | "Locked";
    };
    responses: never;
    parameters: never;
    requestBodies: never;
    headers: never;
    pathItems: never;
}
export type $defs = Record<string, never>;
export interface operations {
    "access-logs.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: date-time */
                        occurredAt: string;
                        traceId: string;
                        /** Format: uuid */
                        userId?: string | null;
                        userName?: string | null;
                        /** Format: uuid */
                        sessionId?: string | null;
                        clientIp?: string | null;
                        userAgent?: string | null;
                        operationId: string;
                        protocol?: string | null;
                        method?: string | null;
                        path?: string | null;
                        result: components["schemas"]["AccessResult"];
                        /** Format: int32 */
                        statusCode?: number | null;
                        errorCode?: string | null;
                        errorMessage?: string | null;
                        /** Format: int64 */
                        durationMs: number;
                        browser?: string | null;
                        os?: string | null;
                        device?: string | null;
                        referer?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "access-logs.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["AccessLogResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "auth.activate-roles": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    roleIds: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        accessToken: string;
                        /** Format: date-time */
                        accessExpiresAt: string;
                        refreshToken: string;
                        /** Format: date-time */
                        refreshExpiresAt: string;
                        /** Format: uuid */
                        sessionId: string;
                        tenantId?: string | null;
                        mustChangePassword: boolean;
                        user: components["schemas"]["UserResponse"];
                        tenantName?: string | null;
                        impersonatorUserName?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "auth.captcha": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": Record<string, never>;
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        captchaId: string;
                        image: string;
                        /** Format: int32 */
                        expiresInSeconds: number;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "auth.external-login": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    ticket: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        token?: components["schemas"]["TokenResponse"] | null;
                        twoFactor?: components["schemas"]["TwoFactorChallengeResponse"] | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "auth.lock": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    password?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        reason: string;
                        /** Format: date-time */
                        lockedAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "auth.login": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    userName: string;
                    password: string;
                    tenant?: string | null;
                    captchaId?: string | null;
                    captchaCode?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        token?: components["schemas"]["TokenResponse"] | null;
                        twoFactor?: components["schemas"]["TwoFactorChallengeResponse"] | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "auth.login-config": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        loginMethods: string[];
                        oAuthProviders: components["schemas"]["LoginProviderResponse"][];
                        captchaEnabled: boolean;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "auth.logout": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": Record<string, never>;
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        sessionId: string;
                        /** Format: date-time */
                        revokedAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "auth.me": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        user: components["schemas"]["UserResponse"];
                        tenantId?: string | null;
                        /** Format: uuid */
                        sessionId?: string | null;
                        roles: string[];
                        mustChangePassword: boolean;
                        tenantName?: string | null;
                        impersonatorUserName?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "auth.permissions": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        permissions: string[];
                        roles: components["schemas"]["RoleSummary"][];
                        menus: components["schemas"]["MenuNode"][];
                        pages: string[];
                        isPlatform: boolean;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "auth.refresh": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    refreshToken: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        accessToken: string;
                        /** Format: date-time */
                        accessExpiresAt: string;
                        refreshToken: string;
                        /** Format: date-time */
                        refreshExpiresAt: string;
                        /** Format: uuid */
                        sessionId: string;
                        tenantId?: string | null;
                        mustChangePassword: boolean;
                        user: components["schemas"]["UserResponse"];
                        tenantName?: string | null;
                        impersonatorUserName?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "auth.session-roles": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["SessionRoleItemResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "auth.switch-tenant": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    tenantId?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        accessToken: string;
                        /** Format: date-time */
                        accessExpiresAt: string;
                        refreshToken: string;
                        /** Format: date-time */
                        refreshExpiresAt: string;
                        /** Format: uuid */
                        sessionId: string;
                        tenantId?: string | null;
                        mustChangePassword: boolean;
                        user: components["schemas"]["UserResponse"];
                        tenantName?: string | null;
                        impersonatorUserName?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "auth.unlock": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    password?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        unlocked: boolean;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "auth.verify-two-factor": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    ticket: string;
                    method: components["schemas"]["TwoFactorMethod"];
                    code: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        accessToken: string;
                        /** Format: date-time */
                        accessExpiresAt: string;
                        refreshToken: string;
                        /** Format: date-time */
                        refreshExpiresAt: string;
                        /** Format: uuid */
                        sessionId: string;
                        tenantId?: string | null;
                        mustChangePassword: boolean;
                        user: components["schemas"]["UserResponse"];
                        tenantName?: string | null;
                        impersonatorUserName?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "bot-configs.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    code: string;
                    name: string;
                    provider: components["schemas"]["BotProvider"];
                    webhookUrl: string;
                    secret?: string | null;
                    keyword?: string | null;
                    isEnabled?: boolean;
                    /** Format: int32 */
                    order?: number;
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        provider: components["schemas"]["BotProvider"];
                        webhookUrl: string;
                        hasSecret: boolean;
                        keyword?: string | null;
                        isDefault: boolean;
                        isEnabled: boolean;
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "bot-configs.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "bot-configs.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        provider: components["schemas"]["BotProvider"];
                        webhookUrl: string;
                        hasSecret: boolean;
                        keyword?: string | null;
                        isDefault: boolean;
                        isEnabled: boolean;
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "bot-configs.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    name: string;
                    provider: components["schemas"]["BotProvider"];
                    webhookUrl: string;
                    secret?: string | null;
                    keyword?: string | null;
                    isEnabled?: boolean;
                    /** Format: int32 */
                    order?: number;
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        provider: components["schemas"]["BotProvider"];
                        webhookUrl: string;
                        hasSecret: boolean;
                        keyword?: string | null;
                        isDefault: boolean;
                        isEnabled: boolean;
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "bot-configs.set-default": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        provider: components["schemas"]["BotProvider"];
                        webhookUrl: string;
                        hasSecret: boolean;
                        keyword?: string | null;
                        isDefault: boolean;
                        isEnabled: boolean;
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "bot-configs.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["BotConfigResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "condition-hits.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: date-time */
                        occurredAt: string;
                        traceId: string;
                        /** Format: uuid */
                        userId?: string | null;
                        userName?: string | null;
                        /** Format: uuid */
                        sessionId?: string | null;
                        clientIp?: string | null;
                        userAgent?: string | null;
                        source: components["schemas"]["ConditionHitSource"];
                        /** Format: uuid */
                        policyId?: string | null;
                        policyCode: string;
                        policyName?: string | null;
                        action: components["schemas"]["ConditionHitAction"];
                        phase: components["schemas"]["ConditionHitPhase"];
                        isBlocked: boolean;
                        operationId?: string | null;
                        method?: string | null;
                        path?: string | null;
                        permissionCodes?: string | null;
                        /** Format: int32 */
                        hitCount: number;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "condition-hits.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["ConditionHitResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "configs.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    key: string;
                    name: string;
                    dataType?: components["schemas"]["ConfigDataType"];
                    group?: string | null;
                    value?: string | null;
                    defaultValue?: string | null;
                    isEncrypted?: boolean;
                    allowTenantOverride?: boolean;
                    /** Format: int32 */
                    order?: number;
                    description?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        ownerTenantId: string;
                        isPlatform: boolean;
                        key: string;
                        name: string;
                        group?: string | null;
                        dataType: components["schemas"]["ConfigDataType"];
                        value?: string | null;
                        defaultValue?: string | null;
                        hasValue: boolean;
                        isEncrypted: boolean;
                        isBuiltIn: boolean;
                        allowTenantOverride: boolean;
                        status: components["schemas"]["SettingStatus"];
                        /** Format: int32 */
                        order: number;
                        description?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "configs.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "configs.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        ownerTenantId: string;
                        isPlatform: boolean;
                        key: string;
                        name: string;
                        group?: string | null;
                        dataType: components["schemas"]["ConfigDataType"];
                        value?: string | null;
                        defaultValue?: string | null;
                        hasValue: boolean;
                        isEncrypted: boolean;
                        isBuiltIn: boolean;
                        allowTenantOverride: boolean;
                        status: components["schemas"]["SettingStatus"];
                        /** Format: int32 */
                        order: number;
                        description?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "configs.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    name: string;
                    dataType?: components["schemas"]["ConfigDataType"];
                    group?: string | null;
                    value?: string | null;
                    defaultValue?: string | null;
                    isEncrypted?: boolean;
                    allowTenantOverride?: boolean;
                    /** Format: int32 */
                    order?: number;
                    description?: string | null;
                    status?: components["schemas"]["SettingStatus"];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        ownerTenantId: string;
                        isPlatform: boolean;
                        key: string;
                        name: string;
                        group?: string | null;
                        dataType: components["schemas"]["ConfigDataType"];
                        value?: string | null;
                        defaultValue?: string | null;
                        hasValue: boolean;
                        isEncrypted: boolean;
                        isBuiltIn: boolean;
                        allowTenantOverride: boolean;
                        status: components["schemas"]["SettingStatus"];
                        /** Format: int32 */
                        order: number;
                        description?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "configs.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["ConfigResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "constraint-rules.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    code: string;
                    name: string;
                    type: components["schemas"]["ConstraintType"];
                    targetType: components["schemas"]["ConstraintTargetType"];
                    items: components["schemas"]["ConstraintItemRequest"][];
                    /** Format: int32 */
                    maxAllowed?: number;
                    conditions?: components["schemas"]["AccessPolicyConditionRequest"][] | null;
                    violationAction?: components["schemas"]["ConstraintViolationAction"];
                    /** Format: int32 */
                    priority?: number;
                    status?: components["schemas"]["ConstraintRuleStatus"];
                    /** Format: date-time */
                    effectiveAt?: string | null;
                    /** Format: date-time */
                    expiresAt?: string | null;
                    description?: string | null;
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        type: components["schemas"]["ConstraintType"];
                        targetType: components["schemas"]["ConstraintTargetType"];
                        /** Format: int32 */
                        maxAllowed: number;
                        conditions: components["schemas"]["AccessPolicyConditionResponse"][];
                        violationAction: components["schemas"]["ConstraintViolationAction"];
                        /** Format: int32 */
                        priority: number;
                        status: components["schemas"]["ConstraintRuleStatus"];
                        /** Format: date-time */
                        effectiveAt?: string | null;
                        /** Format: date-time */
                        expiresAt?: string | null;
                        isEffective: boolean;
                        description?: string | null;
                        remark?: string | null;
                        items: components["schemas"]["ConstraintItemResponse"][];
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "constraint-rules.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "constraint-rules.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        type: components["schemas"]["ConstraintType"];
                        targetType: components["schemas"]["ConstraintTargetType"];
                        /** Format: int32 */
                        maxAllowed: number;
                        conditions: components["schemas"]["AccessPolicyConditionResponse"][];
                        violationAction: components["schemas"]["ConstraintViolationAction"];
                        /** Format: int32 */
                        priority: number;
                        status: components["schemas"]["ConstraintRuleStatus"];
                        /** Format: date-time */
                        effectiveAt?: string | null;
                        /** Format: date-time */
                        expiresAt?: string | null;
                        isEffective: boolean;
                        description?: string | null;
                        remark?: string | null;
                        items: components["schemas"]["ConstraintItemResponse"][];
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "constraint-rules.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    code: string;
                    name: string;
                    type: components["schemas"]["ConstraintType"];
                    targetType: components["schemas"]["ConstraintTargetType"];
                    items: components["schemas"]["ConstraintItemRequest"][];
                    /** Format: int32 */
                    maxAllowed?: number;
                    conditions?: components["schemas"]["AccessPolicyConditionRequest"][] | null;
                    violationAction?: components["schemas"]["ConstraintViolationAction"];
                    /** Format: int32 */
                    priority?: number;
                    status?: components["schemas"]["ConstraintRuleStatus"];
                    /** Format: date-time */
                    effectiveAt?: string | null;
                    /** Format: date-time */
                    expiresAt?: string | null;
                    description?: string | null;
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        type: components["schemas"]["ConstraintType"];
                        targetType: components["schemas"]["ConstraintTargetType"];
                        /** Format: int32 */
                        maxAllowed: number;
                        conditions: components["schemas"]["AccessPolicyConditionResponse"][];
                        violationAction: components["schemas"]["ConstraintViolationAction"];
                        /** Format: int32 */
                        priority: number;
                        status: components["schemas"]["ConstraintRuleStatus"];
                        /** Format: date-time */
                        effectiveAt?: string | null;
                        /** Format: date-time */
                        expiresAt?: string | null;
                        isEffective: boolean;
                        description?: string | null;
                        remark?: string | null;
                        items: components["schemas"]["ConstraintItemResponse"][];
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "constraint-rules.set-status": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    status: components["schemas"]["ConstraintRuleStatus"];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        type: components["schemas"]["ConstraintType"];
                        targetType: components["schemas"]["ConstraintTargetType"];
                        /** Format: int32 */
                        maxAllowed: number;
                        conditions: components["schemas"]["AccessPolicyConditionResponse"][];
                        violationAction: components["schemas"]["ConstraintViolationAction"];
                        /** Format: int32 */
                        priority: number;
                        status: components["schemas"]["ConstraintRuleStatus"];
                        /** Format: date-time */
                        effectiveAt?: string | null;
                        /** Format: date-time */
                        expiresAt?: string | null;
                        isEffective: boolean;
                        description?: string | null;
                        remark?: string | null;
                        items: components["schemas"]["ConstraintItemResponse"][];
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "constraint-rules.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["ConstraintRuleResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "delegations.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    delegatorId: string;
                    /** Format: uuid */
                    delegateeId: string;
                    /** Format: date-time */
                    expiresAt: string;
                    permission?: string | null;
                    /** Format: uuid */
                    roleId?: string | null;
                    /** Format: date-time */
                    effectiveAt?: string | null;
                    reason?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: uuid */
                        delegatorId: string;
                        delegatorName?: string | null;
                        /** Format: uuid */
                        delegateeId: string;
                        delegateeName?: string | null;
                        permission?: string | null;
                        /** Format: uuid */
                        roleId?: string | null;
                        roleName?: string | null;
                        /** Format: date-time */
                        effectiveAt?: string | null;
                        /** Format: date-time */
                        expiresAt: string;
                        reason?: string | null;
                        status: components["schemas"]["DelegationStatus"];
                        /** Format: date-time */
                        revokedAt?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "delegations.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "delegations.revoke": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: uuid */
                        delegatorId: string;
                        delegatorName?: string | null;
                        /** Format: uuid */
                        delegateeId: string;
                        delegateeName?: string | null;
                        permission?: string | null;
                        /** Format: uuid */
                        roleId?: string | null;
                        roleName?: string | null;
                        /** Format: date-time */
                        effectiveAt?: string | null;
                        /** Format: date-time */
                        expiresAt: string;
                        reason?: string | null;
                        status: components["schemas"]["DelegationStatus"];
                        /** Format: date-time */
                        revokedAt?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "delegations.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["DelegationResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "departments.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    code: string;
                    name: string;
                    /** Format: uuid */
                    parentId?: string | null;
                    /** Format: uuid */
                    leaderUserId?: string | null;
                    /** Format: int32 */
                    order?: number;
                    description?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        /** Format: uuid */
                        parentId?: string | null;
                        /** Format: uuid */
                        leaderUserId?: string | null;
                        status: components["schemas"]["OrganizationStatus"];
                        /** Format: int32 */
                        order: number;
                        description?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "departments.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "departments.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        department: components["schemas"]["DepartmentResponse"];
                        ancestors: components["schemas"]["DepartmentResponse"][];
                        leaderUserName?: string | null;
                        /** Format: int64 */
                        childCount: number;
                        /** Format: int64 */
                        memberCount: number;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "departments.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    name: string;
                    /** Format: uuid */
                    leaderUserId?: string | null;
                    /** Format: int32 */
                    order?: number;
                    description?: string | null;
                    status?: components["schemas"]["OrganizationStatus"];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        /** Format: uuid */
                        parentId?: string | null;
                        /** Format: uuid */
                        leaderUserId?: string | null;
                        status: components["schemas"]["OrganizationStatus"];
                        /** Format: int32 */
                        order: number;
                        description?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "departments.move": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    /** Format: uuid */
                    parentId?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        /** Format: uuid */
                        parentId?: string | null;
                        /** Format: uuid */
                        leaderUserId?: string | null;
                        status: components["schemas"]["OrganizationStatus"];
                        /** Format: int32 */
                        order: number;
                        description?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "departments.tree": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["DepartmentNode"][];
                        /** Format: int64 */
                        total: number;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "dict-items.list": {
        parameters: {
            query: {
                dictId: string;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["DictItemResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "dict-items.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    dictId: string;
                    code: string;
                    name: string;
                    /** Format: uuid */
                    parentId?: string | null;
                    value?: string | null;
                    description?: string | null;
                    metadata?: string | null;
                    isDefault?: boolean;
                    /** Format: int32 */
                    order?: number;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: uuid */
                        dictId: string;
                        /** Format: uuid */
                        parentId?: string | null;
                        code: string;
                        name: string;
                        value?: string | null;
                        description?: string | null;
                        metadata?: string | null;
                        isDefault: boolean;
                        status: components["schemas"]["SettingStatus"];
                        /** Format: int32 */
                        order: number;
                        /** Format: int32 */
                        depth: number;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "dict-items.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "dict-items.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: uuid */
                        dictId: string;
                        /** Format: uuid */
                        parentId?: string | null;
                        code: string;
                        name: string;
                        value?: string | null;
                        description?: string | null;
                        metadata?: string | null;
                        isDefault: boolean;
                        status: components["schemas"]["SettingStatus"];
                        /** Format: int32 */
                        order: number;
                        /** Format: int32 */
                        depth: number;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "dict-items.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    name: string;
                    /** Format: uuid */
                    parentId?: string | null;
                    value?: string | null;
                    description?: string | null;
                    metadata?: string | null;
                    isDefault?: boolean;
                    /** Format: int32 */
                    order?: number;
                    status?: components["schemas"]["SettingStatus"];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: uuid */
                        dictId: string;
                        /** Format: uuid */
                        parentId?: string | null;
                        code: string;
                        name: string;
                        value?: string | null;
                        description?: string | null;
                        metadata?: string | null;
                        isDefault: boolean;
                        status: components["schemas"]["SettingStatus"];
                        /** Format: int32 */
                        order: number;
                        /** Format: int32 */
                        depth: number;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "dicts.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    code: string;
                    name: string;
                    category?: string | null;
                    description?: string | null;
                    /** Format: int32 */
                    order?: number;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        ownerTenantId: string;
                        isPlatform: boolean;
                        code: string;
                        name: string;
                        category?: string | null;
                        description?: string | null;
                        isBuiltIn: boolean;
                        status: components["schemas"]["SettingStatus"];
                        /** Format: int32 */
                        order: number;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "dicts.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "dicts.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        ownerTenantId: string;
                        isPlatform: boolean;
                        code: string;
                        name: string;
                        category?: string | null;
                        description?: string | null;
                        isBuiltIn: boolean;
                        status: components["schemas"]["SettingStatus"];
                        /** Format: int32 */
                        order: number;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "dicts.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    name: string;
                    category?: string | null;
                    description?: string | null;
                    /** Format: int32 */
                    order?: number;
                    status?: components["schemas"]["SettingStatus"];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        ownerTenantId: string;
                        isPlatform: boolean;
                        code: string;
                        name: string;
                        category?: string | null;
                        description?: string | null;
                        isBuiltIn: boolean;
                        status: components["schemas"]["SettingStatus"];
                        /** Format: int32 */
                        order: number;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "dicts.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["DictResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "dicts.options": {
        parameters: {
            query: {
                code: string;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["DictOption"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "diff-logs.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: date-time */
                        occurredAt: string;
                        traceId: string;
                        /** Format: uuid */
                        userId?: string | null;
                        userName?: string | null;
                        /** Format: uuid */
                        sessionId?: string | null;
                        clientIp?: string | null;
                        userAgent?: string | null;
                        operationId?: string | null;
                        kind: components["schemas"]["DiffKind"];
                        entityType: string;
                        tableName?: string | null;
                        entityId?: string | null;
                        changedFields?: string | null;
                        before?: string | null;
                        after?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "diff-logs.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["DiffLogResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "editions.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["EditionResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "editions.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    code: string;
                    name: string;
                    description?: string | null;
                    isDefault?: boolean;
                    /** Format: int32 */
                    order?: number;
                    permissions?: string[] | null;
                    /** Format: int32 */
                    userLimit?: number | null;
                    /** Format: int64 */
                    storageLimitMb?: number | null;
                    /** Format: decimal */
                    price?: number | null;
                    /** Format: int32 */
                    billingPeriodMonths?: number | null;
                    isFree?: boolean;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        edition: components["schemas"]["EditionResponse"];
                        permissions: string[];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "editions.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "editions.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        edition: components["schemas"]["EditionResponse"];
                        permissions: string[];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "editions.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    name: string;
                    description?: string | null;
                    isDefault?: boolean;
                    /** Format: int32 */
                    order?: number;
                    /** Format: int32 */
                    userLimit?: number | null;
                    /** Format: int64 */
                    storageLimitMb?: number | null;
                    /** Format: decimal */
                    price?: number | null;
                    /** Format: int32 */
                    billingPeriodMonths?: number | null;
                    isFree?: boolean;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        edition: components["schemas"]["EditionResponse"];
                        permissions: string[];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "editions.set-permissions": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    permissions: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        edition: components["schemas"]["EditionResponse"];
                        permissions: string[];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "email-configs.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    code: string;
                    name: string;
                    smtpHost: string;
                    fromEmail: string;
                    /** Format: int32 */
                    smtpPort?: number;
                    useSsl?: boolean;
                    acceptInvalidCertificate?: boolean;
                    fromName?: string | null;
                    userName?: string | null;
                    password?: string | null;
                    isEnabled?: boolean;
                    /** Format: int32 */
                    order?: number;
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        smtpHost: string;
                        /** Format: int32 */
                        smtpPort: number;
                        useSsl: boolean;
                        acceptInvalidCertificate: boolean;
                        fromEmail: string;
                        fromName?: string | null;
                        userName?: string | null;
                        hasPassword: boolean;
                        isDefault: boolean;
                        isEnabled: boolean;
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "email-configs.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "email-configs.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        smtpHost: string;
                        /** Format: int32 */
                        smtpPort: number;
                        useSsl: boolean;
                        acceptInvalidCertificate: boolean;
                        fromEmail: string;
                        fromName?: string | null;
                        userName?: string | null;
                        hasPassword: boolean;
                        isDefault: boolean;
                        isEnabled: boolean;
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "email-configs.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    name: string;
                    smtpHost: string;
                    fromEmail: string;
                    /** Format: int32 */
                    smtpPort?: number;
                    useSsl?: boolean;
                    acceptInvalidCertificate?: boolean;
                    fromName?: string | null;
                    userName?: string | null;
                    password?: string | null;
                    isEnabled?: boolean;
                    /** Format: int32 */
                    order?: number;
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        smtpHost: string;
                        /** Format: int32 */
                        smtpPort: number;
                        useSsl: boolean;
                        acceptInvalidCertificate: boolean;
                        fromEmail: string;
                        fromName?: string | null;
                        userName?: string | null;
                        hasPassword: boolean;
                        isDefault: boolean;
                        isEnabled: boolean;
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "email-configs.set-default": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        smtpHost: string;
                        /** Format: int32 */
                        smtpPort: number;
                        useSsl: boolean;
                        acceptInvalidCertificate: boolean;
                        fromEmail: string;
                        fromName?: string | null;
                        userName?: string | null;
                        hasPassword: boolean;
                        isDefault: boolean;
                        isEnabled: boolean;
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "email-configs.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["EmailConfigResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "emails.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "emails.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        toEmail: string;
                        subject: string;
                        content?: string | null;
                        isHtml: boolean;
                        templateCode?: string | null;
                        status: components["schemas"]["MessageSendStatus"];
                        /** Format: int32 */
                        attempts: number;
                        /** Format: int32 */
                        maxAttempts: number;
                        /** Format: date-time */
                        nextAttemptAt?: string | null;
                        /** Format: date-time */
                        sentAt?: string | null;
                        errorMessage?: string | null;
                        /** Format: uuid */
                        configId?: string | null;
                        /** Format: uuid */
                        senderId?: string | null;
                        /** Format: uuid */
                        receiverId?: string | null;
                        businessType?: string | null;
                        businessId?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "emails.cancel": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    ids: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "emails.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["EmailRecordResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "emails.resend": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    ids: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "exception-logs.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: date-time */
                        occurredAt: string;
                        traceId: string;
                        /** Format: uuid */
                        userId?: string | null;
                        userName?: string | null;
                        /** Format: uuid */
                        sessionId?: string | null;
                        clientIp?: string | null;
                        userAgent?: string | null;
                        operationId?: string | null;
                        protocol?: string | null;
                        method?: string | null;
                        path?: string | null;
                        exceptionType: string;
                        message: string;
                        source?: string | null;
                        location?: string | null;
                        machineName?: string | null;
                        environmentName?: string | null;
                        appVersion?: string | null;
                        stackTrace?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "exception-logs.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["ExceptionLogResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "export-tasks.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "export-tasks.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        businessType: string;
                        taskName: string;
                        scope: components["schemas"]["ExportScope"];
                        format: components["schemas"]["ExportFormat"];
                        status: components["schemas"]["ExportTaskStatus"];
                        /** Format: int32 */
                        progress: number;
                        /** Format: int64 */
                        totalCount: number;
                        /** Format: int64 */
                        processedCount: number;
                        /** Format: uuid */
                        fileId?: string | null;
                        fileName?: string | null;
                        /** Format: int64 */
                        fileSize?: number | null;
                        errorMessage?: string | null;
                        /** Format: date-time */
                        startedAt?: string | null;
                        /** Format: date-time */
                        finishedAt?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "export-tasks.cancel": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        businessType: string;
                        taskName: string;
                        scope: components["schemas"]["ExportScope"];
                        format: components["schemas"]["ExportFormat"];
                        status: components["schemas"]["ExportTaskStatus"];
                        /** Format: int32 */
                        progress: number;
                        /** Format: int64 */
                        totalCount: number;
                        /** Format: int64 */
                        processedCount: number;
                        /** Format: uuid */
                        fileId?: string | null;
                        fileName?: string | null;
                        /** Format: int64 */
                        fileSize?: number | null;
                        errorMessage?: string | null;
                        /** Format: date-time */
                        startedAt?: string | null;
                        /** Format: date-time */
                        finishedAt?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "export-tasks.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["ExportTaskResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "export-tasks.submit": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    businessType: string;
                    columns: components["schemas"]["ExportColumnRequest"][];
                    taskName?: string | null;
                    scope?: components["schemas"]["ExportScope"];
                    format?: components["schemas"]["ExportFormat"];
                    querySnapshot?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        businessType: string;
                        taskName: string;
                        scope: components["schemas"]["ExportScope"];
                        format: components["schemas"]["ExportFormat"];
                        status: components["schemas"]["ExportTaskStatus"];
                        /** Format: int32 */
                        progress: number;
                        /** Format: int64 */
                        totalCount: number;
                        /** Format: int64 */
                        processedCount: number;
                        /** Format: uuid */
                        fileId?: string | null;
                        fileName?: string | null;
                        /** Format: int64 */
                        fileSize?: number | null;
                        errorMessage?: string | null;
                        /** Format: date-time */
                        startedAt?: string | null;
                        /** Format: date-time */
                        finishedAt?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "field-security.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    targetKind: components["schemas"]["FieldSecurityTargetKind"];
                    /** Format: uuid */
                    targetId: string;
                    entity: string;
                    field: string;
                    strategy: components["schemas"]["FieldMaskStrategy"];
                    /** Format: int32 */
                    keepHead?: number | null;
                    /** Format: int32 */
                    keepTail?: number | null;
                    replacement?: string | null;
                    isEditable?: boolean;
                    status?: components["schemas"]["FieldRuleStatus"];
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        targetKind: components["schemas"]["FieldSecurityTargetKind"];
                        /** Format: uuid */
                        targetId: string;
                        targetName?: string | null;
                        entity: string;
                        field: string;
                        strategy: components["schemas"]["FieldMaskStrategy"];
                        /** Format: int32 */
                        keepHead?: number | null;
                        /** Format: int32 */
                        keepTail?: number | null;
                        replacement?: string | null;
                        isEditable: boolean;
                        status: components["schemas"]["FieldRuleStatus"];
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "field-security.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "field-security.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        targetKind: components["schemas"]["FieldSecurityTargetKind"];
                        /** Format: uuid */
                        targetId: string;
                        targetName?: string | null;
                        entity: string;
                        field: string;
                        strategy: components["schemas"]["FieldMaskStrategy"];
                        /** Format: int32 */
                        keepHead?: number | null;
                        /** Format: int32 */
                        keepTail?: number | null;
                        replacement?: string | null;
                        isEditable: boolean;
                        status: components["schemas"]["FieldRuleStatus"];
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "field-security.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    targetKind: components["schemas"]["FieldSecurityTargetKind"];
                    /** Format: uuid */
                    targetId: string;
                    entity: string;
                    field: string;
                    strategy: components["schemas"]["FieldMaskStrategy"];
                    /** Format: int32 */
                    keepHead?: number | null;
                    /** Format: int32 */
                    keepTail?: number | null;
                    replacement?: string | null;
                    isEditable?: boolean;
                    status?: components["schemas"]["FieldRuleStatus"];
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        targetKind: components["schemas"]["FieldSecurityTargetKind"];
                        /** Format: uuid */
                        targetId: string;
                        targetName?: string | null;
                        entity: string;
                        field: string;
                        strategy: components["schemas"]["FieldMaskStrategy"];
                        /** Format: int32 */
                        keepHead?: number | null;
                        /** Format: int32 */
                        keepTail?: number | null;
                        replacement?: string | null;
                        isEditable: boolean;
                        status: components["schemas"]["FieldRuleStatus"];
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "field-security.entities": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["FieldSecurityEntityResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "field-security.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["FieldSecurityRuleResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "files.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "files.signed-content": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                token: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/octet-stream": string;
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "files.upload": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "multipart/form-data": {
                    /** Format: binary */
                    file?: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["FileResponse"];
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "files.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["FileResponse"];
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "files.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    accessLevel?: components["schemas"]["FileAccessLevel"];
                    isTemporary?: boolean;
                    /** Format: date-time */
                    expiresAt?: string | null;
                    tags?: string | null;
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        name: string;
                        originalName: string;
                        extension?: string | null;
                        kind: components["schemas"]["FileKind"];
                        contentType: string;
                        /** Format: int64 */
                        size: number;
                        hash: string;
                        status: components["schemas"]["FileStatus"];
                        accessLevel: components["schemas"]["FileAccessLevel"];
                        isTemporary: boolean;
                        /** Format: date-time */
                        expiresAt?: string | null;
                        source: string;
                        tags?: string | null;
                        remark?: string | null;
                        /** Format: int32 */
                        downloadCount: number;
                        /** Format: date-time */
                        lastDownloadedAt?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        createdBy?: string | null;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "files.content": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/octet-stream": string;
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "files.presign": {
        parameters: {
            query?: {
                expiresInSeconds?: number | null;
            };
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        url: string;
                        /** Format: date-time */
                        expiresAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "files.set-primary-storage": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    /** Format: uuid */
                    storageId: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        name: string;
                        originalName: string;
                        extension?: string | null;
                        kind: components["schemas"]["FileKind"];
                        contentType: string;
                        /** Format: int64 */
                        size: number;
                        hash: string;
                        status: components["schemas"]["FileStatus"];
                        accessLevel: components["schemas"]["FileAccessLevel"];
                        isTemporary: boolean;
                        /** Format: date-time */
                        expiresAt?: string | null;
                        source: string;
                        tags?: string | null;
                        remark?: string | null;
                        /** Format: int32 */
                        downloadCount: number;
                        /** Format: date-time */
                        lastDownloadedAt?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        createdBy?: string | null;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "files.set-storage-status": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    /** Format: uuid */
                    storageId: string;
                    status: components["schemas"]["FileStorageStatus"];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        name: string;
                        originalName: string;
                        extension?: string | null;
                        kind: components["schemas"]["FileKind"];
                        contentType: string;
                        /** Format: int64 */
                        size: number;
                        hash: string;
                        status: components["schemas"]["FileStatus"];
                        accessLevel: components["schemas"]["FileAccessLevel"];
                        isTemporary: boolean;
                        /** Format: date-time */
                        expiresAt?: string | null;
                        source: string;
                        tags?: string | null;
                        remark?: string | null;
                        /** Format: int32 */
                        downloadCount: number;
                        /** Format: date-time */
                        lastDownloadedAt?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        createdBy?: string | null;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "files.storages": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["FileStorageResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "files.verify-storage": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    /** Format: uuid */
                    storageId: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        name: string;
                        originalName: string;
                        extension?: string | null;
                        kind: components["schemas"]["FileKind"];
                        contentType: string;
                        /** Format: int64 */
                        size: number;
                        hash: string;
                        status: components["schemas"]["FileStatus"];
                        accessLevel: components["schemas"]["FileAccessLevel"];
                        isTemporary: boolean;
                        /** Format: date-time */
                        expiresAt?: string | null;
                        source: string;
                        tags?: string | null;
                        remark?: string | null;
                        /** Format: int32 */
                        downloadCount: number;
                        /** Format: date-time */
                        lastDownloadedAt?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        createdBy?: string | null;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "files.fast-upload": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    hash: string;
                    /** Format: int64 */
                    size: number;
                    originalName: string;
                    contentType?: string | null;
                    accessLevel?: components["schemas"]["FileAccessLevel"];
                    isTemporary?: boolean;
                    /** Format: date-time */
                    expiresAt?: string | null;
                    tags?: string | null;
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        file?: components["schemas"]["FileResponse"] | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "files.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["FileResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "files.set-status": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    ids: string[];
                    status: components["schemas"]["FileStatus"];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "impersonation.candidates": {
        parameters: {
            query?: {
                tenantId?: string | null;
                keyword?: string | null;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["ImpersonationCandidateResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "impersonation.start": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    targetUserId: string;
                    tenantId?: string | null;
                    reason?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        accessToken: string;
                        /** Format: date-time */
                        accessExpiresAt: string;
                        refreshToken: string;
                        /** Format: date-time */
                        refreshExpiresAt: string;
                        /** Format: uuid */
                        sessionId: string;
                        tenantId?: string | null;
                        mustChangePassword: boolean;
                        user: components["schemas"]["UserResponse"];
                        tenantName?: string | null;
                        impersonatorUserName?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "impersonation.stop": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": Record<string, never>;
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        accessToken: string;
                        /** Format: date-time */
                        accessExpiresAt: string;
                        refreshToken: string;
                        /** Format: date-time */
                        refreshExpiresAt: string;
                        /** Format: uuid */
                        sessionId: string;
                        tenantId?: string | null;
                        mustChangePassword: boolean;
                        user: components["schemas"]["UserResponse"];
                        tenantName?: string | null;
                        impersonatorUserName?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "import-histories.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    pageCode: string;
                    fileName: string;
                    /** Format: int32 */
                    totalCount: number;
                    /** Format: int32 */
                    successCount: number;
                    /** Format: int32 */
                    failCount: number;
                    resourceCode?: string | null;
                    errorSummary?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        pageCode: string;
                        resourceCode?: string | null;
                        fileName: string;
                        /** Format: int32 */
                        totalCount: number;
                        /** Format: int32 */
                        successCount: number;
                        /** Format: int32 */
                        failCount: number;
                        errorSummary?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "import-histories.recent": {
        parameters: {
            query: {
                pageCode: string;
                count?: number;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["ImportHistoryResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "inbox.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "inbox.confirm": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: int32 */
                        count: number;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "inbox.mark-popup-shown": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: int32 */
                        count: number;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "inbox.banners": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["InboxItemResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "inbox.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["InboxItemResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "inbox.mandatory": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["InboxItemResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "inbox.mark-all-read": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": Record<string, never>;
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: int32 */
                        count: number;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "inbox.mark-read": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    ids: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "inbox.popups": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["InboxItemResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "login-bans.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        settings: components["schemas"]["LoginAutoBanSettingsResponse"];
                        items: components["schemas"]["LoginBanResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "login-bans.lift": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    ip: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        ip: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "login-logs.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: date-time */
                        occurredAt: string;
                        traceId: string;
                        /** Format: uuid */
                        userId?: string | null;
                        userName?: string | null;
                        /** Format: uuid */
                        sessionId?: string | null;
                        clientIp?: string | null;
                        userAgent?: string | null;
                        event: components["schemas"]["LoginEvent"];
                        succeeded: boolean;
                        errorCode?: string | null;
                        errorMessage?: string | null;
                        accountTenantId?: string | null;
                        /** Format: uuid */
                        impersonatorUserId?: string | null;
                        detail?: string | null;
                        browser?: string | null;
                        os?: string | null;
                        device?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "login-logs.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["LoginLogResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "memberships.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    userId: string;
                    /** Format: uuid */
                    departmentId: string;
                    /** Format: uuid */
                    positionId?: string | null;
                    isPrimary?: boolean;
                    employeeNumber?: string | null;
                    jobGrade?: string | null;
                    /** Format: date-time */
                    hiredAt?: string | null;
                    /** Format: int32 */
                    order?: number;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: uuid */
                        userId: string;
                        userName: string;
                        displayName: string;
                        /** Format: uuid */
                        departmentId: string;
                        departmentName: string;
                        /** Format: uuid */
                        positionId?: string | null;
                        positionName?: string | null;
                        isPrimary: boolean;
                        employeeNumber?: string | null;
                        jobGrade?: string | null;
                        /** Format: date-time */
                        hiredAt?: string | null;
                        /** Format: int32 */
                        order: number;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "memberships.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "memberships.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    /** Format: uuid */
                    positionId?: string | null;
                    isPrimary?: boolean;
                    employeeNumber?: string | null;
                    jobGrade?: string | null;
                    /** Format: date-time */
                    hiredAt?: string | null;
                    /** Format: int32 */
                    order?: number;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: uuid */
                        userId: string;
                        userName: string;
                        displayName: string;
                        /** Format: uuid */
                        departmentId: string;
                        departmentName: string;
                        /** Format: uuid */
                        positionId?: string | null;
                        positionName?: string | null;
                        isPrimary: boolean;
                        employeeNumber?: string | null;
                        jobGrade?: string | null;
                        /** Format: date-time */
                        hiredAt?: string | null;
                        /** Format: int32 */
                        order: number;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "memberships.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["MembershipResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "menus.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["MenuResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "menus.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    kind: components["schemas"]["MenuKind"];
                    code: string;
                    title: string;
                    icon?: string | null;
                    /** Format: int32 */
                    order?: number;
                    /** Format: uuid */
                    parentId?: string | null;
                    externalUrl?: string | null;
                    permission?: string | null;
                    isVisible?: boolean;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        kind: components["schemas"]["MenuKind"];
                        package: string;
                        /** Format: uuid */
                        parentId?: string | null;
                        route: string;
                        component: string;
                        titleKey: string;
                        title?: string | null;
                        icon?: string | null;
                        /** Format: int32 */
                        order: number;
                        permission?: string | null;
                        externalUrl?: string | null;
                        isVisible: boolean;
                        keepAlive: boolean;
                        isAffix: boolean;
                        isCustomized: boolean;
                        children: components["schemas"]["MenuResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "menus.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "menus.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        kind: components["schemas"]["MenuKind"];
                        package: string;
                        /** Format: uuid */
                        parentId?: string | null;
                        route: string;
                        component: string;
                        titleKey: string;
                        title?: string | null;
                        icon?: string | null;
                        /** Format: int32 */
                        order: number;
                        permission?: string | null;
                        externalUrl?: string | null;
                        isVisible: boolean;
                        keepAlive: boolean;
                        isAffix: boolean;
                        isCustomized: boolean;
                        children: components["schemas"]["MenuResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "menus.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    title?: string | null;
                    icon?: string | null;
                    /** Format: int32 */
                    order?: number;
                    /** Format: uuid */
                    parentId?: string | null;
                    isVisible?: boolean;
                    keepAlive?: boolean;
                    isAffix?: boolean;
                    externalUrl?: string | null;
                    permission?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        kind: components["schemas"]["MenuKind"];
                        package: string;
                        /** Format: uuid */
                        parentId?: string | null;
                        route: string;
                        component: string;
                        titleKey: string;
                        title?: string | null;
                        icon?: string | null;
                        /** Format: int32 */
                        order: number;
                        permission?: string | null;
                        externalUrl?: string | null;
                        isVisible: boolean;
                        keepAlive: boolean;
                        isAffix: boolean;
                        isCustomized: boolean;
                        children: components["schemas"]["MenuResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "menus.reset": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        kind: components["schemas"]["MenuKind"];
                        package: string;
                        /** Format: uuid */
                        parentId?: string | null;
                        route: string;
                        component: string;
                        titleKey: string;
                        title?: string | null;
                        icon?: string | null;
                        /** Format: int32 */
                        order: number;
                        permission?: string | null;
                        externalUrl?: string | null;
                        isVisible: boolean;
                        keepAlive: boolean;
                        isAffix: boolean;
                        isCustomized: boolean;
                        children: components["schemas"]["MenuResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "message-templates.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    code: string;
                    channel: components["schemas"]["TemplateChannel"];
                    name: string;
                    content: string;
                    subject?: string | null;
                    isHtml?: boolean;
                    description?: string | null;
                    isEnabled?: boolean;
                    /** Format: int32 */
                    order?: number;
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        channel: components["schemas"]["TemplateChannel"];
                        name: string;
                        subject?: string | null;
                        content: string;
                        isHtml: boolean;
                        description?: string | null;
                        isEnabled: boolean;
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "message-templates.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "message-templates.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        channel: components["schemas"]["TemplateChannel"];
                        name: string;
                        subject?: string | null;
                        content: string;
                        isHtml: boolean;
                        description?: string | null;
                        isEnabled: boolean;
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "message-templates.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    name: string;
                    content: string;
                    subject?: string | null;
                    isHtml?: boolean;
                    description?: string | null;
                    isEnabled?: boolean;
                    /** Format: int32 */
                    order?: number;
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        channel: components["schemas"]["TemplateChannel"];
                        name: string;
                        subject?: string | null;
                        content: string;
                        isHtml: boolean;
                        description?: string | null;
                        isEnabled: boolean;
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "message-templates.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["MessageTemplateResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "my-activity.summary": {
        parameters: {
            query?: {
                timeZone?: string | null;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        today: components["schemas"]["ActivityCountsResponse"];
                        thisWeek: components["schemas"]["ActivityCountsResponse"];
                        thisMonth: components["schemas"]["ActivityCountsResponse"];
                        /** Format: date-time */
                        lastLoginAt?: string | null;
                        /** Format: date-time */
                        lastAccessAt?: string | null;
                        /** Format: date-time */
                        lastOperationAt?: string | null;
                        trend: components["schemas"]["ActivityDayResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "my-api-credentials.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["MyApiCredentialResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "my-api-credentials.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    name?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        appKey: string;
                        secret: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "my-api-credentials.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "my-api-credentials.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    isEnabled: boolean;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        name: string;
                        appKey: string;
                        isEnabled: boolean;
                        /** Format: date-time */
                        lastUsedAt?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "my-api-credentials.rotate-secret": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        appKey: string;
                        secret: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "my-api-credentials.settings": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        signatureAlgorithm: string;
                        ipAllowList: string[];
                        algorithms: string[];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "my-api-credentials.update-settings": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    signatureAlgorithm: string;
                    ipAllowList?: string[] | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        signatureAlgorithm: string;
                        ipAllowList: string[];
                        algorithms: string[];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "my-login-logs.list": {
        parameters: {
            query?: {
                page?: number;
                pageSize?: number;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["MyLoginLogResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "my-oauth-apps.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["OAuthAppResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "my-oauth-apps.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    appName: string;
                    clientType: components["schemas"]["OAuthClientType"];
                    homepage?: string | null;
                    appDescription?: string | null;
                    redirectUris: string[];
                    logo?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        clientId: string;
                        clientType: components["schemas"]["OAuthClientType"];
                        clientSecret?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "my-oauth-apps.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "my-oauth-apps.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        appName: string;
                        appDescription?: string | null;
                        clientId: string;
                        clientType: components["schemas"]["OAuthClientType"];
                        appType: components["schemas"]["OAuthAppType"];
                        grantTypes: string[];
                        redirectUris: string[];
                        scopes: string[];
                        /** Format: int32 */
                        accessTokenLifetime: number;
                        /** Format: int32 */
                        refreshTokenLifetime: number;
                        /** Format: int32 */
                        authorizationCodeLifetime: number;
                        logo?: string | null;
                        homepage?: string | null;
                        skipConsent: boolean;
                        isEnabled: boolean;
                        /** Format: uuid */
                        ownerUserId?: string | null;
                        ownerUserName?: string | null;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "my-oauth-apps.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    appName: string;
                    homepage?: string | null;
                    appDescription?: string | null;
                    redirectUris: string[];
                    logo?: string | null;
                    isEnabled: boolean;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        appName: string;
                        appDescription?: string | null;
                        clientId: string;
                        clientType: components["schemas"]["OAuthClientType"];
                        appType: components["schemas"]["OAuthAppType"];
                        grantTypes: string[];
                        redirectUris: string[];
                        scopes: string[];
                        /** Format: int32 */
                        accessTokenLifetime: number;
                        /** Format: int32 */
                        refreshTokenLifetime: number;
                        /** Format: int32 */
                        authorizationCodeLifetime: number;
                        logo?: string | null;
                        homepage?: string | null;
                        skipConsent: boolean;
                        isEnabled: boolean;
                        /** Format: uuid */
                        ownerUserId?: string | null;
                        ownerUserName?: string | null;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "my-oauth-apps.reset-secret": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        clientId: string;
                        clientType: components["schemas"]["OAuthClientType"];
                        clientSecret?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "notifications.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    title: string;
                    content?: string | null;
                    contentFormat?: components["schemas"]["NotificationContentFormat"];
                    notificationType?: components["schemas"]["NotificationType"];
                    priority?: components["schemas"]["NotificationPriority"];
                    targetType?: components["schemas"]["NotificationTargetType"];
                    targetIds?: string[] | null;
                    icon?: string | null;
                    link?: string | null;
                    businessType?: string | null;
                    businessId?: string | null;
                    needConfirm?: boolean;
                    isMandatory?: boolean;
                    isBanner?: boolean;
                    isPopup?: boolean;
                    /** Format: date-time */
                    startsAt?: string | null;
                    /** Format: date-time */
                    expiresAt?: string | null;
                    remark?: string | null;
                    deliveryChannels?: components["schemas"]["DeliveryChannel"][] | null;
                    publishImmediately?: boolean;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        title: string;
                        content?: string | null;
                        contentFormat: components["schemas"]["NotificationContentFormat"];
                        notificationType: components["schemas"]["NotificationType"];
                        priority: components["schemas"]["NotificationPriority"];
                        icon?: string | null;
                        link?: string | null;
                        businessType?: string | null;
                        businessId?: string | null;
                        deliveryChannels: components["schemas"]["DeliveryChannel"][];
                        targetType: components["schemas"]["NotificationTargetType"];
                        targetIds: string[];
                        needConfirm: boolean;
                        isMandatory: boolean;
                        isBanner: boolean;
                        isPopup: boolean;
                        /** Format: date-time */
                        startsAt?: string | null;
                        /** Format: date-time */
                        expiresAt?: string | null;
                        isPublished: boolean;
                        /** Format: date-time */
                        publishedAt?: string | null;
                        /** Format: uuid */
                        publisherId?: string | null;
                        /** Format: int32 */
                        recipientCount: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "notifications.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "notifications.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        title: string;
                        content?: string | null;
                        contentFormat: components["schemas"]["NotificationContentFormat"];
                        notificationType: components["schemas"]["NotificationType"];
                        priority: components["schemas"]["NotificationPriority"];
                        icon?: string | null;
                        link?: string | null;
                        businessType?: string | null;
                        businessId?: string | null;
                        deliveryChannels: components["schemas"]["DeliveryChannel"][];
                        targetType: components["schemas"]["NotificationTargetType"];
                        targetIds: string[];
                        needConfirm: boolean;
                        isMandatory: boolean;
                        isBanner: boolean;
                        isPopup: boolean;
                        /** Format: date-time */
                        startsAt?: string | null;
                        /** Format: date-time */
                        expiresAt?: string | null;
                        isPublished: boolean;
                        /** Format: date-time */
                        publishedAt?: string | null;
                        /** Format: uuid */
                        publisherId?: string | null;
                        /** Format: int32 */
                        recipientCount: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "notifications.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    title: string;
                    content?: string | null;
                    contentFormat?: components["schemas"]["NotificationContentFormat"];
                    notificationType?: components["schemas"]["NotificationType"];
                    priority?: components["schemas"]["NotificationPriority"];
                    targetType?: components["schemas"]["NotificationTargetType"];
                    targetIds?: string[] | null;
                    icon?: string | null;
                    link?: string | null;
                    businessType?: string | null;
                    businessId?: string | null;
                    needConfirm?: boolean;
                    isMandatory?: boolean;
                    isBanner?: boolean;
                    isPopup?: boolean;
                    /** Format: date-time */
                    startsAt?: string | null;
                    /** Format: date-time */
                    expiresAt?: string | null;
                    remark?: string | null;
                    deliveryChannels?: components["schemas"]["DeliveryChannel"][] | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        title: string;
                        content?: string | null;
                        contentFormat: components["schemas"]["NotificationContentFormat"];
                        notificationType: components["schemas"]["NotificationType"];
                        priority: components["schemas"]["NotificationPriority"];
                        icon?: string | null;
                        link?: string | null;
                        businessType?: string | null;
                        businessId?: string | null;
                        deliveryChannels: components["schemas"]["DeliveryChannel"][];
                        targetType: components["schemas"]["NotificationTargetType"];
                        targetIds: string[];
                        needConfirm: boolean;
                        isMandatory: boolean;
                        isBanner: boolean;
                        isPopup: boolean;
                        /** Format: date-time */
                        startsAt?: string | null;
                        /** Format: date-time */
                        expiresAt?: string | null;
                        isPublished: boolean;
                        /** Format: date-time */
                        publishedAt?: string | null;
                        /** Format: uuid */
                        publisherId?: string | null;
                        /** Format: int32 */
                        recipientCount: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "notifications.publish": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: int32 */
                        recipientCount: number;
                        /** Format: date-time */
                        publishedAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "notifications.read-stats": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: int32 */
                        recipientCount: number;
                        /** Format: int32 */
                        readCount: number;
                        /** Format: int32 */
                        unreadCount: number;
                        needConfirm: boolean;
                        /** Format: int32 */
                        confirmedCount: number;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "notifications.recipients": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    page?: components["schemas"]["PageInfo"] | null;
                    filter?: components["schemas"]["NotificationRecipientFilter"];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["NotificationRecipientResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "notifications.remind": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: int32 */
                        recipientCount: number;
                        /** Format: date-time */
                        publishedAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "notifications.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["NotificationResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "numbering-rules.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    code: string;
                    name: string;
                    prefix?: string | null;
                    separator?: string | null;
                    dateFormat?: components["schemas"]["NumberingDateFormat"];
                    /** Format: int32 */
                    serialLength?: number;
                    resetCycle?: components["schemas"]["NumberingResetCycle"];
                    timeZoneId?: string;
                    allowTenantUse?: boolean;
                    /** Format: int32 */
                    order?: number;
                    description?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        ownerTenantId: string;
                        isPlatform: boolean;
                        code: string;
                        name: string;
                        prefix?: string | null;
                        separator: string;
                        dateFormat: components["schemas"]["NumberingDateFormat"];
                        /** Format: int32 */
                        serialLength: number;
                        resetCycle: components["schemas"]["NumberingResetCycle"];
                        timeZoneId: string;
                        template: string;
                        /** Format: int64 */
                        currentValue: number;
                        currentPeriod: string;
                        hasAllocated: boolean;
                        allowTenantUse: boolean;
                        status: components["schemas"]["SettingStatus"];
                        /** Format: int32 */
                        order: number;
                        description?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "numbering-rules.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "numbering-rules.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        ownerTenantId: string;
                        isPlatform: boolean;
                        code: string;
                        name: string;
                        prefix?: string | null;
                        separator: string;
                        dateFormat: components["schemas"]["NumberingDateFormat"];
                        /** Format: int32 */
                        serialLength: number;
                        resetCycle: components["schemas"]["NumberingResetCycle"];
                        timeZoneId: string;
                        template: string;
                        /** Format: int64 */
                        currentValue: number;
                        currentPeriod: string;
                        hasAllocated: boolean;
                        allowTenantUse: boolean;
                        status: components["schemas"]["SettingStatus"];
                        /** Format: int32 */
                        order: number;
                        description?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "numbering-rules.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    name: string;
                    prefix?: string | null;
                    separator?: string | null;
                    dateFormat?: components["schemas"]["NumberingDateFormat"];
                    /** Format: int32 */
                    serialLength?: number;
                    resetCycle?: components["schemas"]["NumberingResetCycle"];
                    timeZoneId?: string;
                    allowTenantUse?: boolean;
                    /** Format: int32 */
                    order?: number;
                    description?: string | null;
                    status?: components["schemas"]["SettingStatus"];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        ownerTenantId: string;
                        isPlatform: boolean;
                        code: string;
                        name: string;
                        prefix?: string | null;
                        separator: string;
                        dateFormat: components["schemas"]["NumberingDateFormat"];
                        /** Format: int32 */
                        serialLength: number;
                        resetCycle: components["schemas"]["NumberingResetCycle"];
                        timeZoneId: string;
                        template: string;
                        /** Format: int64 */
                        currentValue: number;
                        currentPeriod: string;
                        hasAllocated: boolean;
                        allowTenantUse: boolean;
                        status: components["schemas"]["SettingStatus"];
                        /** Format: int32 */
                        order: number;
                        description?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "numbering-rules.allocations": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    page?: components["schemas"]["PageInfo"] | null;
                    keyword?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["NumberingAllocationResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "numbering-rules.reset": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    nextValue: string;
                    reason: string;
                    confirmCode?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        ownerTenantId: string;
                        isPlatform: boolean;
                        code: string;
                        name: string;
                        prefix?: string | null;
                        separator: string;
                        dateFormat: components["schemas"]["NumberingDateFormat"];
                        /** Format: int32 */
                        serialLength: number;
                        resetCycle: components["schemas"]["NumberingResetCycle"];
                        timeZoneId: string;
                        template: string;
                        /** Format: int64 */
                        currentValue: number;
                        currentPeriod: string;
                        hasAllocated: boolean;
                        allowTenantUse: boolean;
                        status: components["schemas"]["SettingStatus"];
                        /** Format: int32 */
                        order: number;
                        description?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "numbering-rules.generate": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    code: string;
                    idempotencyKey: string;
                    /** Format: int32 */
                    count?: number;
                    scope?: components["schemas"]["NumberingScope"];
                    businessType?: string | null;
                    businessId?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        ruleId: string;
                        ruleCode: string;
                        scope: components["schemas"]["NumberingScope"];
                        requestTenantId: string;
                        idempotencyKey: string;
                        periodKey: string;
                        /** Format: int64 */
                        startValue: number;
                        /** Format: int64 */
                        endValue: number;
                        numbers: string[];
                        /** Format: date-time */
                        generatedAt: string;
                        isReplay: boolean;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "numbering-rules.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["NumberingRuleResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "numbering-rules.preview": {
        parameters: {
            query?: {
                prefix?: string | null;
                separator?: string | null;
                dateFormat?: components["schemas"]["NumberingDateFormat"];
                serialLength?: number;
                resetCycle?: components["schemas"]["NumberingResetCycle"];
                timeZoneId?: string;
                startValue?: string;
                count?: number;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        numbers: string[];
                        periodKey: string;
                        dateText?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "oauth-apps.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    appName: string;
                    appDescription?: string | null;
                    clientId?: string | null;
                    clientType: components["schemas"]["OAuthClientType"];
                    appType: components["schemas"]["OAuthAppType"];
                    grantTypes: string[];
                    redirectUris: string[];
                    scopes: string[];
                    /** Format: int32 */
                    accessTokenLifetime: number;
                    /** Format: int32 */
                    refreshTokenLifetime: number;
                    /** Format: int32 */
                    authorizationCodeLifetime: number;
                    logo?: string | null;
                    homepage?: string | null;
                    skipConsent: boolean;
                    isEnabled: boolean;
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        clientId: string;
                        clientType: components["schemas"]["OAuthClientType"];
                        clientSecret?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "oauth-apps.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "oauth-apps.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        appName: string;
                        appDescription?: string | null;
                        clientId: string;
                        clientType: components["schemas"]["OAuthClientType"];
                        appType: components["schemas"]["OAuthAppType"];
                        grantTypes: string[];
                        redirectUris: string[];
                        scopes: string[];
                        /** Format: int32 */
                        accessTokenLifetime: number;
                        /** Format: int32 */
                        refreshTokenLifetime: number;
                        /** Format: int32 */
                        authorizationCodeLifetime: number;
                        logo?: string | null;
                        homepage?: string | null;
                        skipConsent: boolean;
                        isEnabled: boolean;
                        /** Format: uuid */
                        ownerUserId?: string | null;
                        ownerUserName?: string | null;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "oauth-apps.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    appName: string;
                    appDescription?: string | null;
                    appType: components["schemas"]["OAuthAppType"];
                    grantTypes: string[];
                    redirectUris: string[];
                    scopes: string[];
                    /** Format: int32 */
                    accessTokenLifetime: number;
                    /** Format: int32 */
                    refreshTokenLifetime: number;
                    /** Format: int32 */
                    authorizationCodeLifetime: number;
                    logo?: string | null;
                    homepage?: string | null;
                    skipConsent: boolean;
                    isEnabled: boolean;
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        appName: string;
                        appDescription?: string | null;
                        clientId: string;
                        clientType: components["schemas"]["OAuthClientType"];
                        appType: components["schemas"]["OAuthAppType"];
                        grantTypes: string[];
                        redirectUris: string[];
                        scopes: string[];
                        /** Format: int32 */
                        accessTokenLifetime: number;
                        /** Format: int32 */
                        refreshTokenLifetime: number;
                        /** Format: int32 */
                        authorizationCodeLifetime: number;
                        logo?: string | null;
                        homepage?: string | null;
                        skipConsent: boolean;
                        isEnabled: boolean;
                        /** Format: uuid */
                        ownerUserId?: string | null;
                        ownerUserName?: string | null;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "oauth-apps.reset-secret": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        clientId: string;
                        clientType: components["schemas"]["OAuthClientType"];
                        clientSecret?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "oauth-apps.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["OAuthAppResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "oauth-consents.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["OAuthConsentResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "oauth-consents.approve": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    responseType?: string | null;
                    clientId?: string | null;
                    redirectUri?: string | null;
                    scope?: string | null;
                    state?: string | null;
                    codeChallenge?: string | null;
                    codeChallengeMethod?: string | null;
                    nonce?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        redirectUri: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "oauth-consents.deny": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    responseType?: string | null;
                    clientId?: string | null;
                    redirectUri?: string | null;
                    scope?: string | null;
                    state?: string | null;
                    codeChallenge?: string | null;
                    codeChallengeMethod?: string | null;
                    nonce?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        redirectUri: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "oauth-consents.resolve": {
        parameters: {
            query?: {
                responseType?: string | null;
                clientId?: string | null;
                redirectUri?: string | null;
                scope?: string | null;
                state?: string | null;
                codeChallenge?: string | null;
                codeChallengeMethod?: string | null;
                nonce?: string | null;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        valid: boolean;
                        error?: string | null;
                        errorDescription?: string | null;
                        clientId?: string | null;
                        appName?: string | null;
                        appDescription?: string | null;
                        logo?: string | null;
                        homepage?: string | null;
                        skipConsent: boolean;
                        scopes: string[];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "oauth-consents.revoke": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    clientId: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        revoked: boolean;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "open-api-logs.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: date-time */
                        occurredAt: string;
                        traceId: string;
                        /** Format: uuid */
                        userId?: string | null;
                        userName?: string | null;
                        /** Format: uuid */
                        sessionId?: string | null;
                        clientIp?: string | null;
                        userAgent?: string | null;
                        clientId?: string | null;
                        appName?: string | null;
                        operationId?: string | null;
                        method: string;
                        path: string;
                        signatureAlgorithm?: string | null;
                        signatureValid: boolean;
                        /** Format: int32 */
                        statusCode: number;
                        succeeded: boolean;
                        errorCode?: string | null;
                        errorMessage?: string | null;
                        /** Format: int64 */
                        durationMs: number;
                        /** Format: int64 */
                        requestSize: number;
                        /** Format: int64 */
                        responseSize: number;
                        requestBody?: string | null;
                        responseBody?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "open-api-logs.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["OpenApiLogResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "open.echo": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    message?: string | null;
                    /** Format: int32 */
                    number?: number | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        ok: boolean;
                        message: string;
                        appKey: string;
                        echo: components["schemas"]["OpenEchoRequest"];
                        /** Format: date-time */
                        serverTime: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "open.ping": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        ok: boolean;
                        message: string;
                        appKey: string;
                        /** Format: uuid */
                        ownerUserId: string;
                        /** Format: date-time */
                        serverTime: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "operation-logs.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: date-time */
                        occurredAt: string;
                        traceId: string;
                        /** Format: uuid */
                        userId?: string | null;
                        userName?: string | null;
                        /** Format: uuid */
                        sessionId?: string | null;
                        operationId: string;
                        succeeded: boolean;
                        errorCode?: string | null;
                        errorMessage?: string | null;
                        resource?: string | null;
                        /** Format: int64 */
                        durationMs?: number | null;
                        /** Format: int64 */
                        sequence: number;
                        details?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "operation-logs.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["OperationLogResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "permission-change-logs.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: date-time */
                        occurredAt: string;
                        traceId: string;
                        /** Format: uuid */
                        userId?: string | null;
                        userName?: string | null;
                        /** Format: uuid */
                        sessionId?: string | null;
                        clientIp?: string | null;
                        userAgent?: string | null;
                        changeType: components["schemas"]["PermissionChangeType"];
                        /** Format: uuid */
                        roleId?: string | null;
                        roleName?: string | null;
                        /** Format: uuid */
                        targetUserId?: string | null;
                        targetUserName?: string | null;
                        /** Format: uuid */
                        editionId?: string | null;
                        editionCode?: string | null;
                        targetTenantId?: string | null;
                        permissionCodes?: string | null;
                        detail?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "permission-change-logs.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["PermissionChangeLogResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "permission-conditions.list": {
        parameters: {
            query: {
                subjectKind: components["schemas"]["GrantSubjectKind"];
                subjectId: string;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        subjectKind: components["schemas"]["GrantSubjectKind"];
                        /** Format: uuid */
                        subjectId: string;
                        bindings: components["schemas"]["ConditionBindingResponse"][];
                        conditions: components["schemas"]["PermissionConditionResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "permission-conditions.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    subjectKind: components["schemas"]["GrantSubjectKind"];
                    /** Format: uuid */
                    subjectId: string;
                    permission: string;
                    action: components["schemas"]["PermissionAction"];
                    attributeName: string;
                    operator: components["schemas"]["ConditionOperator"];
                    /** Format: int32 */
                    groupNo?: number;
                    isNegated?: boolean;
                    value?: string | null;
                    description?: string | null;
                    status?: components["schemas"]["ConditionStatus"];
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        permission: string;
                        action: components["schemas"]["PermissionAction"];
                        /** Format: int32 */
                        groupNo: number;
                        attributeName: string;
                        operator: components["schemas"]["ConditionOperator"];
                        isNegated: boolean;
                        value?: string | null;
                        description?: string | null;
                        status: components["schemas"]["ConditionStatus"];
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "permission-conditions.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "permission-conditions.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    attributeName: string;
                    operator: components["schemas"]["ConditionOperator"];
                    /** Format: int32 */
                    groupNo?: number;
                    isNegated?: boolean;
                    value?: string | null;
                    description?: string | null;
                    status?: components["schemas"]["ConditionStatus"];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        permission: string;
                        action: components["schemas"]["PermissionAction"];
                        /** Format: int32 */
                        groupNo: number;
                        attributeName: string;
                        operator: components["schemas"]["ConditionOperator"];
                        isNegated: boolean;
                        value?: string | null;
                        description?: string | null;
                        status: components["schemas"]["ConditionStatus"];
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "permission-conditions.attributes": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["ConditionAttributeResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "permission-conditions.batch-trial": {
        parameters: {
            query: {
                subjectKind: components["schemas"]["GrantSubjectKind"];
                subjectId: string;
                permission: string;
                ip?: string | null;
                time?: string | null;
                userAgent?: string | null;
                path?: string | null;
                method?: string | null;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        permission: string;
                        /** Format: int32 */
                        totalUsers: number;
                        truncated: boolean;
                        items: components["schemas"]["ConditionBatchTrialItemResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "permission-conditions.inventory": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: date-time */
                        generatedAt: string;
                        /** Format: int32 */
                        conditionCount: number;
                        /** Format: int32 */
                        bindingCount: number;
                        items: components["schemas"]["ConditionInventoryItemResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "permission-conditions.trial": {
        parameters: {
            query: {
                userId: string;
                permission: string;
                ip?: string | null;
                time?: string | null;
                userAgent?: string | null;
                path?: string | null;
                method?: string | null;
                isImpersonating?: boolean;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        verdict: components["schemas"]["ConditionVerdict"];
                        /** Format: uuid */
                        userId: string;
                        userName: string;
                        permission: string;
                        ip?: string | null;
                        /** Format: date-time */
                        time: string;
                        timeZone: string;
                        denies: components["schemas"]["ConditionTrialSetResponse"][];
                        paths: components["schemas"]["ConditionTrialPathResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "permissions.list": {
        parameters: {
            query?: {
                side?: string | null;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["PermissionResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "policies.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    code: string;
                    name: string;
                    conditions: components["schemas"]["AccessPolicyConditionRequest"][];
                    description?: string | null;
                    /** Format: int32 */
                    priority?: number;
                    /** Format: date-time */
                    effectiveAt?: string | null;
                    /** Format: date-time */
                    expiresAt?: string | null;
                    templateCode?: string | null;
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        description?: string | null;
                        action: components["schemas"]["AccessPolicyAction"];
                        /** Format: int32 */
                        priority: number;
                        status: components["schemas"]["AccessPolicyStatus"];
                        /** Format: date-time */
                        effectiveAt?: string | null;
                        /** Format: date-time */
                        expiresAt?: string | null;
                        isEffective: boolean;
                        phase: components["schemas"]["ConditionHitPhase"];
                        templateCode?: string | null;
                        remark?: string | null;
                        conditions: components["schemas"]["AccessPolicyConditionResponse"][];
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "policies.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "policies.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        description?: string | null;
                        action: components["schemas"]["AccessPolicyAction"];
                        /** Format: int32 */
                        priority: number;
                        status: components["schemas"]["AccessPolicyStatus"];
                        /** Format: date-time */
                        effectiveAt?: string | null;
                        /** Format: date-time */
                        expiresAt?: string | null;
                        isEffective: boolean;
                        phase: components["schemas"]["ConditionHitPhase"];
                        templateCode?: string | null;
                        remark?: string | null;
                        conditions: components["schemas"]["AccessPolicyConditionResponse"][];
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "policies.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    code: string;
                    name: string;
                    conditions: components["schemas"]["AccessPolicyConditionRequest"][];
                    description?: string | null;
                    /** Format: int32 */
                    priority?: number;
                    /** Format: date-time */
                    effectiveAt?: string | null;
                    /** Format: date-time */
                    expiresAt?: string | null;
                    templateCode?: string | null;
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        description?: string | null;
                        action: components["schemas"]["AccessPolicyAction"];
                        /** Format: int32 */
                        priority: number;
                        status: components["schemas"]["AccessPolicyStatus"];
                        /** Format: date-time */
                        effectiveAt?: string | null;
                        /** Format: date-time */
                        expiresAt?: string | null;
                        isEffective: boolean;
                        phase: components["schemas"]["ConditionHitPhase"];
                        templateCode?: string | null;
                        remark?: string | null;
                        conditions: components["schemas"]["AccessPolicyConditionResponse"][];
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "policies.set-action": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    action: components["schemas"]["AccessPolicyAction"];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        description?: string | null;
                        action: components["schemas"]["AccessPolicyAction"];
                        /** Format: int32 */
                        priority: number;
                        status: components["schemas"]["AccessPolicyStatus"];
                        /** Format: date-time */
                        effectiveAt?: string | null;
                        /** Format: date-time */
                        expiresAt?: string | null;
                        isEffective: boolean;
                        phase: components["schemas"]["ConditionHitPhase"];
                        templateCode?: string | null;
                        remark?: string | null;
                        conditions: components["schemas"]["AccessPolicyConditionResponse"][];
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "policies.set-status": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    status: components["schemas"]["AccessPolicyStatus"];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        description?: string | null;
                        action: components["schemas"]["AccessPolicyAction"];
                        /** Format: int32 */
                        priority: number;
                        status: components["schemas"]["AccessPolicyStatus"];
                        /** Format: date-time */
                        effectiveAt?: string | null;
                        /** Format: date-time */
                        expiresAt?: string | null;
                        isEffective: boolean;
                        phase: components["schemas"]["ConditionHitPhase"];
                        templateCode?: string | null;
                        remark?: string | null;
                        conditions: components["schemas"]["AccessPolicyConditionResponse"][];
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "policies.attributes": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["ConditionAttributeResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "policies.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["AccessPolicyResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "policies.templates": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["AccessPolicyTemplateResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "policies.trial": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    policyId?: string | null;
                    action?: components["schemas"]["AccessPolicyAction"] | null;
                    conditions?: components["schemas"]["AccessPolicyConditionRequest"][] | null;
                    ip?: string | null;
                    /** Format: date-time */
                    time?: string | null;
                    userAgent?: string | null;
                    path?: string | null;
                    method?: string | null;
                    /** Format: uuid */
                    userId?: string | null;
                    permissionCodes?: string[] | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        outcome?: components["schemas"]["ConditionOutcome"] | null;
                        action: components["schemas"]["AccessPolicyAction"];
                        wouldBlock: boolean;
                        phase: components["schemas"]["ConditionHitPhase"];
                        errors: string[];
                        ip?: string | null;
                        location?: string | null;
                        /** Format: date-time */
                        time: string;
                        timeZone: string;
                        userAgent?: string | null;
                        /** Format: uuid */
                        userId?: string | null;
                        userName?: string | null;
                        groups: components["schemas"]["ConditionTrialGroupResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "positions.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    code: string;
                    name: string;
                    /** Format: int32 */
                    order?: number;
                    description?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        status: components["schemas"]["OrganizationStatus"];
                        /** Format: int32 */
                        order: number;
                        description?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "positions.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "positions.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        status: components["schemas"]["OrganizationStatus"];
                        /** Format: int32 */
                        order: number;
                        description?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "positions.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    name: string;
                    /** Format: int32 */
                    order?: number;
                    description?: string | null;
                    status?: components["schemas"]["OrganizationStatus"];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        status: components["schemas"]["OrganizationStatus"];
                        /** Format: int32 */
                        order: number;
                        description?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "positions.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["PositionResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "profile.get": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        user: components["schemas"]["UserResponse"];
                        twoFactorEnabled: boolean;
                        twoFactorMethods: components["schemas"]["TwoFactorMethod"][];
                        /** Format: date-time */
                        passwordChangedAt?: string | null;
                        mustChangePassword: boolean;
                        /** Format: int32 */
                        failedAccessCount: number;
                        /** Format: date-time */
                        lockoutEndAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "profile.begin-external-bind": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    provider: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        url: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "profile.change-password": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    currentPassword: string;
                    newPassword: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        userId: string;
                        /** Format: date-time */
                        changedAt: string;
                        sessionsRevoked: boolean;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "profile.disable-two-factor": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    method: components["schemas"]["TwoFactorMethod"];
                    code: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        user: components["schemas"]["UserResponse"];
                        twoFactorEnabled: boolean;
                        twoFactorMethods: components["schemas"]["TwoFactorMethod"][];
                        /** Format: date-time */
                        passwordChangedAt?: string | null;
                        mustChangePassword: boolean;
                        /** Format: int32 */
                        failedAccessCount: number;
                        /** Format: date-time */
                        lockoutEndAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "profile.enable-two-factor": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    method: components["schemas"]["TwoFactorMethod"];
                    code: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        user: components["schemas"]["UserResponse"];
                        twoFactorEnabled: boolean;
                        twoFactorMethods: components["schemas"]["TwoFactorMethod"][];
                        /** Format: date-time */
                        passwordChangedAt?: string | null;
                        mustChangePassword: boolean;
                        /** Format: int32 */
                        failedAccessCount: number;
                        /** Format: date-time */
                        lockoutEndAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "profile.external-logins": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["ExternalLoginResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "profile.link-external": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    ticket: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        provider: string;
                        displayName?: string | null;
                        email?: string | null;
                        avatarUrl?: string | null;
                        /** Format: date-time */
                        boundAt: string;
                        /** Format: date-time */
                        lastLoginAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "profile.notification-preference": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        channelInApp: boolean;
                        channelEmail: boolean;
                        channelSms: boolean;
                        typeAnnouncement: boolean;
                        typeTask: boolean;
                        typeSecurity: boolean;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "profile.revoke-other-sessions": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": Record<string, never>;
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["MySessionResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "profile.revoke-session": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    sessionId: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["MySessionResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "profile.sessions": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["MySessionResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "profile.setup-totp": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": Record<string, never>;
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        sharedKey: string;
                        authenticatorUri: string;
                        /** Format: date-time */
                        expiresAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "profile.unlink-external": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    provider: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["ExternalLoginResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "profile.update-notification-preference": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    channelInApp: boolean;
                    channelEmail: boolean;
                    channelSms: boolean;
                    typeAnnouncement: boolean;
                    typeTask: boolean;
                    typeSecurity: boolean;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        channelInApp: boolean;
                        channelEmail: boolean;
                        channelSms: boolean;
                        typeAnnouncement: boolean;
                        typeTask: boolean;
                        typeSecurity: boolean;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    code: string;
                    name: string;
                    description?: string | null;
                    /** Format: int32 */
                    order?: number;
                    dataScope?: components["schemas"]["DataScope"] | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        ownerTenantId: string;
                        code: string;
                        name: string;
                        description?: string | null;
                        /** Format: int32 */
                        order: number;
                        isBuiltIn: boolean;
                        status: components["schemas"]["RoleStatus"];
                        dataScope: components["schemas"]["DataScope"];
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        role: components["schemas"]["RoleResponse"];
                        permissions: string[];
                        /** Format: int64 */
                        memberCount: number;
                        customDepartments: components["schemas"]["DataScopeDepartmentRequest"][];
                        rules: components["schemas"]["PermissionRuleResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    name: string;
                    description?: string | null;
                    /** Format: int32 */
                    order?: number;
                    status?: components["schemas"]["RoleStatus"];
                    dataScope?: components["schemas"]["DataScope"] | null;
                    customDepartments?: components["schemas"]["DataScopeDepartmentRequest"][] | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        ownerTenantId: string;
                        code: string;
                        name: string;
                        description?: string | null;
                        /** Format: int32 */
                        order: number;
                        isBuiltIn: boolean;
                        status: components["schemas"]["RoleStatus"];
                        dataScope: components["schemas"]["DataScope"];
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.assign": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    userIds: string[];
                    /** Format: date-time */
                    effectiveAt?: string | null;
                    /** Format: date-time */
                    expiresAt?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.deny": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    permissions: string[];
                    /** Format: date-time */
                    effectiveAt?: string | null;
                    /** Format: date-time */
                    expiresAt?: string | null;
                    reason?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["PermissionFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.grant": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    permissions: string[];
                    /** Format: date-time */
                    effectiveAt?: string | null;
                    /** Format: date-time */
                    expiresAt?: string | null;
                    reason?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["PermissionFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.hierarchy": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        roleId: string;
                        ancestors: components["schemas"]["RoleRelativeResponse"][];
                        descendants: components["schemas"]["RoleRelativeResponse"][];
                        inherited: components["schemas"]["InheritedPermissionResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.members": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["RoleMemberResponse"][];
                        /** Format: int64 */
                        total: number;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.revoke": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    permissions: string[];
                    /** Format: date-time */
                    effectiveAt?: string | null;
                    /** Format: date-time */
                    expiresAt?: string | null;
                    reason?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["PermissionFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.set-parents": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    parentIds: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        roleId: string;
                        ancestors: components["schemas"]["RoleRelativeResponse"][];
                        descendants: components["schemas"]["RoleRelativeResponse"][];
                        inherited: components["schemas"]["InheritedPermissionResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.unassign": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    userIds: string[];
                    /** Format: date-time */
                    effectiveAt?: string | null;
                    /** Format: date-time */
                    expiresAt?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.update-member": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    /** Format: uuid */
                    userId: string;
                    /** Format: date-time */
                    effectiveAt?: string | null;
                    /** Format: date-time */
                    expiresAt?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        userId: string;
                        userName: string;
                        displayName: string;
                        status: components["schemas"]["UserStatus"];
                        /** Format: date-time */
                        assignedAt: string;
                        /** Format: date-time */
                        effectiveAt?: string | null;
                        /** Format: date-time */
                        expiresAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.by-user": {
        parameters: {
            query: {
                userId: string;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["RoleResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["RoleResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "session-roles.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: uuid */
                        sessionId: string;
                        /** Format: uuid */
                        userId: string;
                        userName?: string | null;
                        /** Format: uuid */
                        roleId: string;
                        roleName?: string | null;
                        roleCode?: string | null;
                        status: components["schemas"]["SessionRoleStatus"];
                        /** Format: date-time */
                        activatedAt: string;
                        /** Format: date-time */
                        deactivatedAt?: string | null;
                        reason?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "session-roles.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["SessionRoleResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "sessions.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["OnlineSessionResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "sessions.revoke": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    ids: string[];
                    reason?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "sessions.summary": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: int32 */
                        activeSessions: number;
                        /** Format: int32 */
                        activeUsers: number;
                        /** Format: int32 */
                        realtimeOnlineUsers: number;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "sms.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "sms-configs.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    code: string;
                    name: string;
                    provider: components["schemas"]["SmsProvider"];
                    accessKeyId: string;
                    signName: string;
                    accessKeySecret?: string | null;
                    sdkAppId?: string | null;
                    region?: string | null;
                    templateMap?: string | null;
                    isEnabled?: boolean;
                    /** Format: int32 */
                    order?: number;
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        provider: components["schemas"]["SmsProvider"];
                        accessKeyId: string;
                        hasAccessKeySecret: boolean;
                        sdkAppId?: string | null;
                        signName: string;
                        region?: string | null;
                        templateMap?: string | null;
                        isDefault: boolean;
                        isEnabled: boolean;
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "sms-configs.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "sms-configs.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        provider: components["schemas"]["SmsProvider"];
                        accessKeyId: string;
                        hasAccessKeySecret: boolean;
                        sdkAppId?: string | null;
                        signName: string;
                        region?: string | null;
                        templateMap?: string | null;
                        isDefault: boolean;
                        isEnabled: boolean;
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "sms-configs.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    name: string;
                    provider: components["schemas"]["SmsProvider"];
                    accessKeyId: string;
                    signName: string;
                    accessKeySecret?: string | null;
                    sdkAppId?: string | null;
                    region?: string | null;
                    templateMap?: string | null;
                    isEnabled?: boolean;
                    /** Format: int32 */
                    order?: number;
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        provider: components["schemas"]["SmsProvider"];
                        accessKeyId: string;
                        hasAccessKeySecret: boolean;
                        sdkAppId?: string | null;
                        signName: string;
                        region?: string | null;
                        templateMap?: string | null;
                        isDefault: boolean;
                        isEnabled: boolean;
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "sms-configs.set-default": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        provider: components["schemas"]["SmsProvider"];
                        accessKeyId: string;
                        hasAccessKeySecret: boolean;
                        sdkAppId?: string | null;
                        signName: string;
                        region?: string | null;
                        templateMap?: string | null;
                        isDefault: boolean;
                        isEnabled: boolean;
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "sms-configs.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["SmsConfigResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "sms.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        toPhone: string;
                        content: string;
                        templateCode?: string | null;
                        templateParams?: string | null;
                        provider?: components["schemas"]["SmsProvider"] | null;
                        providerMessageId?: string | null;
                        status: components["schemas"]["MessageSendStatus"];
                        /** Format: int32 */
                        attempts: number;
                        /** Format: int32 */
                        maxAttempts: number;
                        /** Format: date-time */
                        nextAttemptAt?: string | null;
                        /** Format: date-time */
                        sentAt?: string | null;
                        errorMessage?: string | null;
                        /** Format: uuid */
                        configId?: string | null;
                        /** Format: uuid */
                        senderId?: string | null;
                        /** Format: uuid */
                        receiverId?: string | null;
                        businessType?: string | null;
                        businessId?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "sms.cancel": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    ids: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "sms.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["SmsRecordResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "sms.resend": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    ids: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "storage-configs.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    code: string;
                    name: string;
                    type: components["schemas"]["StorageType"];
                    bucket: string;
                    endpoint?: string | null;
                    region?: string | null;
                    accessKeyId?: string | null;
                    secretAccessKey?: string | null;
                    isDefault?: boolean;
                    status?: components["schemas"]["StorageConfigStatus"];
                    /** Format: int32 */
                    order?: number;
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        type: components["schemas"]["StorageType"];
                        endpoint?: string | null;
                        region?: string | null;
                        bucket: string;
                        accessKeyId?: string | null;
                        hasSecretAccessKey: boolean;
                        isDefault: boolean;
                        status: components["schemas"]["StorageConfigStatus"];
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "storage-configs.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "storage-configs.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        type: components["schemas"]["StorageType"];
                        endpoint?: string | null;
                        region?: string | null;
                        bucket: string;
                        accessKeyId?: string | null;
                        hasSecretAccessKey: boolean;
                        isDefault: boolean;
                        status: components["schemas"]["StorageConfigStatus"];
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "storage-configs.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    name: string;
                    type: components["schemas"]["StorageType"];
                    bucket: string;
                    endpoint?: string | null;
                    region?: string | null;
                    accessKeyId?: string | null;
                    secretAccessKey?: string | null;
                    /** Format: int32 */
                    order?: number;
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        type: components["schemas"]["StorageType"];
                        endpoint?: string | null;
                        region?: string | null;
                        bucket: string;
                        accessKeyId?: string | null;
                        hasSecretAccessKey: boolean;
                        isDefault: boolean;
                        status: components["schemas"]["StorageConfigStatus"];
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "storage-configs.set-default": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        type: components["schemas"]["StorageType"];
                        endpoint?: string | null;
                        region?: string | null;
                        bucket: string;
                        accessKeyId?: string | null;
                        hasSecretAccessKey: boolean;
                        isDefault: boolean;
                        status: components["schemas"]["StorageConfigStatus"];
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "storage-configs.set-status": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    status: components["schemas"]["StorageConfigStatus"];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        type: components["schemas"]["StorageType"];
                        endpoint?: string | null;
                        region?: string | null;
                        bucket: string;
                        accessKeyId?: string | null;
                        hasSecretAccessKey: boolean;
                        isDefault: boolean;
                        status: components["schemas"]["StorageConfigStatus"];
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "storage-configs.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["StorageConfigResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "subscription.get": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        tenantId: string;
                        tenantName: string;
                        status: components["schemas"]["TenantStatus"];
                        /** Format: date-time */
                        expiresAt?: string | null;
                        isExpired: boolean;
                        editionCode?: string | null;
                        editionName?: string | null;
                        editionDescription?: string | null;
                        isFree: boolean;
                        /** Format: decimal */
                        price?: number | null;
                        /** Format: int32 */
                        billingPeriodMonths?: number | null;
                        /** Format: int32 */
                        userLimit?: number | null;
                        /** Format: int64 */
                        usedSeats: number;
                        /** Format: int64 */
                        storageLimitMb?: number | null;
                        /** Format: int64 */
                        usedStorageBytes: number;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "telegram-bots.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    name: string;
                    token: string;
                    adminUsers?: string | null;
                    allowedGroupChatIds?: string | null;
                    allowedCommands?: string | null;
                    enableFallbackReply?: boolean;
                    broadcastChatIds?: string | null;
                    isEnabled?: boolean;
                    /** Format: int32 */
                    order?: number;
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        name: string;
                        hasToken: boolean;
                        adminUsers?: string | null;
                        allowedGroupChatIds?: string | null;
                        allowedCommands?: string | null;
                        enableFallbackReply: boolean;
                        broadcastChatIds?: string | null;
                        isEnabled: boolean;
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "telegram-bots.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "telegram-bots.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        name: string;
                        hasToken: boolean;
                        adminUsers?: string | null;
                        allowedGroupChatIds?: string | null;
                        allowedCommands?: string | null;
                        enableFallbackReply: boolean;
                        broadcastChatIds?: string | null;
                        isEnabled: boolean;
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "telegram-bots.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    name: string;
                    token?: string | null;
                    adminUsers?: string | null;
                    allowedGroupChatIds?: string | null;
                    allowedCommands?: string | null;
                    enableFallbackReply?: boolean;
                    broadcastChatIds?: string | null;
                    isEnabled?: boolean;
                    /** Format: int32 */
                    order?: number;
                    remark?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        name: string;
                        hasToken: boolean;
                        adminUsers?: string | null;
                        allowedGroupChatIds?: string | null;
                        allowedCommands?: string | null;
                        enableFallbackReply: boolean;
                        broadcastChatIds?: string | null;
                        isEnabled: boolean;
                        /** Format: int32 */
                        order: number;
                        remark?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "telegram-bots.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["TelegramBotResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenant-members.list": {
        parameters: {
            query: {
                tenantId: string;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["TenantMemberResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenant-members.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    tenantId: string;
                    /** Format: uuid */
                    userId: string;
                    memberType?: components["schemas"]["TenantMemberType"];
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        tenantId: string;
                        /** Format: uuid */
                        userId: string;
                        userName: string;
                        displayName: string;
                        homeTenantId: string;
                        memberType: components["schemas"]["TenantMemberType"];
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenant-members.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenants.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    tenantId: string;
                    name: string;
                    description?: string | null;
                    /** Format: date-time */
                    expiresAt?: string | null;
                    /** Format: int32 */
                    order?: number;
                    /** Format: uuid */
                    editionId?: string | null;
                    /** Format: int32 */
                    userLimit?: number | null;
                    /** Format: int64 */
                    storageLimitMb?: number | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        tenantId: string;
                        name: string;
                        description?: string | null;
                        status: components["schemas"]["TenantStatus"];
                        /** Format: date-time */
                        expiresAt?: string | null;
                        /** Format: uuid */
                        adminUserId?: string | null;
                        /** Format: uuid */
                        editionId?: string | null;
                        /** Format: int32 */
                        order: number;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: int32 */
                        userLimit?: number | null;
                        /** Format: int64 */
                        storageLimitMb?: number | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenants.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenants.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        tenant: components["schemas"]["TenantResponse"];
                        adminUserName?: string | null;
                        /** Format: int64 */
                        userCount: number;
                        quota: components["schemas"]["TenantQuotaResponse"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenants.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    name: string;
                    description?: string | null;
                    /** Format: date-time */
                    expiresAt?: string | null;
                    /** Format: int32 */
                    order?: number;
                    /** Format: int32 */
                    userLimit?: number | null;
                    /** Format: int64 */
                    storageLimitMb?: number | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        tenantId: string;
                        name: string;
                        description?: string | null;
                        status: components["schemas"]["TenantStatus"];
                        /** Format: date-time */
                        expiresAt?: string | null;
                        /** Format: uuid */
                        adminUserId?: string | null;
                        /** Format: uuid */
                        editionId?: string | null;
                        /** Format: int32 */
                        order: number;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: int32 */
                        userLimit?: number | null;
                        /** Format: int64 */
                        storageLimitMb?: number | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenants.change-edition": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    /** Format: uuid */
                    editionId?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        tenantId: string;
                        name: string;
                        description?: string | null;
                        status: components["schemas"]["TenantStatus"];
                        /** Format: date-time */
                        expiresAt?: string | null;
                        /** Format: uuid */
                        adminUserId?: string | null;
                        /** Format: uuid */
                        editionId?: string | null;
                        /** Format: int32 */
                        order: number;
                        /** Format: date-time */
                        createdAt: string;
                        /** Format: int32 */
                        userLimit?: number | null;
                        /** Format: int64 */
                        storageLimitMb?: number | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenants.initialize-admin": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    userName: string;
                    password: string;
                    displayName?: string | null;
                    email?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        userName: string;
                        displayName: string;
                        email?: string | null;
                        phoneNumber?: string | null;
                        homeTenantId: string;
                        status: components["schemas"]["UserStatus"];
                        /** Format: date-time */
                        lastLoginAt?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        createdBy?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenants.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["TenantResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenants.mine": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        canAccessPlatform: boolean;
                        platformIsCurrent: boolean;
                        tenants: components["schemas"]["AvailableTenantResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenants.quotas": {
        parameters: {
            query?: {
                overOnly?: boolean;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["TenantQuotaResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenants.resume": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    ids: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenants.suspend": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    ids: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "traces.timeline": {
        parameters: {
            query: {
                dimension?: components["schemas"]["TraceDimension"];
                value: string;
                from?: string | null;
                to?: string | null;
                logTypes?: components["schemas"]["TraceLogType"][] | null;
                maxPerType?: number;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["TraceTimelineItem"][];
                        counts: components["schemas"]["TraceTypeCount"][];
                        truncated: boolean;
                        /** Format: int32 */
                        total: number;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "user-data-scopes.get": {
        parameters: {
            query: {
                userId: string;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        userId: string;
                        dataScope?: components["schemas"]["DataScope"] | null;
                        departments: components["schemas"]["DataScopeDepartmentRequest"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "user-data-scopes.set": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    userId: string;
                    dataScope?: components["schemas"]["DataScope"] | null;
                    departments?: components["schemas"]["DataScopeDepartmentRequest"][] | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        userId: string;
                        dataScope?: components["schemas"]["DataScope"] | null;
                        departments: components["schemas"]["DataScopeDepartmentRequest"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "user-grants.list": {
        parameters: {
            query: {
                userId: string;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        userId: string;
                        grants: components["schemas"]["UserGrantResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "user-grants.deny": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    userId: string;
                    permissions: string[];
                    /** Format: date-time */
                    effectiveAt?: string | null;
                    /** Format: date-time */
                    expiresAt?: string | null;
                    reason?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["PermissionFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "user-grants.grant": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    userId: string;
                    permissions: string[];
                    /** Format: date-time */
                    effectiveAt?: string | null;
                    /** Format: date-time */
                    expiresAt?: string | null;
                    reason?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["PermissionFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "user-grants.revoke": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    userId: string;
                    permissions: string[];
                    /** Format: date-time */
                    effectiveAt?: string | null;
                    /** Format: date-time */
                    expiresAt?: string | null;
                    reason?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["PermissionFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "user-settings.get": {
        parameters: {
            query: {
                scene: components["schemas"]["UserSettingScene"];
                key: string;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        scene: components["schemas"]["UserSettingScene"];
                        key: string;
                        value?: string | null;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "user-settings.update": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    scene: components["schemas"]["UserSettingScene"];
                    key: string;
                    value?: string | null;
                    clientId?: string | null;
                    origin?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        scene: components["schemas"]["UserSettingScene"];
                        key: string;
                        value?: string | null;
                        /** Format: date-time */
                        modifiedAt?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "users.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    userName: string;
                    displayName: string;
                    email?: string | null;
                    phoneNumber?: string | null;
                    password?: string | null;
                    homeTenantId?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        userName: string;
                        displayName: string;
                        email?: string | null;
                        phoneNumber?: string | null;
                        homeTenantId: string;
                        status: components["schemas"]["UserStatus"];
                        /** Format: date-time */
                        lastLoginAt?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        createdBy?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "users.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "users.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        userName: string;
                        displayName: string;
                        email?: string | null;
                        phoneNumber?: string | null;
                        homeTenantId: string;
                        status: components["schemas"]["UserStatus"];
                        /** Format: date-time */
                        lastLoginAt?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        createdBy?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "users.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    displayName: string;
                    email?: string | null;
                    phoneNumber?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        userName: string;
                        displayName: string;
                        email?: string | null;
                        phoneNumber?: string | null;
                        homeTenantId: string;
                        status: components["schemas"]["UserStatus"];
                        /** Format: date-time */
                        lastLoginAt?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        createdBy?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "users.reset-password": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    newPassword: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        userId: string;
                        /** Format: date-time */
                        changedAt: string;
                        sessionsRevoked: boolean;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "users.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["UserResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "users.lock": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    ids: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "users.reset-two-factor": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    ids: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "users.unlock": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    ids: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
}
