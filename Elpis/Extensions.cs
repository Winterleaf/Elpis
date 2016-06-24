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

namespace Elpis
{
    public static class DependencyObjectExtensions
    {
        public static T FindParent<T>(this System.Windows.DependencyObject child) where T : System.Windows.DependencyObject
        {
            while (true)
            {
                System.Windows.DependencyObject parent = System.Windows.LogicalTreeHelper.GetParent(child);
                if (parent == null || typeof (T) == parent.GetType()) return (T) parent;
                child = parent;
            }
        }

        public static T FindParentByName<T>(this System.Windows.DependencyObject child, string name)
            where T : System.Windows.DependencyObject
        {
            System.Windows.DependencyObject parent = System.Windows.LogicalTreeHelper.GetParent(child);
            if (parent != null &&
                (typeof (T) != parent.GetType() ||
                 ((string) parent.GetValue(System.Windows.FrameworkElement.NameProperty)).Equals(name)))
                return parent.FindParent<T>();

            return (T) parent;
        }

        public static T FindSiblingByName<T>(this System.Windows.DependencyObject sibling, string name)
            where T : System.Windows.DependencyObject
        {
            System.Windows.DependencyObject parent = System.Windows.Media.VisualTreeHelper.GetParent(sibling);

            if (parent == null) return null;

            int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                System.Windows.DependencyObject sib = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (!Equals(sib, sibling) && typeof (T) == sib.GetType() &&
                    ((string) sib.GetValue(System.Windows.FrameworkElement.NameProperty)).Equals(name))
                    return (T) sib;
            }

            return null;
        }

