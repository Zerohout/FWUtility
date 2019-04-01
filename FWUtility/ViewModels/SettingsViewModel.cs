namespace FWUtility.ViewModels
{
	using System;
	using System.IO;
	using System.Windows.Forms;
	using Caliburn.Micro;
	using static Helpers.Helper;
	using Screen = Caliburn.Micro.Screen;

	public class SettingsViewModel : Screen
	{
		private string _arcPath;
		private string _fwPath;
		
		/// <summary>
		/// Конструктор
		/// </summary>
		/// <param name="arcPath">Путь до папки Arc</param>
		/// <param name="fwPath">Путь до папки Forsaken World</param>
		public SettingsViewModel(string arcPath, string fwPath)
		{
			ArcPathTemp = arcPath;
			ArcPath = !Directory.Exists(arcPath)
				? $"Укажите {ArcPathString}"
				: arcPath;
			FWPathTemp = fwPath;
			FWPath = !Directory.Exists(fwPath)
				? $"Укажите {FWPathString}"
				: fwPath;
		}

		#region Buttons

		/// <summary>
		/// Кнопка сохранить
		/// </summary>
		public async void SavePaths()
		{
			var wm = new WindowManager();

			if (!Directory.Exists(ArcPath))
			{
				wm.ShowDialog(new DialogViewModel($"{ArcPathString} некорректный", DialogViewModel.DialogType.OK));
				return;
			}

			if (!Directory.Exists(FWPath))
			{
				wm.ShowDialog(new DialogViewModel($"{FWPathString} некорректный", DialogViewModel.DialogType.OK));
				return;
			}

			using (var fs = new FileStream(FWUDataDirectory, FileMode.Create))
			{
				fs.Dispose();
			}

			using (var sw = new StreamWriter(FWUDataDirectory))
			{
				await sw.WriteAsync(ArcPath);
				await sw.WriteAsync(sw.NewLine);
				await sw.WriteAsync(FWPath);
			}

			((MainViewModel)Parent).LoadPathData();
			ArcPathTemp = ArcPath;
			FWPathTemp = FWPath;

			NotifyOfPropertyChange(() => CanSavePaths);
		}

		public bool CanSavePaths
		{
			get
			{
				if (string.IsNullOrWhiteSpace(ArcPath) ||
					string.IsNullOrWhiteSpace(FWPath))
				{
					return false;
				}

				if (ArcPath == ArcPathTemp &&
					FWPath == FWPathTemp)
				{
					return false;
				}

				if (ArcPath == $"Укажите {ArcPathString}" ||
					FWPath == $"Укажите {FWPathString}")
				{
					return false;
				}

				return true;
			}
		}

		/// <summary>
		/// Обозреватель папок
		/// </summary>
		public void BrowseArc()
		{
			var fbd = new FolderBrowserDialog
			{
				RootFolder = Environment.SpecialFolder.MyComputer
			};

			if (fbd.ShowDialog() == DialogResult.OK)
			{
				ArcPath = fbd.SelectedPath;
			}
		}

		public void BrowseFW()
		{
			var fbd = new FolderBrowserDialog
			{
				RootFolder = Environment.SpecialFolder.MyComputer
			};

			if (fbd.ShowDialog() == DialogResult.OK)
			{
				FWPath = fbd.SelectedPath;
			}
		}

		/// <summary>
		/// Кнопка Отмена
		/// </summary>
		public void Cancel()
		{
			TryClose();
		}

		#endregion

		#region Properties

		/// <summary>
		/// Сохранённый путь до папки Arc
		/// </summary>
		public string ArcPathTemp { get; set; }

		/// <summary>
		/// Сохранённый путь до папки Forsaken World
		/// </summary>
		public string FWPathTemp { get; set; }

		/// <summary>
		/// Путь до папки Arc
		/// </summary>
		public string ArcPath
		{
			get => _arcPath;
			set
			{
				_arcPath = value;
				NotifyOfPropertyChange(() => ArcPath);
			}
		}

		/// <summary>
		/// Путь до папки Forsaken World
		/// </summary>
		public string FWPath
		{
			get => _fwPath;
			set
			{
				_fwPath = value;
				NotifyOfPropertyChange(() => FWPath);
			}
		}

		#endregion

		#region Actions

		/// <summary>
		/// Действие при изменении текста
		/// </summary>
		public void TextChanged()
		{
			NotifyOfPropertyChange(() => CanSavePaths);
		}

		#endregion

	}
}
