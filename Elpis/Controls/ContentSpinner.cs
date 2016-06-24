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

namespace Elpis.Wpf.Controls
{
    /// <summary>
    ///     Simple control providing content spinning capability
    /// </summary>
    public class ContentSpinner : System.Windows.Controls.ContentControl
    {
        static ContentSpinner()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (ContentSpinner),
                new System.Windows.FrameworkPropertyMetadata(typeof (ContentSpinner)));
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ContentSpinner" /> class.
        /// </summary>
        public ContentSpinner()
        {
            Loaded += (o, args) =>
            {
                if (AutoStart)
                    StartAnimation();
            };
            SizeChanged += (o, args) => RestartAnimation();
            Unloaded += (o, args) => StopAnimation();
        }

        private const string Animation = "AnimatedRotateTransform";

        /// <summary>
        ///     Gets or sets the number of revolutions per second.
        /// </summary>
        public double RevolutionsPerSecond
        {
            get { return (double) GetValue(RevolutionsPerSecondProperty); }
            set { SetValue(RevolutionsPerSecondProperty, value); }
        }

        /// <summary>
        ///     Gets or sets the number of frames per rotation.
        /// </summary>
        public int NumberOfFrames
        {
            get { return (int) GetValue(NumberOfFramesProperty); }
            set { SetValue(NumberOfFramesProperty, value); }
        }

        /// <summary>
        ///     Gets or sets the content scale.
        /// </summary>
        public double ContentScale
        {
            get { return (double) GetValue(ContentScaleProperty); }
            set { SetValue(ContentScaleProperty, value); }
        }

        public bool AutoStart
        {
            get { return (bool) GetValue(AutoStartProperty); }
            set { SetValue(AutoStartProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _content = GetTemplateChild("PART_Content") as System.Windows.FrameworkElement;
        }

        public void StartAnimation()
        {
            if (_content == null)
                return;

            System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames animation = GetAnimation();

            _content.LayoutTransform = GetContentLayoutTransform();
            _content.RenderTransform = GetContentRenderTransform();

            _storyboard = new System.Windows.Media.Animation.Storyboard();
            _storyboard.Children.Add(animation);

            _storyboard.Begin(this, true);

            _running = true;
        }

        public void StopAnimation()
        {
            if (_storyboard == null) return;
            _storyboard.Stop();
            _storyboard.Remove(this);
            _storyboard = null;
            _running = false;
        }

        private void RestartAnimation()
        {
            if (!AutoStart && (AutoStart || !_running)) return;
            StopAnimation();
            StartAnimation();
        }

        private System.Windows.Media.Transform GetContentLayoutTransform()
        {
            return new System.Windows.Media.ScaleTransform(ContentScale, ContentScale);
        }

        private System.Windows.Media.Transform GetContentRenderTransform()
        {
            System.Windows.Media.RotateTransform rotateTransform = new System.Windows.Media.RotateTransform(0,
                _content.ActualWidth/2*ContentScale, _content.ActualHeight/2*ContentScale);
            RegisterName(Animation, rotateTransform);

            return rotateTransform;
        }

        private System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames GetAnimation()
        {
            System.Windows.NameScope.SetNameScope(this, new System.Windows.NameScope());

            System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames animation =
                new System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames();

            for (int i = 0; i < NumberOfFrames; i++)
            {
                double angle = i*360.0/NumberOfFrames;
                System.Windows.Media.Animation.KeyTime time =
                    System.Windows.Media.Animation.KeyTime.FromPercent((double) i/NumberOfFrames);
                System.Windows.Media.Animation.DoubleKeyFrame frame =
                    new System.Windows.Media.Animation.DiscreteDoubleKeyFrame(angle, time);
                animation.KeyFrames.Add(frame);
            }

            animation.Duration = System.TimeSpan.FromSeconds(1/RevolutionsPerSecond);
            animation.RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever;

            System.Windows.Media.Animation.Storyboard.SetTargetName(animation, Animation);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(animation,
                new System.Windows.PropertyPath(System.Windows.Media.RotateTransform.AngleProperty));

            return animation;
        }

        #region Fields

        private System.Windows.FrameworkElement _content;
        private bool _running;
        private System.Windows.Media.Animation.Storyboard _storyboard;

        #endregion

        #region Dependency properties

        public static System.Windows.DependencyProperty NumberOfFramesProperty =
            System.Windows.DependencyProperty.Register("NumberOfFrames", typeof (int), typeof (ContentSpinner),
                new System.Windows.FrameworkPropertyMetadata(16, OnPropertyChange), ValidateNumberOfFrames);

        public static System.Windows.DependencyProperty RevolutionsPerSecondProperty =
            System.Windows.DependencyProperty.Register("RevolutionsPerSecond", typeof (double), typeof (ContentSpinner),
                new System.Windows.PropertyMetadata(1.0, OnPropertyChange), ValidateRevolutionsPerSecond);

        public static System.Windows.DependencyProperty ContentScaleProperty =
            System.Windows.DependencyProperty.Register("ContentScale", typeof (double), typeof (ContentSpinner),
                new System.Windows.PropertyMetadata(1.0, OnPropertyChange), ValidateContentScale);

        public static System.Windows.DependencyProperty AutoStartProperty =
            System.Windows.DependencyProperty.Register("AutoStart", typeof (bool), typeof (ContentSpinner),
                new System.Windows.PropertyMetadata(true, OnPropertyChange));

        #endregion

        #region Validation and prop change methods

        private static bool ValidateNumberOfFrames(object value)
        {
            int frames = (int) value;
            return frames > 0;
        }

        private static bool ValidateContentScale(object value)
        {
            double scale = (double) value;
            return scale > 0.0;
        }

        private static bool ValidateRevolutionsPerSecond(object value)
        {
            double rps = (double) value;
            return rps > 0.0;
        }

        private static void OnPropertyChange(System.Windows.DependencyObject target,
            System.Windows.DependencyPropertyChangedEventArgs args)
        {
            ContentSpinner spinner = (ContentSpinner) target;
            spinner.RestartAnimation();
        }

        #endregion
    }
}