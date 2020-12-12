using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RepoMan.Repository
{
    public static class BitBucketCloudExtensions
    {
        public static IEnumerable<string> GetBitBucketCloudStateFilters(this ItemState itemState)
        {
            switch (itemState)
            {
                case ItemState.Open:
                    yield return "OPEN";
                    yield break;
                case ItemState.Closed:
                    yield return "MERGED";
                    yield return "SUPERSEDED";
                    yield return "DECLINED";
                    yield break;
                case ItemState.All:
                    yield break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemState), itemState, null);
            }
        }

        /// <summary>
        /// Returns a string that looks like (state="superseded" OR state="merged" OR state="declined"), which will typically be URL-encoded
        /// </summary>
        /// <param name="stateFilter"></param>
        /// <returns></returns>
        public static string GetBbCloudStateFilter(this ItemState stateFilter)
        {
            var itemStates = GetBitBucketCloudStateFilters(stateFilter).ToList();
            if (!itemStates.Any())
            {
                return null;
            }

            var joinQuery = itemStates.Select(s => $"state=\"{s}\"");
            var joined = string.Join(" OR ", joinQuery);
            return "(" + joined + ")";
        }

        public static string GetBbCloudUpdatedAtFilter(this DateTimeOffset updatedAfter, IClock clock)
        {
            return updatedAfter > clock.DateTimeOffsetUtcNow()
                ? null
                : $"updated_on>{updatedAfter:s}";
        }

        public static string BuildFullQuery(IEnumerable<string> args, string separator)
        {
            var needsSeparator = false;
            var builder = new StringBuilder();
            foreach (var arg in args)
            {
                if (string.IsNullOrWhiteSpace(arg))
                {
                    continue;
                }
                
                if (needsSeparator)
                {
                    builder.Append(separator);
                }

                builder.Append(arg);
                needsSeparator = true;
            }

            return builder.ToString();
        }
    }
}