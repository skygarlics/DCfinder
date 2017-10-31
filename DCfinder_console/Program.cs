#define DEBUG

using System;
using Library;

namespace DCfinder_console
{
    class Program
    {
        static ArticleCollection articlecollection = new ArticleCollection();
        private static GalleryDictionary dic;
        static DCfinder dcfinder = new MDCfinder();

        static void Main(string[] args)
        {
            Console.Write("find mode:\n[1] 갤러리 목록 검색\n[2] 갤러리 내 검색\n");
            string input = Console.ReadKey().KeyChar.ToString();
            int mode = Convert.ToInt32(input);
            switch (mode) {
                case 1:
                    findGallery();
                    break;
                case 2:
                    searchGallery();
                    break;
                default:
                    Console.Write("입력이 올바르지 않음");
                    Console.ReadKey();
                    break;
            }
        }

        private static void findGallery()
        {
            Console.Clear();
            Console.Write("갤러리명 : ");
            string keyword = Console.ReadLine();

            /*
            if (dic == null)
            {
                dic = dcfinder.GetGalleries();
            }
            */

            dic = dcfinder.GetGalleryByName(keyword);

            foreach (var key in dic.Keys)
            {
                if (key.Contains(keyword))
                {
                    Console.Write("Gallery_id : ");
                    Console.WriteLine(dic[key].gallery_id);
                }
            }
            Console.WriteLine("search end");
            Console.ReadKey();
        }

        private static void searchGallery()
        {
            string[] searchArray = { "search_all", "search_subject", "search_memo", "search_name", "search_subject_memo" };
#if DEBUG
            string gall_id = "gfl";
            string keyword = "파밍";
            string mode = searchArray[0];
            Console.Write("mode : ");
            Console.WriteLine(mode);
            uint depth = 2;
            uint searchpos;
            Console.Write("Get Search position...");
            searchpos = dcfinder.GetSearchPos(gall_id, keyword, mode);
            Console.WriteLine("END.");
            bool recommend = false;
#else
            Console.Write("Gallery Id: ");
            string gall_id = Console.ReadLine();

            Console.Write("Keyword: ");
            string keyword = Console.ReadLine();

            Console.Write("Select mode: [1]전체 [2]제목 [3]내용 [4]글쓴이 [5]제목+글쓴이 ");
            string mode = searchArray[Convert.ToInt32(Console.ReadLine()) - 1];
            
            Console.Write("Position (default 0): ");
            uint pos = Convert.ToUInt32(Console.ReadLine());

            Console.Write("Depth (search to end if 0): ");
            uint depth = Convert.ToUInt32(Console.ReadLine());

            bool? recommend = null;
            while (recommend == null)
            {
                Console.Write("Recommend only?(Y/N)");
                var tmp = Console.ReadLine();
                if (tmp == "Y" | tmp == "y")
                {
                    recommend = true;
                }
                else if (tmp == "N" | tmp == "n")
                {
                    recommend = false;
                }
            }

            uint searchpos;
            if (pos == 0)
            {
                searchpos = DCfinder.GetSearchPos(gall_id, keyword, mode);
            }
            else
            {
                searchpos = pos;
            }
#endif
            Console.WriteLine("Search start");
            for (uint idx = 0; idx < depth; idx++)
            {
                PrintArticles(dcfinder.CrawlSearch(gall_id, keyword, mode, searchpos - (idx * 10000), recommend));
            }

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
