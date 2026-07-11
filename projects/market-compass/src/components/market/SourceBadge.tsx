import type { SearchSource } from "@/types";
import { Badge } from "@/components/ui/Badge";

// Shows where a search hit came from: the local PSX universe vs a global
// (Yahoo) symbol. Honest provenance on every result (Auditor D2/F3).
export function SourceBadge({ source }: { source: SearchSource }) {
  return source === "psx" ? (
    <Badge tone="brand">PSX</Badge>
  ) : (
    <Badge tone="neutral">Global</Badge>
  );
}
