using System;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;

namespace Library
{
    public class DCfinder
    {
        protected string gall_base_url = "http://gall.dcinside.com";
        private static HtmlDocument parser = new HtmlDocument();

        public string gallurl()
        {
            return gall_base_url;
        }

        public DCfinder()
        {
            this.Proxy = WebRequest.DefaultWebProxy;
        }
        public IWebProxy Proxy { get; set; }

        /////////////////////////////////////

        public delegate void ArticleDele(ArticleCollection articles);

        #region GetSearchPos
        public uint GetSearchPos(string gallery_id, string keyword, string search_type)
        {
            return GetSearchPosAsync(gallery_id, keyword, search_type).Result;
        }

        private Regex rSearchPos = new Regex("search_pos=-(\\d*)");
        private static Regex rMinorgall = new Regex("^<script>window.location.replace\\('(?:.+)id=(.+)'\\);<\\/script>$");
        
        public async Task<uint> GetSearchPosAsync(string gallery_id, string keyword, string search_type)
        {
            HtmlNodeCollection links;

            string board_url = gall_base_url + "/board/lists/?id=" + gallery_id;

            string search_query = String.Format("&s_type={0}&s_keyword={1}", search_type, keyword);
            string resp = await GetPageAsync(board_url + search_query);
            Match match = rMinorgall.Match(resp);
            if (match.Success)
            {
                // 마이너 갤러리 리디렉션
                return 987654321;
            }

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
            string next_pos = rSearchPos.Match(next_search_url).Groups[1].Value;
            if (next_pos == "")
            {
                // 일반 갤러리 리디렉션
                return 987654321;
            }
            return Convert.ToUInt32(next_pos) + 10000;
        }
        #endregion

        #region CrawlSearch
        public ArticleCollection CrawlSearch(string gallery_id, string keyword, string search_type, uint search_pos, bool recommend)
        {
            return CrawlSearchAsync(gallery_id, keyword, search_type, search_pos, recommend).Result;
        }

        public async Task<ArticleCollection> CrawlSearchAsync(string gallery_id, string keyword, string search_type, uint search_pos, bool recommend)
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
                HtmlNode last_btn = page_btns.ChildNodes[12];
                page_len = GetLastPage(last_btn);
            }
            else
            {
                page_len = CountPages(page_btns);
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
        public string GetPage(string url)
        {
            Task<string> resp = GetPageAsync(url);
            return resp.Result;
        }

        public async Task<string> GetPageAsync(string url)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            // WebProxy proxy = new WebProxy("127.0.0.1:8080");
            // req.Proxy = proxy;

            req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            req.ContentType = "application/x-www-form-urlencoded";
            // req.Headers["Accept-Encoding"] = "UTF-8";
            // req.Headers["Accept-Language"] = "ko-KR,ko;q=0.8,en-US;q=0.6,en;q=0.4";
            // req.Connection = "close";
            // req.Connection = "keep-alive";
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0";
            req.Method = "GET";
            req.Referer = gall_base_url;
            WebResponse response = await req.GetResponseAsync();
            return StreamFromResponse(response);
        }
       

        private string StreamFromResponse(WebResponse resp)
        {
            using (Stream responseStream = resp.GetResponseStream())
            using (StreamReader sr = new StreamReader(responseStream))
            {
                //Need to return this response 
                string strContent = sr.ReadToEnd();
                return strContent;
            }
        }
        #endregion

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

        private static int GetLastPage(string page_btn)
        {
            parser.LoadHtml(page_btn);
            return GetLastPage(parser);
        }

        private static int GetLastPage(HtmlDocument parser)
        {
            return 0;
        }

        private static int GetLastPage(HtmlNode last_btn)
        {
            string link = last_btn.Attributes["href"].Value;
            var rLastPage = new Regex("page=(\\d*)");
            return Int32.Parse(rLastPage.Match(link).Groups[1].Value);
        }

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

        private GalleryDictionary galleries;

        public GalleryDictionary GetGalleries()
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
                if (links == null)
                {
                    continue;
                }
                list_dic = new GalleryDictionary(links);
                ConcatDictionary(ref galleries, ref list_dic);
            }
            return galleries;
        }

        private void ConcatDictionary(ref GalleryDictionary dic1, ref GalleryDictionary dic2)
        {
            foreach (var item in dic2)
            {
                dic1[item.Key] = item.Value;
            }
        }
    }
    public class MDCfinder : DCfinder
    {
        public MDCfinder()
        {
            gall_base_url = "http://gall.dcinside.com/mgallery";
        }
    }
}
