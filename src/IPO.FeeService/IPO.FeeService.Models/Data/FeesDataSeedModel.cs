using System;

namespace IPO.FeeService.Models.Data
{
    public class FeesDataSeedModel
    {
        public FeesDataSeedModel(int version, string path)
        {
            Version = version;
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public int Version { get; set; }
        public string Path { get; set; }
    }
}
