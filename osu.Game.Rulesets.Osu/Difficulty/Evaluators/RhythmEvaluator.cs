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

            return entropy * entropymultiplier;
        }

        /// <summary>
        /// Calculates the average probability of occurrence of the specified delta time in the past window of delta times.
        /// </summary>
        /// <param name="deltaTime1">The targetted delta time.</param>
        /// <param name="deltaTimes">The window of delta times.</param>
        /// <returns>The average probability of occurence.</returns>
        private static double p(double deltaTime1, double[] deltaTimes)
        {
            double probability = 0;

            foreach (double deltaTime2 in deltaTimes)
                probability += coefficient(deltaTime1, deltaTime2);

            return probability / deltaTimes.Length;
        }

        /// <summary>
        /// Calculates the coefficient for the rhythmic difference between two delta times.
        /// </summary>
        /// <param name="deltaTime1">The first delta time.</param>
        /// <param name="deltaTime2">The second delta time.</param>
        /// <returns>The coefficient for the ratio between the two delta times.</returns>
        private static double coefficient(double deltaTime1, double deltaTime2)
        {
            double coef = 0;

            // TODO: document why we do multiple iterations
            for (int i = 1; i <= coefiterations; i++)
            {
                // TODO: what is calculated down there
                double cos = Math.Pow(Math.Cos(deltaTime1 / deltaTime2 * i * Math.PI), coefcosinepower);
                double sin = Math.Pow(Math.Sin(cos * Math.PI / 2), coefsinepower);

                // TODO: why do we divide by the biggest prime factor
                coef += sin / biggestPrimeFactor[i - 1];
            }

            // TODO: why do we divide by the inverse sum of the biggest prime factors
            return coef / inverseBiggestPrimeFactorSum;
        }

        /// <summary>
        /// A lookup table with the biggest prime factors, starting at the number 1.
        /// </summary>
        private static int[] biggestPrimeFactor =
        [
            1, // 1
            2, // 2
            3, // 3
            2, // 4
            5, // 5
            3, // 6
            7, // 7
            2, // 8
        ];

        /// <summary>
        /// The inverse sum of the first <see cref="coefiterations"/> numbers in <see cref="biggestPrimeFactor"/>.
        /// </summary>
        private static double inverseBiggestPrimeFactorSum = biggestPrimeFactor.Take(coefiterations).Sum(x => 1d / x);
    }
}
