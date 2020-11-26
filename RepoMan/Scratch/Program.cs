using System;
using RepoMan.Analysis.Counters;
using RepoMan.Analysis.Counters.Comments;
using RepoMan.Analysis.Scoring;

namespace Scratch
{
    class Program
    {
        static void Main(string[] args)
        {
            var codeBlockCounter = new CodeFragmentCounter();
            var codeBlockScorer = new CodeFenceScorer();
            var s = "```this is a code block``` and `this` is a code fragment, and together they should have a score of 12";
            Console.WriteLine("Hello World!");
        }
    }
}