namespace FWUtility.ViewModels
{
	using System.Data.Entity;
	using System.IO;
	using Caliburn.Micro;
	using Database;
	using Model;
	using static Helpers.Helper;

	public class SettingsViewModel : Screen
	{
		private string _arcPath;

		public string ArcPathTemp { get; set; }
		
		public string ArcPath
		{
			get => _arcPath;
			set
			{
				_arcPath = value;
				NotifyOfPropertyChange(() => ArcPath);
			}
		}
		
		public SettingsViewModel(string arcPath)
		{
			ArcPathTemp = arcPath;
			ArcPath = !Directory.Exists(arcPath)
				? $"Укажите {ArcPathString}"
				: arcPath;
		}

		public async void SavePaths()
		{
			var wm = new WindowManager();
			if (!Directory.Exists(ArcPath))
			{
				wm.ShowDialog(new DialogViewModel($"{ArcPathString} некорректный", DialogViewModel.DialogType.OK));
				return;
			}

			using (var fs = new FileStream(ArcPathDirectory, FileMode.Create))
			{
				fs.Dispose();
			}

			using (var sw = new StreamWriter(ArcPathDirectory))
			{
				await sw.WriteLineAsync(ArcPath);
			}

			((MainViewModel)Parent).LoadPathData();
			ArcPathTemp = ArcPath;
		}

		public bool CanSavePaths => 
			!string.IsNullOrWhiteSpace(ArcPath) 
			&& ArcPath != ArcPathTemp 
			&& ArcPath != $"Укажите {ArcPathString}";

		public void Cancel()
		{
			TryClose();
		}

		public void TextChanged()
		{
			NotifyOfPropertyChange(() => CanSavePaths);
		}
	}
}
