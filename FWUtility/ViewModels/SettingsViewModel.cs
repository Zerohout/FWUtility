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
		
		/// <summary>
		/// Конструктор
		/// </summary>
		/// <param name="arcPath">Путь до папки Arc</param>
		public SettingsViewModel(string arcPath)
		{
			ArcPathTemp = arcPath;
			ArcPath = !Directory.Exists(arcPath)
				? $"Укажите {ArcPathString}"
				: arcPath;
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

			NotifyOfPropertyChange(() => CanSavePaths);
		}

		public bool CanSavePaths =>
			!string.IsNullOrWhiteSpace(ArcPath)
			&& ArcPath != ArcPathTemp
			&& ArcPath != $"Укажите {ArcPathString}";

		/// <summary>
		/// Обозреватель папок
		/// </summary>
		public void Browse()
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
		/// Сохранённый путь
		/// </summary>
		public string ArcPathTemp { get; set; }


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
