using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace Library
{
    public class GalleryCollection : List<Gallery>
    {
        public GalleryCollection() : base()
        {
        }

        public GalleryCollection(IEnumerable<Gallery> collection) : base(collection)
        {
        }

        public GalleryCollection(HtmlNodeCollection links)
        {
            SetGalleryCollection(links);
        }
        private static Regex rGalleryId = new Regex("id=([^\\&]+)");
        public void SetGalleryCollection(HtmlNodeCollection links)
        {
            foreach (HtmlNode link in links)
            {
                string url = link.GetAttributeValue("href", "");
                string name = link.InnerText;
                string id = rGalleryId.Match(url).Groups[1].Value;
                this.Add(new Gallery(name, id));
            }
        }

    }

    public class GalleryDictionary : Dictionary<string, Gallery>
    {
        public GalleryDictionary() : base()
        {
        }

        public GalleryDictionary(HtmlNodeCollection links)
        {
            SetGalleryDictionary(links);
        }
        private static Regex rGalleryId = new Regex("id=([^\\&]+)");
        private void SetGalleryDictionary(HtmlNodeCollection links)
        {
            foreach (HtmlNode link in links)
            {
                string url = link.GetAttributeValue("href", "");
                string name = Gallery.RegularName(link.InnerText);
                string id = rGalleryId.Match(url).Groups[1].Value;
                this[name] = new Gallery(name, id);
                // this.Add(name, new Gallery(name, id));
            }
        }
    }

    public class Gallery
    {
        public string gallery_name { get; set; }
        public string gallery_id { get; set; }

        public Gallery() : base()
        {
        }

        public Gallery(string name)
        {
            this.SetGallery(name, null);
        }

        public Gallery(string name, string id)
        {
            this.SetGallery(name, id);
        }

        public void SetGallery(string gallery_name, string gallery_id)
        {
            this.gallery_name = gallery_name;
            this.gallery_id = gallery_id;
        }

        private static Regex rName = new Regex("[- ]*(.+)");
        public static string RegularName(string name)
        {
            return rName.Match(name).Groups[1].Value;
        }
    }
}
