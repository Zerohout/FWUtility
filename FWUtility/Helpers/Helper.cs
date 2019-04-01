using System;
using System.Collections.Generic;

namespace FWUtility.Helpers
{
	public static class Helper
	{
		public enum EditingState
		{
			EDITING,
			CREATING
		}

		public const string CreatingName = "Добавить";
		public const string ArcPathString = "Путь до папки Arc";
		public const string FWPathString = "Путь до папки Forsaken World";
		public const string ArcEndPath = @"\Arc.exe";
		public const string FWEndPath = @"\patcher.exe";
		public const string LauncherEndPath = @"\ArcLauncher.exe";
		public const string LauncherParameter = "gamecustom fw";
		public static List<int> PemIds = new List<int>();
		public static List<int> OverlayIds = new List<int>();
		public static readonly string FWUDataDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}\\ArcPath.fwu";
	}
}
