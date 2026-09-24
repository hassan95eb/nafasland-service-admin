import Link from "next/link";

import { actorDisplayName, type ActorLike } from "@/features/audit/lib/audit-labels";

/** The actor's name, linking to that admin's activity page — except for a system record, which has no one to link to. */
export function ActorLink({ actor }: { actor: ActorLike }) {
  const name = actorDisplayName(actor);
  if (!actor.actorUserId) {
    return <span className="text-[var(--muted)]">{name}</span>;
  }

  return (
    <Link
      className="font-bold text-[var(--primary)] hover:underline"
      href={`/admin/audit/users/${encodeURIComponent(actor.actorUserId)}`}
      onClick={(event) => event.stopPropagation()}
    >
      {name}
    </Link>
  );
}
