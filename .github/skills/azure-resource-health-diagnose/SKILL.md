---
name: azure-resource-health-diagnose
description: 'Analyze Azure resource health, diagnose issues from logs and telemetry, and create a remediation plan for identified problems.'
---

# Azure Resource Health & Issue Diagnosis

Analyzes a specific Azure resource to assess health status, diagnose issues from logs and telemetry, and develop a remediation plan.

## Prerequisites
- Azure MCP server configured and authenticated
- Target Azure resource identified (name and optionally resource group/subscription)

## Workflow

### Step 1: Resource Discovery

```bash
# Find resource across subscriptions
az resource list --name <resource-name>

# Get detailed info
az resource show --ids <resource-id>
```

Resource types and their key diagnostic sources:
- **Web Apps / Function Apps**: Application logs, HTTP response codes, dependency tracking
- **Virtual Machines**: System logs, performance counters, boot diagnostics
- **Cosmos DB**: Request metrics, throttling, partition statistics
- **IoT Hub**: Device telemetry, message routing, connection events
- **Storage Accounts**: Access logs, availability, latency
- **SQL Database**: Query performance, connection logs, DTU utilization

### Step 2: Health Status Assessment

Check:
- Provisioning state and operational status
- Recent deployment or configuration changes
- Current resource utilization (CPU, memory, storage, throughput)
- Service-specific indicators (HTTP codes, connection rates, error frequency)

### Step 3: Log & Telemetry Analysis

Find Log Analytics workspaces:
```bash
azmcp-monitor-workspace-list --subscription <id>
```

Key KQL queries:
```kql
// Recent errors
union isfuzzy=true AzureDiagnostics, AppServiceHTTPLogs, AppServiceAppLogs, AzureActivity
| where TimeGenerated > ago(24h)
| where Level == "Error" or ResultType != "Success"
| summarize ErrorCount=count() by Resource, ResultType, bin(TimeGenerated, 1h)

// Performance degradation
Perf
| where TimeGenerated > ago(7d)
| where ObjectName == "Processor" and CounterName == "% Processor Time"
| summarize avg(CounterValue) by Computer, bin(TimeGenerated, 1h)
| where avg_CounterValue > 80

// IoT Hub - device connection events
AzureDiagnostics
| where ResourceProvider == "MICROSOFT.DEVICES"
| where Category == "Connections"
| summarize count() by ResultType, bin(TimeGenerated, 1h)
```

### Step 4: Issue Classification

| Severity | Examples |
|----------|---------|
| **Critical** | Service unavailable, data loss, security breach |
| **High** | Performance degradation, intermittent failures, high error rates |
| **Medium** | Warnings, suboptimal configuration, minor performance issues |
| **Low** | Informational, optimization opportunities |

Root cause categories: Configuration, Resource Constraints, Network, Application, External Dependencies, Security.

### Step 5: Remediation Plan

Generate three phases:
1. **Immediate (0–2 hours)**: Emergency fixes, temporary workarounds
2. **Short-term (2–24 hours)**: Config adjustments, patching, scaling
3. **Long-term (1–4 weeks)**: Architectural improvements, preventive measures

Each remediation step includes:
- Specific Azure CLI commands
- Testing and validation procedures
- Rollback plan
- Monitoring to verify resolution

### Step 6: Report Format

```markdown
# Azure Resource Health Report: [Resource Name]

**Overall Health**: [Healthy/Warning/Critical]

## Health Metrics
- Availability: X% over last 24h
- Error Rate: X% over last 24h
- Resource Utilization: CPU X%, Memory X%

## Issues Identified
### Critical Issues
- **[Issue]**: Root cause, impact, immediate action

## Remediation Plan
### Phase 1: Immediate (0–2 hours)
```bash
[Azure CLI commands]
```

## Monitoring Recommendations
- Alerts to configure
- Dashboard suggestions
```

## Success Criteria
- Resource health accurately assessed
- All significant issues identified and categorized
- Root cause analysis completed
- Actionable remediation plan with rollback procedures
- Monitoring and prevention recommendations included
