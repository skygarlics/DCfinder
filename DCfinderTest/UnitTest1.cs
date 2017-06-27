using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System;

namespace DCfinderTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestArticle()
        {
            var test = @"<tr onmouseover=""this.style.backgroundColor='#eae9f7';"" onmouseout=""this.style.backgroundColor='';"" class=""tb"" style="""">
                  <td class=""t_notice"">9041654</td>
                  <td class=""t_subject"" style=""text-overflow:ellipsis; overflow:hidden; white-space:nowrap; padding:2px;""><a href=""/board/view/?id=rhythmgame&amp;no=9041654&amp;page=1&amp;search_pos=&amp;s_type=search_all&amp;s_keyword=%EB%A1%9C%EB%A6%AC"" class=""icon_pic_n"" style=""max-width:90%; overflow:hidden; vertical-align:middle;"">옛날 프루나엔 진짜 <span style=""color:red; background:yellow;"">로리</span>야동도 그냥 돌아다니지 않았나 ㅋㅋ</a><a href=""/board/comment_view/?id=rhythmgame&amp;no=9041654&amp;page=1&amp;search_pos=&amp;s_type=search_all&amp;s_keyword=%EB%A1%9C%EB%A6%AC""><em>[3]</em></a></td>
                  <td class=""t_writer user_layer"" user_id="""" user_name=""플라스틱"" style=""cursor:pointer;""><span class=""user_nick_nm"" title=""플라스틱"">플라스틱</span>                  </td>
                  <td class=""t_date"" title=""2017.03.20 20:59:15"">2017.03.20</td>
                                    <td class=""t_hits"">81</td>
                                    <td class=""t_hits"">1</td>
                                                  </tr>";
            /// string test
            /*
            var article = new Dcfinder.Article(test);
            Console.WriteLine(article.notice);
            Debug.Assert(article.notice == "9041654");
            Console.WriteLine(article.subject);
            Debug.Assert(article.subject == "옛날 프루나엔 진짜 로리야동도 그냥 돌아다니지 않았나 ㅋㅋ[3]");
            Console.WriteLine(article.writer);
            Debug.Assert(article.writer == "플라스틱");
            Console.WriteLine(article.date);
            Debug.Assert(article.date == "2017.03.20");
            */
        }
    }
}
