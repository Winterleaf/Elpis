﻿/*
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

namespace Elpis.Wpf.Pages
{
    /// <summary>
    ///     Interaction logic for LoadingPage.xaml
    /// </summary>
    public partial class LoadingPage
    {
        public LoadingPage()
        {
            InitializeComponent();
        }

        public void UpdateStatus(string msg)
        {
            this.BeginDispatch(() => lblStatus.Text = msg);
        }
    }
}