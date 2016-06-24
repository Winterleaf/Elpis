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
    public class TextBlockUtils
    {
        public static readonly System.Windows.DependencyProperty AutoTooltipProperty =
            System.Windows.DependencyProperty.RegisterAttached("AutoTooltip", typeof (bool), typeof (TextBlockUtils),
                new System.Windows.PropertyMetadata(false, OnAutoTooltipPropertyChanged));

        public static readonly System.Windows.DependencyProperty AutoTooltipFontSizeProperty =
            System.Windows.DependencyProperty.RegisterAttached("AutoTooltipFontSize", typeof (double),
                typeof (TextBlockUtils), new System.Windows.PropertyMetadata(0.0, OnAutoTooltipFontSizePropertyChanged));

        public static bool GetAutoTooltip(System.Windows.DependencyObject obj)
        {
            return (bool) obj.GetValue(AutoTooltipProperty);
        }

        public static void SetAutoTooltip(System.Windows.DependencyObject obj, bool value)
        {
            obj.SetValue(AutoTooltipProperty, value);
        }

        private static void OnAutoTooltipPropertyChanged(System.Windows.DependencyObject d,
            System.Windows.DependencyPropertyChangedEventArgs e)
        {
            System.Windows.Controls.TextBlock textBlock = (System.Windows.Controls.TextBlock) d;
            if (textBlock == null)
                return;

            if (e.NewValue.Equals(true))
            {
                textBlock.TextTrimming = System.Windows.TextTrimming.CharacterEllipsis;
                ComputeAutoTooltip(textBlock);
                textBlock.SizeChanged += TextBlock_SizeChanged;
            }
            else
            {
                textBlock.SizeChanged -= TextBlock_SizeChanged;
            }
        }

        public static double GetAutoTooltipFontSize(System.Windows.DependencyObject obj)
        {
            double result = (double) obj.GetValue(AutoTooltipFontSizeProperty);
            return result.Equals(0.0) ? ((System.Windows.Controls.TextBlock) obj).FontSize : result;
        }

        public static void SetAutoTooltipFontSize(System.Windows.DependencyObject obj, double value)
        {
            if (value.Equals(0.0))
                value = ((System.Windows.Controls.TextBlock) obj).FontSize;
            obj.SetValue(AutoTooltipFontSizeProperty, value);
        }

        private static void OnAutoTooltipFontSizePropertyChanged(System.Windows.DependencyObject d,
            System.Windows.DependencyPropertyChangedEventArgs e) {}

        private static void TextBlock_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            System.Windows.Controls.TextBlock textBlock = (System.Windows.Controls.TextBlock) sender;
            ComputeAutoTooltip(textBlock);
        }

        private static void ComputeAutoTooltip(System.Windows.Controls.TextBlock textBlock)
        {
            textBlock.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            double width = textBlock.DesiredSize.Width;

            if (textBlock.ActualWidth < width)
            {
                System.Windows.Controls.TextBlock toolBlock = new System.Windows.Controls.TextBlock
                {
                    Text = textBlock.Text,
                    FontSize = GetAutoTooltipFontSize(textBlock)
                };
                System.Windows.Controls.ToolTipService.SetToolTip(textBlock, toolBlock);
            }
            else
            {
                System.Windows.Controls.ToolTipService.SetToolTip(textBlock, null);
            }
        }
    }
}