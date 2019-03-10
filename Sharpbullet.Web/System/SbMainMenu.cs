using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SharpBullet.Web.System
{
    public class SbMainMenu
    {
        private SbMainMenuConfiguration configuration;

        public SbMainMenuConfiguration Configuration
        {
            get { if (configuration == null) configuration = new SbMainMenuConfiguration(); return configuration; }
            set { configuration = value; }
        }

        private List<SbMainMenuItem> menuItems;

        public List<SbMainMenuItem> MenuItems
        {
            get
            {
                if (menuItems == null)
                {
                    menuItems = new List<SbMainMenuItem>();
                    if (Configuration.AutoGenerate) AutoGenerateMenuItems(menuItems);
                }
                return menuItems;
            }
            set { menuItems = value; }
        }

        private void AutoGenerateMenuItems(List<SbMainMenuItem> menuItems)
        {
            var context = HttpContext.Current;
            if (context == null) return;

            var root = context.Server.MapPath("/");
            var files = Directory.GetFiles(root, "*.aspx", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                string physicalFile = file.Replace('\\', '/').Substring(root.Length);
                string routeUrl = physicalFile.Substring(0, physicalFile.Length - 5);

                var parts = routeUrl.Split('/');
                var menuIterator = menuItems;
                for (int i = 0; i < parts.Length; i++)
                {
                    var item = menuIterator.FirstOrDefault(x => x.Text == parts[i]);
                    if (item == null)
                    {
                        item = new SbMainMenuItem()
                        {
                            Text = parts[i]
                        };
                        if (i+1 == parts.Length) item.Link = routeUrl;

                        menuIterator.Add(item);
                    }
                    menuIterator = item.SubMenus;
                }
            }
        }
    }

    public class SbMainMenuConfiguration : SbConfiguration
    {
        public bool AutoGenerate { get; set; }




        public SbMainMenuConfiguration()
        {
            AutoGenerate = true;
        }
    }

    public class SbMainMenuItem
    {
        private List<SbMainMenuItem> subMenus;

        public List<SbMainMenuItem> SubMenus
        {
            get { if (subMenus == null) subMenus = new List<SbMainMenuItem>(); return subMenus; }
            set { subMenus = value; }
        }

        public string Link { get; set; }

        public string Text { get; set; }
    }
}