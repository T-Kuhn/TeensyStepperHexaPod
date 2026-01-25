using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace SampleAppDevicePath
{
    class DShowNativeMethods
    {

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetDevicesCount(out int Cnt);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool GetDevicePaths(char** DevicePath);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool GetDeviceName(char* DevicePath);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool InitExtensionUnit(char* InstanceID);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool DeinitExtensionUnit();

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ReadFirmwareVersion(out int pMajorVersion, out int pMinorVersion1, out int pMinorVersion2,out int pMinorVersion3);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool GetCameraUniqueID(char* szUniqueID);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool GetSpecialEffects24CUG(ref byte uEffectsMode);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool SetSpecialEffects24CUG(byte uEffectsMode);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool GetDenoiseValue24CUG(ref byte uDenoiseValue);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool SetDenoiseValue24CUG(byte uDenoiseValue);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool GetExpRoiMode24CUG(ref byte uExpRoiMode, ref byte uXPos, ref byte uYPos, ref byte uWindowSize);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool SetExpRoiMode24CUG(byte uExpRoiMode, byte uXPos, byte uYPos, byte uWindowSize);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool GetQFactor24CUG(ref byte uQFactorVal);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool SetQFactor24CUG(byte uQFactorVal);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool RestoreDefault24CUG();

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool GetFlipMode24CUG(ref byte uFlipMode);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool SetFlipMode24CUG(byte uFlipMode);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool GetFaceDetectionRect24CUG(ref byte uState, ref byte uStatusStructState, ref byte uOverlayRectState);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool SetFaceDetectionRect24CUG( byte uState,  byte uStatusStructState, byte uOverlayRectState);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool GetExposureCompensation24CUG(ref int iExposureComp);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool SetExposureCompensation24CUG(int iExposureComp);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool GetFrameRateValue24CUG(ref byte uFrameRateVal);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool SetFrameRateValue24CUG(byte uFrameRateVal);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool GetStrobeMode24CUG(ref byte uFlashValue);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool SetStrobeMode24CUG(byte uFlashValue);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool GetAntiFlickerMode24CUG(ref byte iFlickerMode);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool SetAntiFlickerMode24CUG(byte iFlickerMode);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool GetStreamMode24CUG(ref byte iStreamMode, ref byte iAutoFunctionsLock);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool SetStreamMode24CUG( byte iStreamMode,  byte iAutoFunctionsLock);

        [DllImport("eCAMFwSw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ResetDevice();
    }
}
