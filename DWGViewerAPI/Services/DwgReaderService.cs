using ACadSharp;
using ACadSharp.IO;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services
{
    public class DwgReaderService : IDwgReaderService
    {
        public CadDocument ReadDwg(string filePath)
        {
            using (DwgReader reader = new DwgReader(filePath))
            {
                return reader.Read();
            }
        }
    }
}
