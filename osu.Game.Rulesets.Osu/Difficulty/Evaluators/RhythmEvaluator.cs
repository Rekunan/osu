// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators
{
    public static class RhythmEvaluator
    {
        /// <summary>
        /// The amount of past hit objects (including the current) to consider.
        /// </summary>
        private const int maxobjectcount = 16;

        /// <summary>
        /// The amount of iterations for the coefficient.
        /// </summary>
        private const int coefiterations = 8;

        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            // Get the delta times for the past hit objects.
            double[] deltaTimes = Enumerable.Range(0, maxobjectcount)
                .TakeWhile(x => current.Previous(x - 1) is not null)
                .Select(x => current.Previous(x - 1).DeltaTime).ToArray();

            double entropy = 0;

            // Calculate the probability of occurrence for each delta time in the past window of delta times and adjust the entropy.
            foreach (double x in deltaTimes)
            {
                double probability = p(x, deltaTimes);

                entropy += -probability * Math.Log(probability);
            }

            return entropy * 0.4;
        }

        private static double p(double x, double[] deltaTimes)
        {
            // Calculates the coefficient for the rhythmic difference between two delta times.
            double coef(double x, double i)
                => Enumerable.Range(1, coefiterations)
                     .Sum(n => Math.Pow(Math.Cos(x / i * n * Math.PI), 8) / biggestPrimeFactor[n])
                     / Enumerable.Range(1, coefiterations).Sum(x => 1d / biggestPrimeFactor[x]);

            double probability = 0;

            // Calculate the probability of occurrence for a delta time x in the past window of delta times.
            foreach (double i in deltaTimes)
                probability += coef(x, i);

            // Get the average probability.
            return probability / deltaTimes.Length;
        }

        /// <summary>
        /// An array with the biggest prime factor of the index.
        /// </summary>
        private static int[] biggestPrimeFactor = new int[]
        {
            0,
            1,
            2,
            3,
            2,
            5,
            3,
            7,
            2
        };
    }
}
