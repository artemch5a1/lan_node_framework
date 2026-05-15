import type { Book } from "./types";
import { fetchBackend } from "./backendFetch";

export async function fetchBooksList(baseUrl: string): Promise<Book[]> {
  const r = await fetchBackend(baseUrl, `${baseUrl}/api/Books`);
  if (!r.ok) throw new Error(`книги: HTTP ${r.status}`);
  return (await r.json()) as Book[];
}
