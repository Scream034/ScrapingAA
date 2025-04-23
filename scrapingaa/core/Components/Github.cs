using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Core.Components.Github;

public class GitRepository
{
	private static readonly HttpClient httpClient = new HttpClient();
	public string Owner { get; set; }
	public string Repo { get; set; }
	public string Url => $"https://github.com/{Owner}/{Repo}";
	public string ApiUrl => $"https://api.github.com/repos/{Owner}/{Repo}";

	public GitRepository(string owner, string repo)
	{
		Owner = owner;
		Repo = repo;
		httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ScrapingAA", Constants.Version.ToString().Replace(',', '.'))); // Замените на имя вашего приложения
	}

	public async Task<GitReleaseInfo?> GetReleaseAsync(string tag)
	{
		var releasesUrl = $"{ApiUrl}/releases/tags/{tag}";
		var response = await httpClient.GetAsync(releasesUrl);
		response.EnsureSuccessStatusCode();
		var content = await response.Content.ReadAsStringAsync();
		return JsonSerializer.Deserialize<GitReleaseInfo>(content);
	}

	public async Task<GitReleaseInfo?> GetLatestReleaseAsync()
	{
		var releasesUrl = $"{ApiUrl}/releases/latest";
		var response = await httpClient.GetAsync(releasesUrl);
		if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
		{
			return null; // Релизы отсутствуют
		}

		response.EnsureSuccessStatusCode();
		var content = await response.Content.ReadAsStringAsync();
		return JsonSerializer.Deserialize<GitReleaseInfo>(content);
	}
}

// Дополнительные классы для десериализации
public class GitRepositoryInfo
{
	public int StargazersCount { get; set; }
	public int ForksCount { get; set; }
}

public class GitReleaseInfo
{
	[JsonPropertyName("tag_name")]
	public string TagName { get; set; } = null!;

	[JsonPropertyName("name")]
	public string Name { get; set; } = null!;

	[JsonPropertyName("html_url")]
	public string HtmlUrl { get; set; } = null!;

	[JsonPropertyName("assets")]
	public GitReleaseAsset[] Assets { get; set; } = null!;
}

public class GitReleaseAsset
{
	[JsonPropertyName("browser_download_url")]
	public string BrowserDownloadUrl { get; set; } = null!;
}