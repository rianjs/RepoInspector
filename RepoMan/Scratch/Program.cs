using System;
using System.Linq;

namespace Scratch
{
    class Program
    {
        static void Main(string[] args)
        {
            var foo = Enumerable.Range(1, 8).ToList();
            foo.ForEach(Console.WriteLine);
            
            var isOdd = foo.Count % 2 == 1;
            var medianIndex = (foo.Count - 1) / 2;
            
            if (isOdd)
            {
                Console.WriteLine($"Median: {foo[medianIndex]}");
            }
            else
            {
                // it's even and we need to average the two 
                double first = foo[medianIndex];
                double second = foo[medianIndex + 1];
                var average = (first + second) / 2;
                var asInt = (int) Math.Round(average, MidpointRounding.AwayFromZero);
                Console.WriteLine($"Median: {asInt}");
            }

            
            
            var middleElement = foo[medianIndex];
            
            Console.WriteLine("Hello World!");
        }
    }
}