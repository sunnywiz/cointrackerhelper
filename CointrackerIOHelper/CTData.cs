using System.Collections.Generic;

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