namespace Core.IO;

using System;
using System.Linq;
using Godot;

using Log;

public static class Directory
{
	public static bool Exists(in string path) => DirAccess.DirExistsAbsolute(path) || System.IO.Directory.Exists(path);

	public static string? GetPathAbsolute(in string path)
	{
		DirAccess dir = DirAccess.Open(path);
		Error error = DirAccess.GetOpenError();
		if (error != Error.Ok)
		{
			Log.Error($"Failed to open directory {path}: {error}");
			return null;
		}

		return dir.GetCurrentDir();
	}

	public static string? GetName(in string path)
	{
		if (Exists(path))
		{
			var parts = path.Split('/');
			return parts[^1];
		}
		else if (File.Exists(path))
		{
			var parts = path.Split('/');
			return parts[^2];
		}

		return null;
	}

	public static bool Remove(in string path)
	{
		if (DirAccess.RemoveAbsolute(path) is not Error.Ok)
		{
			try
			{
				System.IO.Directory.Delete(path, true);
				return true;
			}
			catch (Exception ex)
			{
				Log.Error($"Failed to remove directory {path}: {ex.Message}");
			}
		}

		return false;
	}
}