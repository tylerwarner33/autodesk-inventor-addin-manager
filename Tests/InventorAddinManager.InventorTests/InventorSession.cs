using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace InventorAddinManager.InventorTests;

/// <summary>
/// 	The running Inventor of the configuration's release, and the manager in it.
/// </summary>
/// <remarks>
/// 	xUnit creates one instance for all the tests that use it, so the tests find Inventor once.
/// 	When no such Inventor runs, <see cref="SkipReason" /> tells why, and each test skips.
/// </remarks>
public sealed class InventorSession
{
	/// <summary>
	/// 	The ClientId of the manager, from InventorAddinManager.addin.template.
	/// </summary>
	public const string ManagerClientId = "{4421BA74-4DBD-4925-9521-35C05DD70634}";

	private const string _inventorProgId = "Inventor.Application";

	public InventorSession()
	{
		string autodeskVersion = Metadata("AutodeskVersion");
		int softwareVersion = int.Parse(Metadata("InventorSoftwareVersion"), CultureInfo.InvariantCulture);

		Inventor.Application? application = FindRunningInventor();
		if (application is null)
		{
			SkipReason = $"No Inventor runs. Start Inventor {autodeskVersion} with the manager deployed, then run the tests again.";
			return;
		}

		if (application.SoftwareVersion.Major != softwareVersion)
		{
			SkipReason = $"The running Inventor is {application.SoftwareVersion.DisplayVersion}, not {autodeskVersion}. Run the tests with -c Debug.<release> of the running Inventor.";
			return;
		}

		Application = application;
		ManagerAddIn = application.ApplicationAddIns.OfType<Inventor.ApplicationAddIn>()
			.FirstOrDefault(static addIn => string.Equals(addIn.ClientId, ManagerClientId, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// 	Gets the running Inventor, or <see langword="null" /> when the tests skip.
	/// </summary>
	public Inventor.Application? Application { get; }

	/// <summary>
	/// 	Gets the manager add-in, or <see langword="null" /> when the manager is not deployed.
	/// </summary>
	public Inventor.ApplicationAddIn? ManagerAddIn { get; }

	/// <summary>
	/// 	Gets the reason to skip, or <see langword="null" /> when the right Inventor runs.
	/// </summary>
	public string? SkipReason { get; }

	/// <summary>
	/// 	Gets the build output path of the test commands for this configuration.
	/// </summary>
	public static string CommandsAssemblyPath => Metadata("CommandsAssemblyPath");

	/// <summary>
	/// 	Gets the build output path of the sample commands for this configuration.
	/// </summary>
	public static string SampleCommandsAssemblyPath => Metadata("SampleCommandsAssemblyPath");

	/// <summary>
	/// 	Skips the test when the right Inventor does not run, and gives the manager's automation object.
	/// </summary>
	/// <returns>
	/// 	The <c>ManagerAutomation</c> object. COM gives it through <c>IDispatch</c>, so it is <see langword="dynamic" />.
	/// </returns>
	public dynamic GetManagerAutomation()
	{
		Assert.SkipWhen(SkipReason is not null, SkipReason ?? string.Empty);
		Assert.NotNull(ManagerAddIn);
		Assert.True(ManagerAddIn.Activated, "The manager is deployed but not loaded. Load it in Tools > Add-Ins.");

		object? automation = ManagerAddIn.Automation;
		Assert.NotNull(automation);

		return automation;
	}

	private static string Metadata(string key)
	{
		return typeof(InventorSession).Assembly
			.GetCustomAttributes<AssemblyMetadataAttribute>()
			.Single(attribute => attribute.Key == key)
			.Value!;
	}

	/// <summary>
	/// 	Gets the Inventor that is registered in the running object table.
	/// </summary>
	/// <remarks>
	/// 	.NET has no Marshal.GetActiveObject, so this calls the same OLE functions.
	/// 	Each release registers the same ProgID, so with two releases open, this gets one of them.
	/// </remarks>
	private static Inventor.Application? FindRunningInventor()
	{
		if (NativeMethods.CLSIDFromProgID(_inventorProgId, out Guid classId) < 0)
		{
			return null;
		}

		return NativeMethods.GetActiveObject(ref classId, IntPtr.Zero, out object? instance) < 0
			? null
			: instance as Inventor.Application;
	}

	private static class NativeMethods
	{
		[DllImport("ole32.dll", CharSet = CharSet.Unicode)]
		public static extern int CLSIDFromProgID(string progId, out Guid classId);

		[DllImport("oleaut32.dll")]
		public static extern int GetActiveObject(ref Guid classId, IntPtr reserved, [MarshalAs(UnmanagedType.IUnknown)] out object? instance);
	}
}
