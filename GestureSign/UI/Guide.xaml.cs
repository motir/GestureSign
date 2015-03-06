﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using MahApps.Metro.Controls;
using MahApps.Metro;

using System.Windows.Interop;
using System.Runtime.InteropServices;


//http://www.codeproject.com/Articles/607234/Using-Windows-Interaction-Con
namespace GestureSign.UI
{
    /// <summary>
    /// Guide.xaml 的交互逻辑
    /// </summary>
    public partial class Guide : MetroWindow
    {
        internal const int

             WM_PARENTNOTIFY = 0x0210,
             WM_NCPOINTERUPDATE = 0x0241,
             WM_NCPOINTERDOWN = 0x0242,
             WM_NCPOINTERUP = 0x0243,
             WM_POINTERUPDATE = 0x0245,
             WM_POINTERDOWN = 0x0246,
             WM_POINTERUP = 0x0247,
             WM_POINTERENTER = 0x0249,
             WM_POINTERLEAVE = 0x024A,
             WM_POINTERACTIVATE = 0x024B,
             WM_POINTERCAPTURECHANGED = 0x024C,
             WM_POINTERWHEEL = 0x024E,
             WM_POINTERHWHEEL = 0x024F,

             // WM_POINTERACTIVATE return codes
             PA_ACTIVATE = 1,
             PA_NOACTIVATE = 3,

             MAX_TOUCH_COUNT = 256;
        #region POINTER_INFO
        internal enum POINTER_INPUT_TYPE
        {
            POINTER = 0x00000001,
            TOUCH = 0x00000002,
            PEN = 0x00000003,
            MOUSE = 0x00000004
        }
        [Flags]
        internal enum POINTER_FLAGS
        {
            NONE = 0x00000000,
            NEW = 0x00000001,
            INRANGE = 0x00000002,
            INCONTACT = 0x00000004,
            FIRSTBUTTON = 0x00000010,
            SECONDBUTTON = 0x00000020,
            THIRDBUTTON = 0x00000040,
            FOURTHBUTTON = 0x00000080,
            FIFTHBUTTON = 0x00000100,
            PRIMARY = 0x00002000,
            CONFIDENCE = 0x00004000,
            CANCELED = 0x00008000,
            DOWN = 0x00010000,
            UPDATE = 0x00020000,
            UP = 0x00040000,
            WHEEL = 0x00080000,
            HWHEEL = 0x00100000,
            CAPTURECHANGED = 0x00200000,
        }
        #region POINT

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            public int X;
            public int Y;
        }

        #endregion

        [Flags]
        internal enum VIRTUAL_KEY_STATES
        {
            NONE = 0x0000,
            LBUTTON = 0x0001,
            RBUTTON = 0x0002,
            SHIFT = 0x0004,
            CTRL = 0x0008,
            MBUTTON = 0x0010,
            XBUTTON1 = 0x0020,
            XBUTTON2 = 0x0040
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINTER_INFO
        {
            public POINTER_INPUT_TYPE pointerType;
            public int PointerID;
            public int FrameID;
            public POINTER_FLAGS PointerFlags;
            public IntPtr SourceDevice;
            public IntPtr WindowTarget;
            [MarshalAs(UnmanagedType.Struct)]
            public POINT PtPixelLocation;
            [MarshalAs(UnmanagedType.Struct)]
            public POINT PtPixelLocationRaw;
            [MarshalAs(UnmanagedType.Struct)]
            public POINT PtHimetricLocation;
            [MarshalAs(UnmanagedType.Struct)]
            public POINT PtHimetricLocationRaw;
            public uint Time;
            public uint HistoryCount;
            public uint InputData;
            public VIRTUAL_KEY_STATES KeyStates;
            public long PerformanceCount;
            public int ButtonChangeType;
        }

        #endregion
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetPointerFrameInfo(int pointerID, ref int pointerCount, [MarshalAs(UnmanagedType.LPArray), In, Out] POINTER_INFO[] pointerInfo);
        System.Drawing.Point touchPoint;

