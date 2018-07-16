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
            HtmlNodeCollection trs = parser.DocumentNode.SelectNodes("//tr[@class=\"tb\"]");
            SetArticleCollection(trs);
        }

        private void SetArticleCollection(HtmlNodeCollection trs)
        {
            foreach (HtmlNode tr in trs)
            {
                if (tr.SelectSingleNode("./td[@class='t_notice']").InnerText == "공지")
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
