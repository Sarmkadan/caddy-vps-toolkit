using System;
using System.Collections.Generic;
using CaddyVpsToolkit.Cli;

namespace CaddyVpsToolkit.Benchmarks
{
    public static class ArgumentParserBenchmarksExtensions
    {
        public static bool HasGetCommand(this ArgumentParserBenchmarks benchmarks, string command) 
            => benchmarks.GetCommand_Small == command;

        public static int GetFlagCount(this ArgumentParserBenchmarks benchmarks) 
            => benchmarks.GetAllFlags_Large.Count;

        public static double GetFlagValueAverage(this ArgumentParserBenchmarks benchmarks) 
        {
            var values = new List<double>();
            if (benchmarks.GetFlagValue_EqualsSyntax != null) 
                values.Add(double.Parse(benchmarks.GetFlagValue_EqualsSyntax));
            if (benchmarks.GetFlagValue_SpaceSyntax != null) 
                values.Add(double.Parse(benchmarks.GetFlagValue_SpaceSyntax));

            return values.Count > 0 ? values.Average() : 0;
        }

        public static bool HasAnyFlags(this ArgumentParserBenchmarks benchmarks) 
            => benchmarks.HasFlag_Present || benchmarks.GetAllFlags_Large.Count > 0;
    }
}
