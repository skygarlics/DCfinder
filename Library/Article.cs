using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace Library
{
    public class Article
    {
        public string notice { get; set; }
        public string subject { get; set; }
        public string writer { get; set; }
        public string date { get; set; }

        private HtmlDocument parser = new HtmlDocument();

        #region Generator
        public Article(string html)
        {
            SetArticle(html);
        }

        public Article(HtmlNode tr)
        {
            SetArticle(tr);
        }

        public Article(Match article)
        {
            SetArticle(article.Value);
        }
        #endregion

        #region SetArticle

        private void SetArticle(string html)
        {
            parser.LoadHtml(html);
            SetArticle(parser.DocumentNode.SelectSingleNode("//tr"));
        }

        private void SetArticle(HtmlNode tr)
        {
            HtmlNodeCollection tds = tr.SelectNodes("./td");
            foreach (HtmlNode node in tds)
            {
                switch (node.Attributes["class"].Value)
                {
                    case "t_notice":
                        this.notice = node.InnerText;
                        break;
                    case "t_date":
                        this.date = node.GetAttributeValue("title", "DEFAULT");
                        break;
                    case "t_subject":
                        this.subject = RemoveTabNl(node.InnerText);
                        break;
                    case "t_writer user_layer":
                        // this.writer = RemoveSpan(text);
                        this.writer = node.InnerText;
                        break;
                }
            }
        }

        private Regex rTd = new Regex("<td(.*)<\\/td>");
        private Regex rClass = new Regex("class=\"(.*?)\"");
        private Regex rTdText = new Regex("<td(?:.*)>(.*)<\\/td>");
        private Regex rAText = new Regex("<a(?:.*?)>(.*?)<\\/a>");
        private Regex rSpanText = new Regex("<span(?:.*?)>(.*?)<\\/span>");
        private void SetArticle(Match article)
        {
            MatchCollection tds = rTd.Matches(article.Value);
            foreach (Match td in tds)
            {
                switch (rClass.Match(td.Value).Groups[1].Value)
                {
                    case "t_notice":
                        this.notice = rTdText.Match(td.Value).Groups[1].Value;
                        break;
                    case "t_date":
                        this.date = rTdText.Match(td.Value).Groups[1].Value;
                        break;
                    case "t_subject":
                        var text = rAText.Match(td.Value).Groups[1].Value;
                        this.subject = RemoveSpan(text);
                        break;
                    case "t_writer user_layer":
                        text = rSpanText.Match(td.Value).Groups[1].Value;
                        this.writer = RemoveSpan(text);
                        break;
                }
            }
        }


        #endregion

        private string RemoveTabNl(string text)
        {
            return Regex.Replace(text, @"\t|\n|\r", "");
        }

        #region RemoveSpan
        MatchEvaluator evaluator = new MatchEvaluator(Article.RemoveSpanEvaluator);
        private string RemoveSpan(string haystack)
        {
            return rSpanText.Replace(haystack, evaluator);
        }
        private static string RemoveSpanEvaluator(Match m)
        {
            return m.Groups[1].Value;
        }
        #endregion
    }
}
