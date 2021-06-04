using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup.Primitives;
using System.Windows.Media;

namespace alexbegh.Utility.Helpers.WPFBindings
{
    /// <summary>
    /// This class helps finding bindings for a given DependencyObject
    /// </summary>
    public static class DependencyObjectHelper
    {
        /// <summary>
        /// Returns the first ancestor of a given Framework element which
        /// type is "Type".
        /// </summary>
        /// <typeparam name="Type">The type to search for</typeparam>
        /// <param name="frameworkElement">The framework element to begin the search with</param>
        /// <returns>null if not found, otherwise the first matching ancestor</returns>
        public static Type FindAncestor<Type>(DependencyObject frameworkElement) where Type: class
        {
            DependencyObject item = VisualTreeHelper.GetParent(frameworkElement);
            while (item != null && item is DependencyObject)
            {
                if (item is Type)
                    return item as Type;
                item = VisualTreeHelper.GetParent(item);
            }
            return null;
        }

        /// <summary>
        /// Returns the first ancestor of a given Framework element which
        /// type is "Type".
        /// </summary>
        /// <typeparam name="Type">The type to search for</typeparam>
        /// <param name="frameworkElement">The framework element to begin the search with</param>
        /// <returns>null if not found, otherwise the first matching ancestor</returns>
        public static Type FindAncestorOrSelf<Type>(DependencyObject frameworkElement) where Type : class
        {
            DependencyObject item = frameworkElement;
            while (item != null && item is DependencyObject)
            {
                if (item is Type)
                    return item as Type;
                item = VisualTreeHelper.GetParent(item);
            }
            return null;
        }

        /// <summary>
        /// Finds the first visual child of a given dependency object
        /// which type is "childItem"
        /// </summary>
        /// <typeparam name="childItem">The type to search for</typeparam>
        /// <param name="obj">The root object to search from</param>
        /// <returns>The item if found, null otherwise</returns>
        public static childItem FindVisualChild<childItem>(DependencyObject obj)
           where childItem : DependencyObject
        {
            // Search immediate children first (breadth-first)
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);

                if (child != null && child is childItem)
                    return (childItem)child;

                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);

                    if (childOfChild != null)
                        return childOfChild;
                }
            }

            return null;
        }        /// <summary>
        /// Find all bindings recursively for a given DependencyObject
        /// </summary>
        /// <param name="dObj">The object to check</param>
        /// <param name="bindingList">The bindings</param>
        public static void GetBindingsRecursive(DependencyObject dObj, List<BindingBase> bindingList)
        {
            bindingList.AddRange(GetBindingObjects(dObj));

            int childrenCount = VisualTreeHelper.GetChildrenCount(dObj);
            if (childrenCount > 0)
            {
                for (int i = 0; i < childrenCount; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(dObj, i);
                    GetBindingsRecursive(child, bindingList);
                }
            }
        }

        /// <summary>
        /// Returns all direct bindings for a given object
        /// </summary>
        /// <param name="element">The object to check</param>
        /// <returns>List of bindings</returns>
        public static List<BindingBase> GetBindingObjects(Object element)
        {
            List<BindingBase> bindings = new List<BindingBase>();
            List<DependencyProperty> dpList = new List<DependencyProperty>();
            dpList.AddRange(GetDependencyProperties(element));
            dpList.AddRange(GetAttachedProperties(element));

            foreach (DependencyProperty dp in dpList)
            {
                BindingBase b = BindingOperations.GetBindingBase(element as DependencyObject, dp);
                if (b != null)
                {
                    bindings.Add(b);
                }
            }

            return bindings;
        }

        /// <summary>
        /// Gets all dependency properties for a given object
        /// </summary>
        /// <param name="element">The object to check</param>
        /// <returns>List of DependencyProperties</returns>
        public static List<DependencyProperty> GetDependencyProperties(Object element)
        {
            List<DependencyProperty> properties = new List<DependencyProperty>();
            MarkupObject markupObject = MarkupWriter.GetMarkupObjectFor(element);
            if (markupObject != null)
            {
                foreach (MarkupProperty mp in markupObject.Properties)
                {
                    if (mp.DependencyProperty != null)
                    {
                        properties.Add(mp.DependencyProperty);
                    }
                }
            }

            return properties;
        }

        /// <summary>
        /// Gets all attached properties for a given object
        /// </summary>
        /// <param name="element">The object to check</param>
        /// <returns>List of attached properties</returns>
        public static List<DependencyProperty> GetAttachedProperties(Object element)
        {
            List<DependencyProperty> attachedProperties = new List<DependencyProperty>();
            MarkupObject markupObject = MarkupWriter.GetMarkupObjectFor(element);
            if (markupObject != null)
            {
                foreach (MarkupProperty mp in markupObject.Properties)
                {
                    if (mp.IsAttached)
                    {
                        attachedProperties.Add(mp.DependencyProperty);
                    }
                }
            }

            return attachedProperties;
        }
    }
}