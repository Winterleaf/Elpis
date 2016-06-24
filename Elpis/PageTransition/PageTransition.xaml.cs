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

namespace Elpis.Wpf.PageTransition
{
    public partial class PageTransition
    {
        public PageTransition()
        {
            InitializeComponent();
            ClipToBounds = true;
        }

        //Stack<UserControl> pages = new Stack<UserControl>();

        #region Delegates

        public delegate void CurrentPageSetHandler(System.Windows.Controls.UserControl page);

        #endregion

        public static readonly System.Windows.DependencyProperty TransitionTypeProperty =
            System.Windows.DependencyProperty.Register("TransitionType", typeof(PageTransitionType),
                typeof(PageTransition), new System.Windows.PropertyMetadata(PageTransitionType.Next));

        public bool InTransition
        {
            get
            {
                lock (_inTransitionLock)
                {
                    return _inTransition;
                }
            }
            private set
            {
                lock (_inTransitionLock)
                {
                    _inTransition = value;
                }
            }
        }

        [System.ComponentModel.DefaultValue(null)]
        public System.Windows.Controls.UserControl CurrentPage { get; set; }

        public System.Collections.Generic.List<System.Windows.Controls.UserControl> PageList { get; } =
            new System.Collections.Generic.List<System.Windows.Controls.UserControl>();

        public PageTransitionType TransitionType
        {
            get { return (PageTransitionType)GetValue(TransitionTypeProperty); }
            set { SetValue(TransitionTypeProperty, value); }
        }

        private readonly object _inTransitionLock = new object();

        private System.Windows.Controls.ContentControl _currentContent;
        private bool _inTransition;
        private System.Windows.Controls.UserControl _loadingPage;

        private System.Windows.Controls.UserControl _nextPage;
        private PageTransitionType _nextTrasitionType = PageTransitionType.Auto;

        public event CurrentPageSetHandler CurrentPageSet;

        public void AddPage(System.Windows.Controls.UserControl newPage, int index = -1)
        {
            if (index < 0)
                PageList.Add(newPage);
            else
            {
                if (index > PageList.Count - 1)
                    index = PageList.Count;

                PageList.Insert(index, newPage);
            }

            //Task.Factory.StartNew(() => ShowNewPage());
        }

        public void RemovePage(System.Windows.Controls.UserControl opage)
        {
            if (PageList.Contains(opage))
                PageList.Remove(opage);
        }

        public void ShowPage(System.Windows.Controls.UserControl opage, PageTransitionType type = PageTransitionType.Auto)
        {
            if (Equals(opage, CurrentPage))
                return;

            System.Threading.Tasks.Task.Factory.StartNew(
                () => Dispatcher.Invoke(new System.Action(() => LoadPage(opage, type))));
        }

        public void ShowNextPage()
        {
            //System.Windows.Controls.UserControl page = null;
            //if (CurrentPage == null)
            //    page = PageList[0];

            int i = PageList.IndexOf(CurrentPage);
            int next = i == PageList.Count - 1 ? 0 : i + 1;

            ShowPage(PageList[next]);
        }

        public void ShowPrevPage()
        {
            //System.Windows.Controls.UserControl page = null;
            //if (CurrentPage == null)
            //    page = PageList[PageList.Count - 1];

            int i = PageList.IndexOf(CurrentPage);
            int prev = i == 0 ? PageList.Count - 1 : i - 1;

            ShowPage(PageList[prev]);
        }

        private void LoadPage(System.Windows.Controls.UserControl opage, PageTransitionType type)
        {
            if (opage == null)
                return;

            //If already in a trasition, save it for next time
            //Overwrite to skip if it's already filled
            if (InTransition)
            {
                _nextPage = opage;
                _nextTrasitionType = type;
                return;
            }

            int i = PageList.IndexOf(opage);
            if (i < 0)
                return;

            if (type == PageTransitionType.Auto)
            {
                if (CurrentPage == null)
                    type = PageTransitionType.Next;
                else
                {
                    int c = PageList.IndexOf(CurrentPage);
                    if (c == PageList.Count - 1 && i == 0)
                        type = PageTransitionType.Next;
                    else if (c == 0 && i == PageList.Count - 1)
                        type = PageTransitionType.Previous;
                    else
                        type = c < i ? PageTransitionType.Next : PageTransitionType.Previous;
                }
            }

            _nextPage = null;
            _nextTrasitionType = PageTransitionType.Auto;

            InTransition = true;

            _loadingPage = opage;
            TransitionType = type;

            _loadingPage.Loaded += newPage_Loaded;

            //switch active content control
            _currentContent = _currentContent == contentA ? contentB : contentA;

            _currentContent.Content = _loadingPage;
        }

        private void newPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (TransitionType == PageTransitionType.Auto)
                return;

            _loadingPage.Loaded -= newPage_Loaded;

            System.Windows.Controls.ContentControl oldContent = Equals(_currentContent, contentA) ? contentB : contentA;

            _currentContent.Visibility = System.Windows.Visibility.Visible;
            //_oldContent.Visibility = System.Windows.Visibility.Hidden;

            if (CurrentPage != null)
            {
                System.Windows.Media.Animation.Storyboard hidePage =
                    (Resources[$"{TransitionType}Out"] as System.Windows.Media.Animation.Storyboard)?.Clone();
                System.Windows.Thickness to =
                    (System.Windows.Thickness)
                        ((System.Windows.Media.Animation.ThicknessAnimation)hidePage?.Children[0]).To;
                ((System.Windows.Media.Animation.ThicknessAnimation)hidePage?.Children[0]).To =
                    new System.Windows.Thickness(to.Left * ActualWidth, to.Top * ActualHeight, to.Right * ActualWidth,
                        to.Bottom * ActualHeight);

                hidePage.Completed += hidePage_Completed;

                System.Windows.Media.Animation.Storyboard showNewPage =
                    (Resources[$"{TransitionType}In"] as System.Windows.Media.Animation.Storyboard)?.Clone();
                System.Windows.Thickness from =
                    (System.Windows.Thickness)
                        ((System.Windows.Media.Animation.ThicknessAnimation)showNewPage?.Children[0]).From;
                ((System.Windows.Media.Animation.ThicknessAnimation)showNewPage.Children[0]).From =
                    new System.Windows.Thickness(from.Left * ActualWidth, from.Top * ActualHeight, from.Right * ActualWidth,
                        from.Bottom * ActualHeight);

                showNewPage.Completed += showNewPage_Completed;

                if (CurrentPage != null)
                    hidePage.Begin(oldContent);
                showNewPage.Begin(_currentContent);
            }
            else
                InTransition = false;

            CurrentPage = (System.Windows.Controls.UserControl)sender;
        }

        private void showNewPage_Completed(object sender, System.EventArgs e)
        {
            InTransition = false;

            CurrentPageSet?.Invoke(CurrentPage);

            if (_nextPage != null)
                LoadPage(_nextPage, _nextTrasitionType);
        }

        private void hidePage_Completed(object sender, System.EventArgs e)
        {
            if (Equals(_currentContent, contentA))
            {
                contentB.Visibility = System.Windows.Visibility.Hidden;
                contentB.Content = null;
            }
            else
            {
                contentA.Visibility = System.Windows.Visibility.Hidden;
                contentA.Content = null;
            }
        }
    }
}