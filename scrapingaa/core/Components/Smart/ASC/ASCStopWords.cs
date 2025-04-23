namespace Core.Components.Smart.ASC.Internal;

using System.IO;
using System.Collections.Generic;
using Core.Log;

public sealed class ASCStopWords : HashSet<string>
{
	public readonly string FilePath;

	public ASCStopWords(in string filePath)
	{
		FilePath = filePath;
	}

	public void LoadFromFile()
	{
		if (!File.Exists(FilePath))
		{
			Log.Error($"Stop words file not found: {FilePath}");
			return;
		}
		else if (Count > 0)
		{
			Log.Warning($"Stop words already loaded: {Count}");
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

		Log.Print($"Loaded stop words: {Count}");
	}
}