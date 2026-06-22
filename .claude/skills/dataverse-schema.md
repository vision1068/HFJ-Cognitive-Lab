---
name: dataverse-schema
description: >
  Dataverse table and column design skill. Use when designing or
  reviewing Dataverse schema: tables, columns, relationships,
  business rules, and security roles.
  Based on microsoft/dataverse-business-skills.
---

# Dataverse Schema Design Skill

## When to invoke
- Designing new Dataverse tables for a feature
- Reviewing existing schema for gaps
- Defining relationships (1:N, N:N, lookup)
- Setting up column-level security or field-level security
- Writing Dataverse business rules

## Schema design standards

### Table definition template
| Property       | Value                          |
|----------------|-------------------------------|
| Display Name   | [Business name]                |
| Schema Name    | qdb_[logicalname]              |
| Primary Column | qdb_name (Text, required)      |
| Ownership      | User or Team (default)         |
| Auditing       | Enabled                        |
| Change Tracking| Enabled (for sync scenarios)   |

### Required columns on every custom table
| Column         | Type      | Requirement |
|----------------|-----------|-------------|
| qdb_name       | Text      | Required    |
| statecode      | Status    | System      |
| statuscode     | Status Reason | System  |
| createdby      | Lookup    | System      |
| createdon      | DateTime  | System      |
| modifiedby     | Lookup    | System      |
| modifiedon     | DateTime  | System      |

### Relationship types
- **Lookup (N:1)**: Child has a lookup column to parent. Use for owned records.
- **1:N**: Parent can have many children. Define cascade behavior explicitly.
- **N:N**: Use intersect table. Avoid native N:N for custom logic needs.

### Business rules checklist
- [ ] Client-side only (no server-side scope unless required)
- [ ] Condition and action clearly named
- [ ] Error messages in business language, not technical
- [ ] Tested in both create and update contexts

## Output format
Table name, column list (name / type / required / description),
relationships list, business rules list. Flag any design concerns.
