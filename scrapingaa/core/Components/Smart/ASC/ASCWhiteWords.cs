namespace Core.Components.Smart.ASC.Internal;

using System.IO;
using System.Collections.Generic;
using Core.Log;

public sealed class ASCWhiteWords : HashSet<string>
{
	public readonly string FilePath;

	public ASCWhiteWords(in string filePath)
	{
		FilePath = filePath;
	}

	public void LoadFromFile()
	{
		if (!File.Exists(FilePath))
		{
			Log.Error($"White words file not found: {FilePath}");
			return;
		}
		else if (Count > 0)
		{
			Log.Warning($"White words already loaded: {Count}");
			return;
		}

		using (FileStream fileStream = File.OpenRead(FilePath))
		{
			using (StreamReader reader = new StreamReader(fileStream))
			{
				while (!reader.EndOfStream)
				{
					Add(reader.ReadLine()!);
				}
			}
		}

		Log.Print($"Loaded white words: {Count}");
	}
}