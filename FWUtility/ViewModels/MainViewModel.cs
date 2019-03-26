namespace FWUtility.ViewModels
{
	using System;
	using System.ComponentModel;
	using System.Data.Entity;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using Caliburn.Micro;
	using Database;
	using Models;
	using WindowsInput;
	using WindowsInput.Native;
	using static Helpers.Helper;
	using Screen = Caliburn.Micro.Screen;

	public class MainViewModel : Conductor<Screen>.Collection.OneActive
	{
		private WindowState _windowState;
		private string _arcPath;
		private BindableCollection<Account> _accounts = new BindableCollection<Account>();
		private Account _selectedAccount;


		public MainViewModel()
		{
			LoadAccountData();
			LoadPathData();
		}

		/// <summary>
		/// Загрузка Аккаунтов
		/// </summary>
		public async void LoadAccountData()
		{
			Accounts.Clear();
			using (var ctx = new FWUDbContext())
			{
				await ctx.Accounts.LoadAsync();
				Accounts.AddRange(ctx.Accounts.Local.OrderBy(a => a.Name));
			}

			if (Accounts.All(a => a.Name != CreatingName))
			{
				Accounts.Add(CreatingAccount);
			}
		}

		/// <summary>
		/// Загрузка пути к Arc
		/// </summary>
		public async void LoadPathData()
		{
			if (!File.Exists(ArcPathDirectory))
			{
				using (var fs = new FileStream(ArcPathDirectory, FileMode.Create))
				{
					fs.Dispose();
				}

				using (var sw = new StreamWriter(ArcPathDirectory))
				{
					await sw.WriteAsync(@"C:\Program Files (x86)\Arc");
				}
			}
			else
			{
				using (var sr = new StreamReader(ArcPathDirectory))
				{
					ArcPath = await sr.ReadLineAsync();
				}
			}
		}

		#region Actions

		/// <summary>
		/// Действие при отметке аккаунта в списке
		/// </summary>
		public void CheckedValidation()
		{
			NotifyOfPropertyChange(() => CanStart);
		}

		/// <summary>
		/// Действие при снятии отметки аккаунта в списке
		/// </summary>
		public void UnCheckedValidation()
		{
			NotifyOfPropertyChange(() => CanStart);
		}

		/// <summary>
		/// Изменение выделения аккаунта в списке
		/// </summary>
		public void SelectionChanged()
		{
			ActiveItem?.TryClose();

			if (_selectedAccount == null)
			{
				return;
			}

			ActiveItem = _selectedAccount.Name == CreatingName
				? new EditableViewModel(new Account(), EditingState.CREATING)
				: new EditableViewModel(new Account
				{
					AccountId = _selectedAccount.AccountId,
					Name = _selectedAccount.Name,
					Email = _selectedAccount.Email,
					Password = _selectedAccount.Password
				}, EditingState.EDITING);
		}

		#endregion
		
		#region Buttons

		/// <summary>
		/// Кнопка Настройки
		/// </summary>
		public void Settings()
		{
			SelectedAccount = null;
			ActiveItem = new SettingsViewModel(ArcPath);
		}

		/// <summary>
		/// Кнопка Запустить
		/// </summary>
		public async void Start()
		{
			WindowState = WindowState.Minimized;

			var wm = new WindowManager();
			if (!Directory.Exists(ArcPath))
			{
				wm.ShowDialog(new DialogViewModel(
								  $"{ArcPathString} некорректный. Измените его в настройках",
								  DialogViewModel.DialogType.OK));
				return;
			}

			var arcs = Process.GetProcessesByName("Arc");

			if (arcs.Length > 0)
			{
				arcs.First().Kill();
				while (true)
				{
					if (arcs.First().HasExited)
					{
						break;
					}
					Thread.Sleep(1000);
				}
			}

			PemIds.Clear();

			var bw = new BackgroundWorker();

			await Task.Run(StartWorking);

			Thread.Sleep(5000);
			Application.Current.Shutdown();
		}

		public bool CanStart =>
			Accounts.Count(a => a.IsChecked) > 0
			&& Accounts.Count(a => a.IsChecked) <= 3;

		#region Запуск лаунчера игры(Методы)

		private async Task StartWorking()
		{
			foreach (var acc in Accounts.Where(a => a.IsChecked))
			{
				await StartingGame(acc);
				await Task.Yield();
			}
		}

		[DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		/// <summary>
		/// Сам запуск лаунчера и необходимых процессов
		/// </summary>
		/// <param name="currAcc"></param>
		/// <returns></returns>
		private async Task StartingGame(Account currAcc)
		{
			var sim = new InputSimulator();

			Process.Start($"{ArcPath}{ArcEndPath}");

			var arcId = await WaitingStartArcProcess();
			await Task.Yield();

			await LoginInGame(currAcc);

			await Task.Yield();

			await WaitingProccess("ArcChat");

			Process.Start($"{ArcPath}{LauncherEndPath}", LauncherParameter);

			await Task.Yield();
			await WaitingProccess("patcher");

			for (var i = 0; i < 11; i++)
			{
				sim.Keyboard.KeyPress(VirtualKeyCode.TAB);
				Thread.Sleep(500);
			}

			Thread.Sleep(2000);

			await WaitingStartingGame();

			Thread.Sleep(5000);

			var arc = Process.GetProcessById(arcId);
			arc.Kill();

			while (true)
			{
				if (arc.HasExited)
				{
					break;
				}
				Thread.Sleep(1000);
			}

			Thread.Sleep(7000);
			sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

			ShowWindow(Process.GetProcessById(PemIds.Last()).MainWindowHandle, 6);
		}

		/// <summary>
		/// Ожидания полного запуска процесса Arc.exe
		/// </summary>
		/// <returns>Id необходимого процесса Arc</returns>
		private Task<int> WaitingStartArcProcess()
		{
			var working = true;
			var arcId = 0;

			while (working)
			{
				foreach (var p in Process.GetProcesses())
				{
					if (p.ProcessName == "Arc")
					{
						if (arcId == 0 || arcId == p.Id)
						{
							if (arcId == 0)
							{
								arcId = p.Id;
							}

							break;
						}

						if (arcId != p.Id)
						{
							arcId = p.Id;
							working = false;
							break;
						}
					}
				}

				Thread.Sleep(500);
			}

			return Task.FromResult(arcId);
		}

		/// <summary>
		/// Ожидание запуска процесса
		/// </summary>
		/// <param name="proccessName">Название процесса</param>
		/// <returns></returns>
		private Task WaitingProccess(string proccessName)
		{
			var working = true;
			while (working)
			{
				foreach (var p in Process.GetProcesses())
				{
					if (p.ProcessName == proccessName)
					{
						working = false;
						break;
					}
				}
				Thread.Sleep(1000);
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Ожидание запуска самой игры
		/// </summary>
		/// <returns></returns>
		private Task WaitingStartingGame()
		{
			var working = true;
			var counter = 0;

			var sim = new InputSimulator();

			while (working && counter < 10)
			{
				foreach (var p in Process.GetProcesses())
				{
					if (p.ProcessName == "pem" && !PemIds.Contains(p.Id))
					{
						PemIds.Add(p.Id);
						working = false;
						break;
					}
				}

				if (working)
				{
					sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
				}

				Thread.Sleep(3000);
				counter++;
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Ввод данных в лаунчере
		/// </summary>
		/// <param name="acc">Требуемый аккаунт</param>
		private Task LoginInGame(Account acc)
		{
			var sim = new InputSimulator();

			Thread.Sleep(5000);
			sim.Keyboard.KeyPress(VirtualKeyCode.TAB);
			Thread.Sleep(500);
			sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.SHIFT, VirtualKeyCode.END);
			Thread.Sleep(500);
			sim.Keyboard.TextEntry(acc.Email);
			Thread.Sleep(500);
			sim.Keyboard.KeyPress(VirtualKeyCode.TAB);
			Thread.Sleep(500);
			sim.Keyboard.TextEntry(acc.Password);
			Thread.Sleep(500);
			sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

			return Task.CompletedTask;
		}

		#endregion


		#endregion

		#region Properties

		/// <summary>
		/// Состояние окна
		/// </summary>
		public WindowState WindowState
		{
			get => _windowState;
			set
			{
				_windowState = value;
				NotifyOfPropertyChange(() => WindowState);
			}
		}

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
		/// Выбранный аккаунт
		/// </summary>
		public Account SelectedAccount
		{
			get => _selectedAccount;
			set
			{
				_selectedAccount = value;
				NotifyOfPropertyChange(() => SelectedAccount);
			}
		}

		/// <summary>
		/// Список аккаунтов
		/// </summary>
		public BindableCollection<Account> Accounts
		{
			get => _accounts;
			set
			{
				_accounts = value;
				NotifyOfPropertyChange(() => Accounts);
			}
		}

		/// <summary>
		/// Аккаунт заглушка для создания игровых аккаунтов
		/// </summary>
		public Account CreatingAccount => new Account
		{
			Name = "Добавить",
			Visibility = Visibility.Hidden
		};

		#endregion

	}
}
