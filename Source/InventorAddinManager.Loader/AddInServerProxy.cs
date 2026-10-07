using System.Runtime.InteropServices;
using Inventor;

namespace InventorAddinManager.Loader;

/// <summary>
/// 	The add-in server that the Inventor 2025 and 2026 manifests name.
/// 	It sends each call to the manager in <see cref="ManagerLoadContext" />.
/// </summary>
/// <remarks>
/// 	Inventor creates the server from the manifest's ClassId, always in the default context, so the manifest names this proxy.
/// 	The Guid is the same as the manager's, so the ClientId of the ribbon and the saved UI customization do not change.
/// 	The proxy casts the manager's server to <see cref="ApplicationAddInServer" /> directly.
/// 	Both sides get the interop from the default context, so it is one type.
/// </remarks>
[Guid("4421BA74-4DBD-4925-9521-35C05DD70634")]
[ComVisible(true)]
public sealed class AddInServerProxy : ApplicationAddInServer
{
	private const string _managerServerTypeFullName = "InventorAddinManager.AddInServer";

	private ApplicationAddInServer? _managerServer;

	public object? Automation => _managerServer?.Automation;

	public void Activate(ApplicationAddInSite AddInSiteObject, bool FirstTime)
	{
		try
		{
			_managerServer = (ApplicationAddInServer)ManagerLoadContext.CreateInstance(_managerServerTypeFullName);
		}
		catch (Exception exception)
		{
			// Inventor ignores this exception, and the manager's log does not exist yet.
			StartupLog.WriteFailure("loading the manager", exception);

			throw;
		}

		_managerServer.Activate(AddInSiteObject, FirstTime);
	}

	public void Deactivate()
	{
		_managerServer?.Deactivate();
		_managerServer = null;
	}

	[Obsolete("Inventor no longer calls this member. The ApplicationAddInServer interface still requires it.")]
	public void ExecuteCommand(int CommandID)
	{
	}
}
