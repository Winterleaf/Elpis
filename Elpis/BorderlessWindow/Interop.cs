/*
 * Copyright 2012 - Adam Haile
 * http://adamhaile.net
 *
 * This file is part of Elpis.
 * Elpis is free software: you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation, either version 3 of the License, or 
 * (at your option) any later version.
 * 
 * Elpis is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License 
 * along with Elpis. If not, see http://www.gnu.org/licenses/.
*/

namespace Elpis.Wpf.BorderlessWindow
{

    #region structs

    /// <summary>
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential,
        CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    public class Monitorinfo
    {
        /// <summary>
        /// </summary>
        public int cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof (Monitorinfo));

        /// <summary>
        /// </summary>
        public int dwFlags;

        /// <summary>
        /// </summary>
        public Rect rcMonitor;

        /// <summary>
        /// </summary>
        public Rect rcWork;
    }

    /// <summary> Win32 </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 0)]
    public struct Rect
    {
        /// <summary> Win32 </summary>
        public readonly int left;

        /// <summary> Win32 </summary>
        public readonly int top;

        /// <summary> Win32 </summary>
        public readonly int right;

        /// <summary> Win32 </summary>
        public int bottom;

        /// <summary> Win32 </summary>
        public static readonly Rect Empty;

        /// <summary> Win32 </summary>
        public int Width => System.Math.Abs(right - left);

        /// <summary> Win32 </summary>
        public int Height => bottom - top;

        /// <summary> Win32 </summary>
        public Rect(int left, int top, int right, int bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }

        /// <summary> Win32 </summary>
        public Rect(Rect rcSrc)
        {
            left = rcSrc.left;
            top = rcSrc.top;
            right = rcSrc.right;
            bottom = rcSrc.bottom;
        }

        /// <summary> Win32 </summary>
        public bool IsEmpty => left >= right || top >= bottom;

        /// <summary> Return a user friendly representation of this struct </summary>
        public override string ToString()
        {
            if (this == Empty)
            {
                return "RECT {Empty}";
            }
            return "RECT { left : " + left + " / top : " + top + " / right : " + right + " / bottom : " + bottom + " }";
        }

        /// <summary> Determine if 2 RECT are equal (deep compare) </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is Rect))
            {
                return false;
            }
            return this == (Rect) obj;
        }

        /// <summary>Return the HashCode for this struct (not garanteed to be unique)</summary>
        public override int GetHashCode()
        {
            return left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
        }

        /// <summary> Determine if 2 RECT are equal (deep compare)</summary>
        public static bool operator ==(Rect rect1, Rect rect2)
        {
            return rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right &&
                   rect1.bottom == rect2.bottom;
        }

        /// <summary> Determine if 2 RECT are different(deep compare)</summary>
        public static bool operator !=(Rect rect1, Rect rect2)
        {
            return !(rect1 == rect2);
        }
    }

    /// <summary>
    ///     POINT aka POINTAPI
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct POINT
    {
        /// <summary>
        ///     x coordinate of point.
        /// </summary>
        public int x;

        /// <summary>
        ///     y coordinate of point.
        /// </summary>
        public int y;

        /// <summary>
        ///     Construct a point of coordinates (x,y).
        /// </summary>
        public POINT(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    #endregion

    public class Win32
    {
        public Win32(System.Windows.Window win)
        {
            _window = win;
        }

        public Win32() {}

        private readonly System.Windows.Window _window;
        
        private POINT _maxTrack;
        private POINT _minTrack;

        [System.Runtime.InteropServices.DllImport("user32")]
        internal static extern bool GetMonitorInfo(System.IntPtr hMonitor, Monitorinfo lpmi);

        [System.Runtime.InteropServices.DllImport("User32")]
        internal static extern System.IntPtr MonitorFromWindow(System.IntPtr handle, int flags);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern System.IntPtr SendMessage(System.IntPtr hWnd, uint Msg, System.IntPtr wParam,
            System.IntPtr lParam);

        public System.IntPtr SendWMMessage(System.IntPtr hWnd, uint Msg, System.IntPtr wParam, System.IntPtr lParam)
        {
            return SendMessage(hWnd, Msg, wParam, lParam);
        }

        public System.IntPtr WindowProc(System.IntPtr hwnd, int msg, System.IntPtr wParam, System.IntPtr lParam,
            ref bool handled)
        {
            switch (msg)
            {
                case 0x0024:
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }

            return (System.IntPtr) 0;
        }

        public void WmGetMinMaxInfo(System.IntPtr hwnd, System.IntPtr lParam)
        {
            MINMAXINFO mmi =
                (MINMAXINFO) System.Runtime.InteropServices.Marshal.PtrToStructure(lParam, typeof (MINMAXINFO));

            // Adjust the maximized size and position to fit the work area of the correct monitor
            const int MONITOR_DEFAULTTONEAREST = 0x00000002;
            System.IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != System.IntPtr.Zero)
            {
                
                    _maxTrack = mmi.ptMaxTrackSize;
                    _minTrack = mmi.ptMinTrackSize;
                

                Monitorinfo monitorInfo = new Monitorinfo();
                GetMonitorInfo(monitor, monitorInfo);
                Rect rcWorkArea = monitorInfo.rcWork;
                Rect rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = System.Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = System.Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.x = System.Math.Abs(rcWorkArea.right - rcWorkArea.left);
                mmi.ptMaxSize.y = System.Math.Abs(rcWorkArea.bottom - rcWorkArea.top);

                if (_window != null)
                {
                    if (_window.WindowState == System.Windows.WindowState.Maximized)
                    {
                        mmi.ptMaxTrackSize = _maxTrack;
                        mmi.ptMinTrackSize = _minTrack;
                    }
                    else
                    {
                        // Bug Fix #13: Translate WPF scaled coordinates to screen coordinates.
                        System.Windows.Point windowMaxDimens =
                            WpfToScreenPixels(new System.Windows.Point(_window.MaxWidth, _window.MaxHeight));
                        System.Windows.Point windowMinDimens =
                            WpfToScreenPixels(new System.Windows.Point(_window.MinWidth, _window.MinHeight));

                        mmi.ptMaxTrackSize.x = (int) windowMaxDimens.X;
                        mmi.ptMaxTrackSize.y = (int) windowMaxDimens.Y;
                        mmi.ptMinTrackSize.x = (int) windowMinDimens.X;
                        mmi.ptMinTrackSize.y = (int) windowMinDimens.Y;
                    }
                }
            }

            System.Runtime.InteropServices.Marshal.StructureToPtr(mmi, lParam, true);
        }

        public void SourceInitialized(System.Windows.Window source)
        {
            System.IntPtr handle = new System.Windows.Interop.WindowInteropHelper(source).Handle;
            System.Windows.Interop.HwndSource.FromHwnd(handle).AddHook(WindowProc);
        }

        /// <summary>
        ///     Transforms a WPF Point to screen point accounting for UI scaling.
        ///     See: http://stackoverflow.com/questions/6931333/wpf-converting-between-screen-coordinates-and-wpf-coordinates
        /// </summary>
        /// <param name="p">The point to transform.</param>
        /// <returns>The transformed point.</returns>
        private System.Windows.Point WpfToScreenPixels(System.Windows.Point p)
        {
            System.Windows.Media.Matrix t =
                System.Windows.PresentationSource.FromVisual(_window).CompositionTarget.TransformFromDevice;
            t.Invert();
            return t.Transform(p);
        }
    }
}