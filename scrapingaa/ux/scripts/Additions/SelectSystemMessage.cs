using System.Collections.Generic;

using Godot;

using Core.Chat;
using Core.Log;

namespace UX.Additions;

public partial class SelectSystemMessage : Control
{
	AISystemMessages SystemMessages = new();
	OptionButton optionButton = null!;

	private Dictionary<string, AISystemMessageFile> _messageByFileName = new();

	public override void _Ready()
	{
		optionButton = GetNode<OptionButton>("OptionButton");
		optionButton.Connect(OptionButton.SignalName.ItemSelected, new(this, nameof(OnSelectSystemMessage)));
		InitAISystemMessages();
	}

	public override void _ExitTree()
	{
		SaveSelected();
	}

	public void InitAISystemMessages()
	{
		SystemMessages.Load();
		foreach (var messageFile in SystemMessages.MessageFiles)
		{
			string fileName = System.IO.Path.GetFileNameWithoutExtension(messageFile.FilePath);
			optionButton.AddItem(fileName);
			_messageByFileName.Add(fileName, messageFile);
		}

		string? sasm = Global.Instance.LoadSelectedAISystemMessage();
		if (sasm != null)
		{
			Global.Instance.ChatBot.SystemMessage = _messageByFileName[sasm].message.Text;
		}
	}

	public void SaveSelected()
	{
		string? sasm = optionButton.GetItemText(optionButton.GetSelected());
		if (sasm != null)
		{
			Global.Instance.ChatBot.SystemMessage = _messageByFileName[sasm].message.Text;
			Global.Instance.SaveSelectedAISystemMessage(sasm);
		}
	}

	public void OnSelectSystemMessage(int index)
	{
		string fileName = optionButton.GetItemText(index);
		Log.Print($"Selected system message: {fileName}");
		Global.Instance.ChatBot.SystemMessage = _messageByFileName[fileName].message.Text;
	}
}
