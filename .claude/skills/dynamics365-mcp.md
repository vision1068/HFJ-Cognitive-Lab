---
name: dynamics365-mcp
description: >
  MCP server integration skill for Microsoft Dynamics 365 and Dataverse.
  Use when querying CRM data, retrieving entity records, or integrating
  Claude Code with live Dynamics 365 / Dataverse environments.
  Based on srikanth-paladugula/mcp-dynamics365-server and
  microsoft/Dataverse-MCP.
---

# Dynamics 365 MCP Integration Skill

## When to invoke
- Querying live Dynamics 365 / Dataverse data from Claude Code
- Retrieving entity metadata, records, or option sets
- Validating schema against a live environment
- Testing integration logic against real CRM data

## MCP server setup

### Dynamics 365 MCP (srikanth-paladugula)
Connects to Dynamics 365 via OData / Web API.

Configuration in `.claude/mcp-servers.json`:
```json
{
  "dynamics365": {
    "command": "npx",
    "args": ["mcp-dynamics365-server"],
    "env": {
      "D365_URL": "https://<org>.crm.dynamics.com",
      "TENANT_ID": "<azure-tenant-id>",
      "CLIENT_ID": "<app-registration-client-id>",
      "CLIENT_SECRET": "<secret>"
    }
  }
}
```

### Dataverse MCP (microsoft/Dataverse-MCP)
Direct Dataverse table access with richer metadata support.

```json
{
  "dataverse": {
    "command": "npx",
    "args": ["@microsoft/dataverse-mcp"],
    "env": {
      "DATAVERSE_URL": "https://<org>.crm.dynamics.com",
      "CLIENT_ID": "<client-id>",
      "CLIENT_SECRET": "<secret>",
      "TENANT_ID": "<tenant-id>"
    }
  }
}
```

## Security requirements
- Use dedicated service account app registration (not user credentials)
- Grant minimum required Dataverse security role to the app user
- Rotate CLIENT_SECRET every 90 days
- Never commit secrets — use environment variables or Key Vault references

## Common query patterns

### Retrieve records (OData)
```
GET /api/data/v9.2/accounts?$select=name,accountnumber&$filter=statecode eq 0&$top=50
```

### Retrieve entity metadata
```
GET /api/data/v9.2/EntityDefinitions(LogicalName='qdb_loanapplication')
```

### On-premise note
On-premise CRM does not expose OData to external MCP servers directly.
Use an API gateway or Azure Relay as an intermediary — document this
explicitly in the architecture before attempting MCP integration.

## Output format
When using this skill, return: query used, records retrieved (summarized),
and any schema gaps found. Flag on-premise connectivity blockers.
