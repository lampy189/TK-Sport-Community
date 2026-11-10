# Skill exception visibility before ticket purchase

The previous Sport Center policy exposed Players to venue reviewers only through tickets. A Player requesting an exception before registration therefore disappeared from the API join.

Upgrade `014-skill-exception-player-visibility.sql` preserves existing ticket-based access and adds access for Referee/Manager when the Player has an exception request in their venue. The API still enforces the Referee session assignment. Staff and other venues remain blocked.

The regression test creates its own prerequisites inside a transaction and rolls them back. It checks no-ticket visibility, profile/history access, venue boundaries and Staff restrictions.

Apply the upgrade only after approval for the target database. No exception decision, skill score or ticket is changed by the upgrade.
