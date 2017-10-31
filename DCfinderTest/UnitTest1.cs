using Microsoft.VisualStudio.TestTools.UnitTesting;
using Library;
using System.Diagnostics;

namespace DCfinderTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestArticle()
        {
        }

        [TestMethod]
        public void TestHost()
        {
            DCfinder dcfinder = new DCfinder();
            Debug.Assert(dcfinder.gallurl().Equals("http://gall.dcinside.com"));

            MDCfinder mdcfinder = new MDCfinder();
            Debug.Assert(mdcfinder.gallurl().Equals("http://gall.dcinside.com/mgallery"));
        }
    }
}
