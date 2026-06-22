---
name: power-platform-dev
description: >
  Power Platform development skill. Use when building or reviewing
  Power Apps canvas apps, model-driven apps, Power Automate flows,
  Dataverse tables, PCF components, or Power Platform ALM pipelines.
  Inspired by microsoft/power-platform-skills and
  DanielKerridge/claude-code-power-platform-skills.
---

# Power Platform Development Skill

## When to invoke
- Creating or reviewing Power Apps (canvas or model-driven)
- Designing Power Automate flows
- Defining Dataverse schema (tables, columns, relationships)
- Setting up ALM pipelines for Power Platform solutions
- Configuring PCF controls

## Approach

### Canvas Apps
1. Start with a data model review — confirm Dataverse tables exist
2. Define screens: Home, Detail, Form, Confirmation
3. Apply responsive layout (LayoutContainer with horizontal/vertical)
4. Use named formulas (App.Formulas) for reusable logic
5. Minimize OnStart logic — defer to named formulas
6. Delegation check: all Gallery filters must be delegable

### Model-Driven Apps
1. Confirm all required tables, views, and forms exist in solution
2. Site map: Group → Area → Sub-area hierarchy
3. Business rules for client-side validation (no code required)
4. Business Process Flows for guided multi-stage processes
5. Dashboards: use system charts, not embedded Power BI unless needed

### Power Automate
1. Trigger type: Automated (event-driven) preferred over polling
2. Error handling: Scope + Configure Run After on every critical action
3. Environment variables for all endpoints and thresholds
4. Parallel branches for independent actions
5. Always log to a Dataverse log table on failure

### ALM Checklist
- [ ] All components in a managed solution with publisher prefix qdb_
- [ ] Environment variables defined (not hardcoded values)
- [ ] Connection references used (not hardcoded connections)
- [ ] Solution exported as managed for Test/UAT/Prod
- [ ] Pipeline: Export → Unpack → Commit → Build → Deploy

## Output format
Produce: screen list, component list, flow diagram (text), and
Dataverse table list with key columns. Flag delegation issues.
