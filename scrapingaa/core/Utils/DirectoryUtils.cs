using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Core.Components;

public static class DirectoryUtils
{
	/// <summary>
	/// Создает директории по всем публичным статическим полям или свойствам заданного класса.
	/// </summary>
	/// <param name="type">Тип класса, из которого будут получены значения директорий.</param>
	public static void CreateDirectoriesFromStaticMembers(Type type)
	{
		// Получаем все публичные статические поля и свойства
		var members = type.GetFields(BindingFlags.Public | BindingFlags.Static)
				.Cast<MemberInfo>()
				.Concat(type.GetProperties(BindingFlags.Public | BindingFlags.Static));

		// Проходим по каждому члену и создаем директорию
		foreach (var member in members)
		{
			string? directoryPath = null;

			// Если это поле, получаем его значение
			if (member is FieldInfo field)
			{
				directoryPath = (string?)field.GetValue(null);
			}
			// Если это свойство, получаем его значение
			else if (member is PropertyInfo property)
			{
				directoryPath = (string?)property.GetValue(null);
			}

			if (!string.IsNullOrWhiteSpace(directoryPath))
			{
				Directory.CreateDirectory(directoryPath);
			}
		}
	}
}