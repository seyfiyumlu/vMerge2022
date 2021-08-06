using System.Windows;
using System.Windows.Media;
using System.Xml;
using System.Globalization;
using System.Windows.Controls;
using System.Linq;
using System;

namespace alexbegh.Utility.SerializationHelpers
{
    /// <summary>
    /// Serializes a wpf window into a string and back
    /// </summary>
    public static class ViewSettingsSerializer
    {
        private static void SerializeElement(DependencyObject obj, XmlElement parent)
        {
            XmlElement elem = parent;
            var windowObj = obj as Window;
            var datagridObj = obj as DataGrid;
            if (windowObj != null)
            {
                elem = parent.OwnerDocument.CreateElement("window");
                elem.SetAttribute("name", windowObj.Name);
                elem.SetAttribute("posx", windowObj.Left.ToString(CultureInfo.InvariantCulture));
                elem.SetAttribute("posy", windowObj.Top.ToString(CultureInfo.InvariantCulture));
                elem.SetAttribute("width", windowObj.Width.ToString(CultureInfo.InvariantCulture));
                elem.SetAttribute("height", windowObj.Height.ToString(CultureInfo.InvariantCulture));
                elem.SetAttribute("state", windowObj.WindowState.ToString());
                parent.AppendChild(elem);
            }
            else if (datagridObj != null)
            {
                elem = parent.OwnerDocument.CreateElement("datagrid");
                elem.SetAttribute("name", datagridObj.Name);
                foreach (var col in datagridObj.Columns)
                {
                    var colElem = parent.OwnerDocument.CreateElement("column");
                    colElem.SetAttribute("idx", col.DisplayIndex.ToString(CultureInfo.InvariantCulture));
                    colElem.SetAttribute("width", col.ActualWidth.ToString(CultureInfo.InvariantCulture));
                    colElem.SetAttribute("visibility", col.Visibility.ToString());
                    elem.AppendChild(colElem);
                }
                parent.AppendChild(elem);
            }

            int childrenCount = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < childrenCount; ++i)
            {
                SerializeElement(VisualTreeHelper.GetChild(obj, i), elem);
            }
        }

        private static T Find<T>(DependencyObject root, string name) where T : FrameworkElement
        {
            var wnd = root as T;
            if (wnd != null && wnd.Name == name)
                return wnd;
            int childrenCount = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < childrenCount; ++i)
            {
                var res = Find<T>(VisualTreeHelper.GetChild(root, i), name);
                if (res != null)
                    return res;
            }
            return null;
        }

        private static void DeserializeElement(DependencyObject obj, XmlElement item, bool withWindowPosition)
        {
            var elem = obj;
            if (item.Name == "window" && withWindowPosition)
            {
                var wnd = Find<Window>(obj, item.GetAttribute("name"));
                if (wnd != null)
                {
                    if (!String.IsNullOrWhiteSpace(item.GetAttribute("posx")))
                    {
                        wnd.Left = double.Parse(item.GetAttribute("posx"), CultureInfo.InvariantCulture);
                        wnd.Top = double.Parse(item.GetAttribute("posy"), CultureInfo.InvariantCulture);
                    }
                    wnd.Width = int.Parse(item.GetAttribute("width"), CultureInfo.InvariantCulture);
                    wnd.Height = int.Parse(item.GetAttribute("height"), CultureInfo.InvariantCulture);
                    wnd.WindowState = (WindowState)Enum.Parse(typeof(WindowState), item.GetAttribute("state"));
                    obj = wnd;
                }
            }
            else if (item.Name == "datagrid")
            {
                var datagrid = Find<DataGrid>(obj, item.GetAttribute("name"));
                if (datagrid == null && System.Diagnostics.Debugger.IsAttached)
                    System.Diagnostics.Debugger.Break();
                if (datagrid != null && datagrid.Columns != null)
                {
                    foreach (var col in datagrid.Columns)
                    {
                        var colElem = item.SelectSingleNode("column[@idx=" + col.DisplayIndex + "]") as XmlElement;
                        if (colElem != null)
                        {
                            col.Width = double.Parse(colElem.GetAttribute("width"), CultureInfo.InvariantCulture);
                            col.Visibility = (Visibility)Enum.Parse(typeof(Visibility), colElem.GetAttribute("visibility"));
                        }
                    }
                }
                return;
            }

            foreach (var childElem in item.ChildNodes.OfType<XmlElement>())
            {
                DeserializeElement(elem, childElem, withWindowPosition);
            }
        }

        /// <summary>
        /// Extension method for a wpf window. Serializes settings of that window into a string.
        /// </summary>
        /// <param name="window">The window to serialize settings</param>
        /// <returns>Window settings as string</returns>
        public static string SerializeToString(this DependencyObject window)
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement("viewsettings");
            doc.AppendChild(root);
            SerializeElement(window, root);
            return doc.DocumentElement.OuterXml;
        }

        /// <summary>
        /// Extension method for a wpf window. Deserializes settings from a string into that window.
        /// </summary>
        /// <param name="window">The window to deserialize into.</param>
        /// <param name="data">Window settings as string</param>
        /// <param name="withWindowPosition">true if also the window position should be deserialized</param>
        public static void DeserializeFromString(this DependencyObject window, string data, bool withWindowPosition = true)
        {
            if (data == null)
                return;

            var doc = new XmlDocument();
            doc.LoadXml(data);
            DeserializeElement(window, doc.DocumentElement, withWindowPosition);
        }
    }
}
