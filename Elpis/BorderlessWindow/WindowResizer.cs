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

using Enumerable = System.Linq.Enumerable;

namespace Elpis.Wpf.BorderlessWindow
{
    /// <summary>
    ///     Determines the position of a window border.
    /// </summary>
    public enum BorderPosition
    {
        Left = 61441,
        Right = 61442,
        Top = 61443,
        TopLeft = 61444,
        TopRight = 61445,
        Bottom = 61446,
        BottomLeft = 61447,
        BottomRight = 61448
    }

    /// <summary>
    ///     Represents a Framework element which is acting as a border for a window.
    /// </summary>
    public class WindowBorder
    {
        /// <summary>
        ///     Creates a new window border using the specified element and position.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="element"></param>
        public WindowBorder(BorderPosition position, System.Windows.FrameworkElement element)
        {
            if (element == null)
            {
                throw new System.ArgumentNullException(nameof(element));
            }

            Position = position;
            Element = element;
        }

        /// <summary>
        ///     The element which is acting as the border.
        /// </summary>
        public System.Windows.FrameworkElement Element { get; }

        /// <summary>
        ///     The position of the border.
        /// </summary>
        public BorderPosition Position { get; }
    }

    /// <summary>
    ///     Class which manages resizing of borderless windows.
    ///     Based heavily on Kirupa Chinnathambi's code at http://blog.kirupa.com/?p=256.
    /// </summary>
    public class WindowResizer
    {
        /// <summary>
        ///     Creates a new WindowResizer for the specified Window using the
        ///     specified border elements.
        /// </summary>
        /// <param name="window">The Window which should be resized.</param>
        /// <param name="borders">The elements which can be used to resize the window.</param>
        public WindowResizer(System.Windows.Window window, params WindowBorder[] borders)
        {
            if (window == null)
            {
                throw new System.ArgumentNullException(nameof(window));
            }
            if (borders == null)
            {
                throw new System.ArgumentNullException(nameof(borders));
            }

            _window = window;
            _borders = borders;

            foreach (WindowBorder border in borders)
            {
                border.Element.PreviewMouseLeftButtonDown += Resize;
                border.Element.MouseMove += DisplayResizeCursor;
                border.Element.MouseLeave += ResetCursor;
            }

            _win32 = new Win32(window);
            window.SourceInitialized +=
                (o, e) =>
                    _hwndSource =
                        (System.Windows.Interop.HwndSource)
                            System.Windows.PresentationSource.FromVisual((System.Windows.Media.Visual) o);
            window.SourceInitialized += (o, e) => _win32.SourceInitialized(window);
            // window.SizeChanged += (o, e) => ConfineMinMax();
        }

        private readonly Win32 _win32;

        /// <summary>
        ///     The borders for the window.
        /// </summary>
        private readonly WindowBorder[] _borders;

        /// <summary>
        ///     Defines the cursors that should be used when the mouse is hovering
        ///     over a border in each position.
        /// </summary>
        private readonly System.Collections.Generic.Dictionary<BorderPosition, System.Windows.Input.Cursor> _cursors =
            new System.Collections.Generic.Dictionary<BorderPosition, System.Windows.Input.Cursor>
            {
                {BorderPosition.Left, System.Windows.Input.Cursors.SizeWE},
                {BorderPosition.Right, System.Windows.Input.Cursors.SizeWE},
                {BorderPosition.Top, System.Windows.Input.Cursors.SizeNS},
                {BorderPosition.Bottom, System.Windows.Input.Cursors.SizeNS},
                {BorderPosition.BottomLeft, System.Windows.Input.Cursors.SizeNESW},
                {BorderPosition.TopRight, System.Windows.Input.Cursors.SizeNESW},
                {BorderPosition.BottomRight, System.Windows.Input.Cursors.SizeNWSE},
                {BorderPosition.TopLeft, System.Windows.Input.Cursors.SizeNWSE}
            };

        /// <summary>
        ///     The WPF window.
        /// </summary>
        private readonly System.Windows.Window _window;

        /// <summary>
        ///     The handle to the window.
        /// </summary>
        private System.Windows.Interop.HwndSource _hwndSource;

        //private void ConfineMinMax()
        //{
        //    if (_window.ActualHeight <= _window.MinHeight)
        //        _window.Height = _window.MinHeight;
        //    if (_window.ActualHeight >= _window.MaxHeight)
        //        _window.Height = _window.MaxHeight;

        //    if (_window.ActualWidth <= _window.MinWidth)
        //        _window.Width = _window.MinWidth;
        //    if (_window.ActualWidth >= _window.MaxWidth)
        //        _window.Width = _window.MaxWidth;
        //}

        /// <summary>
        ///     Puts a resize message on the message queue for the specified border position.
        /// </summary>
        /// <param name="direction"></param>
        private void ResizeWindow(BorderPosition direction)
        {
            _win32.SendWMMessage(_hwndSource.Handle, 0x112, (System.IntPtr) direction, System.IntPtr.Zero);
        }

        /// <summary>
        ///     Resets the cursor when the left mouse button is not pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetCursor(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (System.Windows.Input.Mouse.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
            {
                _window.Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        /// <summary>
        ///     Resizes the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Resize(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            WindowBorder border = Enumerable.Single(_borders, b => b.Element.Equals(sender));
            _window.Cursor = _cursors[border.Position];
            ResizeWindow(border.Position);
        }

        /// <summary>
        ///     Ensures that the correct cursor is displayed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayResizeCursor(object sender, System.Windows.Input.MouseEventArgs e)
        {
            WindowBorder border = Enumerable.Single(_borders, b => b.Element.Equals(sender));
            _window.Cursor = _cursors[border.Position];
        }
    }
}