# Why Scal.Interpreting.Commands exists

## TL;DR

> **Scal.Interpreting.Commands** models your CLI instead of just parsing it.
>
> It stays simple for small tools, scales naturally to complex command structures, and keeps parsing, validation, and execution cleanly separated.

---

## The problem space

Command-line interfaces in .NET often start simple: a few options, a single command, minimal validation.

Over time, they evolve:
- multiple commands
- shared options and behaviors
- contextual validation
- increasing structural complexity

At that point, the command-line is no longer just input parsing, it becomes a **model of intent**.

---

## Design perspective

This library approaches command-line parsing as a **modeling problem**, not just a parsing problem.

Rather than mapping arguments to a flat structure, it focuses on:
- representing commands as **composable elements**
- making relationships **explicit**
- using **strongly-typed commands** to naturally separate behaviors
- allowing similar commands to be **discriminated by their parameters**

This enables distinct processing paths without relying on manual conditionals or fragile branching logic.

The goal is to align the command-line structure with how the domain is expressed.

---

## Core principles

**Composability**  
Commands and options can be combined, reused, and extended naturally.

**Explicitness**  
Behavior, constraints, and relationships are visible and predictable.

**Strong typing**  
Commands are modeled as distinct types, enabling clear and safe processing flows.

**Scalability**  
The same model supports both simple and complex command-line interfaces without artificial limits.

**Separation of concerns**  
Parsing, validation, and execution remain distinct and independently evolvable.

---

## What this enables

- Natural representation of command hierarchies  
- Separation of similar commands through typing instead of branching  
- Reuse of command components across contexts  
- Validation without coupling to parsing  
- Clear and deterministic error reporting  
- Smooth evolution of the CLI over time  
- Consistent approach from small tools to large systems  

---

## Design intent

This library is designed to remain:
- simple when the problem is simple
- structured when the problem grows

It does not require upfront complexity, but it does not impose limits when complexity emerges.

---

## When this approach fits

- Single-command tools that may evolve  
- Applications with multiple commands or subcommands  
- Systems where command behavior reflects domain logic  
- Scenarios where similar commands require distinct processing paths  
- Projects where clarity and long-term structure matter  

---

## A note on approaches

Command-line parsing can be approached in different ways.

A common approach is to:
- map arguments to a single options structure
- use dedicated attributes to describe parsing rules
- handle behavior through conditional logic

This works well for simple and static command-line interfaces.

---

Scal.Interpreting.Commands follows a different approach:

- commands are modeled as **distinct, strongly-typed elements**
- behavior is separated through **type-based processing**
- relationships between commands are **explicit**

This makes it possible to:
- avoid branching logic based on flags or modes  
- represent intent directly in the type system  
- evolve the command-line structure without rewriting existing code  

---

Both approaches are valid.

The choice depends on whether the command-line is:
- a simple input surface  
- or a structured and evolving part of the application

---

## Summary

Scal.Interpreting.Commands treats the command-line as a structured expression of intent.

This perspective enables a design that remains consistent across scales, from minimal tools to complex command surfaces.
