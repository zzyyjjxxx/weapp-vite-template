export const landDemandKeys = {
  all: ['land-demand'] as const,
  detail: (creditcode: string) => [...landDemandKeys.all, 'detail', creditcode] as const,
}
