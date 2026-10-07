using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace InventorAddinManager.Ribbon;

/// <summary>
/// 	Loads the ribbon icons from the assembly and converts them to the picture type that Inventor takes.
/// </summary>
/// <remarks>
/// 	Add an icon as a PNG in the Resources folder.
/// 	The project embeds every PNG in that folder.
/// </remarks>
internal static class RibbonIcons
{
	private static readonly string _resourceNamespace = $"{nameof(InventorAddinManager)}.Resources";

	/// <summary>
	/// 	Loads an embedded PNG.
	/// </summary>
	/// <param name="fileName">
	/// 	File name in the Resources folder, ex. <c>Manager32.png</c>.
	/// </param>
	/// <returns>
	/// 	The icon as the picture type that <c>ControlDefinitions.AddButtonDefinition</c> takes.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// 	The assembly has no embedded resource with that name.
	/// </exception>
	public static stdole.IPictureDisp Load(string fileName)
	{
		string resourceName = $"{_resourceNamespace}.{fileName}";

		using Stream stream = typeof(RibbonIcons).Assembly.GetManifestResourceStream(resourceName)
			?? throw new InvalidOperationException($"No embedded resource '{resourceName}'. Put the PNG in the Resources folder.");
		using Bitmap bitmap = new(stream);

		return PictureConverter.ToPictureDisp(bitmap);
	}

	/// <remarks>
	/// 	<c>GetIPictureDispFromPicture</c> is a protected static member of <see cref="AxHost" />, so only a subclass can call it.
	/// </remarks>
	private sealed class PictureConverter : AxHost
	{
		private PictureConverter()
			: base(string.Empty)
		{
		}

		public static stdole.IPictureDisp ToPictureDisp(Image image)
		{
			return GetIPictureDispFromPicture(image) as stdole.IPictureDisp
				?? throw new InvalidOperationException("The icon could not be converted to an OLE picture.");
		}
	}
}
