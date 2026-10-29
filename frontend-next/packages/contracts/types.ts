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
        AvailableTenantResponse: {
            tenantId: string;
            name: string;
            isHome: boolean;
            memberType?: components["schemas"]["TenantMemberType"] | null;
            isCurrent: boolean;
        };
        BatchFailure: {
            /** Format: uuid */
            id: string;
            code: string;
        };
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
        GrantSubjectKind: "Role" | "User";
        ImpersonationCandidateResponse: {
            /** Format: uuid */
            id: string;
            userName: string;
            displayName: string;
            homeTenantId: string;
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
        LoginEvent: "Login" | "Refresh" | "Logout" | "SwitchTenant" | "ImpersonationStart" | "ImpersonationStop" | "ChangePassword" | "TwoFactorChallenge" | "TwoFactorEnable" | "TwoFactorDisable" | "TwoFactorReset";
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
        PermissionChangeType: "RoleGrant" | "RoleRevoke" | "RoleAssign" | "RoleUnassign" | "RoleDataScope" | "RoleDelete" | "EditionPermissions" | "TenantEdition" | "RoleDeny" | "RoleAssignmentWindow" | "UserGrant" | "UserDeny" | "UserRevoke" | "UserDataScope" | "FieldSecurity" | "RoleInheritance" | "Delegation" | "ConditionChange";
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
        /** @enum {string} */
        SettingStatus: "Active" | "Disabled";
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
                    };
                };
            };
            /** @description Error */
            default: {
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
                    };
                };
            };
            /** @description Error */
            default: {
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
                    };
                };
            };
            /** @description Error */
            default: {
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
                    };
                };
            };
            /** @description Error */
            default: {
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
