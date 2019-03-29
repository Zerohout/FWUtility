namespace FWUtility.ViewModels
{
	using System;
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
		private string _fwPath;
		private bool IsRusLaunch;

		private BindableCollection<Account> _accounts = new BindableCollection<Account>();
		private Account _selectedAccount;
		
		public MainViewModel()
		{
			LoadAccountData();
			LoadPathData();
		}

		#region Начальная загрузка данных

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
			if (!File.Exists(FWUDataDirectory))
			{
				using (var fs = new FileStream(FWUDataDirectory, FileMode.Create))
				{
					fs.Dispose();
				}

				using (var sw = new StreamWriter(FWUDataDirectory))
				{
					await sw.WriteAsync(@"C:\Program Files (x86)\Arc");
					await sw.WriteAsync(sw.NewLine);
					await sw.WriteAsync(@"C:\Program Files (x86)\Forsaken World_ru");
				}
			}
			else
			{
				using (var sr = new StreamReader(FWUDataDirectory))
				{
					ArcPath = await sr.ReadLineAsync();
					FWPath = await sr.ReadLineAsync();
				}
			}
		}

		#endregion
		
		#region Actions

		/// <summary>
		/// Действие при отметке\снятии отметки о запуске аккаунта в списке
		/// </summary>
		public void CheckedValidation()
		{
			AddAccountToQueue();
			RemoveAccountFromQueue();

			NotifyOfPropertyChange(() => CanStart);
		}

		/// <summary>
		/// Добавление Аккаунта в очередь запуска
		/// </summary>
		private void AddAccountToQueue()
		{
			if (CheckedAccounts.Count == 3)
			{
				return;
			}

			foreach (var a in Accounts.Where(a => a.IsChecked))
			{
				if (CheckedAccounts.Any(c => c.AccountId == a.AccountId))
				{
					continue;
				}

				CheckedAccounts.Add(a);
			}
		}

		/// <summary>
		/// Удаление аккаунта из очереди запуска
		/// </summary>
		private void RemoveAccountFromQueue()
		{
			Account temp = null;

			foreach (var a in CheckedAccounts)
			{
				if (!a.IsChecked)
				{
					temp = a;
					break;
				}
			}

			if (temp != null)
			{
				CheckedAccounts.Remove(temp);
			}
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
			ActiveItem = new SettingsViewModel(ArcPath, FWPath);
		}

		/// <summary>
		/// Кнопка Запустить
		/// </summary>
		public async void Start()
		{
			var wm = new WindowManager();

			if (!Directory.Exists(ArcPath))
			{
				wm.ShowDialog(new DialogViewModel(
								  $"{ArcPathString} некорректный. Измените его в настройках",
								  DialogViewModel.DialogType.OK));
				return;
			}

			if (!Directory.Exists(FWPath))
			{
				wm.ShowDialog(new DialogViewModel(
								  $"{FWPathString} некорректный. Измените его в настройках",
								  DialogViewModel.DialogType.OK));
				return;
			}

			WindowState = WindowState.Minimized;

			await KillArc();
			
			if (FWPath.Contains("_ru"))
			{
				IsRusLaunch = true;
			}
			
			PemIds.Clear();

			await Task.Run(StartWorking);
			Thread.Sleep(5000);
			Application.Current.Shutdown();
		}

		public bool CanStart =>
			Accounts.Count(a => a.IsChecked) > 0
			&& Accounts.Count(a => a.IsChecked) <= 3;

		#region Запуск лаунчера игры(Методы)

		[DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		/// <summary>
		/// Запуск необходимого количество копий программы
		/// </summary>
		/// <returns></returns>
		private async Task StartWorking()
		{
			foreach (var acc in CheckedAccounts)
			{
				await StartLauncher(acc);
			}
		}

		private async Task KillArc()
		{
			await Task.Yield();
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
		}
		
		/// <summary>
		/// Запуск лаунчера и необходимых процессов
		/// </summary>
		/// <param name="currAcc"></param>
		/// <returns></returns>
		private async Task StartLauncher(Account currAcc)
		{
			var sim = new InputSimulator();

			Process.Start(IsRusLaunch
							  ? $"{ArcPath}{ArcEndPath}"
							  : $"{FWPath}{FWEndPath}");

			var arcId = await StartArc();
			
			await LoginInGame(currAcc);

			if (IsRusLaunch)
			{
				await DetectProccess("Arc");
				
				Process.Start($"{ArcPath}{LauncherEndPath}", LauncherParameter);
			}

			await DetectProccess("Forsaken World II");
			
			sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LMENU, VirtualKeyCode.F4);

			Thread.Sleep(2000);

			await StartGame();

			await KillArc();

			ShowWindow(Process.GetProcessById(PemIds.Last()).MainWindowHandle, 6);
		}

		/// <summary>
		/// Ожидания полного запуска процесса Arc.exe
		/// </summary>
		/// <returns>Id необходимого процесса Arc</returns>
		private async Task<int> StartArc()
		{
			await Task.Yield();

			var arcId = 0;
			var counter = 0;

			while (counter < 12)
			{
				foreach (var p in Process.GetProcesses())
				{
					if (p.ProcessName.Contains("crashpad"))
					{
						arcId = Process.GetProcessesByName("Arc").First().Id;
						counter = 12;
						break;
					}
				}

				counter++;
				Thread.Sleep(2000);
			}

			Thread.Sleep(5000);

			return await Task.FromResult(arcId);
		}

		/// <summary>
		/// Ввод данных в лаунчере
		/// </summary>
		/// <param name="acc">Требуемый аккаунт</param>
		private async Task LoginInGame(Account acc)
		{
			await Task.Yield();

			var sim = new InputSimulator();
			sim.Keyboard.Sleep(2000);
			sim.Keyboard.KeyPress(VirtualKeyCode.TAB);
			sim.Keyboard.Sleep(500);
			sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.SHIFT, VirtualKeyCode.END);
			sim.Keyboard.Sleep(500);
			sim.Keyboard.TextEntry(acc.Email);
			sim.Keyboard.Sleep(500);
			sim.Keyboard.KeyPress(VirtualKeyCode.TAB);
			sim.Keyboard.Sleep(500);
			sim.Keyboard.TextEntry(acc.Password);
			sim.Keyboard.Sleep(500);
			sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
		}

		/// <summary>
		/// Ожидание запуска самой игры
		/// </summary>
		/// <returns></returns>
		private async Task StartGame()
		{
			await Task.Yield();

			var sim = new InputSimulator();

			for (var i = 0; i < 1; i++)
			{
				sim.Keyboard.KeyPress(VirtualKeyCode.TAB);
				sim.Keyboard.Sleep(500);
			}

			sim.Keyboard.Sleep(500);
			sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
			
			await DetectProccess("Forsaken World -");

			sim.Keyboard.Sleep(1000);
			sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
		}

		/// <summary>
		/// Ожидание запуска процесса
		/// </summary>
		/// <param name="proccessName">Название процесса</param>
		/// <returns></returns>
		private async Task DetectProccess(string proccessName)
		{
			await Task.Yield();

			var counter = 0;
			while (counter < 12)
			{
				foreach (var p in Process.GetProcesses())
				{
					if (p.MainWindowTitle.Contains(proccessName))
					{
						if (!PemIds.Contains(p.Id) 
						    && proccessName == "Forsaken World -")
						{
							PemIds.Add(p.Id);
							counter = 12;
							break;
						}
					}
				}

				counter++;
				Thread.Sleep(2000);
			}
		}

		#endregion

		#endregion

		#region Properties

		public BindableCollection<Account> CheckedAccounts { get; set; } = new BindableCollection<Account>();

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
