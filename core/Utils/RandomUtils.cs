namespace Core.Components;

using System;

public static class RandomUtils
{
	public static string RandomString(int length)
	{
		const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
		string result = "";

		for (int i = 0; i < length; i++)
		{
			result += chars[Random.Shared.Next(chars.Length)];
		}

		return result;
	}
}