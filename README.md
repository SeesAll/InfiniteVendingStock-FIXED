# InfiniteVendingStock

A lightweight Rust (uMod/Oxide) plugin that makes **NPC vending machines
behave as if they have infinite stock**.

After a purchase, the vending machine stock is refreshed almost
immediately so players can continue buying without waiting for the
machine to refill.

This plugin is designed to remain **very lightweight and event-driven**,
ensuring minimal server performance impact.

------------------------------------------------------------------------

# Credits

**Original Plugin Author:** Unknown / Original InfiniteVendingStock
contributor

This version has been **reviewed, fixed, optimized, and enhanced by
SeesAll** to improve:

-   Rust API compatibility
-   Plugin stability
-   Performance
-   Compatibility with other vending-related plugins

Enhancements were made while preserving the core purpose of the original
plugin.

------------------------------------------------------------------------

# What This Version Fixes

The original version had several issues that could cause problems on
modern Rust servers.

### Fixes and Improvements

-   Removed unsafe **self-reloading behavior** during server
    initialization
-   Fixed multiple **Rust API compatibility issues**
-   Fixed **ulong / uint network ID mismatches**
-   Removed deprecated or missing API calls that caused compile failures
-   Improved compatibility with different **Rust server builds**
-   Reduced unnecessary logic and cleaned up unused code
-   Improved vending refresh logic so machines **restock almost
    instantly after purchases**
-   Implemented **event-driven updates** instead of heavy looping logic
-   Added safeguards to avoid conflicts with other vending plugins

------------------------------------------------------------------------

# Compatibility

This plugin has been tested to work alongside:

**CustomVendingSetup**

Custom vending machines managed by that plugin are ignored so both
plugins can safely run together.

Plugin behavior:

-   **InfiniteVendingStock** manages vanilla NPC vending machines
-   **CustomVendingSetup** manages customized vending machines

This separation prevents conflicts and ensures reliable vending
behavior.

------------------------------------------------------------------------

# How It Works

The plugin refreshes NPC vending machine stock when:

-   The server initializes
-   A vending machine spawns
-   A vending transaction occurs

When a player purchases items, the machine stock is quickly refreshed so
the vending machine **never feels empty**.

Players can immediately continue buying items without waiting for stock
timers.

------------------------------------------------------------------------

# Performance

This plugin is intentionally designed to be **very lightweight**.

Characteristics:

-   No repeating timers
-   No global entity scans
-   Only reacts to vending-related events
-   Minimal CPU usage even on high population servers

Performance impact is **negligible** even on busy modded servers.

------------------------------------------------------------------------

# Installation

1.  Download `InfiniteVendingStock.cs`
2.  Place it in:

```{=html}
<!-- -->
```
    /oxide/plugins/

3.  Reload plugins or restart the server:

```{=html}
<!-- -->
```
    o.reload InfiniteVendingStock

------------------------------------------------------------------------

# Configuration

This plugin currently requires **no configuration file**.

Stock behavior is handled automatically.

------------------------------------------------------------------------

# Support

If you encounter issues:

-   Verify your Rust server and Oxide/uMod installation are up to date
-   Ensure no other plugins are aggressively modifying vanilla NPC
    vending machines

------------------------------------------------------------------------

# License

This plugin retains credit to the original author.

Enhancements and fixes by **SeesAll**.

Use and modify freely within the Rust server community.
