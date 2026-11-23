# Readable operational audit history

These captures use synthetic API fixtures reconstructed from the reported operational-history shape. They contain no real transactions or personal accounts.

- [Desktop, 1440px](desktop.png)
- [Mobile, 390px](mobile.png)

The history is expanded. Action, reason, role, status, handler and timestamp are presented as readable Vietnamese fields, including decoded Unicode from the server JSON payload.

Reproduce with `npm run test:owner:ui` in `fe_da`. The cases are in `tests/owner/json-history.spec.js`.
