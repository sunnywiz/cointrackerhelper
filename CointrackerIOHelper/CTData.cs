using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CointrackerIOHelper
{
    public class CtData
    {
        public List<CtExportRow> Rows { get; set; }

        public CtData()
        {
            Rows = new List<CtExportRow>();
        }

    }
}