namespace Core.Components.Smart.ASC.Internal;

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Core.Log;

public sealed class ASCFrequenciesData : ConcurrentDictionary<ProductCharacteristics.Type, ConcurrentDictionary<string, ASCharacteristicValue>>
{
	public readonly string DirectoryPath;
	public readonly string FileExtension;

	public ASCFrequenciesData(string directoryPath, string fileExtension)
	{
		DirectoryPath = directoryPath;
		FileExtension = fileExtension;
	}

	public new void Clear()
	{
		base.Clear();
		foreach (var type in ProductCharacteristics.TypeNames)
		{
			if (type.Key == ProductCharacteristics.Type.None || type.Key == ProductCharacteristics.Type.Unknown) continue;
			TryAdd(type.Key, new());
		}
	}

	public void SaveToFile()
	{
		Parallel.ForEach(this, storage =>
		{
			File.WriteAllText(_GetPath(storage.Key), ToStringIO(storage.Value), Encoding.UTF8);
			Log.Print($"Saved: {storage.Key}");
		});
	}

	public void LoadFromFile()
	{
		Parallel.ForEach(ProductCharacteristics.TypeNames, type =>
		{
			if (type.Key == ProductCharacteristics.Type.None || type.Key == ProductCharacteristics.Type.Unknown) return;
			AddOrUpdate(type.Key, _ => GetFromStringIO(type.Key), (_, _) => GetFromStringIO(type.Key));
			Log.Print($"Loaded: {type.Key}");
		});
	}

	public string ToStringIO(ConcurrentDictionary<string, ASCharacteristicValue> singleCategory)
	{
		StringBuilder sb = new();
		foreach (var pair in singleCategory)
		{
			sb.AppendLine($"{pair.Key} {pair.Value.Value} {pair.Value.IdfValue}");
		}
		return sb.ToString();
	}

	public ConcurrentDictionary<string, ASCharacteristicValue> GetFromStringIO(ProductCharacteristics.Type type)
	{
		ConcurrentDictionary<string, ASCharacteristicValue> frequencies = new();

		string path = _GetPath(type);
		if (!File.Exists(path))
		{
			Log.Error($"Frequencies file not found: {path}");
			return frequencies;
		};

		using (FileStream fileStream = File.OpenRead(path))
		{
			using (StreamReader reader = new StreamReader(fileStream))
			{
				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine()!;
					string[] split = line.Split(' ', 3, StringSplitOptions.None);
					frequencies.TryAdd(split[0], new(double.Parse(split[1]), double.Parse(split[2])));
				}
			}
		}

		return frequencies;
	}

	private string _GetPath(ProductCharacteristics.Type type)
	{
		return Path.Join(DirectoryPath, Path.ChangeExtension(type.ToString(), FileExtension));
	}
}