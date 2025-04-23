using Core.UI.Window;
using Godot;

using UX.Option.First.Windows.Photo;

namespace UX.Option.First.Windows;

public partial class PhotoCarouselWindow : BaseWindow
{
    [Export] public PhotoCarouselHandler Handler = null!;

    public static PhotoCarouselWindow Create(string title = "Изображения")
    {
        PackedScene scene = GD.Load<PackedScene>("res://ux/windows/photo_carousel.tscn");
        var window = scene.Instantiate<PhotoCarouselWindow>();
        window.Title = title;
        return window;
    }
}