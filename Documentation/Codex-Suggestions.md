# Codex Suggestions for Scal.Interpreting.Commands

## Context

This document summarizes feature suggestions and a lightweight impact/effort analysis based on the current project positioning:

- deterministic verb/noun grammar and resolution
- strongly typed command modeling
- DataAnnotations validation
- contextual feedback/help generation
- response file support

---

## Clarification on already-possible behaviors

### Dry-run
A practical dry-run mode already exists by calling `Interpret<T>(args)` and intentionally not executing the instantiated command.

### Error formatting
Output customization is already possible through `Feedback(Action<string>)`, i.e., caller-controlled formatting/sinks via delegate.

---

## Feature suggestions (impact vs effort)

### High impact / Low-to-Medium effort (recommended first)

1. **Typo suggestions** (`Unknown command` / unknown parameter → closest matches)
   - **Impact:** High UX improvement, fewer support issues.
   - **Effort:** Medium (string similarity + ranking + tests).
   - **Rationale:** Naturally integrates with existing unknown/ambiguous command handling.

2. **Stable command aliases** (explicit synonyms, distinct from abbreviations)
   - **Impact:** High for documentation and script stability over time.
   - **Effort:** Medium (metadata + match precedence rules).
   - **Rationale:** Complements current abbreviation behavior, which is intentionally adaptive.

3. **Environment variable binding fallback** (opt-in per parameter/property)
   - **Impact:** High for CI/CD and containerized usage.
   - **Effort:** Medium (attribute design + precedence strategy).
   - **Rationale:** Extends typed binding without weakening deterministic CLI semantics.

### Medium impact / Low effort (quick wins)

4. **Machine-readable help output** (e.g., JSON)
   - **Impact:** Medium (tooling automation, wrapper tooling, UIs).
   - **Effort:** Low-to-medium.
   - **Rationale:** Existing command/parameter model already exposes the structure needed.

5. **Richer response-file diagnostics** (include chain, nested path trace, line numbers)
   - **Impact:** Medium (debuggability in larger CLI setups).
   - **Effort:** Low.
   - **Rationale:** Builds on existing response-file recursion/depth mechanisms.

### High impact / Higher effort (strategic)

6. **Shell completion generation** (Bash/Zsh/PowerShell)
   - **Impact:** High daily usability for interactive users.
   - **Effort:** High (cross-shell generation/testing and packaging).
   - **Rationale:** Leverages existing command/parameter metadata.

7. **Parameter relationship rules**
   - Examples: mutually exclusive groups, requires-with, one-of groups.
   - **Impact:** High for complex command surfaces and correctness.
   - **Effort:** High (new validation abstractions + conflict reporting UX).
   - **Rationale:** Strong alignment with model-first design intent.

---

## Suggested implementation order

1. Typo suggestions
2. Stable aliases
3. Environment variable binding
4. JSON help output
5. Response-file diagnostics
6. Shell completion generation
7. Parameter relationship rules

---

## Optional next step

If useful, this can be converted into a milestone roadmap (`vNext`, `vNext+1`) with:

- acceptance criteria per feature
- compatibility constraints
- test matrix and sample CLI scenarios
