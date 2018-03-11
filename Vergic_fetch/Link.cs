using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vergic_fetch
{
    class Link
    {
        public string name { get; set;  }
        public string HtmlLink { get; set; }

        public List<Link> subLinks {get; set; }
    }
}
