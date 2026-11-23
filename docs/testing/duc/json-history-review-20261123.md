# Readable audit history — 23 November 2026

The reported Owner operational-history screenshot exposed a defect missed by the earlier page-load/workflow checks: collapsed history rendered the audit `newValue` directly. This displayed serialized JSON, internal action codes and escaped Unicode. The new regression cases explicitly expand history and assert its visible content.

## Changes

| Surface | Fix |
| --- | --- |
| Issue Hub and operational exceptions | Shared paginated history with Vietnamese actions/fields, decoded reasons, handler names when available and Vietnam timestamps |
| Refund history | Readable decisions, payout amounts, recipient, bank reference, proof and independent verification |
| Payment review reasons | Structured details instead of raw `newValue` |
| Payment recovery | Normalize legacy/double-encoded evidence and result fields before rendering |
| Operations feedback | Decode `Content` safely; malformed/unknown payloads no longer fall back to raw JSON |
| Appointment and operational decision details | Readable nested reasons; translated role labels |

The shared presenter supports PascalCase/camelCase payload fields, legacy text, double-encoded JSON, nullable values and malformed payloads. Known business fields are displayed using labels; unknown technical fields are not dumped into the UI. Unreadable records get an explicit fallback message. React renders all text normally; the implementation does not inject HTML.

Original audit storage and business approval/settlement rules are unchanged. No database upgrade is required.

## Validation

- **86 browser tests passed**: 12 Owner (including six new JSON-history cases), 39 Duc and 35 Manager.
- **26 frontend model tests passed**: 19 Duc (including nine new audit presentation cases) and seven Owner.
- Frontend production build and ESLint for changed production files passed. The existing bundle-size warning remains.
- New browser cases reproduce the screenshot payload on desktop/mobile, verify refund proof and payment-review presentation, decode feedback and ensure malformed history does not break issue controls.
- Source scan found no remaining direct audit `newValue` text interpolation; payment-review records now pass through the shared presenter. Other existing history views already extract named business fields.

[Desktop and mobile evidence](../../evidence/owner-json-history-20261123/README.md) uses synthetic data. These checks exercise real React components with mocked APIs, not live money transfers or database writes.
