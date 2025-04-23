using System.Diagnostics;
using System.IO;
using System.Net.Http;

using static Core.Log.Log;

namespace Core.Components.Archive;

public static class ArchiveUtils
{
	public const string ArchivatorPath = @"C:\Program Files\7-Zip\7z.exe";
	public const string DownloadUrl = "https://www.7-zip.org/a/7z2409-x64.exe"; // Ссылка на установщик 7-Zip
	public const string InstallerPath = "7z2409-x64.exe";

	public static void ExtractArchive(string archivePath, string extractionPath, string password = "")
	{
		if (!File.Exists(ArchivatorPath))
		{
			Install7ZipAsync();
		}

		// Создаем процесс для распаковки
		ProcessStartInfo processStartInfo = new ProcessStartInfo
		{
			FileName = ArchivatorPath,
			Arguments = $"x \"{archivePath}\" -o\"{extractionPath}\" -p\"{password}\" -aoa",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using (Process? process = Process.Start(processStartInfo))
		{
			if (process == null)
			{
				Error("Failed to start 7z process");
				return;
			}

			Print($"Starting 7z to extract archive: PID: {process.Id}, Archive path: {archivePath}, Extraction path: {extractionPath}");
			process.WaitForExit();

			// Читаем вывод
			string output = process.StandardOutput.ReadToEnd();
			string error = process.StandardError.ReadToEnd();

			Print(output);

			if (process.ExitCode != 0)
			{
				Error($"Extract archive error: {error}");
			}
			else
			{
				Print("Archive extracted successfully.");
			}
		}
	}

	private static async void Install7ZipAsync()
	{
		Print($"Downloading 7z... ({InstallerPath})");

		var test = new HttpClient();
		var file = await test.GetAsync(DownloadUrl);
		File.WriteAllBytes(InstallerPath, await file.Content.ReadAsByteArrayAsync());

		Print($"Downloaded 7z ({DownloadUrl})");

		// Запускаем установщик
		Process installerProcess = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = InstallerPath,
				Arguments = "/S", // Скрытая установка
				UseShellExecute = true,
				Verb = "runas" // Запуск с правами администратора
			}
		};

		installerProcess.Start();
		installerProcess.WaitForExit();

		// Удаляем установщик после установки
		if (File.Exists(InstallerPath))
		{
			File.Delete(InstallerPath);
		}

		Print("7-Zip installed successfully.");
	}
}