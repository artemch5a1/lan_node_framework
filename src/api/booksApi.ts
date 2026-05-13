import type { Book } from "./types";

export async function fetchBooksList(baseUrl: string): Promise<Book[]> {
  const r = await fetch(`${baseUrl}/api/Books`);
  if (!r.ok) throw new Error(`книги: HTTP ${r.status}`);
  return (await r.json()) as Book[];
}
