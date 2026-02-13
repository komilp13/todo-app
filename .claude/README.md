# .claude Directory

This directory contains custom tools and configurations for Claude Code development on this project.

## Structure

```
.claude/
├── README.md              # This file
├── skills/                # Custom skill definitions
│   ├── scaffold-slice.md
│   ├── add-migration.md
│   ├── test-slice.md
│   ├── check-architecture.md
│   ├── api-client-gen.md
│   └── seed-data-gen.md
└── agents/                # Custom agent prompts
    ├── domain-modeling.md
    ├── api-contract.md
    ├── test-strategy.md
    ├── db-query-optimization.md
    ├── frontend-component-arch.md
    ├── security-review.md
    └── integration-test.md
```

## Quick Start

### Using Skills

Skills are invoked with a `/` prefix from Claude Code:

```bash
/scaffold-slice CreateTask command      # Generate vertical slice boilerplate
/add-migration AddField --apply         # Create database migration
/test-slice CreateTask                  # Run tests for feature
/check-architecture --strict            # Validate architecture
/api-client-gen Tasks                   # Generate TypeScript interfaces
/seed-data-gen --minimal                # Generate seed data
```

### Using Agents

Agents are specialized advisors for complex tasks. Use the Task tool with agent prompts:

```
Task tool:
  subagent_type: general-purpose / Explore / Plan
  prompt: [Copy relevant prompt from /.claude/agents/{agent-name}.md]
```

**Available Agents:**
- Domain Modeling — Design pure domain entities
- API Contract — Review endpoint designs
- Test Strategy — Plan comprehensive tests
- DB Query Optimization — Optimize complex queries
- Frontend Component Architecture — Design React components
- Security Review — Audit authentication and authorization
- Integration Test — Write integration tests

## Reference

For complete documentation on all skills and agents, see:
- **Main Guide**: `/SKILLS_AND_AGENTS.md`

## Adding New Skills/Agents

1. Create markdown file in `/.claude/skills/` or `/.claude/agents/`
2. Follow the template from existing files
3. Add entry to `/SKILLS_AND_AGENTS.md` quick reference
4. Test with sample use case

## Project Context

This is a GTD (Getting Things Done) todo application using:
- **Backend**: C# 9, ASP.NET Core, Entity Framework Core, PostgreSQL
- **Frontend**: React, Next.js, TypeScript, Tailwind CSS
- **Architecture**: Clean Architecture + Vertical Slice Architecture

See `/CLAUDE.md` for detailed project guidance.
