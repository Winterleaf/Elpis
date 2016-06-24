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

namespace Elpis.Controls
{
    public class ImageButton : System.Windows.Controls.Button
    {
        public ImageButton()
        {
            //Do not want any of these buttons to respond to Space select, do this for all
            PreviewKeyDown += (o, e) => e.Handled = true;
        }

        public static readonly System.Windows.DependencyProperty ActiveImageUriProperty =
            System.Windows.DependencyProperty.RegisterAttached("ActiveImageUri", typeof (System.Uri),
                typeof (ImageButton), new System.Windows.PropertyMetadata(null));

        public static readonly System.Windows.DependencyProperty InactiveImageUriProperty =
            System.Windows.DependencyProperty.RegisterAttached("InactiveImageUri", typeof (System.Uri),
                typeof (ImageButton), new System.Windows.PropertyMetadata(null));

        public static readonly System.Windows.DependencyProperty IsActiveProperty =
            System.Windows.DependencyProperty.RegisterAttached("IsActive", typeof (bool), typeof (ImageButton),
                new System.Windows.PropertyMetadata(false));

        public System.Uri ActiveImageUri
        {
            get { return (System.Uri) GetValue(ActiveImageUriProperty); }
            set { SetValue(ActiveImageUriProperty, value); }
        }

        public System.Uri InactiveImageUri
        {
            get { return (System.Uri) GetValue(InactiveImageUriProperty); }
            set { SetValue(InactiveImageUriProperty, value); }
        }

        public bool IsActive
        {
            get { return (bool) GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }
    }
}