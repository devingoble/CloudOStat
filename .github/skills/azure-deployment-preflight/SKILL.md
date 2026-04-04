---
name: azure-deployment-preflight
description: 'Performs comprehensive preflight validation of Bicep deployments to Azure, including template syntax validation, what-if analysis, and permission checks. Use this skill before any deployment to Azure to preview changes, identify potential issues, and ensure the deployment will succeed. Activate when users mention deploying to Azure, validating Bicep files, checking deployment permissions, previewing infrastructure changes, running what-if, or preparing for azd provision.'
---

# Azure Deployment Preflight Validation

Validates Bicep deployments before execution, supporting both Azure CLI (`az`) and Azure Developer CLI (`azd`) workflows.

## When to Use This Skill

- Before deploying infrastructure to Azure
- When preparing or reviewing Bicep files
- To preview what changes a deployment will make
- To verify permissions are sufficient for deployment
- Before running `azd up`, `azd provision`, or `az deployment` commands

## Validation Process

Follow these steps in order. Continue even if a previous step fails — capture all issues in the final report.

### Step 1: Detect Project Type

1. **Check for azd project**: Look for `azure.yaml` in the project root
   - Found → Use **azd workflow**
   - Not found → Use **az CLI workflow**

2. **Locate Bicep files**: Find all `.bicep` files
   - For azd: Check `infra/` directory first
   - For standalone: Check `infra/`, `deploy/`, project root

3. **Auto-detect parameter files**:
   - `<filename>.bicepparam` (preferred)
   - `<filename>.parameters.json`

### Step 2: Validate Bicep Syntax

```bash
bicep build <bicep-file> --stdout
```

Capture syntax errors with line/column numbers and warnings.

### Step 3: Run Preflight Validation

**For azd Projects:**
```bash
azd provision --preview
azd provision --preview --environment <env-name>
```

**For Standalone Bicep** (scope from `targetScope` declaration):
```bash
# Resource Group (default)
az deployment group what-if \
  --resource-group <rg-name> \
  --template-file <bicep-file> \
  --parameters <param-file> \
  --validation-level Provider

# Subscription scope
az deployment sub what-if \
  --location <location> \
  --template-file <bicep-file> \
  --validation-level Provider
```

**Fallback** (if RBAC permission errors):
```bash
az deployment group what-if \
  --resource-group <rg-name> \
  --template-file <bicep-file> \
  --validation-level ProviderNoRbac
```

### Step 4: Categorize What-If Results

| Symbol | Change Type | Meaning |
|--------|-------------|---------|
| `+` | Create | New resource will be created |
| `-` | Delete | Resource will be deleted |
| `~` | Modify | Resource properties will change |
| `=` | NoChange | Resource unchanged |
| `!` | Deploy | Changes unknown |

### Step 5: Generate Report

Create `preflight-report.md` in the project root with:

1. **Summary** — Overall status, timestamp, files validated
2. **Tools Executed** — Commands run with versions
3. **Issues** — All errors and warnings with severity
4. **What-If Results** — Resources to create/modify/delete
5. **Recommendations** — Actionable next steps

## Error Handling

| Error Type | Action |
|------------|--------|
| Not logged in | Suggest `az login` or `azd auth login` |
| Permission denied | Fall back to `ProviderNoRbac`, note in report |
| Bicep syntax error | Include all errors, continue |
| Tool not installed | Note in report, skip that step |
| Resource group not found | Suggest creating it |

## Tool Requirements

```bash
az --version        # Azure CLI 2.76.0+ recommended
azd version         # For azd projects
bicep --version     # For syntax validation
```
