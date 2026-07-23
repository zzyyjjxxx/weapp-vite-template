# UI guidelines

The visual language is intentionally small and product-neutral. Use tokens from
`src/styles/tokens.scss` instead of adding one-off colors or spacing.

- Primary: `#0052d9`
- Success: `#00a870`
- Warning: `#ed7b2f`
- Error: `#d54941`
- Text: `#1d2129` / `#4e5969`
- Page: `#f5f7fa`
- Card: `#fff`
- Spacing: `16rpx`, `24rpx`, `32rpx`, `48rpx`

`PageShell`, `AppLoading`, `AppEmpty`, and `AppError` are semantic primitives;
they do not import domain code. Pages use native mini-program nodes and can use
TDesign MiniProgram when a full component is needed. Do not add a second UI
library or wrap every primitive without stable semantics/behavior to justify it.

`weapp-tailwindcss` is permitted for mini-program-compatible styling if a future
feature needs it. Native Web TailwindCSS config, browser class scanning and a
browser Tailwind runtime are intentionally out of scope.

Styling changes require `pnpm stylelint`, a build, and runtime screenshot/diff
evidence when DevTools/MCP is available.
