using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CointrackerIOHelper
{
    public class CTData
    {
        public List<CTExportRow> Rows { get; set; }

        public CTData()
        {
            Rows = new List<CTExportRow>();
        }

    }
}