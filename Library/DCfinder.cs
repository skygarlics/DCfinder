using System;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace Library
{
    public class DCfinder
    {
        public string gall_base_url = "http://gall.dcinside.com";
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
        private const int pos_per_depth = 10000;

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
            string resp = await RequestPageAsync(board_url + search_query);
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
            return Convert.ToUInt32(next_pos) + pos_per_depth;
        }
        #endregion

        #region CrawlSearch
        public ArticleCollection CrawlSearch(string gallery_id, string keyword, string search_type, uint search_pos, bool recommend, ArticleCollection results, CancellationToken token)
        {
            return CrawlSearchAsync(gallery_id, keyword, search_type, search_pos, recommend, results, token).Result;
        }

        public async Task<ArticleCollection> CrawlSearchAsync(string gallery_id, string keyword, string search_type, uint search_pos, bool recommend, ArticleCollection results, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return null;
            }

            string search_query = "&page={0}&search_pos=-{1}&s_type={2}&s_keyword={3}";
            if (recommend)
            {
                search_query += "&exception_mode=recommend";
            }
            string board_url = gall_base_url + "/board/lists/?id=" + gallery_id;

            // get page length of this search
            string request_url = board_url + String.Format(search_query, 1, search_pos, search_type, keyword);
            string html = await RequestPageAsync(request_url);
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
            {
                ArticleCollection articles = new ArticleCollection(html);
                foreach (var article in articles)
                {
                    results.Add(article);
                }
            }

            // get rest of pages
            List<Task<ArticleCollection>> tasks = new List<Task<ArticleCollection>>();
            {
                const int MAX_TASK = 10;
                int page_idx;
                int cnt;
                for (page_idx = 2, cnt = 1; page_idx <= page_len; ++page_idx, ++cnt)
                {
                    if (token.IsCancellationRequested)
                    {
                        return null;
                    }
                    request_url = board_url + String.Format(search_query, page_idx, search_pos, search_type, keyword);
                    tasks.Add(GetArticlesAsync(request_url));

                    if (cnt >= MAX_TASK)
                    {
                        var articleCollectionList = await Task.WhenAll<ArticleCollection>(tasks);
                        foreach(var articles in articleCollectionList)
                            foreach(var article in articles)
                                results.Add(article);
                    }
                }
            }

            return results;
        }
        #endregion
  
        #region GetPage
        public string RequestPage(string url)
        {
            Task<string> resp = RequestPageAsync(url);
            return resp.Result;
        }

        public string RequestPage(string url, string data)
        {
            Task<string> resp = RequestPageAsync(url, data);
            return resp.Result;
        }


        public async Task<string> RequestPageAsync(string url)
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

        public async Task<string> RequestPageAsync(string url, string data)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);;

            byte[] sendData = Encoding.UTF8.GetBytes(data);

            req.ContentLength = sendData.Length;
            req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            req.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0";
            req.Method = "POST";
            req.Referer = gall_base_url;

            Stream reqStream = req.GetRequestStream();
            reqStream.Write(sendData, 0, sendData.Length);
            reqStream.Close();

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
        public ArticleCollection GetArticles(string url)
        {
            Task<ArticleCollection> articles = GetArticlesAsync(url);
            return articles.Result;
        }
        public async Task<ArticleCollection> GetArticlesAsync(string url)
        {
            string html = await RequestPageAsync(url);
            return new ArticleCollection(html);
        }
        #endregion

        #region CountPages
        private static Regex rLink = new Regex("<a");

        public int GetLastPage(string page_btn)
        {
            parser.LoadHtml(page_btn);
            return GetLastPage(parser);
        }

        public int GetLastPage(HtmlDocument parser)
        {
            return 0;
        }

        public int GetLastPage(HtmlNode last_btn)
        {
            string link = last_btn.Attributes["href"].Value;
            var rLastPage = new Regex("page=(\\d*)");
            return Int32.Parse(rLastPage.Match(link).Groups[1].Value);
        }

        public int CountPages(string page_btns)
        {
            parser.LoadHtml(page_btns);
            return CountPages(parser);
        }

        public int CountPages(HtmlDocument parser)
        {
            return parser.DocumentNode.SelectNodes("//a").Count - CountPrevBtn(parser) - CountNextBtn(parser);
        }

        public int CountPages(HtmlNode page_btns)
        {
            return page_btns.SelectNodes("./a").Count - CountPrevBtn(page_btns) - CountNextBtn(page_btns);
        }

        public int CountPrevBtn(string page_btns)
        {
            parser.LoadHtml(page_btns);
            return CountPrevBtn(parser);
        }

        public int CountPrevBtn(HtmlDocument parser)
        {
            var nodes = parser.DocumentNode.SelectNodes("//a[@class='b_prev']");
            if (nodes != null)
                return nodes.Count;
            else
                return 0;
        }

        public int CountPrevBtn(HtmlNode page_btns)
        {
            var nodes = page_btns.SelectNodes("./a[@class='b_prev']");
            if (nodes != null)
                return nodes.Count;
            else
                return 0;
        }

        public int CountNextBtn(string page_btns)
        {
            parser.LoadHtml(page_btns);
            return CountNextBtn(parser);
        }

        public int CountNextBtn(HtmlDocument parser)
        {
            var nodes = parser.DocumentNode.SelectNodes("//a[@class=\"b_next\"]");
            if (nodes != null)
                return nodes.Count;
            else
                return 0;
        }

        public int CountNextBtn(HtmlNode page_btns)
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

        Regex rGallid = new Regex("page_move\\(\\\"(.+)\\\",\\\"(.+)\\\"\\)");
        public GalleryDictionary GetGalleryByName(string needle)
        {
            GalleryDictionary retdic = new GalleryDictionary();

            // 일반 갤
            GalleryDictionary tmpdic = GetGalleryByName(needle, "http://gall.dcinside.com/gallmain/SetListBoxSearch.php");
            ConcatDictionary(ref retdic, ref tmpdic);
            // 마이너갤
            tmpdic = GetGalleryByName(needle, "http://gall.dcinside.com/m/mgallmain/SetListBoxSearch.php");
            ConcatDictionary(ref retdic, ref tmpdic);

            return retdic;
        }

        private GalleryDictionary GetGalleryByName(string needle, string search_url)
        {
            GalleryDictionary ret = new GalleryDictionary();
            string data = String.Format("key={0}", needle);
            string resp = RequestPage(search_url, data);
            parser.LoadHtml(resp);
            HtmlNode result_list = parser.DocumentNode.SelectSingleNode("//div[@class='result_list']");
            HtmlNodeCollection results = result_list.SelectNodes(".//div");
            if (results != null)
            {
                foreach (HtmlNode result in results)
                {
                    string name = result.InnerText;

                    string js = result.Attributes["onclick"].Value;
                    Match match = rGallid.Match(js);
                    string id = match.Groups[2].Value;

                    ret[name] = new Gallery(name, id);
                }
            }
            return ret;
        }
        
        public GalleryDictionary GetGalleries()
        {
            if (galleries == null)
            {
                galleries = new GalleryDictionary();
                
                // 메이저 갤러리 리스트
                string major_gall_html = RequestPage("http://wstatic.dcinside.com/gallery/gallindex_iframe_new_gallery.html");
                GetGalleryFromHtml(major_gall_html);

                // 마이너 갤러리 리스트
                string minor_gall_html = RequestPage("http://wstatic.dcinside.com/gallery/mgallindex_iframe.html");
                GetMGalleryFromHtml(minor_gall_html);   
            }
            return galleries;
        }

        private void GetGalleryFromHtml(string html)
        {
            parser.LoadHtml(html);
            // 1째열 갤러리들
            HtmlNodeCollection list_title = parser.DocumentNode.SelectNodes("//a[@class='list_title']");
            var list_dic = new GalleryDictionary(list_title);
            ConcatDictionary(ref galleries, ref list_dic);

            // 2째열 갤러리들
            list_title = parser.DocumentNode.SelectNodes("//a[@class='list_title1']");
            list_dic = new GalleryDictionary(list_title);
            ConcatDictionary(ref galleries, ref list_dic);
            
            // 레이어 갤러리들
            // HtmlNodeCollection list_categories = parser.DocumentNode.SelectNodes("//ul[@class='list_category']");
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
        }

        private void GetMGalleryFromHtml(string html)
        {
            parser.LoadHtml(html);
            // 1째열 갤러리들
            HtmlNodeCollection list_title = parser.DocumentNode.SelectNodes("//a[@class='list_title']");
            var list_dic = new GalleryDictionary(list_title);
            ConcatDictionary(ref galleries, ref list_dic);

            // 레이어 갤러리들
            // more 버튼들
            var btn_mores = parser.DocumentNode.SelectNodes("//div[@class='btn_layer_more']");
            foreach (HtmlNode btn_more in btn_mores)
            {
                HtmlNode link = btn_more.SelectSingleNode("./a");
                string layername = link.Attributes["data-target"].Value;
                if (!layername.Contains("LayerM"))
                {
                    continue;
                }
                var layer_url = "http://wstatic.dcinside.com/gallery/mgallindex_underground/" + layername + ".html";
                var layer_html = RequestPage(layer_url);
                parser.LoadHtml(layer_html);
                // 1째열 갤러리들
                list_title = parser.DocumentNode.SelectNodes(".//a[@class='list_title']");
                list_dic = new GalleryDictionary(list_title);
                ConcatDictionary(ref galleries, ref list_dic);
            }
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
