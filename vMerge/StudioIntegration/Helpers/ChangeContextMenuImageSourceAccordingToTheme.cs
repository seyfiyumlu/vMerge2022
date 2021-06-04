using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace alexbegh.vMerge.StudioIntegration.Helpers
{
    public static class ChangeContextMenuImageSourceAccordingToTheme
    {
        public static void Process(ContextMenu ctx)
        {
            foreach (var item in ctx.Items)
            {
                var menuItem = item as MenuItem;
                if (menuItem != null)
                {
                    Image itemImage = menuItem.Icon as Image;
                    if (itemImage != null)
                    {
                        string source = itemImage.Source.ToString();
                        if (source != null && source.EndsWith(".png"))
                        {
                            bool isDark = source.EndsWith("d.png");
                            string raw = source.Substring(0, source.Length - (isDark ? 5 : 4));
                            if (vMerge.StudioIntegration.Framework.vMergePackage.IsDarkTheme)
                            {
                                source = raw + "d.png";
                            }
                            else
                            {
                                source = raw + ".png";
                            }
                            itemImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(source));
                        }
                    }
                }
            }
        }
    }
}
