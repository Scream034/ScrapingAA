namespace Core;

using System.IO;
using Godot;

public static class Constants
{
	public static string ProjectName => (string)ProjectSettings.GetSetting("application/config/name");
	public static float Version => (float)ProjectSettings.GetSetting("application/config/version");
	public static readonly bool IsEditorHint = OS.IsDebugBuild() && Engine.IsEditorHint();

	public const string IncorrectTitleString = "И3МеНиТь";
	public static char[] InvalidFileNameChars => System.IO.Path.GetInvalidFileNameChars();
	public static string[] ImageExtensions => ["jpg", "jpeg", "png", "gif", "bmp", "tiff", "webp"];
	public static string[] GodotImageExtensions => ["jpg", "jpeg", "png"];

	/// <summary>
	/// Класс для хранения имен путей.
	/// </summary>
	public static class PathName
	{
		/// <summary>
		/// Подкласс для хранения имен папок.
		/// </summary>
		public static class Folder
		{
			public static readonly string ProgramData = $"data_{ProjectName}_windows_x86_64";
			public const string User = "user";
			public const string Temp = "temp";
			public const string Images = "images";
			public const string Logs = "logs";
			public const string Results = "results";
			public const string Update = "update";
			public const string AiSystemMessages = "asms";
			public const string ASC = "ASC";
			public const string Themes = "themes";
		}

		/// <summary>
		/// Подкласс для хранения имен файлов.
		/// </summary>
		public static class File
		{
			public const string Excel = "result.xlsx";
			public const string Log = "log.log";
			public const string Admin = "admin";
			public const string Provider = "provider";
			public const string SelectedAISystemMessage = "sasm";
			public const string Info = "info.txt";
			public const string InfoOld = "info";
			public const string InfoPrevious = "info.old";
			public const string IsAdded = "add";
			public const string BotBat = "bot.bat";
			public const string MyStem = "mystem.exe";
			public const string StopWords = "stopWords.txt";
			public const string WhiteList = "whiteList.txt";
			public const string AEXSingle = "singleUnits.txt";
			public const string AEXFull = "fullUnits.txt";
			public const string ThemeBase = "base.theme";
			public const string ThemeChoice = "tch.txt";
		}

		public static class CustomExtension
		{
			public const string Freq = "txt";
		}
	}

	/// <summary>
	/// Класс для хранения ключей действий.
	/// </summary>
	public static class Keys
	{
		public static string DeleteRow => "delete_row";
		public static string MoveRow => "move_row";
		public static string MultiSelectLineRow => "multiselect_line_row";
		public static string MultiSelectOneRow => "multiselect_one_row";
	}

	/// <summary>
	/// Класс для хранения путей к файлам и папкам.
	/// </summary>
	public static class Path
	{
		/// <summary>
		/// Подкласс для хранения путей к папкам.
		/// </summary>
		public static class Folder
		{
			public static string ProgramData => ProjectSettings.GlobalizePath("res://" + PathName.Folder.ProgramData);
			public static string User => ProjectSettings.GlobalizePath("res://" + PathName.Folder.User);
			public static string Temp => Core.IO.Path.Join(User, PathName.Folder.Temp);
			public static string Logs => Core.IO.Path.Join(User, PathName.Folder.Logs);
			public static string Results => Core.IO.Path.Join(User, PathName.Folder.Results);
			public static string UserProfile => System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
			public static string Python => Core.IO.Path.Join(UserProfile, "AppData/Local/Programs/Python");
			public static string Python312 => Core.IO.Path.Join(Python, "Python312");
			public static string Update => Core.IO.Path.Join(User, PathName.Folder.Update);
			public static string AiSystemMessages => Core.IO.Path.Join(User, PathName.Folder.AiSystemMessages);
			public static string ASC => Core.IO.Path.Join(User, PathName.Folder.ASC);
			public const string Themes = "res://core/resources/themes";
		}

		/// <summary>
		/// Подкласс для хранения путей к файлам.
		/// </summary>
		public static class File
		{
			public static string Log => Core.IO.Path.Join(Folder.Logs, GenerateLogFileName());
			public static string Excel => Core.IO.Path.Join(Folder.Results, PathName.File.Excel);
			public static string BotBat => Core.IO.Path.Join(Folder.User, PathName.File.BotBat);
			public static string Provider => Core.IO.Path.Join(Folder.User, PathName.File.Provider);
			public static string SelectedAISystemMessage => Core.IO.Path.Join(Folder.User, PathName.File.SelectedAISystemMessage);
			public static string MyStem => Core.IO.Path.Join(Folder.User, PathName.File.MyStem);
			public static string AEXSingle => Core.IO.Path.Join(Folder.ASC, PathName.File.AEXSingle);
			public static string AEXFull => Core.IO.Path.Join(Folder.ASC, PathName.File.AEXFull);
			public static string ASCStopWords => Core.IO.Path.Join(Folder.ASC, PathName.File.StopWords);
			public static string ASCWhiteWords => Core.IO.Path.Join(Folder.ASC, PathName.File.WhiteList);
			public static string ThemeChoice => Core.IO.Path.Join(Folder.User, PathName.File.ThemeChoice);

			/// <summary>
			/// Генерирует имя файла для лога с текущей датой и временем.
			/// </summary>
			/// <returns>Имя файла для лога.</returns>
			private static string GenerateLogFileName()
			{
				return $"{Microsoft.VisualBasic.DateAndTime.Now:yyyyMMdd_HHmmss}_log.log";
			}
		}
	}
}