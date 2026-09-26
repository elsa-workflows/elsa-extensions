# Elsa.Mcp.Abstractions

The names and rules an MCP tool derived from an Elsa workflow is built on, in a package with no dependencies beyond
the BCL so that both a server and a Blazor client can use it.

## Why this exists separately

`Elsa.Mcp.Server` turns a published workflow into a tool: it derives the tool name from the workflow name and reads
four custom properties off the definition. An editor that lets an author fill those properties in has to agree with
the server on both — in particular it has to be able to show the author the same tool name the model will see.

Duplicating the rule was the alternative, and it was rejected: silent drift between two copies is the shape of the
bug this was written to prevent.

## What it contains

| Type | What it does |
|------|--------------|
| `McpToolName` | Derives a tool name from a workflow name, validates one an author wrote, and disambiguates a shared one |
| `McpCustomProperties` | The four custom property names, and reading their value in every shape it comes back in |

## The custom properties

| Property | Meaning |
|----------|---------|
| `mcp:enabled` | The workflow is exposed as a tool |
| `mcp:name` | The tool name, instead of the one derived from the workflow name |
| `mcp:instructions` | Instructions for an agent, appended to the tool description |
| `mcp:input:<name>` | The instruction for one input, keyed by the input's name |

A value survives code, the designer, the API and JSON persistence in different shapes — `bool`, `string`,
`JsonElement` — so `ReadFlag` and `ReadText` accept all of them. Keys are matched without regard to casing, because
they are typed by hand.
