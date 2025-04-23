namespace Core.Components.Smart.AEXU;

using Log;

using System.IO;
using System.Text;
using System.Collections.Generic;
using Core.Components;
using System;
using System.Collections.Concurrent;

/// <summary>
/// Algorithm for Extraction of Units
/// </summary>
public sealed class AEXUnits
{
	public const float SaveToCacheThreshold = 0.416f;
	public const float SimilarityThreshold = 0.7f;

	public List<AEXUnit> SingleUnits { get; private set; } = new();
	public List<AEXUnit> FullUnits { get; private set; } = new();

	public readonly string SingleUnitsPath;
	public readonly string FullUnitsPath;

	private ConcurrentDictionary<string, Tuple<float, AEXUnit?>> cachedSimilarity = new();

	public AEXUnits(string singleUnitsPath, string fullUnitsPath)
	{
		SingleUnitsPath = singleUnitsPath;
		FullUnitsPath = fullUnitsPath;
	}

	public List<Tuple<float, AEXUnit>> Extract(AEXUToken token)
	{
		List<Tuple<float, AEXUnit>> list = new();

		// Собираем наиболее вероятные токены
		var (bestWords, bestTokens) = _GetBestWords(token);

		foreach (var bestWord in bestWords)
		{
			if (bestWord.Item1 > SimilarityThreshold)
			{
				list.Add(bestWord!);
			}
			else
			{
				// Иначе пробуем комбинировать слова и вычислять вероятность
				Tuple<float, AEXUnit?> maxTuple = new(0, null);
				string myBestToken = bestTokens[bestWords.IndexOf(bestWord)];
				foreach (string bestToken in bestTokens)
				{
					if (bestToken == myBestToken) continue;

					var tuple = GetMaxSimilarity(myBestToken + ' ' + bestToken);
					if (tuple.Item1 > maxTuple.Item1)
					{
						maxTuple = tuple;
					}
				}

				if (maxTuple.Item2 != null && maxTuple.Item1 > SimilarityThreshold)
				{
					list.Add(maxTuple!);
				}
			}
		}

		return list;
	}

	public string GetStringWithoutUnits(AEXUToken aexToken)
	{
		List<string> bestTokens = new();
		foreach (string word in aexToken.Tokens)
		{
			var tuple = GetMaxSimilarityInternal(word, SaveToCacheThreshold * 1.1f);
			if (tuple.Item2 != null)
			{
				bestTokens.Add(word);
			}
		}

		StringBuilder buffer = new();

		foreach (string token in aexToken.Tokens)
		{
			if (bestTokens.Contains(token))
			{
				continue;
			}

			buffer.Append(token);
		}

		return buffer.ToString();
	}

	private Tuple<List<Tuple<float, AEXUnit?>>, List<string>> _GetBestWords(AEXUToken token)
	{
		List<Tuple<float, AEXUnit?>> bestWords = new();
		List<string> bestTokens = new();
		foreach (string word in token.Tokens)
		{
			var tuple = GetMaxSimilarity(word);
			if (tuple.Item2 != null)
			{
				bestWords.Add(tuple);
				bestTokens.Add(word);
			}
		}

		return new(bestWords, bestTokens);
	}

	private Tuple<float, AEXUnit?> GetMaxSimilarity(string word)
	{
		if (cachedSimilarity.TryGetValue(word, out var tuple))
		{
			return tuple;
		}

		return GetMaxSimilarityInternal(word, SaveToCacheThreshold);
	}

	private Tuple<float, AEXUnit?> GetMaxSimilarityInternal(string word, float minThreshold = 0.5f)
	{
		float maxSimilarity = 0;
		AEXUnit? maxUnit = null;

		foreach (var unit in FullUnits)
		{
			float similarity = StringUtils.GetDamerauLevenshteinSimilarity(word, unit.Name);
			if (similarity >= minThreshold && similarity > maxSimilarity)
			{
				maxSimilarity = similarity;
				maxUnit = unit.Clone(word);
			}
		}

		if (maxSimilarity == 0)
		{
			foreach (var unit in SingleUnits)
			{
				float similarity = StringUtils.GetDamerauLevenshteinSimilarity(word, unit.Name);
				if (similarity >= minThreshold && similarity > maxSimilarity)
				{
					maxSimilarity = similarity;
					maxUnit = unit.Clone(word);
				}
			}
		}

		Tuple<float, AEXUnit?> tuple = new(maxSimilarity, maxUnit);
		cachedSimilarity.TryAdd(word, tuple);
		return tuple;
	}

	public void LoadSingleUnits()
	{
		if (!File.Exists(SingleUnitsPath))
		{
			Log.Error("File not found: " + SingleUnitsPath);
			return;
		}

		using (FileStream file = new FileStream(SingleUnitsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
		{
			using (StreamReader reader = new StreamReader(file, Encoding.UTF8))
			{
				while (!reader.EndOfStream)
				{
					string? line = reader.ReadLine();

					if (AEXUnit.Parse(line) is AEXUnit unit)
					{
						unit.IsSingle = true;
						SingleUnits.Add(unit);
					}
				}
			}
		}

		Log.Print($"Loaded {SingleUnits.Count} units");
	}

	public void LoadFullUnits()
	{
		if (!File.Exists(FullUnitsPath))
		{
			Log.Error("File not found: " + FullUnitsPath);
			return;
		}

		using (FileStream file = new FileStream(FullUnitsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
		{
			using (StreamReader reader = new StreamReader(file, Encoding.UTF8))
			{
				while (!reader.EndOfStream)
				{
					string? line = reader.ReadLine();

					if (AEXUnit.Parse(line) is AEXUnit unit)
					{
						unit.IsSingle = false;
						FullUnits.Add(unit);
					}
				}
			}
		}

		Log.Print($"Loaded {FullUnits.Count} units");
	}

	public void LoadUnits()
	{
		LoadSingleUnits();
		LoadFullUnits();
	}
}