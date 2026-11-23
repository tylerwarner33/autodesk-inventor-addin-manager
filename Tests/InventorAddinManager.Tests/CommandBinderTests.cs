using System.Reflection;
using InventorAddinManager.Loading;

namespace InventorAddinManager.Tests;

public sealed class CommandBinderTests
{
	[Fact]
	public void IsCommand_StaticExecute_IsTrue()
	{
		Assert.True(CommandBinder.IsCommand(typeof(StaticNoParameters)));
	}

	[Fact]
	public void IsCommand_InstanceExecuteWithSessionConstructor_IsTrue()
	{
		Assert.True(CommandBinder.IsCommand(typeof(SessionConstructor)));
	}

	[Theory]
	[InlineData(typeof(NoExecute))]
	[InlineData(typeof(UnbindableParameter))]
	[InlineData(typeof(UnbindableConstructor))]
	[InlineData(typeof(AbstractInstance))]
	[InlineData(typeof(Generic<>))]
	[InlineData(typeof(NotPublic))]
	[InlineData(typeof(Structure))]
	public void IsCommand_TypeTheManagerCannotRun_IsFalse(Type type)
	{
		Assert.False(CommandBinder.IsCommand(type));
	}

	[Fact]
	public void FindEntryMethod_Overloads_PicksFewestBindableParameters()
	{
		MethodInfo? method = CommandBinder.FindEntryMethod(typeof(Overloads));

		Assert.NotNull(method);
		Assert.Single(method.GetParameters());
		Assert.Equal(typeof(Inventor.Application), method.GetParameters()[0].ParameterType);
	}

	[Fact]
	public void BindArguments_SessionAndDefaultValue_GivesSessionAndDefault()
	{
		ParameterInfo[] parameters = typeof(Mixed).GetMethod(CommandBinder.EntryMethodName)!.GetParameters();
		Inventor.Application session = null!;

		object?[] arguments = CommandBinder.BindArguments(parameters, session);

		Assert.Equal(new object?[] { null, 7, "text" }, arguments);
	}

	public static class StaticNoParameters
	{
		public static void Execute()
		{
		}
	}

	public sealed class SessionConstructor(Inventor.Application inventorApplication)
	{
		public string? Execute()
		{
			return inventorApplication?.ToString();
		}
	}

	public sealed class NoExecute
	{
		public void Run()
		{
		}
	}

	public sealed class UnbindableParameter
	{
		public void Execute(string text)
		{
			_ = text;
		}
	}

	public sealed class UnbindableConstructor(string text)
	{
		public string Execute()
		{
			return text;
		}
	}

	public abstract class AbstractInstance
	{
		public void Execute()
		{
		}
	}

	public sealed class Generic<T>
	{
		public T? Execute()
		{
			return default;
		}
	}

	internal sealed class NotPublic
	{
		public void Execute()
		{
		}
	}

	public struct Structure
	{
		public readonly void Execute()
		{
		}
	}

	public sealed class Overloads
	{
		public void Execute(string required)
		{
			_ = required;
		}

		public void Execute(Inventor.Application inventorApplication)
		{
			_ = inventorApplication;
		}

		public void Execute(Inventor.Application inventorApplication, int count = 1)
		{
			_ = (inventorApplication, count);
		}
	}

	public sealed class Mixed
	{
		public void Execute(Inventor.Application inventorApplication, int count = 7, string text = "text")
		{
			_ = (inventorApplication, count, text);
		}
	}
}
