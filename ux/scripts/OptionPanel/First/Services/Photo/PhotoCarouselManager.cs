using System;
using Godot;
using Core;
using Core.Log;

using UX.Option.First.Windows;
using UX.Components.Table;

namespace UX.Option.First.Services.Photo;

public class PhotoCarouselManager
{
	private PhotoCarouselWindow? _window;

	public event Action WindowClosed = null!;

	public void EnsureWindowInitialized()
	{
		if (_window == null || !Node.IsInstanceValid(_window))
		{
			_window = PhotoCarouselWindow.Create();
			Global.Instance.GetTree().Root.AddChild(_window);
		}

		if (_window.Name != nameof(PhotoCarouselWindow))
		{
			_window.Name = nameof(PhotoCarouselWindow);
		}
	}

	public void ShowWindow()
	{
		if (_window == null)
		{
			Log.Print("PhotoCarouselWindow is null.");
			return;
		}

		_window.PopupCentered();
		_window.Borderless = true;
		_window.TreeExited += OnWindowClosed;
	}

	public void LoadImages(ProductTableRow xRow)
	{
		if (_window == null || !Node.IsInstanceValid(_window))
		{
			Log.Print("PhotoCarouselWindow is not valid.");
			return;
		}

		_window.Handler.ClearImages();
		_window.Handler.LoadImagesFromProduct(xRow);
		_window.Handler.ShowImageAtIndex(0);
	}

	private void OnWindowClosed()
	{
		WindowClosed?.Invoke();
		_window!.TreeExited -= OnWindowClosed;
	}
}