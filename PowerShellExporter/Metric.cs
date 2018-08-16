using System.Collections;
using System.Text;

namespace PowerShellExporter
{
    public class Metric
    {
        public Metric(double value, Hashtable labels)
        {
            Value = value;
            Labels = labels;
        }

        public double Value { get; private set; }

        public Hashtable Labels { get; private set; }

        public override string ToString()
        {
            var labels = new StringBuilder();

            if (Labels.Count != 0)
            {
                foreach (DictionaryEntry entry in Labels)
                {
                    if (labels.Length > 0)
                    {
                        labels.Append(", ");
                    }
                    labels.AppendFormat("{0}={1}", entry.Key, entry.Value);
                }
            }

            return string.Format("{0}{1}", Labels.Count != 0 ? string.Format("{{{0}}}", labels) : null, Value);
        }
    }
}
