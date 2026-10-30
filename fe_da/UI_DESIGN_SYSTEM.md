# TKSC frontend — UI system

## Design direction

Forest green, warm neutral surfaces and restrained lime accents preserve the sport identity. Use the dark primary action for transactional tasks; use lime for discovery and the home hero. Cards use a single fine border with shadows reserved for hover and booking emphasis.

## Shared foundations

- `src/styles/tokens.css` is the single source for colors, radii and elevation.
- `src/index.css` contains reset, form defaults, focus rings, buttons, badges, modal surfaces and common state presentation. Form defaults use low specificity so component spacing can override them.
- `src/styles/home.css` owns the shell, sidebar, hero, overview and footer.
- `src/styles/sessions.css` owns discovery filters, session cards, booking and registration feedback.
- `src/components/ui/ModuleIcon.jsx` maps existing navigation labels to Lucide icons.
- `src/components/ui/StatePanel.jsx` presents empty, loading and error states.
- Login and payment CSS are scoped to their own page classes. No dependencies were added to the application.

## Composition and responsive rules

Desktop authenticated pages use a 224px sidebar, reducing to 196px on smaller desktops. At 900px navigation becomes a horizontal scrollable row. Guest navigation stays horizontal. Main grids use `minmax(0, 1fr)` to avoid content overflow. Mobile margins are 16px; cards generally use 20–24px padding. Payment details and booking collapse to one column. Inputs use 16px text on small screens to avoid mobile browser zoom.

Use the existing Be Vietnam Pro font with a system fallback. Headlines are 28–46px depending on context; body copy is 12–14px. Keep secondary labels smaller than primary actions. Transitions are 180–200ms and respect `prefers-reduced-motion`. Native labelled selects and date inputs preserve keyboard and platform behavior.

## Scope and behavior preserved

Audited the active route tree: guest and all six authenticated role homes, three login portals, session discovery/detail, payment, tickets and staff payment review. Existing API helpers, authentication, home route resolution, payment client, backend and database are unchanged. Related existing workflows are UC-08 (session registration), UC-11 (payment) and UC-22 (review); this redesign does not implement new business requirements.

Role modules that were placeholders remain explicitly marked as unavailable. Existing profile/exception alert actions remain placeholders. No synthetic operational numbers were added to live screens. Removed hardcoded nearest-session data and hardcoded referee identity from the session presentation; dates no longer describe fixed October 2026 dates as today/tomorrow. Generic registration failures now render a visible message.

The legacy `components/Navbar.jsx`, `MyTicketsView.jsx` and `DevLoginModal.jsx` are not mounted by the current route tree. Their business behavior was not rewritten. Shared control/modal styles continue to apply if they are mounted later.

## Verification

Run `npm run build` and `npm run lint` from `fe_da`.

The pre-change full-repository lint baseline was 39 errors and 2 warnings. The remaining lint issues are in existing API/legacy components, plus the two existing effect dependency warnings in the session components. Do not confuse these with a passing full lint run.

Browser QA uses intercepted API fixtures only, so it checks rendering, interactions and request contracts without writing to the database or sending bank payments. It does not certify Google OAuth, the running .NET service, payOS webhooks or real transaction settlement.

Browser results (26 October 2026): 120 checks passed, with no page JavaScript exceptions or document horizontal overflow across widths 320, 390, 768, 1024 and 1440px. Coverage includes seven role shells, unavailable modules, three sign-in portals, discovery, details, registration feedback, checkout display, tickets, filters/reset and guest navigation. Additional targeted checks verified login icon spacing, visible keyboard focus and submit enabled/disabled states at 390 and 1440px. The final production build passed. Scoped lint on edited JSX files reported zero errors and the two pre-existing effect dependency warnings; full lint retains 18 pre-existing errors outside the redesigned active UI.

## Public home — Premium Sports Community (27 October 2026)

`/guest/home` now uses `GuestLayout` and `GuestHome`, with styles scoped to `guest-*` in `src/styles/guest.css`. It no longer renders the role dashboard, numeric statistics, module tiles, checklist or next-steps sidebar. The obsolete guest dashboard content and its CSS overrides have been removed; authenticated role layouts retain their previous behavior.

The default page has three purposeful sections: a photography-led hero, two sport discovery panels, and the community story. One header row combines brand, navigation and sign-in. A small footer retains staff/admin portal access. Mobile navigation supports keyboard focus, Escape dismissal and native in-page anchors.

The optional upcoming-session section reads the existing `api.getSessions()` endpoint. It displays at most three upcoming sessions with remaining places, ordered by date/time. Selection is presentation-only; the existing details/register endpoints remain authoritative. No mock sessions, invented ratings or user counts are shipped in the page. Empty, failed or malformed responses produce no featured section. The existing full schedule remains reachable. Selecting a sport preselects the existing schedule filter; no new API or business rules were introduced (related requirements UC-06 / FR-04).

Photos are real editorial stock assets hosted in `public/images/community`, with photographer/source details in `SOURCES.md`. Each image has 640/960/1600px WebP variants, responsive `srcSet`, dimensions and descriptive alt text. The hero loads at high priority; lower images load lazily. No image-generation assets or external image hotlinks are required.
