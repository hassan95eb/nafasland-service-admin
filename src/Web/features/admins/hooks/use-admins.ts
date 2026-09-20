"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import {
  adminQueryKeys,
  createAdmin,
  getAdmin,
  listAdmins,
  listPermissions,
  listRoles,
  listRoleSummaries,
  resetAdminPassword,
  setAdminPermission,
  setAdminRoles,
  setRolePermissions,
  toggleAdminActive,
  type PermissionEffect,
} from "@/features/admins/api/admins";

export function useAdmins() {
  return useQuery({ queryKey: adminQueryKeys.list(), queryFn: listAdmins });
}

export function useAdmin(id: string) {
  return useQuery({ queryKey: adminQueryKeys.detail(id), queryFn: () => getAdmin(id) });
}

export function useRoleSummaries() {
  return useQuery({ queryKey: adminQueryKeys.roleSummaries(), queryFn: listRoleSummaries });
}

export function useRoles(enabled = true) {
  return useQuery({ queryKey: adminQueryKeys.roles(), queryFn: listRoles, enabled });
}

export function usePermissions(enabled = true) {
  return useQuery({ queryKey: adminQueryKeys.permissions(), queryFn: listPermissions, enabled });
}

export function useCreateAdmin() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createAdmin,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: adminQueryKeys.list() }),
  });
}

export function useToggleAdminActive() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: toggleAdminActive,
    onSuccess: (_, id) => {
      void queryClient.invalidateQueries({ queryKey: adminQueryKeys.list() });
      void queryClient.invalidateQueries({ queryKey: adminQueryKeys.detail(id) });
    },
  });
}

export function useResetAdminPassword() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, password }: { id: string; password: string }) => resetAdminPassword(id, password),
    onSuccess: (_, { id }) => {
      void queryClient.invalidateQueries({ queryKey: adminQueryKeys.list() });
      void queryClient.invalidateQueries({ queryKey: adminQueryKeys.detail(id) });
    },
  });
}

export function useSetAdminRoles(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (roleIds: string[]) => setAdminRoles(id, roleIds),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: adminQueryKeys.list() });
      void queryClient.invalidateQueries({ queryKey: adminQueryKeys.detail(id) });
    },
  });
}

export function useSetAdminPermission(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ key, effect }: { key: string; effect: PermissionEffect }) => setAdminPermission(id, key, effect),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: adminQueryKeys.detail(id) }),
  });
}

export function useSetRolePermissions() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ roleId, permissionKeys }: { roleId: string; permissionKeys: string[] }) =>
      setRolePermissions(roleId, permissionKeys),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: adminQueryKeys.roles() }),
  });
}
