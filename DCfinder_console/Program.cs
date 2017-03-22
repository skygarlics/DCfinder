using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Library;
using static Library.DCfinder;

namespace DCfinder_console
{
    class Program
    {
        static ArticleCollection articlecollection = new ArticleCollection();

        static void Main(string[] args)
        {
            #region Interface

            Console.Write("Gallery Id: ");
            string gall_id = Console.ReadLine();

            Console.Write("Keyword: ");
            string keyword = Console.ReadLine();

            Console.Write("Select mode: [1]전체 [2]제목 [3]내용 [4]글쓴이 [5]제목+글쓴이 ");
            string[] searchArray = { "search_all", "search_subject", "search_memo", "search_name", "search_subject_memo" };
            string mode = searchArray[Convert.ToInt32(Console.ReadLine())];

            Console.Write("Position (default 0): ");
            uint pos = Convert.ToUInt32(Console.ReadLine());

            Console.Write("Depth (search to end if 0): ");
            uint depth = Convert.ToUInt32(Console.ReadLine());

            uint searchpos = DCfinder.GetSearchPos(gall_id, keyword, mode);
            for (uint idx = 0; idx < depth; idx++)
            {
                PrintArticles(DCfinder.CrawlSearch(gall_id, keyword, mode, searchpos - (idx * 10000), true));
            }
            #endregion

            Console.ReadKey();
        }

        private static void PrintArticles(ArticleCollection articles)
        {
            foreach (Article article in articles)
            {
                Console.WriteLine(article.notice + " " + article.subject);
            }
            Console.WriteLine("================");
        }

        private static void AddArticles(ArticleCollection articles)
        {
            articlecollection.AddRange(articles);
            PrintArticles(articlecollection);
        }
    }
}
