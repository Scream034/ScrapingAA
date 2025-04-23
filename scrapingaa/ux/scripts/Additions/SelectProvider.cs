using Godot;

using Core.Log;

namespace UX.Additions;

public partial class SelectProvider : Control
{
	OptionButton optionButton = null!;

	public override async void _Ready()
	{
		optionButton = GetNode<OptionButton>("OptionButton");
		optionButton.Connect(OptionButton.SignalName.ItemSelected, new(this, nameof(OnSelectProvider)));

		if (await Global.Instance.ChatBot.StartServerAsync())
		{
			InitProvidersAsync();
		}
	}

	public async void InitProvidersAsync()
	{
		var providers = await Global.Instance.ChatBot.GetProvidersAsync();
		foreach (var provider in providers)
		{
			optionButton.AddItem(provider.Name);
			if (provider.Name == Global.Instance.Provider.Name) optionButton.Select(optionButton.ItemCount - 1);
		}
	}

	public void OnSelectProvider(int index)
	{
		string provider = optionButton.GetItemText(index);
		Log.Print($"Selected provider: {provider}");
		Global.Instance.Provider = new(provider);
	}
}
