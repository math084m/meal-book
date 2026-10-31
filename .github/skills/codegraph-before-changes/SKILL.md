---
name: codegraph-before-changes
description: Use before planning, debugging, or implementing any code change in Meal Book. Query the relevant Codegraph index to discover architecture, call paths, callers, and tests before editing.
---

# Codegraph before changes

Use the Codegraph MCP explore tool before changing code. Choose the index for
the code being changed:

| Code area | Project path |
|---|---|
| Angular frontend | `src/frontend` |
| .NET backend | `src/backend` |

For changes spanning both areas, query both indexes. Ask a focused question
about the affected behavior and request the relevant symbols, callers, and
tests. Use the returned call paths and source excerpts to guide investigation.

Treat graph results as navigation, not as proof that code or tests are current.
Inspect the actual files before editing, verify dependent call sites, and run
the smallest relevant tests or checks after the change.

If the MCP tool is unavailable or the selected index is missing, continue with
repository search and file reads. State the limitation when relevant. Do not
install Codegraph, initialize/rebuild indexes, or run a full graph-generation
pipeline unless the user asks.
