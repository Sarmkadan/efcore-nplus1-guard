using System;
using System.Collections.Generic;
using System.Linq;

namespace EfCoreNPlusOneGuard
{
    public class DuplicateQueryDetector
    {
        private readonly int _threshold;
        private readonly Dictionary<(string sql, string? parameters), int> _queryCounts = new();

        public DuplicateQueryDetector(int threshold = 2)
        {
            _threshold = threshold;
        }

        public void Record(string sql, string? parameters)
        {
            var key = (sql, parameters);
            if (_queryCounts.TryGetValue(key, out int count))
            {
                _queryCounts[key] = count + 1;
            }
            else
            {
                _queryCounts[key] = 1;
            }
        }

        public IReadOnlyList<DuplicateQueryGroup> GetDuplicates()
        {
            return _queryCounts
                .Where(kvp => kvp.Value >= _threshold)
                .Select(kvp => new DuplicateQueryGroup
                {
                    Sql = kvp.Key.sql,
                    Parameters = kvp.Key.parameters,
                    Count = kvp.Value
                })
                .ToList();
        }

        public void Clear()
        {
            _queryCounts.Clear();
        }

        public class DuplicateQueryGroup
        {
            public string Sql { get; set; } = string.Empty;
            public string? Parameters { get; set; }
            public int Count { get; set; }
        }
    }
}
