using Core;
using Core.Components.Smart.AEXU;
using Core.Components.Smart.ASC;
using Core.Components.Smart.ASC.Internal;
using Core.Components.Smart.ASC.Internal.Tests;
using Godot;

public sealed partial class TestNode : Node
{
	public override async void _Ready()
	{
		Global.Instance.SetSite("istk");
		ASCFrequenciesData data = new(Constants.Path.Folder.ASC, Constants.PathName.CustomExtension.Freq);
		ASCClassifier classifier = new(new(Constants.Path.File.AEXSingle, Constants.Path.File.AEXFull), data);
		Tests_ASCAVGError tests = new(classifier);
		tests.Products = await Global.Instance.Manager!.Parser.GetIOProductsAsync();
		tests.Test(); //  14,48 30.03.2025

		// ASCFrequencyCreator creator = new(data);
		// creator.Create(await Global.Instance.Manager!.Parser.GetIOProductsAsync());
		// data.SaveToFile();
	}
}