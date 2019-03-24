using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FWUtility.Helpers
{
	public static class Helper
	{
		public enum EditingState
		{
			EDITING,
			CREATING
		}

		public const string CreateName = "Добавить";
		public const string ArcPathString = "Путь до папки Arc";
		public const string ArcEndPath = @"\Arc.exe";
		public const string LauncherEndPath = @"\ArcLauncher.exe";
		public const string LauncherParameter = "gamecustom fw";
		public static readonly string ArcPathDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}\\ArcPath.fwu";
	}
}
