# What's Changed
## V2.16.2 (2026-07-16)
- Fixed: Company Files versions could disappear from the creation/migration screens — the startup sync could wipe a working local clone when the Git source was unreachable; it now clones to a temporary folder and only replaces the existing clone on success
- Fixed: deleting a repository configuration that used a "Local Cloned Folder" no longer deletes the content of that user folder
- Added: a Git repository marked "Use as version X.Y.Z" now also works for Company Files — develop new company files locally (no release/tag needed); shown as VX.Y.Z in the Company Files version selector on the creation and migration screens
- Fixed: "Use as version X.Y.Z" mutual exclusion is now scoped per section, so a Templates and a Company Files VX.Y.Z can be used at the same time
- Restored: the "Use Company Files" toggle in the Configuration screen
## V2.16.1 (2026-07-07)
- Replaced the large "Select a project to begin" overlay with a compact callout anchored under the Browse button of the project selector, floating above the page content
- Separated the shared project selector banner from the page content (border + shadow)
## V1.8.4 (2025-03-26)
Update possible in Program Files