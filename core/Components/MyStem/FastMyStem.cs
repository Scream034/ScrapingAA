namespace Core.Components.MyStem;

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Core.Log;

/// <summary>
/// <b>Cannot be used in a multithreaded environment!</b> A class for interacting with the MyStem executable.
/// </summary>
public sealed class FastMyStem : IDisposable
{
	/// <summary>
	/// The default timeout for reading data from the MyStem process.
	/// </summary>
	public static readonly int TimeoutMs = 50;

	/// <summary>
	/// Factor used to estimate the initial total buffer size for reading the output.
	/// </summary>
	public static float TotalBufferFactorSize = 3.5f;

	/// <summary>
	/// Factor used to estimate the initial step buffer size for reading the output.
	/// </summary>
	public static float StepBufferFactorSize = 2.5f;

	/// <summary>
	/// The string that marks the end of the input for MyStem.
	/// </summary>
	public static string EndString = "\nъъ";

	/// <summary>
	/// The string that is replaced with an empty string in the output.
	/// </summary>
	public static string EndReplaceString = "ъъ??\r\n";

	private static readonly Encoding encoding = Encoding.UTF8;

	/// <summary>
	/// The process instance for the MyStem executable.
	/// </summary>
	private Process? mystemProcess;

	/// <summary>
	/// Indicates whether the object has been disposed.
	/// </summary>
	private bool disposed = false;

	/// <summary>
	/// The options to configure the MyStem process.
	/// </summary>
	public MyStemOptions Options { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="FastMyStem"/> class with the specified options. <b>Requires the <c>LineByLine</c> option to be set to true.</b>
	/// </summary>
	/// <param name="options">The MyStem options to use.</param>
	public FastMyStem(MyStemOptions? options = null)
	{
		Options = options ?? new MyStemOptions();
		Options.LineByLine = true; // Required for stream reading
	}

	/// <summary>
	/// Initializes the MyStem process if it's not already running or if the processed text length exceeds the maximum limit.
	/// This method is thread-safe.
	/// </summary>
	private void InitializeProcess()
	{
		if (mystemProcess == null || mystemProcess.HasExited)
		{
			mystemProcess?.Dispose();
			mystemProcess = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = MyStemOptions.PathToMyStem,
					Arguments = Options.GetArguments(),
					UseShellExecute = false,
					RedirectStandardInput = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden,
					StandardOutputEncoding = Encoding.UTF8
				}
			};

			if (!mystemProcess.Start())
			{
				throw new Exception("Failed to start MyStem process.");
			}
		}
	}

	/// <summary>
	/// Analyzes the given text using the MyStem executable.
	/// </summary>
	/// <param name="text">The text to analyze.</param>
	/// <returns>The analysis result from MyStem.</returns>
	/// <exception cref="FileNotFoundException">If the MyStem executable is not found at the specified path.</exception>
	/// <exception cref="FormatException">If an error occurs during the MyStem analysis.</exception>
	public string MultiAnalysis(string text)
	{
		try
		{
			InitializeProcess();
			return GetResults(text);
		}
		catch (Exception ex)
		{
			Log.Error(ex, $"Error during MyStem analysis for text: '{text}'");
			throw new FormatException($"Error during MyStem analysis. See logs for details. Text: '{text}'", ex);
		}
	}

	/// <summary>
	/// Считывает результаты анализа от процесса MyStem синхронно, накапливая байты и декодируя строку один раз.
	/// </summary>
	/// <param name="inputText">Входной текст, отправляемый в MyStem.</param>
	/// <returns>Строка с результатами анализа.</returns>
	private string GetResults(string inputText)
	{
		// Добавляем завершающую последовательность к входному тексту
		inputText += EndString;
		mystemProcess!.StandardInput.WriteLine(inputText);
		mystemProcess.StandardInput.Flush();

		// Создаем MemoryStream для накопления всех байт
		MemoryStream memoryStream = new((int)Math.Round(inputText.Length * TotalBufferFactorSize));

		// Размер буфера определяется как функция от размера входного текста
		byte[] byteBuffer = new byte[(int)Math.Round(inputText.Length * StepBufferFactorSize)];

		int totalBytesRead = 0;
		bool timeoutOccurred = false;

		// Основной цикл чтения
		while (mystemProcess.StandardOutput.BaseStream.CanRead)
		{
			int bytesRead = 0;
			if (!timeoutOccurred)
			{
				try
				{
					// Синхронное чтение из BaseStream
					bytesRead = mystemProcess.StandardOutput.BaseStream.Read(byteBuffer, 0, byteBuffer.Length);
				}
				catch (Exception ex)
				{
					Log.Warning($"MyStem reading error: {inputText}. Exception: {ex.Message}");
					break;
				}
			}
			else
			{
				IAsyncResult asyncResult = null!;
				try
				{
					// Асинхронное чтение с явным ожиданием
					asyncResult = mystemProcess.StandardOutput.BaseStream.BeginRead(byteBuffer, 0, byteBuffer.Length, null, null);
					if (asyncResult.AsyncWaitHandle.WaitOne(TimeoutMs))
					{
						bytesRead = mystemProcess.StandardOutput.BaseStream.EndRead(asyncResult);
					}
					else
					{
						Log.Warning($"MyStem reading timeout: {inputText}");
					}
				}
				catch (Exception ex)
				{
					Log.Warning($"MyStem reading exception: {inputText}. Exception: {ex.Message}");
				}
				finally
				{
					asyncResult?.AsyncWaitHandle.Close();
				}
			}

			if (bytesRead <= 0)
			{
				// Если данных больше нет – завершаем чтение
				break;
			}

			totalBytesRead += bytesRead;
			// Записываем прочитанные байты в MemoryStream
			memoryStream.Write(byteBuffer, 0, bytesRead);

			// Декодируем только что полученный кусок, чтобы проверить наличие завершающей последовательности "ъъ"
			string chunk = encoding.GetString(byteBuffer, 0, bytesRead);
			if (chunk.IndexOf("ъъ", StringComparison.Ordinal) >= 0)
			{
				break;
			}

			// Если набрано достаточное количество байт, переключаемся в режим с таймаутом
			if (!timeoutOccurred && totalBytesRead >= inputText.Length)
			{
				timeoutOccurred = true;
			}
		}

		// Сбрасываем позицию в начале MemoryStream для декодирования
		memoryStream.Seek(0, SeekOrigin.Begin);
		// Декодируем все накопленные байты за один раз и заменяем специальную последовательность, если она есть
		string result = encoding.GetString(memoryStream.ToArray()).Replace(EndReplaceString, string.Empty);

		memoryStream.Dispose();
		return result;
	}

	/// <summary>
	/// Disposes of the resources used by the <see cref="FastMyStem"/> object.
	/// </summary>
	public void Dispose()
	{
		if (!disposed)
		{
			if (mystemProcess != null)
			{
				if (!mystemProcess.HasExited)
				{
					try
					{
						mystemProcess.Kill();
					}
					catch { /* Ignore errors during termination */ }
				}
				mystemProcess.Dispose();
			}
			disposed = true;
		}
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Finalizes the <see cref="FastMyStem"/> object before it is reclaimed by garbage collection.
	/// </summary>
	~FastMyStem() => Dispose();
}