        public Guide()
        {
            InitializeComponent();
            this.GuideFlipView.HideControlButtons();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd).AddHook(new HwndSourceHook(WndProc));
            GestureSign.Input.TouchCapture.Instance.MessageWindow.PointsIntercepted += MessageWindow_PointsIntercepted;
        }

        void MessageWindow_PointsIntercepted(object sender, Common.Input.PointsMessageEventArgs e)
        {
            if (GestureSign.Configuration.AppConfig.XRatio == 0 && touchPoint.X != 0)
            {
                bool XAxisDirection = false, YAxisDirection = false;
                switch (System.Windows.Forms.SystemInformation.ScreenOrientation)
                {
                    case System.Windows.Forms.ScreenOrientation.Angle0:
                        XAxisDirection = YAxisDirection = true;
                        break;
                    case System.Windows.Forms.ScreenOrientation.Angle90:
                        XAxisDirection = false;
                        YAxisDirection = true;
                        break;
                    case System.Windows.Forms.ScreenOrientation.Angle180:
                        XAxisDirection = YAxisDirection = false;
                        break;
                    case System.Windows.Forms.ScreenOrientation.Angle270:
                        XAxisDirection = true;
                        YAxisDirection = false;
                        break;
                    default: break;
                }
                double rateX;
                double rateY;
                rateX = XAxisDirection ?
                    ((double)e.RawTouchsData[0].RawPointsData.X / (double)touchPoint.X) :
                    (double)e.RawTouchsData[0].RawPointsData.X / (double)(System.Windows.Forms.SystemInformation.PrimaryMonitorSize.Width - touchPoint.X);

                rateY = YAxisDirection ?
                    ((double)e.RawTouchsData[0].RawPointsData.Y) / (double)touchPoint.Y :
                    (double)e.RawTouchsData[0].RawPointsData.Y / (double)(System.Windows.Forms.SystemInformation.PrimaryMonitorSize.Height - touchPoint.Y);

                GestureSign.Configuration.AppConfig.XRatio = rateX;
                GestureSign.Configuration.AppConfig.YRatio = rateY;
                GestureSign.Configuration.AppConfig.Save();

                this.GuideFlipView.SelectedIndex = 1;
            }
        }
        internal static void CheckLastError()
        {
            int errCode = Marshal.GetLastWin32Error();
            if (errCode != 0)
            {
                throw new System.ComponentModel.Win32Exception(errCode);
            }
        }
        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_POINTERENTER:
                case WM_POINTERLEAVE:
                case WM_POINTERDOWN:
                case WM_POINTERUP:
                case WM_POINTERUPDATE:
                case WM_POINTERCAPTURECHANGED:
                    {
                        int pointerID = (int)(wParam.ToInt64() & 0xffff);
                        int pCount = 0;
                        if (!GetPointerFrameInfo(pointerID, ref pCount, null))
                        {
                            CheckLastError();
                        }
                        POINTER_INFO[] pointerInfos = new POINTER_INFO[pCount];
                        if (!GetPointerFrameInfo(pointerID, ref pCount, pointerInfos))
                        {
                            CheckLastError();
                        }
                        System.Diagnostics.Debug.WriteLine(":  " + pointerInfos[0].PtPixelLocation.X + "  " + pointerInfos[0].PtPixelLocationRaw.X);
                        touchPoint = new System.Drawing.Point(pointerInfos[0].PtPixelLocation.X, pointerInfos[0].PtPixelLocation.Y);

                        return IntPtr.Zero;
                    }
                default:
                    return IntPtr.Zero;
            }
        }


        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            UI.FormManager.Instance.MainWindow.Show();
            UI.FormManager.Instance.MainWindow.Activate();
            UI.FormManager.Instance.MainWindow.availableAction.BindActions();
        }
    }
}