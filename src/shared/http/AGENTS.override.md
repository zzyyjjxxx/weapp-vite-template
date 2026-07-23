# HTTP boundary rules

Before changing this subsystem, read `docs/http-client.md`, inspect the public
exports and consumers, and add focused tests before implementation changes.

- `transport.ts` performs one request and maps transport or envelope failures to `ApiError`.
- `client.ts` is the public JSON request boundary. It owns authentication lookup, one 401 replay, and safe request logging.
- `token-refresh.ts` calls the transport directly and must not import or call the public client.
- Do not log Authorization headers, tokens, complete request bodies, response bodies, or personal information.
- Domain services should depend on `client.ts`, not on `wevu/fetch` or the transport seam.
- Do not show Toast/Dialog or other UI feedback from this layer.
- Do not retry non-idempotent writes automatically.
- A request may be replayed after refresh at most once; refresh itself must not
  recursively refresh.
- HTTP 4xx/5xx, malformed JSON, timeout, cancellation and network failures
  must remain distinguishable `ApiError` kinds.
- Required evidence includes success, business error, HTTP error, malformed
  response, timeout, cancellation, concurrent 401 refresh and redacted logs.
