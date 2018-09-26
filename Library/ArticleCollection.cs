using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Library
{
    public class ArticleCollection: ObservableCollection<Article>
    {
        private HtmlDocument parser = new HtmlDocument();
        public ArticleCollection() : base()
        {
        }

        public ArticleCollection(IEnumerable<Article> collection) : base(collection)
        {
        }

        #region Generator
        public ArticleCollection(string html)
        {
            SetArticleCollection(html);
        }

        public ArticleCollection(HtmlNodeCollection trs)
        {
            SetArticleCollection(trs);
        }

        public ArticleCollection(MatchCollection matches)
        {
            SetArticleCollection(matches);
        }
        #endregion

        #region SetArticleCollection
        private void SetArticleCollection(string html)
        {
            parser.LoadHtml(html);
            HtmlNodeCollection trs = parser.DocumentNode.SelectSingleNode("//tbody").SelectNodes("./tr");
            SetArticleCollection(trs);
        }

        private void SetArticleCollection(HtmlNodeCollection trs)
        {
            foreach (HtmlNode tr in trs)
            {
                string gall_num = tr.SelectSingleNode("./td[@class='gall_num']").InnerText;
                if (gall_num == "-" || gall_num == "공지")
                {
                    continue;
                }
                else
                {
                        this.Add(new Article(tr));
                }
            }
        }

        private void SetArticleCollection(MatchCollection matches)
        {
            foreach (Match match in matches)
            {
                this.Add(new Article(match));
            }
        }
        #endregion

    }
}
