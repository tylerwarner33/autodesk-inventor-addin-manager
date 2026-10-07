using System.Reflection;

namespace InventorAddinManager.Loading;

/// <summary>
/// 	The convention that makes a type a command, and the arguments that the manager gives it.
/// </summary>
/// <remarks>
/// 	A command is a public class with a public method named <c>Execute</c>.
/// 	The method can be static or an instance method, and can return a value.
/// 	Each parameter of the method or the constructor must be an <c>Inventor.Application</c> or have a default value.
///
/// 	Types are matched by full name, not by <see cref="Type" /> identity.
/// 	A build with embedded interop types has its own copy of each interface, which COM treats as the same type and .NET does not.
/// 	The command assembly needs no reference to the manager.
/// </remarks>
internal static class CommandBinder
{
	public const string EntryMethodName = "Execute";

	private const string _inventorApplicationTypeName = "Inventor.Application";

	/// <summary>
	/// 	Finds the method that the manager calls.
	/// </summary>
	/// <returns>
	/// 	The first public <c>Execute</c> method that the manager can give arguments to, or <see langword="null" />.
	/// </returns>
	public static MethodInfo? FindEntryMethod(Type type)
	{
		return type
			.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
			.Where(static method => method.Name == EntryMethodName && method.IsGenericMethodDefinition is false)
			.OrderBy(static method => method.GetParameters().Length)
			.FirstOrDefault(static method => method.GetParameters().All(IsBindable));
	}

	/// <summary>
	/// 	Finds the constructor that the manager uses for an instance <c>Execute</c> method.
	/// </summary>
	/// <returns>
	/// 	A public constructor that the manager can give arguments to, or <see langword="null" />.
	/// </returns>
	public static ConstructorInfo? FindConstructor(Type type)
	{
		if (type.IsAbstract)
		{
			return null;
		}

		return type
			.GetConstructors()
			.OrderBy(static constructor => constructor.GetParameters().Length)
			.FirstOrDefault(static constructor => constructor.GetParameters().All(IsBindable));
	}

	/// <summary>
	/// 	Gets a value that tells if the manager can run the type.
	/// </summary>
	public static bool IsCommand(Type type)
	{
		if (type.IsClass is false || type.IsVisible is false || type.ContainsGenericParameters)
		{
			return false;
		}

		return FindEntryMethod(type) is MethodInfo method && (method.IsStatic || FindConstructor(type) is not null);
	}

	/// <summary>
	/// 	Makes the argument list for a method or a constructor.
	/// </summary>
	/// <returns>
	/// 	The running Inventor session for each <c>Inventor.Application</c> parameter, and the default value for each other parameter.
	/// </returns>
	public static object?[] BindArguments(ParameterInfo[] parameters, Inventor.Application inventorApplication)
	{
		object?[] arguments = new object?[parameters.Length];
		for (int index = 0; index < parameters.Length; index++)
		{
			arguments[index] = IsInventorApplication(parameters[index].ParameterType)
				? inventorApplication
				: parameters[index].DefaultValue;
		}

		return arguments;
	}

	private static bool IsBindable(ParameterInfo parameter)
	{
		return IsInventorApplication(parameter.ParameterType) || parameter.HasDefaultValue;
	}

	private static bool IsInventorApplication(Type type)
	{
		return type.FullName == _inventorApplicationTypeName;
	}
}
