---
name: az-cost-optimize
description: 'Analyze Azure resources used in the app (IaC files and/or resources in a target rg) and optimize costs - creating GitHub issues for identified optimizations.'
---

# Azure Cost Optimize

Analyzes Infrastructure-as-Code files and Azure resources to generate cost optimization recommendations. Creates individual GitHub issues per optimization plus one EPIC coordinating issue.

## Prerequisites
- Azure MCP server configured and authenticated
- GitHub MCP server configured and authenticated
- Target GitHub repository identified
- IaC files present (Bicep, Terraform, ARM JSON)

## Workflow

### Step 1: Get Azure Best Practices
Execute `azmcp-bestpractices-get` to get the latest Azure optimization guidelines.

### Step 2: Discover Azure Infrastructure
```bash
# List subscriptions
azmcp-subscription-list

# List resource groups
azmcp-group-list --subscription <subscription-id>

# List all resources in a group
az resource list --subscription <id> --resource-group <name>
```

Also scan IaC files: `**/*.bicep`, `**/*.tf`, `**/main.json`. Only use IaC files — not other source files.

### Step 3: Collect Usage Metrics

Use `azmcp-monitor-log-query` with KQL:
```kql
// CPU utilization
AppServiceAppLogs
| where TimeGenerated > ago(7d)
| summarize avg(CpuTime) by Resource, bin(TimeGenerated, 1h)

// Cosmos DB RU consumption
AzureDiagnostics
| where ResourceProvider == "MICROSOFT.DOCUMENTDB"
| summarize avg(RequestCharge) by Resource
```

### Step 4: Generate Recommendations

**Compute**: Right-size App Service Plans, move low-usage Function Apps to Consumption
**Database**: Cosmos DB Provisioned → Serverless for variable workloads; right-size SQL DTUs
**Storage**: Lifecycle policies (Hot → Cool → Archive), consolidate accounts
**Infrastructure**: Remove unused resources, implement auto-scaling

Priority Score = (Value Score × Monthly Savings) / (Risk Score × Implementation Days)
- High Priority: Score > 20
- Medium Priority: Score 5–20

### Step 5: User Confirmation

Present summary before creating issues:
```
🎯 Azure Cost Optimization Summary
• Total Resources Analyzed: X
• Current Monthly Cost: $X
• Potential Monthly Savings: $Y
• Optimization Opportunities: Z
❓ Proceed with creating GitHub issues? (y/n)
```

### Step 6: Create GitHub Issues

**Individual issue title**: `[COST-OPT] [Resource Type] - [Description] - $X/month savings`

Each issue body includes:
- Monthly savings estimate
- Current vs. target configuration
- Azure CLI commands for implementation
- IaC changes if files were found
- Evidence from monitoring data
- Risk and validation steps

### Step 7: Create EPIC Issue

**Title**: `[EPIC] Azure Cost Optimization Initiative - $X/month potential savings`

Includes:
- Architecture diagram (Mermaid)
- Implementation tracking checklist linking all individual issues
- Prioritized phases
- Success criteria

## Error Handling

- **No IaC files found**: STOP and report to user
- **Authentication failure**: Provide Azure CLI setup steps
- **Insufficient usage data**: Note limitations, provide configuration-based recommendations only
- **GitHub creation failure**: Output formatted recommendations to console
