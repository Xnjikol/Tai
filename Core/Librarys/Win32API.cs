﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Core.Librarys
{
    public static class Win32API
    {
        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType,
          IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr
          hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess,
          uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowThreadProcessId(IntPtr hwnd, out int ID);

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern int GetModuleFileNameExA(IntPtr hProcess, IntPtr hModule, StringBuilder lpFilename, int nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(UIntPtr hObject);



        internal struct WINDOWINFO
        {
            public uint ownerpid;
            public uint childpid;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowProc lpEnumFunc, IntPtr lParam);



        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] int dwFlags, [Out] StringBuilder lpExeName, ref int lpdwSize);


        public delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);

        public const UInt32 PROCESS_QUERY_INFORMATION = 0x400;
        public const UInt32 PROCESS_VM_READ = 0x010;

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        public static string UWP_AppName(IntPtr hWnd, uint pID)
        {
            WINDOWINFO windowinfo = new WINDOWINFO();
            windowinfo.ownerpid = pID;
            windowinfo.childpid = windowinfo.ownerpid;

            IntPtr pWindowinfo = Marshal.AllocHGlobal(Marshal.SizeOf(windowinfo));

            Marshal.StructureToPtr(windowinfo, pWindowinfo, false);

            EnumWindowProc lpEnumFunc = new EnumWindowProc(EnumChildWindowsCallback);
            EnumChildWindows(hWnd, lpEnumFunc, pWindowinfo);

            windowinfo = (WINDOWINFO)Marshal.PtrToStructure(pWindowinfo, typeof(WINDOWINFO));

            IntPtr proc;
            if ((proc = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, (int)windowinfo.childpid)) == IntPtr.Zero) return null;

            int capacity = 2000;
            StringBuilder sb = new StringBuilder(capacity);
            QueryFullProcessImageName(proc, 0, sb, ref capacity);

            Marshal.FreeHGlobal(pWindowinfo);

            return sb.ToString(0, capacity);
        }

        private static bool EnumChildWindowsCallback(IntPtr hWnd, IntPtr lParam)
        {
            WINDOWINFO info = (WINDOWINFO)Marshal.PtrToStructure(lParam, typeof(WINDOWINFO));

            uint pID;
            GetWindowThreadProcessId(hWnd, out pID);

            if (pID != info.ownerpid) info.childpid = pID;

            Marshal.StructureToPtr(info, lParam, true);

            return true;
        }


        /// <summary>
        /// 获取鼠标坐标
        /// </summary>
        /// <param name="lpPoint"></param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out Point lpPoint);


        #region 声音判断
        /// <summary>
        /// 指示系统当前是否在播放声音
        /// </summary>
        /// <returns></returns>
        public static bool IsWindowsPlayingSound()
        {
            try
            {
                IMMDeviceEnumerator enumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                IMMDevice speakers = enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
                IAudioMeterInformation meter = (IAudioMeterInformation)speakers.Activate(typeof(IAudioMeterInformation).GUID, 0, IntPtr.Zero);
                if (meter != null)
                {

                    float value = meter.GetPeakValue();

                    // this is a bit tricky. 0 is the official "no sound" value
                    // but for example, if you open a video and plays/stops with it (w/o killing the app/window/stream),
                    // the value will not be zero, but something really small (around 1E-09)
                    // so, depending on your context, it is up to you to decide
                    // if you want to test for 0 or for a small value
                    return value > 1E-08;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ec)
            {
                Logger.Error(ec.Message);
                return false;
            }
        }

        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumerator
        {
        }

        private enum EDataFlow
        {
            eRender,
            eCapture,
            eAll,
        }

        private enum ERole
        {
            eConsole,
            eMultimedia,
            eCommunications,
        }

        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
        private interface IMMDeviceEnumerator
        {
            void NotNeeded();
            IMMDevice GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role);
            // the rest is not defined/needed
        }

        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("D666063F-1587-4E43-81F1-B948E807363F")]
        private interface IMMDevice
        {
            [return: MarshalAs(UnmanagedType.IUnknown)]
            object Activate([MarshalAs(UnmanagedType.LPStruct)] Guid iid, int dwClsCtx, IntPtr pActivationParams);
            // the rest is not defined/needed
        }

        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064")]
        private interface IAudioMeterInformation
        {
            float GetPeakValue();
            // the rest is not defined/needed
        }
        #endregion
    }
}