# Device Control UI Testing Guide

## Overview
This document provides instructions for testing the device control UI that was added to the CloudOStat MAUI and Blazor applications, enabling remote control of the Meadow device's configuration through Azure IoT Hub device twin properties.

## Prerequisites

### For MAUI App Testing:
1. CloudOStat.App (MAUI) built and running on a device or emulator
2. IoT Hub credentials configured in the app
3. Meadow device connected and working
4. Device credentials stored in AppSettings:
   ```csharp
   AppSettings.IotHubName = "usw-iot-cloudostat";
   AppSettings.DeviceId = "cloudostat-meadow";
   AppSettings.SharedAccessKey = "your-shared-access-key";
   ```

### For Blazor WASM Testing:
1. CloudOStat.App.Web (backend) running locally or deployed
2. CloudOStat.App.Web.Client (WASM) accessible
3. Backend configured with IoT Hub credentials in appsettings.json:
   ```json
   {
     "IoTHub": {
       "HubName": "usw-iot-cloudostat",
       "DeviceId": "cloudostat-meadow",
       "SharedAccessKey": "your-shared-access-key"
     }
   }
   ```
4. Backend API endpoints accessible:
   - `GET /api/device/status`
   - `POST /api/device/twin/desired`

## Test Scenarios

### Test 1: View Current Device Status (UI Load)
**Objective:** Verify the DeviceControl page loads and displays current device status

**Steps:**
1. Start MAUI app or navigate to `/device-control` in Blazor
2. Observe the page loading (loading bar should appear briefly)
3. Verify the following information displays:
   - Air Temperature
   - Meat Probe 1 Temperature
   - Meat Probe 2 Temperature
   - Device Status (Heating, Over, On Temp, etc.)
   - Last Updated timestamp
   - Current telemetry interval in seconds

**Expected Result:**
- Page loads without errors
- All device temperatures display with 2 decimal places and °F unit
- Status shows the actual device state
- Last update timestamp is recent (within the last telemetry interval)

### Test 2: Adjust Telemetry Interval with Slider
**Objective:** Test the slider control for updating telemetry interval

**Steps:**
1. On the DeviceControl page, locate the "Set New Interval" slider
2. Verify slider range is 5-300 seconds (validation message shows)
3. Move slider to different value (e.g., from 20 to 30 seconds)
4. Observe the value display updates as you drag
5. Click "Update Interval" button
6. Watch for success toast notification
7. Observe "Current interval" updates to new value

**Expected Result:**
- Slider responds smoothly to user input
- Value display updates in real-time
- Success notification appears: "✓ Telemetry interval updated to 30 seconds"
- Current interval in the card updates to reflect new value
- Device receives and applies the change (verify in Meadow console logs)

### Test 3: Invalid Interval Values (Validation)
**Objective:** Test that validation prevents out-of-range values

**Steps:**
1. Manually change the slider to a value outside the valid range (if possible)
2. Try to update with value < 5 or > 300:
   - MAUI: Modify AppSettings before pushing update
   - Blazor: Manually change form value in browser dev tools
3. Click Update button
4. Observe error notification

**Expected Result:**
- Warning toast appears: "Invalid interval. Must be between 5 and 300 seconds."
- No update is sent to device
- Current interval remains unchanged

### Test 4: Reported Properties Update
**Objective:** Verify reported properties sync back to UI

**Steps:**
1. After updating interval to (e.g., 15 seconds), wait for next device report
2. Click "Refresh Status" button
3. Verify "Current telemetry interval" matches the requested change
4. In Azure Portal Device Twin, confirm:
   - `desired.telemetry_interval_seconds` = 15
   - `reported.telemetry_interval_seconds` = 15

**Expected Result:**
- Refreshed status shows updated interval
- Azure Portal shows both desired and reported in sync
- Device console shows "Updating telemetry interval from Xms to 15000ms"

### Test 5: Error Handling - Network Failure
**Objective:** Test error handling when device is unreachable

**Steps:**
1. Disconnect network or shut down Meadow device
2. Try to update telemetry interval
3. Wait for request timeout (~10 seconds)
4. Observe error notification

**Expected Result:**
- Error toast appears describing the failure
- UI doesn't freeze during timeout
- "Update Interval" button becomes enabled again
- Can retry once network is restored

### Test 6: Error Handling - Invalid Credentials
**Objective:** Test error handling with bad authentication

**Steps:**
1. Update AppSettings or appsettings.json with invalid SharedAccessKey
2. Click "Update Interval" or "Refresh Status"
3. Wait for response

**Expected Result:**
- Error notification appears indicating authentication failure
- Status code error (401 Unauthorized) or similar
- UI gracefully handles the error

