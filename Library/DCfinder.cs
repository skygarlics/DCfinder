using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;

namespace Library
{
    public class DCfinder
    {
        private static HtmlDocument parser = new HtmlDocument();

        public DCfinder()
        {
            this.Proxy = WebRequest.DefaultWebProxy;
        }
        public IWebProxy Proxy { get; set; }

        /////////////////////////////////////

        #region properties
        const string gall_base_url = "http://gall.dcinside.com";
        #endregion

        public delegate void ArticleDele(ArticleCollection articles);

        #region GetSearchPos
        public static uint GetSearchPos(string gallery_id, string keyword, string search_type)
        {
            return GetSearchPosAsync(gallery_id, keyword, search_type).Result;
        }

        private static Regex rSearchPos = new Regex("search_pos=-(\\d*)");

        public static async Task<uint> GetSearchPosAsync(string gallery_id, string keyword, string search_type)
        {
            HtmlNodeCollection links;

            string board_url = gall_base_url + "/board/lists/?id=" + gallery_id;
            string search_query = String.Format("&s_type={0}&s_keyword={1}", search_type, keyword);
            string resp = await GetPageAsync(board_url + search_query);

            parser.LoadHtml(resp);
            try
            {
                HtmlNode page_btns = parser.DocumentNode.SelectSingleNode("//div[@id='dgn_btn_paging']");
                links = page_btns.SelectNodes("./a");
            }
            catch (NullReferenceException)
            {
                return 0;
            }
            string next_search_url = links[links.Count - 1].OuterHtml;
            return Convert.ToUInt32(rSearchPos.Match(next_search_url).Groups[1].Value) + 10000;
        }
        #endregion

        #region CrawlSearch
        public static ArticleCollection CrawlSearch(string gallery_id, string keyword, string search_type, uint search_pos, bool recommend)
        {
            return CrawlSearchAsync(gallery_id, keyword, search_type, search_pos, recommend).Result;
        }

        public static async Task<ArticleCollection> CrawlSearchAsync(string gallery_id, string keyword, string search_type, uint search_pos, bool recommend)
        {
            string search_query = "&page={0}&search_pos=-{1}&s_type={2}&s_keyword={3}";
            if (recommend)
            {
                search_query += "&exception_mode=recommend";
            }
            string board_url = gall_base_url + "/board/lists/?id=" + gallery_id;

            // get page length of this search
            string request_url = board_url + String.Format(search_query, 1, search_pos, search_type, keyword);
            string html = await GetPageAsync(request_url);
            parser.LoadHtml(html);
            HtmlNode page_btns = parser.DocumentNode.SelectSingleNode("//div[@id='dgn_btn_paging']");
            int page_len = 1;
            if (CountNextBtn(page_btns.OuterHtml) > 1)
            {
                // board length > 10
                page_len = CountPages(page_btns);
            }
            else
            {
                page_len = CountPages(page_btns.OuterHtml);
            }

            // get articles of page1, which already loaded
            ArticleCollection articles = new ArticleCollection(html);

            for (int page_idx = 2; page_idx < page_len + 1; page_idx++)
            {
                request_url = board_url + String.Format(search_query, page_idx, search_pos, search_type, keyword);
                html = await GetPageAsync(request_url);
                articles.AddRange(new ArticleCollection(html)); // with LIst<T>
                // articles.Concat(new ArticleCollection(html)); // with ObservableCollection<T>
            }


            return articles;
        }
        #endregion

        #region GetPage
        public static string GetPage(string url)
        {
            Task<string> resp = GetPageAsync(url);
            return resp.Result;
        }

        public static async Task<string> GetPageAsync(string url)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.ContentType = "application/x-www-form-urlencoded";
            req.Method = "GET";
            req.Referer = gall_base_url;
            WebResponse response = await req.GetResponseAsync();
            return StreamFromResponse(response);
        }
        #endregion

