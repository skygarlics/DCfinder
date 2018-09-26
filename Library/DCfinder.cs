using System;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;

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
                // 갤러리 리디렉션
                return 987654321;
            }

            parser.LoadHtml(resp);

            try
            {
                HtmlNode page_btns = parser.DocumentNode.SelectSingleNode("//div[contains(concat(' ', @class, ' '), ' bottom_paging_box ')]");
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
        public ArticleCollection CrawlSearch(string gallery_id, string keyword, string search_type, uint search_pos, bool recommend, CancellationToken token)
        {
            return CrawlSearchAsync(gallery_id, keyword, search_type, search_pos, recommend, token).Result;
        }

        public async Task<ArticleCollection> CrawlSearchAsync(string gallery_id, string keyword, string search_type, uint search_pos, bool recommend, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return null;
            }

            ArticleCollection results = new ArticleCollection();

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
            HtmlNode page_btns = parser.DocumentNode.SelectSingleNode("//div[contains(concat(' ', @class, ' '), ' bottom_paging_box ')]");
            int page_len = 1;
            if (IsPageNextExist(page_btns))
            {
                // board length > 10
                page_len = GetLastPage(page_btns);
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
                        foreach (var articles in articleCollectionList)
                            foreach (var article in articles)
                                results.Add(article);

                        // finalize
                        tasks.Clear();
                        cnt = 0;
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
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url); ;

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

        public int GetLastPage(HtmlNode page_btns)
        {
            var last = page_btns.SelectSingleNode("//a[contains(concat(' ', @class, ' '), ' page_end ')]");
            string link = last.GetAttributeValue("herf", "");
            var rLastPage = new Regex("page=(\\d*)");
            return Int32.Parse(rLastPage.Match(link).Groups[1].Value);
        }

        public int CountPages(HtmlNode page_btns)
        {
            int cnt = 0;
            var links = page_btns.SelectNodes("./a");
            foreach (var link in links)
            {
                string classes = link.GetAttributeValue("class", "");
                if (string.IsNullOrEmpty(classes))
                    cnt++;
            }
            return cnt;
        }

        public bool IsPageNextExist(HtmlNode page_btns)
        {
            var next_btn = page_btns.SelectNodes("//a[contains(concat(' ', @class, ' '), ' page_next ')]");
            if (next_btn != null)
                return true;
            return false;
        }
        #endregion

        ///////////////////////////////////////

        private GalleryDictionary galleries;

        Regex rGallid = new Regex("page_move\\(\\\"(.+)\\\",\\\"(.+)\\\"\\)");
        public GalleryDictionary GetGalleryByName(string needle)
        {
            GalleryDictionary retdic = new GalleryDictionary();

            // 통합 검색
            string url = $"https://search.dcinside.com/autocomplete?k={needle}";
            string resp = RequestPage(url);
            resp = resp.Substring(1, resp.Length - 3);
            /* Response example
             jQuery32107671421274898425_1537893408440({0: [{ko_name: "소녀전선", name: "gfl"}], 1: [{m_ko_name: "소녀전선 2", name: "gfl2"}],…});
                0: [{ko_name: "소녀전선", name: "gfl"}]
                1: [{m_ko_name: "소녀전선 2", name: "gfl2"}]
                2: [{o_gallName: "F.O.X", galleryId: "fox"}, {o_gallName: "MICATEAM", galleryId: "micateam"},…]
                3: [{title: "소녀전선"}, {title: "소녀전선 2 마이너 갤러리"}, {title: "소녀전선 HK416.jpg"},…]
                time: "1537893537151"
             */
            var result = JsonConvert.DeserializeObject<GallSearchResp>(resp);

            foreach(var gall in result.Majors)
                retdic.Add(gall.Name, new Gallery(gall.Name, gall.Id));

            foreach (var gall in result.Minors)
                retdic.Add(gall.Name, new Gallery(gall.Name, gall.Id));

            return retdic;
        }

        #region search result classes
        public class GallSearchResp
        {
            public string time { get; set; }
            [JsonProperty("0")]
            public MjaorResult[] Majors { get; set; }
            [JsonProperty("1")]
            public MinorResult[] Minors { get; set; }
            [JsonProperty("2")]
            public RecommResult[] Recommends { get; set; }
            [JsonProperty("3")]
            public DCWiki[] DCWikis { get; set; }
        }

        public class MjaorResult
        {
            [JsonProperty("ko_name")]
            public string Name { get; set; }
            [JsonProperty("name")]
            public string Id { get; set; }
        }

        public class MinorResult
        {
            [JsonProperty("m_ko_name")]
            public string Name { get; set; }
            [JsonProperty("name")]
            public string Id { get; set; }
        }

        public class RecommResult
        {
            [JsonProperty("o_gallName")]
            public string Name { get; set; }
            [JsonProperty("galleryId")]
            public string Id { get; set; }
        }

        public class DCWiki
        {
            public string title { get; set; }
        }
        #endregion

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
