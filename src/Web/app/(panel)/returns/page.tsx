import { ReturnsView } from "@/features/returns/components/returns-view";
import { requireAuthenticatedUser } from "@/shared/permissions/require-permission";

export default async function ReturnsPage() {
  // Every signed-in user; the backend only returns their own records unless they hold returns.read.all.
  await requireAuthenticatedUser();
  return <ReturnsView />;
}
