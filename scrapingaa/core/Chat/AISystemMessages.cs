namespace Core.Chat;

using System.Collections.Generic;
using System.IO;

public class AISystemMessages
{
	public static readonly string DirectoryPath = Path.Combine(Constants.Path.Folder.User, "asms");

	public List<AISystemMessageFile> MessageFiles = new();

	public AISystemMessages()
	{
		Directory.CreateDirectory(DirectoryPath);
	}

	public void Load()
	{
		LoadFromDirectory(DirectoryPath);
	}

	public void LoadFromDirectory(string directoryPath)
	{
		MessageFiles.Clear();

		foreach (string filePath in Directory.GetFiles(directoryPath))
		{
			MessageFiles.Add(new AISystemMessageFile(filePath));
		}
	}
}

public class AISystemMessageFile
{
	public readonly string FilePath;
	public readonly AISystemMessage message;

	public AISystemMessageFile(string filePath)
	{
		if (File.Exists(filePath))
		{
			message = new AISystemMessage(File.ReadAllText(filePath));
		}
		else
		{
			throw new FileNotFoundException($"File not found: {filePath}");
		}

		FilePath = filePath;
	}
}

public class AISystemMessage
{
	public AISystemMessage(string text)
	{
		Text = text;
	}

	public string Text { get; set; }
}