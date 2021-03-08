using System;
namespace UnityEngine.Purchasing
{
	[Obsolete("Use WindowsStore.Name for Universal Windows Apps")]
	public class WinRT
	{
		public const string Name = "WinRT";
	}

	public class WindowsStore
	{
		// The value of this constant must be left as 'WinRT' for legacy reasons.
		// It may be hard coded inside Applications and elsewhere, such that changing
		// it would cause breakage.
		public const string Name = "WinRT";
	}
}
