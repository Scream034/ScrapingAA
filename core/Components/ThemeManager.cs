namespace Core.Components;

using System;
using Microsoft.Win32;
using Godot;
using Core.Log;
using Core.IO;

public sealed class ThemeManager
{
	public enum Theme
	{
		Auto,
		Dark,
		Light
	}

	public readonly string ThemeFolder;
	public readonly string ThemeBase;
	public readonly string ThemeChoice;

	public Godot.Theme CurrentTheme { get; private set; } = null!;
	public string? CurrentThemeName => Directory.GetName(CurrentTheme.ResourcePath);

	public ThemeManager(string themeFolder, string themeBase, string themeChoice)
	{
		ThemeFolder = themeFolder;
		ThemeBase = themeBase;
		ThemeChoice = themeChoice;
	}

	public void SetCurrentTheme(in Theme theme)
	{
		switch (theme)
		{
			case Theme.Dark:
				SetCurrentThemeInternal("dark");
				break;

			case Theme.Light:
				SetCurrentThemeInternal("light");
				break;

			case Theme.Auto:
				SetCurrentThemeInternal(GetThemeInSystem() ?? "light");
				break;
		}

		if (!Save())
		{
			Log.Warning("Failed to save theme choice");
		}
	}

	public string? GetThemeInSystem()
	{
		string? themeName = null;
		using (RegistryKey? key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize"))
		{
			if (key != null)
			{
				object? value = key.GetValue("AppsUseLightTheme");
				if (value != null)
				{
					themeName = (int)value == 0 ? "dark" : "light";
				}
			}
		}
		return themeName;
	}

	public bool Save()
	{
		if (!Node.IsInstanceValid(CurrentTheme)) return false;

		if (string.IsNullOrEmpty(CurrentThemeName))
		{
			Log.Warning($"Theme {CurrentTheme.ResourcePath} is not in a valid directory");
			return false;
		}

		var file = FileAccess.Open(ThemeChoice, FileAccess.ModeFlags.Write);
		var error = FileAccess.GetOpenError();
		if (error != Error.Ok)
		{
			Log.Error($"Failed to open {ThemeChoice} for writing: {error}");
			return false;
		}

		bool result = file.StoreString(CurrentThemeName);
		file.Close();
		return result;
	}

	public bool Load()
	{
		if (!FileAccess.FileExists(ThemeChoice)) return false;

		var file = FileAccess.Open(ThemeChoice, FileAccess.ModeFlags.Read);
		var error = FileAccess.GetOpenError();
		if (error != Error.Ok)
		{
			Log.Error($"Failed to open {ThemeChoice} for reading: {error}");
			file.Close();
			return false;
		}

		var themeName = file.GetAsText();
		file.Close();
		if (string.IsNullOrWhiteSpace(themeName)) return false;

		return SetCurrentThemeInternal(themeName);
	}

	private bool SetCurrentThemeInternal(in string themeName)
	{
		var theme = GetCurrentThemeInternal(themeName);
		if (!Node.IsInstanceValid(theme))
		{
			return false;
		}

		ProjectSettings.SetSetting("gui/theme/custom", theme);
		Global.Instance.GetTree().Root.Theme = theme;
		CurrentTheme = theme;

		return true;
	}

	private Godot.Theme? GetCurrentThemeInternal(in string themeName)
	{
		Godot.Theme? theme = null;

		try
		{
			theme = ResourceLoader.Load<Godot.Theme>(Core.IO.Path.Join(ThemeFolder, themeName, ThemeBase));
		}
		catch (InvalidCastException)
		{
			Log.Error($"Theme {themeName} is not a Godot.Theme");
		}

		return theme;
	}
}