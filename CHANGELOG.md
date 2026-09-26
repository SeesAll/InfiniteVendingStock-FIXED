# Changelog

All notable changes to the maintained Scavengers Haven version of
InfiniteVendingStock are documented here.

## 1.3.2 - 2026-09-26

### Fixed

- Updated `OnVendingTransaction` to the current five-parameter Rust/Oxide
  hook signature. This restores automatic restocking after purchases.
- Updated `OnRefreshVendingStock` to receive an `ItemDefinition`, matching
  the current Rust/Oxide hook signature.
- Removed obsolete item-based refresh code that could no longer be called by
  the current hook.

### Changed

- Rentable shop vending machines are now excluded from automatic stock
  management, preventing player-operated shop inventory from being altered.
- Preserved the existing CustomVendingSetup compatibility, queued refreshes,
  blueprint/skin-aware stock matching, and immediate vending UI updates.

## 1.3.1 - 2026-03-10

### Changed

- Removed unsafe self-reloading during server initialization.
- Added event-driven stock refreshes for server initialization, vending
  machine spawns, and vending transactions.
- Added blueprint- and skin-aware stock calculations.
- Added immediate vending-machine network/UI updates.
- Added compatibility safeguards for CustomVendingSetup-managed machines.
- Added queued and deduplicated refresh processing.
