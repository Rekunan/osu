// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;

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

        /// <summary>
        /// The power of the sine curve for the coefficient.
        /// </summary>
        private const int coefsinepower = 8;

        /// <summary>
        /// The power of the cosine curve for the coefficient.
        /// </summary>
        private const int coefcosinepower = 2;

        /// <summary>
        /// A multiplier for the entropy value.
        /// </summary>
        private const double entropymultiplier = 0.4;

        public static double EvaluateDifficultyOf(OsuDifficultyHitObject current)
        {
            // Get the strain times for the past hit objects.
            double[] strainTimes = Enumerable.Range(0, maxobjectcount)
                .TakeWhile(x => current.Previous(x - 1) is not null)
                .Select(x => ((OsuDifficultyHitObject)current.Previous(x - 1)).StrainTime).ToArray();

            double entropy = 0;

            // Calculate the probability of occurrence for each strain time in the past window of strain times and adjust the entropy.
            foreach (double x in strainTimes)
            {
                double prob = probability(x, strainTimes);

                entropy += -prob * Math.Log(prob);
            }

            return entropy * entropymultiplier;
        }

        /// <summary>
        /// Calculates the average probability of occurrence of the specified strain time in the past window of strain times.
        /// </summary>
        /// <param name="strainTime1">The targetted strain time.</param>
        /// <param name="strainTimes">The window of strain times.</param>
        /// <returns>The average probability of occurence.</returns>
        private static double probability(double strainTime1, double[] strainTimes)
        {
            double probability = 0;

            foreach (double strainTime2 in strainTimes)
                probability += coefficient(strainTime1, strainTime2);

            return probability / strainTimes.Length;
        }

        /// <summary>
        /// Calculates the coefficient for the rhythmic difference between two strain times.
        /// </summary>
        /// <param name="strainTime1">The first strain time.</param>
        /// <param name="strainTime2">The second strain time.</param>
        /// <returns>The coefficient for the ratio between the two strain times.</returns>
        private static double coefficient(double strainTime1, double strainTime2)
        {
            double coef = 0;

            // TODO: document why we do multiple iterations
            for (int i = 1; i <= coefiterations; i++)
            {
                // TODO: what is calculated down there
                double cos = Math.Pow(Math.Cos(strainTime1 / strainTime2 * i * Math.PI), coefcosinepower);
                double sin = Math.Pow(Math.Sin(cos * Math.PI / 2), coefsinepower);

                // TODO: why do we divide by the biggest prime factor
                coef += sin / biggestprimefactor[i - 1];
            }

            // TODO: why do we divide by the inverse sum of the biggest prime factors
            return coef / inversebiggestprimefactorsum;
        }

        /// <summary>
        /// A lookup table with the biggest prime factors, starting at the number 1.
        /// </summary>
        private static readonly int[] biggestprimefactor =
        [
            1, // 1, actually undefined but we for our needs we'll consider it 1
            2, // 2
            3, // 3
            2, // 4
            5, // 5
            3, // 6
            7, // 7
            2, // 8
        ];

        /// <summary>
        /// The inverse sum of the first <see cref="coefiterations"/> numbers in <see cref="biggestprimefactor"/>.
        /// </summary>
        private static readonly double inversebiggestprimefactorsum = biggestprimefactor.Take(coefiterations).Sum(x => 1d / x);
    }
}
