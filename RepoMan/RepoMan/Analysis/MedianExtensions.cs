using System;
using System.Collections.Generic;
using System.Linq;

namespace RepoMan.Analysis
{
    public static class MedianExtensions
    {
        public static int CalculateMedian(this ICollection<int> integers)
        {
            if (!integers.Any())
            {
                return 0;
            }
            
            // No, I don't particularly care that there are faster algorithms
            var sorted = new List<int>(integers.Count);
            sorted.AddRange(integers.OrderBy(i => i));

            var isOdd = sorted.Count % 2 == 1;
            var medianIndex = (sorted.Count - 1) / 2;
            
            if (isOdd)
            {
                return sorted[medianIndex];
            }

            var first = sorted[medianIndex];
            var second = sorted[medianIndex + 1];
            if (first == second)
            {
                return first;
            }
            
            var average = ((double)first + second) / 2;
            return (int) Math.Round(average, MidpointRounding.AwayFromZero);
        }
        
        public static double CalculateMedian(this ICollection<double> doubles)
        {
            if (!doubles.Any())
            {
                return 0;
            }
            
            // No, I don't particularly care that there are faster algorithms
            var sorted = new List<double>(doubles.Count);
            sorted.AddRange(doubles.OrderBy(i => i));

            var isOdd = sorted.Count % 2 == 1;
            var medianIndex = (sorted.Count - 1) / 2;
            
            if (isOdd)
            {
                return sorted[medianIndex];
            }

            var first = sorted[medianIndex];
            var second = sorted[medianIndex + 1];
            if (first == second)
            {
                return first;
            }
            
            var average = (first + second) / 2;
            return average;
        }
    }
}