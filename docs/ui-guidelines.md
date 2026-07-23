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

`AppIcon` is the runtime icon boundary. It uses a deliberately small, vendored
subset of Reicon SVG files through the native mini-program `<image>` component;
do not import `reicon-vue` directly into a Wevu SFC. Add an icon to the registry
and commit its local asset when a feature needs one, instead of shipping the
entire icon database to the first package.

`AppTabBar` is the custom bottom navigation boundary. It is rendered by
`PageShell` only when Home or Profile passes an explicit active tab path, uses
`AppIcon` for the selected/outline states, and navigates through the typed
router. Do not add a second native `tabBar` configuration or call
`wx.switchTab` for these routes.

`weapp-tailwindcss` is permitted for mini-program-compatible styling if a future
feature needs it. Native Web TailwindCSS config, browser class scanning and a
browser Tailwind runtime are intentionally out of scope.

Styling changes require `pnpm stylelint`, a build, and runtime screenshot/diff
evidence when DevTools/MCP is available.