### Test 7: Loading States
**Objective:** Verify loading indicators work correctly

**Steps:**
1. On DeviceControl page, watch for page load indicator
2. Click "Refresh Status" and observe refresh behavior
3. Click "Update Interval" and observe loading state
   - Progress circle should appear in button
   - Button should be disabled during update
4. Let operation complete

**Expected Result:**
- Loading indicators appear and disappear appropriately
- Buttons are disabled during operations
- No double-submits occur (button properly disabled)

### Test 8: Multiple Sequential Updates
**Objective:** Test multiple updates in quick succession

**Steps:**
1. Change interval to 10 seconds, click Update
2. Wait for success notification
3. Immediately change to 30 seconds, click Update
4. Verify both operations complete successfully
5. Final value should be 30 seconds
6. Verify in Azure Portal

**Expected Result:**
- Both updates succeed
- No conflicts or errors
- Final state is correct (30 seconds)
- Device applies the most recent value

### Test 9: Refresh After Update
**Objective:** Test that refresh properly syncs UI with device

**Steps:**
1. Update telemetry interval to new value
2. Immediately click "Refresh Status"
3. Verify the "Current interval" updates to new value
4. Wait for a device telemetry cycle (in seconds)
5. Click "Refresh Status" again
6. Verify "Last Updated" timestamp is recent

**Expected Result:**
- Refresh loads latest device status from twin
- Current interval matches what was set
- Last updated time is within one telemetry cycle
- All temperatures refresh with latest values

### Test 10: End-to-End MAUI vs Blazor
**Objective:** Verify device control works from both platforms

**Steps:**
1. Update interval from MAUI app to 25 seconds
2. In Blazor WASM, navigate to Device Control
3. Refresh and verify it shows 25 seconds
4. Update from Blazor to 35 seconds
5. Go back to MAUI, refresh
6. Verify it shows 35 seconds

**Expected Result:**
- Both platforms can update the device
- Changes sync across platforms
- Device receives all updates correctly
- Azure Portal shows correct desired/reported state

## Troubleshooting

### Symptom: "Unable to load device status" Error
**Possible Causes:**
- IoT Hub credentials not configured or invalid
- Meadow device not connected to IoT Hub
- Network connectivity issues
- API endpoint not running (Blazor backend)

**Solutions:**
1. Verify credentials in AppSettings / appsettings.json
2. Check Meadow device is connected (display shows "Connected")
3. In browser dev tools, check if API request is successful
4. Verify backend is running and accessible
5. Check Azure IoT Hub diagnostics

### Symptom: Update Succeeds But Device Doesn't Change
**Possible Causes:**
- Device not subscribed to desired properties
- Device validation rejecting value as out of range
- Device not processing desired property updates

**Solutions:**
1. Check Meadow console logs for "Received desired properties"
2. Verify value is between 5-300 seconds
3. Verify device is still authenticated
4. Check for any error messages in device logs
5. May need to restart Meadow device

### Symptom: Slider Only Shows "0 seconds"
**Possible Cause:**
- Device status not loaded or reported properties missing

**Solution:**
1. Check Meadow device is sending reported properties
2. In Azure Portal, verify device twin has reported properties
3. Click "Refresh Status" to reload from device

### Symptom: Toast Notifications Not Appearing
**Possible Cause:**
- MudBlazor Snackbar not properly initialized

**Solution:**
1. Check browser console for JavaScript errors
2. Verify MudServices.AddMudServices() is called in Program.cs
3. Verify page has @inject ISnackbar directive

## Performance Notes

- Initial status load: 500-2000ms (depends on network)
- Slider update: 1-3 seconds to device
- Refresh: 500-2000ms per request
- Device processes changes within ~1 second
- Reported properties sent every telemetry cycle (default 20s)

## Security Considerations

- **MAUI:** Uses SAS tokens directly (acceptable for native app)
- **Blazor WASM:** Calls backend API (credentials never exposed to browser)
- **Backend:** Uses SAS tokens from configuration, properly scoped to device
- All API calls use HTTPS
- SAS tokens expire and refresh automatically

## Next Steps

After successful testing:
1. Deploy backend to production Azure App Service (if using Blazor)
2. Update app configuration for production credentials
3. Monitor logs for device control operations
4. Gather user feedback on UI/UX
5. Consider adding more device properties for control

## Support

For issues or questions:
- Check device console logs
- Check backend API logs
- Review Azure IoT Hub diagnostics
- See [DEVICE_TWIN_TESTING.md](../CloudOStat.Meadow/DEVICE_TWIN_TESTING.md) for device-side testing
