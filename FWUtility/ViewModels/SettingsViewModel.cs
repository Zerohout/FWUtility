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
		private bool _alternativeLaunch;

		/// <summary>
		/// Конструктор
		/// </summary>
		/// <param name="arcPath">Путь до папки Arc</param>
		/// <param name="fwPath">Путь до папки Forsaken World</param>
		/// <param name="alterLaunch">Статус Альтернативного запуска</param>
		public SettingsViewModel(string arcPath, string fwPath, bool alterLaunch)
		{
			ArcPathTemp = arcPath;
			ArcPath = !Directory.Exists(arcPath)
				? $"Укажите {ArcPathString}"
				: arcPath;
			FWPathTemp = fwPath;
			FWPath = !Directory.Exists(fwPath)
				? $"Укажите {FWPathString}"
				: fwPath;
			AlterLaunchTemp = alterLaunch;
			AlternativeLaunch = alterLaunch;

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
				await sw.WriteAsync(sw.NewLine);
				await sw.WriteAsync(AlternativeLaunch.ToString());
			}

			((MainViewModel)Parent).LoadPathData();
			ArcPathTemp = ArcPath;
			FWPathTemp = FWPath;
			AlterLaunchTemp = AlternativeLaunch;

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
				    FWPath == FWPathTemp &&
				    AlternativeLaunch == AlterLaunchTemp)
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
		public void ArcBrowse()
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

		public void FWBrowse()
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
		/// Сохраненный статус альтернативного запуска игры
		/// </summary>
		public bool AlterLaunchTemp { get; set; }
		
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
		
		/// <summary>
		/// Альтернативный запуск игры
		/// </summary>
		public bool AlternativeLaunch
		{
			get => _alternativeLaunch;
			set
			{
				_alternativeLaunch = value;
				NotifyOfPropertyChange(() => AlternativeLaunch);
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

		/// <summary>
		/// Действие при изменение CheckBox'а "Альтернативный запуск"
		/// </summary>
		public void AlterLaunchCheckedValidation()
		{
			NotifyOfPropertyChange(() => CanSavePaths);
		}

		#endregion

	}
}
