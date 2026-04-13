export function uniqueName(prefix: string): string {
  const id = `${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
  return `${prefix} ${id}`;
}