        private static string StreamFromResponse(WebResponse resp)
        {
            using (Stream responseStream = resp.GetResponseStream())
            using (StreamReader sr = new StreamReader(responseStream))
            {
                //Need to return this response 
                string strContent = sr.ReadToEnd();
                return strContent;
            }
        }

        #region GetArticles
        public ArticleCollection GetArticles(string html)
        {
            parser.LoadHtml(html);
            return GetArticles(parser);
        }

        public ArticleCollection GetArticles(HtmlDocument parser)
        {
            HtmlNode tbody = parser.DocumentNode.SelectSingleNode("//tbody[@class=\"list_tbody\"");
            HtmlNodeCollection trs = tbody.SelectNodes("//tr");
            return new ArticleCollection(trs);
        }
        #endregion

        #region CountPages
        private static Regex rLink = new Regex("<a");

        private static int CountPages(string page_btns)
        {
            parser.LoadHtml(page_btns);
            return CountPages(parser);
        }

        private static int CountPages(HtmlDocument parser)
        {
            return parser.DocumentNode.SelectNodes("//a").Count - CountPrevBtn(parser) - CountNextBtn(parser);
        }

        private static int CountPages(HtmlNode page_btns)
        {
            return page_btns.SelectNodes("./a").Count - CountPrevBtn(page_btns) - CountNextBtn(page_btns);
        }

        private static int CountPrevBtn(string page_btns)
        {
            parser.LoadHtml(page_btns);
            return CountPrevBtn(parser);
        }

        private static int CountPrevBtn(HtmlDocument parser)
        {
            var nodes = parser.DocumentNode.SelectNodes("//a[@class='b_prev']");
            if (nodes != null)
                return nodes.Count;
            else
                return 0;
        }

        private static int CountPrevBtn(HtmlNode page_btns)
        {
            var nodes = page_btns.SelectNodes("./a[@class='b_prev']");
            if (nodes != null)
                return nodes.Count;
            else
                return 0;
        }

        private static int CountNextBtn(string page_btns)
        {
            parser.LoadHtml(page_btns);
            return CountNextBtn(parser);
        }

        private static int CountNextBtn(HtmlDocument parser)
        {
            var nodes = parser.DocumentNode.SelectNodes("//a[@class=\"b_next\"]");
            if (nodes != null)
                return nodes.Count;
            else
                return 0;
        }

        private static int CountNextBtn(HtmlNode page_btns)
        {
            var nodes = page_btns.SelectNodes("./a[@class='b_next']");
            if (nodes != null)
                return nodes.Count;
            else
                return 0;
        }
        #endregion

        ///////////////////////////////////////

        private static GalleryDictionary galleries;

        public static GalleryDictionary GetGalleries()
        {
            if (galleries != null)
            {
                return galleries;
            }
            galleries = new GalleryDictionary();

            string html = GetPage("http://wstatic.dcinside.com/gallery/gallindex_iframe_new_gallery.html");
            parser.LoadHtml(html);

            HtmlNodeCollection list_title = parser.DocumentNode.SelectNodes("//a[@class='list_title']");
            var list_dic = new GalleryDictionary(list_title);
            ConcatDictionary(ref galleries, ref list_dic);

            list_title = parser.DocumentNode.SelectNodes("//a[@class='list_title1']");
            list_dic = new GalleryDictionary(list_title);
            ConcatDictionary(ref galleries, ref list_dic);

            HtmlNodeCollection list_categories = parser.DocumentNode.SelectNodes("//ul[@class='list_category']");
            foreach (HtmlNode list_category in list_categories)
            {
                HtmlNodeCollection links = list_category.SelectNodes(".//a");
                list_dic = new GalleryDictionary(links);
                ConcatDictionary(ref galleries, ref list_dic);
            }
            return galleries;
        }

        private static void ConcatDictionary(ref GalleryDictionary dic1, ref GalleryDictionary dic2)
        {
            foreach (var item in dic2)
            {
                dic1[item.Key] = item.Value;
            }
        }
    }
}
