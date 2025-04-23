namespace Core.IO;

using System;
using System.Linq;
using Godot;

using Log;

public static class File
{
	public static bool Exists(in string path) => FileAccess.FileExists(path) || System.IO.File.Exists(path);

	public static string GetExtension(in string path) => StringExtensions.GetExtension(path);

	public static string? GetPathAbsolute(in string path)
	{
		if (!Exists(path)) return null;

		FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		Error error = FileAccess.GetOpenError();
		if (error != Error.Ok) return null;

		return file.GetPathAbsolute();
	}
}