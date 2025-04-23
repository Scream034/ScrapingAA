namespace UX;

using Godot;
using Core.Log;

public partial class Exit : Button
{
	public override void _Pressed()
	{
		Log.Print("Program will exit");
		GetTree().Quit();
	}
}
