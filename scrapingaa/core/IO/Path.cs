namespace Core.IO;

using System;
using System.Linq;
using Godot;

using Log;

public static class Path
{
	public static string Join(params string[] paths) => paths.Aggregate(StringExtensions.PathJoin);

	public static bool Exists(in string path) => File.Exists(path) || Directory.Exists(path);

	public static string? GetPathAbsolute(in string path)
	{
		string? result = Directory.GetPathAbsolute(path);
		if (string.IsNullOrEmpty(result))
		{
			result = File.GetPathAbsolute(path);
		}

		return result;
	}
}