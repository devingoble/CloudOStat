# Device Twin Testing Guide

## Overview
This document provides step-by-step instructions for testing the Azure IoT Hub device twin implementation in CloudOStat.Meadow.

## Prerequisites
- Meadow device deployed with updated CloudOStat.LocalHardware code
- Azure IoT Hub and device configured
- Azure CLI or Azure Portal access to manage device twin
- Device successfully connecting to Azure IoT Hub (verify "Connected" message on display)

## What Was Implemented

### 1. Reported Properties (Device → Cloud)
The device now sends the following reported properties to Azure:
- `air_temperature` - Current air temperature reading (°F)
- `meat1_temperature` - Current meat probe 1 temperature (°F)
- `meat2_temperature` - Current meat probe 2 temperature (°F)
- `device_status` - Current device state (Heating, Over, On Temp)
- `telemetry_interval_seconds` - Current telemetry reporting interval
- `last_update` - Timestamp of last update

Reported properties are sent every time the batch telemetry is sent (default: 20 seconds).

### 2. Desired Properties (Cloud → Device)
You can set the following desired property:
- `telemetry_interval_seconds` - Target telemetry reporting interval (5-300 seconds)

When desired properties are received, the device validates the interval and updates immediately.

## Testing Steps

### Test 1: Verify Reported Properties are Sent
**Objective:** Confirm the device sends reported properties to Azure

1. Deploy the code to Meadow device
2. Verify device displays "Connected" on LCD
3. In Azure Portal:
   - Navigate to your IoT Hub
   - Click "Devices" → select your device
   - Click "Device Twin"
   - Look for `reported` properties containing temperature and status values
4. Wait a few IoT Hub send cycles (default 20 seconds) and refresh
5. **Expected Result:** Properties should show current sensor readings and status

### Test 2: Update Desired Property via Azure CLI
**Objective:** Test updating telemetry interval from cloud to device

1. Open PowerShell/Terminal

2. Update desired telemetry interval to 30 seconds:
   ```powershell
   az iot hub device-twin update `
     --hub-name YOUR_HUB_NAME `
     --device-id YOUR_DEVICE_ID `
     --set properties.desired.telemetry_interval_seconds=30
   ```

3. Watch the Meadow device display - you should see:
   - A message showing "Interval updated to 30s"
   - Console logs indicating the interval change

4. Verify the change in Azure Portal:
   - Device Twin view should show `telemetry_interval_seconds: 30` in both desired and reported
   - Reported should update within the next telemetry cycle

5. **Expected Result:** Device updates interval and logs the change

### Test 3: Update Desired Property via Azure Portal
**Objective:** Verify UI-based desired property updates work

1. In Azure Portal:
   - Navigate to Device Twin for your device
   - Click the Edit icon
   - In the `desired` section, add or modify:
     ```json
     {
       "telemetry_interval_seconds": 45,
       "$metadata": { ... },
       "$version": ...
     }
     ```

2. Click "Save"

3. Watch the device display and console output for the interval change

4. **Expected Result:** Device should update to 45 second interval

### Test 4: Test Invalid Interval Values
**Objective:** Verify validation rejects out-of-range values

1. Attempt to set an invalid interval via CLI:
   ```powershell
   az iot hub device-twin update `
     --hub-name YOUR_HUB_NAME `
     --device-id YOUR_DEVICE_ID `
     --set properties.desired.telemetry_interval_seconds=2
   ```

2. Watch the device console output

3. **Expected Result:** Device should log a warning about rejecting the invalid value (must be 5-300 seconds), and the interval should remain unchanged. The desired property will be sent to the device, but not applied.

### Test 5: Verify Reported Properties Update Interval
**Objective:** Confirm reported properties reflect the active interval

1. Set desired interval to 15 seconds via Azure CLI (Test 2 method)
2. Wait for device to apply the change (< 5 seconds typically)
3. In Azure Portal Device Twin, check `reported.telemetry_interval_seconds`
4. **Expected Result:** Should show 15, matching the active interval

## Troubleshooting

### Device Not Receiving Desired Properties
**Symptoms:** Interval changes don't apply on device

**Checks:**
- Verify device is connected (display shows "Connected")
- Check device console for subscription error messages
- Verify Azure credentials in `Secrets.cs`
- Check Azure IoT Hub diagnostics logs

**Solution:**
```powershell
# View device diagnostics
az iot hub monitor-events --hub-name YOUR_HUB_NAME --device-id YOUR_DEVICE_ID
```

### Reported Properties Not Showing in Azure
**Symptoms:** Azure Portal Device Twin shows no reported properties

**Checks:**
- Wait at least one telemetry interval (default 20s) after device connection
- Verify device logs show "REPORTED PROPERTIES SENT" messages
- Check if device is authenticated (_iotHubController.isAuthenticated = true)

**Solution:**
1. Check device console logs for UpdateReportedPropertiesAsync errors
2. Verify MQTT connection is stable
3. Review Azure IoT Hub activity/diagnostics

### Invalid Interval Error Keeps Appearing
**Symptoms:** Device rejects valid interval values

**Cause:** Validation range is 5-300 seconds

**Solution:**
- Use values between 5 and 300 (inclusive)
- Example valid values: 10, 30, 60, 120, 300

## Monitoring Device Twin Activity

### Via Azure CLI
```powershell
# Get current device twin
az iot hub device-twin show `
  --hub-name YOUR_HUB_NAME `
  --device-id YOUR_DEVICE_ID

# Monitor twin changes
az iot hub monitor-events `
  --hub-name YOUR_HUB_NAME `
  --device-id YOUR_DEVICE_ID
```

### Via Device Console
The device logs all device twin operations:
- "Subscribing to device twin desired properties topic..."
- "Received desired properties: {...}"
- "Updating telemetry interval from Xms to Yms"
- "Updating reported properties: {...}"
- "REPORTED PROPERTIES SENT"

Watch for errors like:
- "Failed to subscribe to desired properties"
- "Failed to parse desired properties"
- "Rejecting invalid telemetry interval"

## Notes
- Device validates telemetry interval: must be between 5 and 300 seconds
- Reported properties are sent synchronously with batch telemetry
- Desired property updates are processed within ~1 second of receipt
- All timestamps in reported properties use UTC format (ISO 8601)
- Version tracking is automatic via Azure IoT Hub
