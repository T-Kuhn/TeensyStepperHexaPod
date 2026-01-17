using System;
using System.Runtime.InteropServices;
using SampleAppDevicePath;
using UnityEngine;

namespace MachineSimulator.UVCCamera
{
    public class EconSystemsExtensions : MonoBehaviour
    {
        public unsafe void Initialize()
        {
            var deviceCnt = 0;
            if (!DShowNativeMethods.GetDevicesCount(out deviceCnt) || deviceCnt <= 0)
            {
                Debug.Log("No devices found or GetDevicesCount failed.");
                return;
            }

            Debug.Log($"Device count: {deviceCnt}");

            char** charPath = null;
            char* deviceName = null;

            try
            {
                // 1. Allocate memory for the device name buffer (assuming 260 chars is enough)
                deviceName = (char*)Marshal.AllocCoTaskMem(260 * sizeof(char));
                if (DShowNativeMethods.GetDeviceName(deviceName))
                {
                    Debug.Log("Device Name: " + Marshal.PtrToStringAuto((IntPtr)deviceName));
                }
                else
                {
                    Debug.Log("GetDeviceName Failed..");
                }

                // 2. Allocate memory for the array of pointers
                charPath = (char**)Marshal.AllocCoTaskMem(deviceCnt * IntPtr.Size);
                for (var i = 0; i < deviceCnt; i++)
                {
                    charPath[i] = (char*)Marshal.AllocCoTaskMem(260 * sizeof(char));
                }

                // 3. Get the device paths
                if (DShowNativeMethods.GetDevicePaths(charPath))
                {
                    for (var i = 0; i < deviceCnt; i++)
                    {
                        var path = Marshal.PtrToStringAuto((IntPtr)charPath[i]);
                        Debug.Log("\t" + (i + 1) + ". " + path);

                        // Disable automatic functionality for the first device as an example, 
                        // or loop through all if needed. Here we do it for all detected devices.
                        if (DShowNativeMethods.InitExtensionUnit(charPath[i]))
                        {
                            try
                            {
                                // Disable Anti-Flicker (assuming 0 is manual/off, usually 0: Disable, 1: 50Hz, 2: 60Hz)
                                if (DShowNativeMethods.SetAntiFlickerMode24CUG(0))
                                {
                                    Debug.Log($"Successfully disabled Anti-Flicker for device {i + 1}");
                                }

                                // Disable Auto Functions Lock via Stream Mode
                                // Based on typical UVC/Extension Unit behavior, iAutoFunctionsLock = 1 might lock/disable auto adjustments
                                // We'll try to set it to 1 to lock current values or disable auto logic.
                                byte streamMode = 0; // Default or current stream mode
                                byte autoFunctionsLock = 1; // 1 to lock/disable auto functions
                                if (DShowNativeMethods.SetStreamMode24CUG(streamMode, autoFunctionsLock))
                                {
                                    Debug.Log($"Successfully locked Auto Functions for device {i + 1}");
                                }
                            }
                            finally
                            {
                                DShowNativeMethods.DeinitExtensionUnit();
                            }
                        }
                        else
                        {
                            Debug.Log($"Failed to InitExtensionUnit for device {i + 1}");
                        }
                    }
                }
                else
                {
                    Debug.Log("GetDevicePaths Failed..");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in Test: {ex.Message}");
            }
            finally
            {
                // 4. Clean up allocated memory
                if (deviceName != null)
                {
                    Marshal.FreeCoTaskMem((IntPtr)deviceName);
                }

                if (charPath != null)
                {
                    for (var i = 0; i < deviceCnt; i++)
                    {
                        if (charPath[i] != null)
                        {
                            Marshal.FreeCoTaskMem((IntPtr)charPath[i]);
                        }
                    }
                    Marshal.FreeCoTaskMem((IntPtr)charPath);
                }
            }
        }
    }
}