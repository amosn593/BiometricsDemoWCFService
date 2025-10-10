using System;
using System.Collections.Generic;

namespace BiometricsDemo.Helpers
{
    public static class NeuroticErrorMessage
    {
        public static string ExtractNeurotecErrorMessage(Exception ex)
        {
            if (ex == null) return string.Empty;

            var messages = new List<string>();
            ExtractMessages(ex, messages);

            // Remove empty and duplicate messages, keep first occurrence only
            var uniqueMessages = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var msg in messages)
            {
                var clean = msg?.Trim();
                if (!string.IsNullOrWhiteSpace(clean) && seen.Add(clean))
                    uniqueMessages.Add(clean);
            }

            return string.Join(" -> ", uniqueMessages);
        }

        private static void ExtractMessages(Exception ex, List<string> messages)
        {
            if (ex == null) return;

            if (ex is AggregateException agg)
            {
                messages.Add(agg.Message);
                foreach (var inner in agg.InnerExceptions)
                    ExtractMessages(inner, messages);
            }
            else
            {
                messages.Add(ex.Message);
                if (ex.InnerException != null)
                    ExtractMessages(ex.InnerException, messages);
            }
        }
    }
}
