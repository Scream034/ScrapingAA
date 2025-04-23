namespace Core.Components.Processes;

using System.Threading;
using System.Diagnostics;
using Godot;

public static class ProcessActions
{
	public static class Constants
	{
		public const int WaitInMs = 100;
	}

	public static bool WaitForOutput(Process process, string expectedOutput, int timeout = 10_000)
	{
		ManualResetEvent outputReceived = new ManualResetEvent(false);

		// Обработчик вывода
		process.OutputDataReceived += (sender, e) =>
		{
			if (!string.IsNullOrEmpty(e.Data))
			{
				Log.Log.Print(e.Data);
				if (e.Data.Contains(expectedOutput))
				{
					outputReceived.Set(); // Устанавливаем сигнал, если вывод содержит нужную строку
				}
			}
		};

		Process.GetProcesses();

		if (Process.GetProcessById(process.Id) != null)
		{
			process.BeginOutputReadLine(); // Начинаем асинхронное чтение
		}

		int currentTimeout = 0;

		// Ожидаем, пока будет установлен сигнал или процесс завершится
		while (!outputReceived.WaitOne(Constants.WaitInMs))
		{
			if (process.HasExited)
			{
				return false;
			}
			else if (currentTimeout >= timeout)
			{
				return false;
			}

			currentTimeout += Constants.WaitInMs;
		}

		return true; // Ожидаемая строка была получена
	}
}