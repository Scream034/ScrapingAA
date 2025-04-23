#if false

using Core.Extensions;
using Core.Log;
using Core.Utils;
using Godot;
using System;
using System.Collections.Generic;

public partial class TestNode : Node
{
	public override void _Ready()
	{
		GD.Print("TEST");
		GD.Print("TEST");
		GD.Print("TEST");

		// string fullTitle = "КРУВИС КПМп (КПМп-12, КПМп-10, КПМп-8, КПМп-6)";
		// string mainTitle = "КПМп-12";
		// string[] targetTitles = ["КПМп-12", "КПМп-10", "КПМп-8", "КПМп-6"];

		string fullTitle = "КРУВИС КПМ-8 (КПМ-6, КПМ-4) (блочно-модульный цфв)";
		string? mainTitle = "КПМ-8";
		string[] targetTitles = ["КПМ-8", "КПМ-6", "КПМ-4"];

		string? @out = GetProductMainTitle(fullTitle, mainTitle);

		GD.Print("Main -> ", mainTitle);
		GD.Print("Out -> ", @out);

		foreach (string targetTitle in targetTitles)
		{
			GD.Print(FormatProductTitle(@out, fullTitle, mainTitle, targetTitle));
		}

		GetTree().Quit();
	}

	public static string FormatProductTitle(in string? @out, in string fullTitle, in string? mainTitle, in string targetTitle)
	{
		// Если скобок нет
		if (string.IsNullOrEmpty(@out))
		{
			// Возвращаем полное название с целевым названием
			return fullTitle + ' ' + targetTitle;
		}
		else if (string.IsNullOrWhiteSpace(mainTitle) || !@out.Contains(mainTitle))
		{
			// Если главное название не найдено
			try
			{
				string common = StringUtils.LongestCommonSubstring(@out, targetTitle);
				Log.Print("ALCSR:\n", "Common:", common, "\n Target:", targetTitle, "\n Result:", @out.Replace(common, targetTitle));
			}
			catch (Exception ex)
			{
				Log.Error("ALCSR:", ex.Message);
			}

			// Возвращаем без скобок с целевым названием
			return @out + ' ' + targetTitle;
		}

		// Возвращаем без скобок и с заменой главного названия на целевое название
		return @out.Replace(mainTitle, targetTitle);
	}

	public static string? GetProductMainTitle(in string fullTitle, in string suggestedMainTitle)
	{
		if (string.IsNullOrEmpty(fullTitle) || string.IsNullOrEmpty(suggestedMainTitle))
			return null;

		string? @out = null;
		string[] perhaps = fullTitle.FindMatches(@"\((?<in>.*?)\)");
		if (perhaps.Length > 0)
		{
			@out = fullTitle;
			foreach (var per in perhaps)
			{
				@out = @out.Replace('(' + per + ')', string.Empty);
			}
			@out = @out.ReduceWhitespace().Trim();
		}

		return @out;
	}
}

#endif