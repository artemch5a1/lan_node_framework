/** Ctrl+Shift+` (Backquote) — одна и та же комбинация открывает и закрывает. */
export function isAdminToggle(e: KeyboardEvent): boolean {
  return (
    e.ctrlKey &&
    e.shiftKey &&
    !e.altKey &&
    !e.metaKey &&
    e.code === "Backquote"
  );
}
