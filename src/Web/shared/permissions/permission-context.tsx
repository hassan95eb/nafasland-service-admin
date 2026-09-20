"use client";

import { createContext, useContext, type ReactNode } from "react";

const PermissionContext = createContext<ReadonlySet<string>>(new Set());

export function PermissionProvider({ permissions, children }: { permissions: string[]; children: ReactNode }) {
  return <PermissionContext value={new Set(permissions)}>{children}</PermissionContext>;
}

export function Can({ permission, children }: { permission: string; children: ReactNode }) {
  const permissions = useContext(PermissionContext);
  return permissions.has(permission) ? children : null;
}
