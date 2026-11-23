# Owner review — 23 November 2026

Scope: Đức's assignment in the [22/11 task document](https://docs.google.com/document/d/1R1DVXNO8BCZ0wuC-4dCL7USe2FJzDSGPhwY7y-M43Gg/edit?tab=t.0): run Owner workflows, review UI/UX and add missing pagination. Initially reviewed against upstream main `ee8b3eb`, then rebased onto `8c4a485` on `DucTM2`, preserving the latest Manager UI improvements. Database checks used the supplied export.

## Changes

- Policy history and all business report tables now paginate. Printing includes every filtered row, regardless of the screen page. Pagination stays outside the horizontal table scroll area on mobile. Changing report dates/center clears the previous result.
- Policy history has text/status filters and a focused decision dialog. Evidence is required; failed decisions retain the draft. Own proposals remain unavailable for approval.
- Appointment requests have text/status filters; account selection supports search and excludes protected Owner accounts. Approved requests offer withdrawal instead of a second approval. Pending, executed and handover states retain their existing backend rules.
- Venue, court, personnel and change history lists support search and reset pagination when filters change. Pagination controls span the card grid. Report filter controls align and mobile pages fit the viewport.
- Policy mutation endpoints now validate antiforgery before invoking business services.
- Fixed application-level visibility of pre-ticket skill exceptions: only Manager/Referee in the request's center can see the needed player/profile/history. Staff and other centers remain excluded with SQL RLS off. This preserves the previously approved exception workflow on the team's current authorization model.
- Updated SQL test fixtures to use actual EF actor scopes, sessions in selected centers and eligible sport profiles. Login tests use separate hosts per actor to respect production authentication limits.

## Coverage

| Owner area | Verification |
| --- | --- |
| Home / operations overview | Real API load; submit trial feedback |
| Business dashboard | Real API load; no browser/API errors |
| Business reports | Date filters; inventory; screen pagination; complete print output |
| Venues and courts | List/search; edit test venue and court; persisted result |
| Center personnel | Search; unassign test Staff; restore via authorized API; history |
| Ticket aftercare | Approve refund; Staff saves simulated payout; block self-verification; Owner verifies; repeat completion |
| Operational exceptions | Manager proposal; independent Owner approval; execute stock change; close; repeat execution |
| Issue Hub | Open report; reply; claim; resolve after business completion |
| Policies and prices | 12-row pagination; filter; independent approval; block own approval; effective price check |
| Appointments | Independent approval; no duplicate approve button; withdraw before execution |
| Approval hub and notifications | Real API navigation; no errors; mobile layout |

SQL invariants after the simulated workflow: stock changed from 10 to 12 with exactly one movement; completed refund has exactly one ledger entry; baseline product price remains 1000 and the approved version yields 1500 at its effective date. Application data was not used for simulated writes.

## Validation

- Backend suite with all SQL opt-ins against a separate test database: **918 passed, 0 failed, 0 skipped**.
- Owner model unit tests: **7 passed**. Existing Duc model regression tests: **10 passed**.
- Owner mocked browser suite: **6 passed**; covers pagination/filter resets, printing, self-approval controls, dialog focus/error retention, account search and mobile layout.
- Existing Duc browser regression suite: **39 passed** after refreshing the assessment label/appeal mock and scoping the no-dropdown assertion to the decision form (pagination may use a page-size select).
- Real browser audit: **11 Owner module loads**, followed by **14 integration scenario groups** on an isolated database; no page JavaScript errors.
- Frontend production build and ESLint for changed production components pass. Backend build passes. Test build reports the existing Firebase `System.Collections.Immutable` assembly conflict warning; frontend retains the existing bundle-size warning.
- New application database integrity checks pass: **51 tables / 2,350 rows**. Old databases remain preserved. Only the approved missing accounting/stock-hold triggers were installed; SQL RLS remains off. Existing `OwnerEffectivePrice` and `OwnerPriceVersion` definitions came from the supplied schema and were not added or changed in this review.

After syncing the two newer main commits, revalidation passed: **6 Owner UI + 39 Duc UI + 35 Manager UI = 80 browser tests**, **17 frontend model tests**, production build and lint for the merged components. Backend source was unchanged by this main sync. The global ticket-operations fixture explicitly selects Owner; Manager tests retain their separate Manager workflow. The Manager policy test checks screen pagination and complete print output separately.

## Running regression checks

```sh
cd fe_da
npm run test:owner
npm run test:owner:ui
npm run test:duc
npm run build
```

For SQL-backed backend tests, use the documented opt-in variables with an isolated test database. Never point persistent workflow fixtures at the shared application database.

Local evidence and raw results are in `docs/testing/duc/round1/owner-20261123/` (Git-ignored because real lab screenshots may contain personal information). `baseline/` contains module captures, `integration/` contains workflow captures/results, and `mock-ui/` contains synthetic mobile captures. No Drive upload was performed in this review.

The refund transfers were simulated. Google sign-in, live bank transfers, SMTP/push delivery and production deployment were not acceptance-tested. Restart the existing backend after changing User Secrets so it loads the new database selection; audit servers were separate localhost processes and are stopped after verification.
