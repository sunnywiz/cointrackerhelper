using System.Collections.Generic;

namespace CointrackerIOHelper
{
    public interface IFormatHelper<T>
    {
        List<T> Data { get; set; }
        bool ChooseAndReadFile();
        List<CtImportRow> ConvertToCTImport();
        void ReadFile(string fileName);
    }
}