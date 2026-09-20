import type { components } from "@/shared/lib/api-types.generated";
import { apiFetch } from "@/shared/lib/api-client";

export type AdminSummary = components["schemas"]["UserListItemResponse"];
export type AdminDetails = components["schemas"]["UserDetailsResponse"];
export type RoleSummary = components["schemas"]["RoleSummaryResponse"];
export type RoleDetails = components["schemas"]["RoleResponse"];
export type PermissionDetails = components["schemas"]["PermissionResponse"];
export type PermissionEffect = "Grant" | "Deny" | null;

export const adminQueryKeys = {
  all: ["admins"] as const,
  list: () => [...adminQueryKeys.all, "list"] as const,
  detail: (id: string) => [...adminQueryKeys.all, "detail", id] as const,
  roleSummaries: () => [...adminQueryKeys.all, "role-summaries"] as const,
  roles: () => [...adminQueryKeys.all, "roles"] as const,
  permissions: () => [...adminQueryKeys.all, "permissions"] as const,
};

export function listAdmins() {
  return apiFetch<AdminSummary[]>("/api/v1/identity/users");
}

export function getAdmin(id: string) {
  return apiFetch<AdminDetails>(`/api/v1/identity/users/${encodeURIComponent(id)}`);
}

export function createAdmin(input: { username: string; initialPassword: string }) {
  return apiFetch<components["schemas"]["CreateUserResult"]>("/api/v1/identity/users", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function resetAdminPassword(id: string, newPassword: string) {
  return apiFetch<void>(`/api/v1/identity/users/${encodeURIComponent(id)}/reset-password`, {
    method: "POST",
    body: JSON.stringify({ newPassword }),
  });
}

export function toggleAdminActive(id: string) {
  return apiFetch<components["schemas"]["ToggleUserActiveResult"]>(
    `/api/v1/identity/users/${encodeURIComponent(id)}/toggle-active`,
    { method: "POST" },
  );
}

export function listRoleSummaries() {
  return apiFetch<RoleSummary[]>("/api/v1/identity/roles/summary");
}

export function listRoles() {
  return apiFetch<RoleDetails[]>("/api/v1/identity/roles");
}

export function setAdminRoles(id: string, roleIds: string[]) {
  return apiFetch<void>(`/api/v1/identity/users/${encodeURIComponent(id)}/roles`, {
    method: "PUT",
    body: JSON.stringify({ roleIds }),
  });
}

export function listPermissions() {
  return apiFetch<PermissionDetails[]>("/api/v1/identity/permissions");
}

export function setAdminPermission(id: string, permissionKey: string, effect: PermissionEffect) {
  return apiFetch<void>(
    `/api/v1/identity/users/${encodeURIComponent(id)}/permissions/${encodeURIComponent(permissionKey)}`,
    { method: "PUT", body: JSON.stringify({ effect }) },
  );
}

export function setRolePermissions(roleId: string, permissionKeys: string[]) {
  return apiFetch<void>(`/api/v1/identity/roles/${encodeURIComponent(roleId)}/permissions`, {
    method: "PUT",
    body: JSON.stringify({ permissionKeys }),
  });
}
