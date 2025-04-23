namespace Core.UI.Window;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using Core.Log;

[GlobalClass]
public partial class ProgressWindow : BaseWindow
{
	[Export] public ProgressBar ProgressBar = null!;
	[Export] public Label Label = null!;
	[Export] public Button? CancelButton;

	private float _progress;
	private Stopwatch? _stopwatch; // Таймер для отслеживания времени
	private string _previousETAText = "something";

	public override void _Ready()
	{
		if (Node.IsInstanceValid(CancelButton))
			CancelButton.Pressed += OnCancelButtonPressed;
	}

	/// <summary>
	/// Устанавливает значение прогресса.
	/// </summary>
	/// <param name="value">Значение прогресса (от 0 до 1).</param>
	public void SetProgress(float value)
	{
		if (value < 0 || value > 1)
			throw new ArgumentOutOfRangeException(nameof(value), "Значение прогресса должно быть в диапазоне от 0 до 1.");
		else if (_stopwatch == null)
		{
			_stopwatch = new();
			_stopwatch.Start(); // Запускаем таймер перед началом задачи
		}

		_progress = value;
		ProgressBar.SetValueNoSignal(value * ProgressBar.MaxValue);

		UpdateETA(); // Обновляем ETA при каждом изменении прогресса
	}

	/// <summary>
	/// Обновляет текст надписи.
	/// </summary>
	/// <param name="text">Новый текст.</param>
	public void SetLabel(string text)
	{
		Label.Text = text;
	}

	/// <summary>
	/// Отменяет операцию.
	/// </summary>
	public void Cancel()
	{
		QueueFree();
	}

	private void OnCancelButtonPressed()
	{
		Cancel();
	}

	/// <summary>
	/// Создает новое окно прогресса.
	/// </summary>
	/// <param name="label">Текст надписи.</param>
	/// <param name="showCancelButton">Показывать кнопку отмены?</param>
	/// <returns>Экземпляр ProgressWindow.</returns>
	public static ProgressWindow Create(string label, bool showCancelButton = false)
	{
		PackedScene scene = GD.Load<PackedScene>("res://core/resources/UI/Window/progress_window.tscn");
		var window = scene.Instantiate<ProgressWindow>();
		window.SetLabel(label);
		if (Node.IsInstanceValid(window.CancelButton))
			window.CancelButton.Visible = showCancelButton;
		return window;
	}

	/// <summary>
	/// Показывает окно прогресса и ожидает завершения задачи.
	/// </summary>
	/// <param name="tree">Дерево сцены.</param>
	/// <param name="label">Текст надписи.</param>
	/// <param name="task">Асинхронная задача с обновлением прогресса.</param>
	/// <param name="showCancelButton">Показывать кнопку отмены?</param>
	/// <returns></returns>
	public static async Task ShowAndAwaitAsync(SceneTree tree, string label, Func<ProgressWindow, Task> task, bool showCancelButton = false)
	{
		var window = Create(label, showCancelButton);
		tree.Root.AddChild(window);
		window.Show();

		try
		{
			await task(window);
		}
		catch (Exception ex)
		{
			GD.PrintErr(ex);
			Log.Error(ex.ToString());
		}
		finally
		{
			if (Node.IsInstanceValid(window))
				window.QueueFree();
		}
	}

	/// <summary>
	/// Обновляет метку ETA, основываясь на текущем прогрессе и времени выполнения.
	/// </summary>
	private void UpdateETA()
	{
		if (_stopwatch == null)
		{
			throw new InvalidOperationException("Stopwatch is not initialized");
		}

		if (_progress > 0)
		{
			// Вычисляем прошедшее время и оставшееся время
			double elapsedTime = _stopwatch.Elapsed.TotalSeconds;
			double remainingTime = (elapsedTime / _progress) - elapsedTime; // Оставшееся время

			string etaText = remainingTime > 0 ? $" ({FormatTime(remainingTime)})" : "...";

			// Обновляем текст метки ETA и заголовка
			Label.Text = $"{Label.Text.Replace(_previousETAText, string.Empty)}{etaText}";
			Title = $"{Title.Replace(_previousETAText, string.Empty)}{etaText}";

			_previousETAText = etaText;
		}
	}

	/// <summary>
	/// Форматирует оставшееся время в удобный для чтения вид.
	/// </summary>
	/// <param name="seconds">Оставшееся время в секундах.</param>
	/// <returns>Форматированная строка времени.</returns>
	private string FormatTime(double seconds)
	{
		TimeSpan time = TimeSpan.FromSeconds(seconds);
		return $"{(int)time.TotalMinutes}:{time.Seconds:D2}"; // Формат "минуты:секунды"
	}
}