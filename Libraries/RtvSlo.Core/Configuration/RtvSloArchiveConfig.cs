using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RtvSlo.Core.Configuration
{
    public class RtvSloArchiveConfig
    {
        public string ArchiveUrl { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public string SearchType { get; set; }

        public string Sections { get; set; }
    }
}
