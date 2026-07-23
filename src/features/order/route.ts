import type { LocationQuery } from 'wevu/router'

export function readOrderId(query: LocationQuery): string {
  return typeof query.id === 'string' ? query.id : ''
}
