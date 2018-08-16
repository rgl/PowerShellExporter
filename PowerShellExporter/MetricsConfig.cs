using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PowerShellExporter
{
    public class MetricsConfig
    {
        public MetricConfig[] Metrics { get; set; }

        internal static MetricsConfig Load(string path)
        {
            using (var f = File.OpenText(path))
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(new UnderscoredNamingConvention())
                    .Build();
                return deserializer.Deserialize<MetricsConfig>(f);
            }
        }
    }

    public class MetricConfig
    {
        public string Name { get; set; }
        public string Cmdlet { get; set; }
        public string Help { get; set; }
    }
}
