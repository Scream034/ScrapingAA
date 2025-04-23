#if false

namespace Core.Components.Smart.ASC.Internal.Tests;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Core.Components.Characteristics;
using Core.Log;

public sealed class Tests_RandomFindHyperParameters
{
	public Tests_ASCAVGError ASCAVGError;

	public Data? BestData;

	public Tests_RandomFindHyperParameters(in ASCClassifier classifier)
	{
		ASCAVGError = new(classifier);
	}

	public void Random(int totalCount = 100)
	{
		float bestAvgError = 0;
		Parallel.For(0, totalCount, (i) =>
		{
			Log.Print($"Iteration {i + 1}/{totalCount}");

			Data data = new()
			{
				ScoreAEXUnitsCoefficient = System.Random.Shared.NextSingle() * 2,
				ScoreAEXUnitsDoubtfulCoefficient = System.Random.Shared.NextSingle() * 2,
				ScoreDifferentFactor = System.Random.Shared.NextSingle() * 20,
				MinWordLength = System.Random.Shared.Next(3, 6)
			};

			ASCConstants.ScoreAEXUnitsCoefficient = data.ScoreAEXUnitsCoefficient;
			ASCConstants.ScoreAEXUnitsDoubtfulCoefficient = data.ScoreAEXUnitsDoubtfulCoefficient;
			ASCConstants.ScoreDifferentFactor = data.ScoreDifferentFactor;
			ASCConstants.MinWordLength = data.MinWordLength;

			float avgError = ASCAVGError.Test(new(new() { PrintOnlyLemmasAndGrammemes = true }));
			if (avgError > bestAvgError)
			{
				bestAvgError = avgError;
				BestData = data;
			}
		});

		Log.Print($"Best avg error: {bestAvgError}");
		Log.Print($"Best data: {BestData}");
	}

	public sealed record class Data(in float ScoreAEXUnitsCoefficient = 0.51f, in float ScoreAEXUnitsDoubtfulCoefficient = 0.21213f, in float ScoreDifferentFactor = 10f, in int MinWordLength = 4) { }
}
#endif