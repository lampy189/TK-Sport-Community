# DucTM Round 1 integration runner

Requires the Development frontend/backend, SQL Server test database with the approved project upgrades (including 014), Node dependencies in `fe_da`, Chrome and .NET 8 support. Connection settings come from local User Secrets or `TKSC_TEST_SQL_CONNECTION` and must never be committed.

From the repository root, provision a fresh, isolated synthetic fixture set:

```sh
dotnet run --project scripts/duc-round1/DucRound1.csproj -- --apply --database <exact-approved-test-database>
```

This explicitly writes synthetic accounts, venue, sessions, payment records and assessments to the specified test database. No real bank transfer occurs. It writes `manifest.json` locally. Do not run against production or reuse a consumed manifest.

With the app running on `http://127.0.0.1:5173`, run from `fe_da`:

```sh
DUC_BUILD=$(git rev-parse --short HEAD) node tests/duc/round1-live.mjs
```

The runner uses real Development logins and app/API requests, checks state and notifications, and captures live UI plus API assertion screenshots. It changes its isolated test data. Local manifests, results and evidence are excluded from Git.

Round 1 covers 35 cases across incidents/flagged payments, ticket exchange/refunds, skill exceptions and rating appeals. Round 2 and Round 3 require separate runs and fresh fixture sets. The report and evidence are maintained in the team's designated Drive destinations.

An initial calibration exposed selector/timing issues in the harness. The official Round 1 used fresh fixtures after those harness corrections, with no application code changes between calibration and the official run.
