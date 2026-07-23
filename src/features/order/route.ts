export function readOrderId(query: unknown): string {
  if (typeof query !== 'object' || query === null || !('id' in query)) {
    return ''
  }

  const value = query.id
  return typeof value === 'string' ? value : ''
}
