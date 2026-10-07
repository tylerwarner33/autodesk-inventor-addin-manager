using Inventor;

namespace SampleCommands;

/// <summary>
/// 	Adds 1 to a user parameter in the active part or assembly.
/// </summary>
/// <remarks>
/// 	Shows three parts of the convention:
/// 	the constructor gets the Inventor session,
/// 	<c>Execute</c> takes no parameters and returns nothing, so a successful run shows no window,
/// 	and an exception goes to the manager's result window.
/// 	With <b>Run as one undo step</b> on, one Undo removes the change.
/// </remarks>
/// <param name="inventorApplication">
/// 	The running Inventor session.
/// </param>
public sealed class RunCounter(Inventor.Application inventorApplication)
{
	private const string _parameterName = "AddInManagerRuns";

	public void Execute()
	{
		Parameters parameters = inventorApplication.ActiveDocument switch
		{
			PartDocument part => part.ComponentDefinition.Parameters,
			AssemblyDocument assembly => assembly.ComponentDefinition.Parameters,
			_ => throw new InvalidOperationException("Open a part or an assembly, then run the command again."),
		};

		UserParameters userParameters = parameters.UserParameters;
		UserParameter? parameter = userParameters.OfType<UserParameter>().FirstOrDefault(static parameter => parameter.Name == _parameterName);

		if (parameter is null)
		{
			_ = userParameters.AddByValue(_parameterName, 1.0, UnitsTypeEnum.kUnitlessUnits);
		}
		else
		{
			parameter.Value = (double)parameter.Value + 1.0;
		}
	}
}
