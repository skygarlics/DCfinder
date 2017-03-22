using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library
{
    public class SearchOptionCollection : List<SearchOption>
    {
    }

    public class SearchOption
    {
        public string Text { get; set; }
        public string Query { get; set; }

        override public string ToString()
        {
            return this.Text;
        }

        public SearchOption()
        {
            this.Text = "";
            this.Query = "";
        }

        public SearchOption(string Text, string Query)
        {
            this.Text = Text;
            this.Query = Query;
        }
    }
}
