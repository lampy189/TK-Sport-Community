# DucTM2 — recovery workflow verification

Scope: the seven recovery requirements assigned to Đức on 16 November 2026. These changes use the existing audit, approval, payment, refund, notification and stock tables. The recovery additions themselves require no schema upgrade or RLS disabling; see the LamNV_cm merge prerequisites below for the integrated source.

| Requirement | Implemented behaviour | Regression coverage |
| --- | --- | --- |
| Propose → decide → execute → close money/stock adjustments | Operational Exceptions replaces the placeholder. A stock proposal records the product, signed base-unit quantity and evidence; execution appends one stock movement and changes the balance atomically; closure is a separate step. Money is handled through the linked payment/refund workflow. | IncidentWorkflowTests, RecoveryWorkflowDatabaseTests, recovery.spec.js |
| Authority, evidence and independent approval | Staff proposals require Manager/Owner; Manager proposals require Owner. Self-approval and decisions for the refund recipient are blocked. Refund payout and independent verification are separate. | RefundWorkflowTests, IncidentWorkflowTests, UnallocatedPaymentReturnTests, authorization tests |
| Payment exceptions, court incidents and refunds | Every case links to the source resource. payOS recheck validates the bank source. Underpaid, overpaid, duplicate and unusable-ticket transfers have a full unallocated-transfer return path, with approval, outgoing evidence and independent verification. | TicketRecoveryTests, UnallocatedPaymentReturnTests, SQL rollback fixture |
| Exchange/refund edge cases | Existing target capacity, price difference and replay guards remain. Exchanges are restricted to the same venue. Failed refunds retain their approval, evidence and history and continue on the same refund. | Existing aftercare/payment tests, RefundWorkflowTests |
| Hourly ticket aftercare and venue isolation | Existing late-entry validation remains active. Source access is checked before case/history/payout reads and writes; SQL RLS remains enabled. Original payers outside the ticket venue do not cause business rows or messages to disappear. | LateEntryDatabaseReadTests, RecoveryWorkflowDatabaseTests, existing late-entry/center tests |
| Rating appeals and future tickets | Existing Referee review and Manager decision remain. Rating changes now follow active ticket funding across cheaper/equal/dearer exchanges, split by payment and original payer, with reservation caps and repeat protection. | FutureTicketFundingTests, existing appeal tests |
| Issue Hub handler, result and pending work | Claim/assign, waiting for bank, waiting for approval, resolve/reopen, optimistic version checks and history are available. Pending source work blocks closure. New Player messages reopen resolved support issues. Closed/rejected/completed cases have separate filtering. | IncidentWorkflowTests, SQL rollback fixture, UI tests |

## Run automated checks

```sh
dotnet build be_da/be_da.csproj
dotnet test tests/TKSC.Api.Tests
dotnet test tests/PaymentTests
cd fe_da
npm run build
npm run test:duc
npm run test:duc:ui
```

For SQL integration tests, provide `TKSC_TEST_SQL_CONNECTION` through a private environment/session. Never put the connection string in Git. Without that variable, SQL tests intentionally skip. The new database fixture creates isolated records inside an outer transaction and always rolls back, including on failure.

To avoid replacing earlier evidence, override `DUC_EVIDENCE_DIR` and `DUC_UI_RESULTS` when running Playwright. Browser scenarios mock the API and visibly identify their fixture data; they do not prove a real bank transfer occurred.

## Manual acceptance flows

1. **Inventory:** Staff opens Operational Exceptions, proposes a signed base-unit adjustment for a product at their venue and attaches the count/evidence reference. A different Manager decides. Staff executes the approved adjustment. Manager checks the balance/movement and closes the file. Retry execution: the stock balance must change only once. A Manager's own proposal must go to Owner.
2. **Ticket refund:** Manager/Owner approves the exact original payer and allocation. A different operator records the actual outgoing transfer reference, document and time. An independent operator verifies that saved proof and completes the refund. Test a failed attempt, retry on the same refund, reused bank reference and self-verification rejection. Legacy processing refunds without a saved decision first return to the approval step.
3. **Unallocated transfer:** In Payment Recovery, claim the issue, select the recorded bank receipt and submit a full-return proposal. After independent approval, record the real outgoing proof in Operational Exceptions; Manager/Owner independently verifies and closes it. Underpayment/overpayment can be returned in full and the Player can rebook. A duplicate receipt can be returned while the separate matched receipt continues protecting existing tickets. The source receipt cannot be reused to activate tickets after return.
4. **Court/report:** Player reports an issue; staff claims it, replies and records what is waiting. A pending refund or unresolved court booking blocks closure. Complete the source work, then close the issue. A subsequent Player message must reopen it.
5. **Appeal after exchange:** Referee reviews, Manager decides a corrected band. Verify affected future tickets are cancelled with requested refunds to each original payer, including tickets exchanged to a cheaper/equal/dearer session. Retry must not create another refund.
6. **Isolation/hourly tickets:** Repeat with personnel assigned to another venue and with expired hourly entry. Foreign resources must be absent/denied and expired entry must not be activated.

## Results and evidence

- API/backend suite with SQL enabled: **743 passed, 0 failed, 0 skipped**.
- Payment rules: **67 passed**.
- Frontend unit checks: **10 passed**.
- Browser UI checks: **34 passed**.
- Backend/frontend builds and scoped ESLint checks passed.

Screenshots in [the evidence folder](../../evidence/duc-recovery-20261116/) show refund approval/payout/independent verification, stock approval/execution/closure, payment recovery and unallocated-transfer return.

Bank-facing tests use a deterministic gateway double. No real outgoing transfer was initiated during verification. In operation, staff records an actual bank transfer and an independent operator verifies it; a gateway outage remains explicitly pending instead of bypassing verification. Unallocated receipts/returns are kept in the exception/bank audit, outside earned ticket revenue; ordinary paid-ticket refunds continue using the existing refund revenue trigger.

## LamNV_cm merge validation

The conflict-resolution merge preserves the team's owner pricing/versioning, notification delivery, application center filters and goods-payment recovery alongside the DucTM2 incident/refund workflows. The shared recovery screen delegates goods transactions to the team's goods service; unallocated-transfer returns remain limited to ticket payments.

Validation of the merged source:

- API/backend: **809 passed, 0 failed, 16 skipped** (825 total).
- Payment rules: **68 passed**; frontend unit checks: **10 passed**.
- Browser UI: **37 passed**, covering both Đức's workflows and the team's goods recovery/invoice screens.
- Backend/frontend builds and scoped lint passed.

The 16 opt-in SQL tests were not run against the merged source. This source now includes LamNV_cm's database prerequisites (notably upgrades 018/019 for goods reservations and Owner policy/pricing versions). No migration, RLS setting or lab data was changed while resolving the PR. The earlier 743-case SQL-enabled result above applies to the pre-merge recovery snapshot, not to this integrated snapshot. Review the team's deployment/migration plan before running the integrated source against the existing lab database.
