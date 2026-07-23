# HTTP boundary rules

- `transport.ts` performs one request and maps transport or envelope failures to `ApiError`.
- `client.ts` is the public JSON request boundary. It owns authentication lookup, one 401 replay, and safe request logging.
- `token-refresh.ts` calls the transport directly and must not import or call the public client.
- Do not log Authorization headers, tokens, complete request bodies, response bodies, or personal information.
- Domain services should depend on `client.ts`, not on `wevu/fetch` or the transport seam.
