using System;
using System.Collections.Generic;
using System.Linq;

namespace RepoMan.Analysis
{
    public static class MathExtensions
    {
        public static int CalculateMedian(this IEnumerable<int> integers)
        {
            var longMedian = integers.Select(Convert.ToInt64).CalculateMedian();
            return Convert.ToInt32(longMedian);
        }

        public static TimeSpan CalculateMedian(this IEnumerable<TimeSpan> durations)
        {
            var tickMedian = durations.Select(d => d.Ticks).CalculateMedian();
            return TimeSpan.FromTicks(tickMedian);
        }
        
        public static long CalculateMedian(this IEnumerable<long> integers)
        {
            if (integers is null || !integers.Any())
            {
                return 0;
            }
            
            // No, I don't particularly care that there are faster algorithms
            var sorted = new List<long>();
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
            return (long) Math.Round(average, MidpointRounding.AwayFromZero);
        }
        
        public static double CalculateMedian(this IEnumerable<double> doubles)
        {
            if (doubles is null || !doubles.Any())
            {
                return 0;
            }
            
            // No, I don't particularly care that there are faster algorithms
            var sorted = new List<double>();
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

        public static double CalculatePopulationStdDeviation(this IEnumerable<double> values)
        {
            if (values is null || values?.Any() == false)
            {
                return 0;
            }
            
            // The square root of the variance
            var variance = CalculatePopulationVariance(values);
            return Math.Sqrt(variance);
        }

        public static double CalculatePopulationVariance(this IEnumerable<double> values)
        {
            if (values is null || values?.Any() == false)
            {
                return 0;
            }
            // The average of the squared differences from the Mean.
            var populationMean = values.Average();
            var variance = values.Average(v => (v - populationMean) * (v - populationMean));
            return variance;
        }
    }
}