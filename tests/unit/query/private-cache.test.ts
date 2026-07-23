import { describe, expect, it } from 'vitest'

import { createQueryClient } from '@/shared/query/client'
import { clearPrivateQueryCaches } from '@/shared/query/private-cache'

describe('private query cache', () => {
  it('removes private queries while preserving public queries', async () => {
    const client = createQueryClient()
    await client.fetchQuery({
      queryKey: ['private', 'profile'],
      queryFn: async () => ({ id: 'profile' }),
      meta: { scope: 'private' },
    })
    await client.fetchQuery({
      queryKey: ['public', 'catalog'],
      queryFn: async () => ['item'],
    })

    clearPrivateQueryCaches(client)

    expect(client.getQueryData(['private', 'profile'])).toBeUndefined()
    expect(client.getQueryData(['public', 'catalog'])).toEqual(['item'])
    client.clear()
    client.unmount()
  })
})
