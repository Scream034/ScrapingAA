using Core.UI.Window;
using Godot;

using UX.Option.First.Windows.GetImage;

namespace UX.Option.First.Windows;

public partial class GetImageWindow : BaseWindow
{
	[Export] public GetImageHandler Handler = null!;

	public static GetImageWindow Create(string title = "Получить изображение")
	{
		PackedScene scene = GD.Load<PackedScene>("res://ux/windows/get_image.tscn");
		var window = scene.Instantiate<GetImageWindow>();
		window.Title = title;
		return window;
	}
}