        public static T FindChildByName<T>(this System.Windows.DependencyObject parent, string childName)
            where T : System.Windows.DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                System.Windows.DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChildByName<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    System.Windows.FrameworkElement frameworkElement = child as System.Windows.FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement == null || frameworkElement.Name != childName) continue;
                    // if the child's name is of the request name
                    foundChild = (T) child;
                    break;
                }
                else
                {
                    // child element found.
                    foundChild = (T) child;
                    break;
                }
            }

            return foundChild;
        }
    }

    public static class DispatcherExtensions
    {
        public static TResult Dispatch<TResult>(this System.Windows.Threading.DispatcherObject source,
            System.Func<TResult> func)
        {
            if (source.Dispatcher.CheckAccess())
                return func();

            return (TResult) source.Dispatcher.Invoke(func);
        }

        public static TResult Dispatch<T, TResult>(this T source, System.Func<T, TResult> func)
            where T : System.Windows.Threading.DispatcherObject
        {
            if (source.Dispatcher.CheckAccess())
                return func(source);

            return (TResult) source.Dispatcher.Invoke(func, source);
        }

        public static TResult Dispatch<TSource, T, TResult>(this TSource source, System.Func<TSource, T, TResult> func,
            T param1) where TSource : System.Windows.Threading.DispatcherObject
        {
            if (source.Dispatcher.CheckAccess())
                return func(source, param1);

            return (TResult) source.Dispatcher.Invoke(func, source, param1);
        }

        public static TResult Dispatch<TSource, T1, T2, TResult>(this TSource source,
            System.Func<TSource, T1, T2, TResult> func, T1 param1, T2 param2)
            where TSource : System.Windows.Threading.DispatcherObject
        {
            if (source.Dispatcher.CheckAccess())
                return func(source, param1, param2);

            return (TResult) source.Dispatcher.Invoke(func, source, param1, param2);
        }

        public static TResult Dispatch<TSource, T1, T2, T3, TResult>(this TSource source,
            System.Func<TSource, T1, T2, T3, TResult> func, T1 param1, T2 param2, T3 param3)
            where TSource : System.Windows.Threading.DispatcherObject
        {
            if (source.Dispatcher.CheckAccess())
                return func(source, param1, param2, param3);

            return (TResult) source.Dispatcher.Invoke(func, source, param1, param2, param3);
        }

        public static void Dispatch(this System.Windows.Threading.DispatcherObject source, System.Action func)
        {
            if (source.Dispatcher.CheckAccess())
                func();
            else
                source.Dispatcher.Invoke(func);
        }

        public static void Dispatch<TSource>(this TSource source, System.Action<TSource> func)
            where TSource : System.Windows.Threading.DispatcherObject
        {
            if (source.Dispatcher.CheckAccess())
                func(source);
            else
                source.Dispatcher.Invoke(func, source);
        }

        public static void Dispatch<TSource, T1>(this TSource source, System.Action<TSource, T1> func, T1 param1)
            where TSource : System.Windows.Threading.DispatcherObject
        {
            if (source.Dispatcher.CheckAccess())
                func(source, param1);
            else
                source.Dispatcher.Invoke(func, source, param1);
        }

        public static void Dispatch<TSource, T1, T2>(this TSource source, System.Action<TSource, T1, T2> func, T1 param1,
            T2 param2) where TSource : System.Windows.Threading.DispatcherObject
        {
            if (source.Dispatcher.CheckAccess())
                func(source, param1, param2);
            else
                source.Dispatcher.Invoke(func, source, param1, param2);
        }

        public static void Dispatch<TSource, T1, T2, T3>(this TSource source, System.Action<TSource, T1, T2, T3> func,
            T1 param1, T2 param2, T3 param3) where TSource : System.Windows.Threading.DispatcherObject
        {
            if (source.Dispatcher.CheckAccess())
                func(source, param1, param2, param3);
            else
                source.Dispatcher.Invoke(func, source, param1, param2, param3);
        }

        //Begin Overloads
        public static void BeginDispatch<TResult>(this System.Windows.Threading.DispatcherObject source,
            System.Func<TResult> func)
        {
            if (source.Dispatcher.CheckAccess())
                func();

            source.Dispatcher.BeginInvoke(func);
        }

        public static void BeginDispatch<T, TResult>(this T source, System.Func<T, TResult> func)
            where T : System.Windows.Threading.DispatcherObject
        {
            if (source.Dispatcher.CheckAccess())
                func(source);

            source.Dispatcher.BeginInvoke(func, source);
        }

        public static void BeginDispatch<TSource, T, TResult>(this TSource source, System.Func<TSource, T, TResult> func,
            T param1) where TSource : System.Windows.Threading.DispatcherObject
        {
            if (source.Dispatcher.CheckAccess())
                func(source, param1);

            source.Dispatcher.BeginInvoke(func, source, param1);
        }

        public static void BeginDispatch<TSource, T1, T2, TResult>(this TSource source,
            System.Func<TSource, T1, T2, TResult> func, T1 param1, T2 param2)
            where TSource : System.Windows.Threading.DispatcherObject
        {
            if (source.Dispatcher.CheckAccess())
                func(source, param1, param2);

            source.Dispatcher.BeginInvoke(func, source, param1, param2);
        }

        public static void BeginDispatch<TSource, T1, T2, T3, TResult>(this TSource source,
            System.Func<TSource, T1, T2, T3, TResult> func, T1 param1, T2 param2, T3 param3)
            where TSource : System.Windows.Threading.DispatcherObject
        {
            if (source.Dispatcher.CheckAccess())
                func(source, param1, param2, param3);

            source.Dispatcher.BeginInvoke(func, source, param1, param2, param3);
        }

        public static void BeginDispatch(this System.Windows.Threading.DispatcherObject source, System.Action func)
        {
            if (source.Dispatcher.CheckAccess())
                func();
            else
                source.Dispatcher.BeginInvoke(func);
        }

        public static void BeginDispatch<TSource>(this TSource source, System.Action<TSource> func)
            where TSource : System.Windows.Threading.DispatcherObject
        {
            if (source.Dispatcher.CheckAccess())
                func(source);
            else
                source.Dispatcher.BeginInvoke(func, source);
        }

        public static void BeginDispatch<TSource, T1>(this TSource source, System.Action<TSource, T1> func, T1 param1)
            where TSource : System.Windows.Threading.DispatcherObject
        {
            if (source.Dispatcher.CheckAccess())
                func(source, param1);
            else
                source.Dispatcher.BeginInvoke(func, source, param1);
        }

        public static void BeginDispatch<TSource, T1, T2>(this TSource source, System.Action<TSource, T1, T2> func,
            T1 param1, T2 param2) where TSource : System.Windows.Threading.DispatcherObject
        {
            if (source.Dispatcher.CheckAccess())
                func(source, param1, param2);
            else
                source.Dispatcher.BeginInvoke(func, source, param1, param2);
        }

        public static void BeginDispatch<TSource, T1, T2, T3>(this TSource source,
            System.Action<TSource, T1, T2, T3> func, T1 param1, T2 param2, T3 param3)
            where TSource : System.Windows.Threading.DispatcherObject
        {
            if (source.Dispatcher.CheckAccess())
                func(source, param1, param2, param3);
            else
                source.Dispatcher.BeginInvoke(func, source, param1, param2, param3);
        }
    }
}