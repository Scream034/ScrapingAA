namespace Core.Admin;

public sealed class AdminInfo
{
	public string Login;
	public string Password;
	public string Url;

	public AdminInfo(in string login, in string password, in string url)
	{
		Login = login;
		Password = password;
		Url = url;
	}
}