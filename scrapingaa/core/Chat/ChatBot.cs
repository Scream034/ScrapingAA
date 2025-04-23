namespace Core.Chat;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

using Core.Log;
using Core.Components.Processes;

public class Provider
{
	public Provider() { }

	public Provider(string? name = null, string? loginUrl = null, bool auth = false)
	{
		Name = name;
		LoginUrl = loginUrl;
		Auth = auth;
	}

	public Provider(JsonElement json)
	{
		LoginUrl = json.TryGetProperty("login_url", out var loginUrl) ? loginUrl.GetString() : null;
		Auth = json.TryGetProperty("auth", out var auth) ? auth.GetBoolean() : false;
		Name = json.TryGetProperty("name", out var name) ? name.GetString() : null;
	}

	public string? Name { get; set; }
	public string? LoginUrl { get; set; }
	public bool Auth { get; set; }
}

public class ChatBot
{
	public const ushort MaxAttemptsConnectToServer = 10;
	public bool IsServerStarted { get; private set; }
	public string SystemMessage;

	private readonly HttpClient _client;
	private readonly Uri _uri;
	private readonly string _model;
	private readonly string _chatId;
	private readonly string _conversationId;
	private readonly string _apiKey;


	public ChatBot(string url, string systemMessage, string model = "gpt-3.5-turbo", string chatId = "0", string conversationId = "1", string apiKey = "")
	{
		_uri = new Uri(url);
		SystemMessage = systemMessage;
		_model = model;
		_chatId = chatId;
		_conversationId = conversationId;
		_apiKey = apiKey;
		_client = new HttpClient();
	}

	public async Task<bool> StartServerAsync()
	{
		if (IsServerStarted)
		{
			return true; // Сервер уже запущен
		}

		// Попробуем подключиться к серверу, чтобы проверить его статус
		bool serverIsRunning = await CheckIfServerRunningAsync();

		if (serverIsRunning)
		{
			IsServerStarted = true;
			return true; // Сервер уже работает
		}

		CreateBatFile();

		Process process = new();
		process.Exited += (_, _) =>
		{
			IsServerStarted = false;
		};
		process.StartInfo.FileName = Constants.Path.File.BotBat;
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.CreateNoWindow = false;
		process.StartInfo.RedirectStandardOutput = true;

		if (process.Start())
		{
			if (ProcessActions.WaitForOutput(process, "* Debug mode: off"))
			{
				for (int i = 0; i < MaxAttemptsConnectToServer; i++)
				{
					try
					{
						Log.Print($"Trying to connect to the server... ({i + 1}/{MaxAttemptsConnectToServer})");
						var response = await _client.GetAsync(new Uri(_uri, "version"));

						if (response.IsSuccessStatusCode)
						{
							break;
						}
					}
					catch (HttpRequestException)
					{
						// игнорируем ошибку
					}
					finally
					{
						Thread.Sleep(1000);
					}
				}
				IsServerStarted = true;
				return true;
			}
		}

		IsServerStarted = false;
		return false;
	}

	// Новый метод для проверки, работает ли сервер
	private async Task<bool> CheckIfServerRunningAsync()
	{
		try
		{
			Log.Print("Check available for server..");
			var response = await _client.GetAsync(new Uri(_uri, "version"));

			Log.Print("True");
			return response.IsSuccessStatusCode; // Если ответ успешный, сервер запущен
		}
		catch (HttpRequestException)
		{
			Log.Print("False");
			return false; // Сервер не доступен
		}
	}

	public bool CreateBatFile()
	{
		File.WriteAllText(Constants.Path.File.BotBat, "@echo off\n%USERPROFILE%\\AppData\\Local\\Programs\\Python\\Python312\\Scripts\\pip.exe install -U g4f\n%USERPROFILE%\\AppData\\Local\\Programs\\Python\\Python312\\python.exe -m g4f.cli gui\npause");
		return true;
	}

	public async Task<string> SendMessageAsync(string userMessage)
	{
		CheckServer();

		// Создаем тело запроса
		var messages = new List<Dictionary<string, string>>
							{
								new Dictionary<string, string>
								{
									{ "role", "system" },
									{ "content", SystemMessage }
								},
								new Dictionary<string, string>
							{
									{ "role", "user" },
									{ "content", userMessage }
								}
							};

		var requestBody = new Dictionary<string, object>
					{
						{ "api_key", _apiKey },
						{ "model", _model },
						{ "provider", Global.Instance.Provider.Name! },
						{ "id", _chatId },
						{ "conversation_id", _conversationId },
						{ "web_search", false },
						{ "messages", messages },
					};

		// Сериализуем запрос
		var json = JsonSerializer.Serialize(requestBody);
		var content = new StringContent(json, Encoding.UTF8, "application/json");

		// Выполняем POST-запрос
		HttpResponseMessage response = await _client.PostAsync(new Uri(_uri, "conversation"), content);

		// Проверяем успешность ответа
		if (response.IsSuccessStatusCode)
		{
			// Получаем ответ от сервера
			string responseBody = await response.Content.ReadAsStringAsync();

			// Разделяем ответ по строкам и десериализуем каждую строку
			var responseLines = responseBody.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
			string resultMessage = string.Empty;

			foreach (var line in responseLines)
			{
				try
				{
					var jsonDoc = JsonDocument.Parse(line);
					if (jsonDoc.RootElement.TryGetProperty("type", out var typeElement))
					{
						switch (typeElement.GetString())
						{
							case "content":
								// Извлекаем сообщение от API
								if (jsonDoc.RootElement.TryGetProperty("content", out var contentElement))
								{
									resultMessage += contentElement.GetString();
								}
								break;
						}
					}
				}
				catch (JsonException ex)
				{
					Log.Error($"Deserialization error on line: {line}");
					Log.Error(ex.Message);
				}
			}

			return resultMessage;
		}
		else
		{
			var errorMessage = await response.Content.ReadAsStringAsync();
			throw new Exception($"Error: {response.StatusCode}, Message: {errorMessage}");
		}
	}

	public async Task<List<Provider>> GetProvidersAsync()
	{
		CheckServer();

		var providers = new List<Provider>();

		HttpResponseMessage response = await _client.GetAsync(new Uri(_uri, "providers"));

		if (response.IsSuccessStatusCode)
		{
			string responseBody = await response.Content.ReadAsStringAsync();

			var jsonDoc = JsonDocument.Parse(responseBody);
			var providersArray = jsonDoc.RootElement.EnumerateArray();
			foreach (var providerElement in providersArray)
			{
				providers.Add(new Provider(providerElement));
			}
		}

		return providers;
	}

	// Функция проверяет наличие запущенного сервера и выдаёт ошибку, если его нет
	public void CheckServer()
	{
		if (!IsServerStarted)
		{
			throw new Exception("Server is not started");
		}
	}
}