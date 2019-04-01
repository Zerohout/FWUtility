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
		
		private static readonly CancellationTokenSource cts = new CancellationTokenSource();
		private readonly CancellationToken token = cts.Token;
		private readonly InputSimulator sim = new InputSimulator();
		private readonly WindowManager wm = new WindowManager();

		private BindableCollection<Account> _accounts = new BindableCollection<Account>();
		private Account _selectedAccount;

		[DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

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
			if (CheckedAccounts.Count == 3) return;

			foreach (var a in Accounts.Where(a => a.IsChecked))
			{
				if (CheckedAccounts
					.Any(c => c.AccountId == a.AccountId)) continue;

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

			KillProcByName("Arc");

			PemIds.Clear();
			OverlayIds.Clear();

			WindowState = WindowState.Minimized;

			//Запуск процедуры входа в игру
			await Task.Run(StartTask, token);
			
			Thread.Sleep(5000);
			Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
		}

		public bool CanStart =>
			Accounts.Count(a => a.IsChecked) > 0
			&& Accounts.Count(a => a.IsChecked) <= 3;

		#region Запуск лаунчера игры(Методы)

		/// <summary>
		/// Подготовка запуска лаунчера Arc
		/// </summary>
		/// <returns></returns>
		private async Task StartTask()
		{
			if (FWPath.Contains("_ru"))
			{
				IsRusLaunch = true;
			}

			foreach (var acc in CheckedAccounts)
			{
				try
				{
					await Task.Run(() => EnterAccountIntoTheGame(acc), token);
				}
				catch (Exception ex)
				{
					ShowError($"{ex.Message}\r{ex.StackTrace}");
					break;
				}
			}
		}

		/// <summary>
		/// Запуск лаунчера и необходимых процессов
		/// </summary>
		/// <param name="currAcc"></param>
		/// <returns></returns>
		private Task EnterAccountIntoTheGame(Account currAcc)
		{
			if (IsRusLaunch)
			{
				Process.Start($"{ArcPath}{LauncherEndPath}", LauncherParameter);
			}
			else
			{
				Process.Start($"{FWPath}{FWEndPath}");
			}

			try
			{
				StartArcLauncher();
				EnterAccountData(currAcc);
				WaitGameClient();
				EnterToGame();

				KillProcByName("Arc");
				ShowWindow(Process.GetProcessById(PemIds.Last()).MainWindowHandle, 6);
			}
			catch (Exception ex)
			{
				ShowError($"{ex.Message}\r{ex.StackTrace}");
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Ожидания полного запуска процесса Arc.exe
		/// </summary>
		/// <returns>Id необходимого процесса Arc</returns>
		private void StartArcLauncher()
		{
			for (var i = 0; i < 300; i++)
			{
				foreach (var p in Process.GetProcesses())
				{
					if (p.ProcessName == "Arc")
					{
						WaitArcLauncherFields(p.Id);
						return;
					}
				}

				Thread.Sleep(50);
			}

			cts.Cancel();
			throw new Exception("Процесс Arc.exe не найден.");
		}

		/// <summary>
		/// Ожидания игрового лаунчера для дальнейшего ввода данных аккаунта
		/// </summary>
		/// <param name="id">ID игрового лаунчера</param>
		private void WaitArcLauncherFields(int id)
		{
			var firstProcessorIdle = true;
			var idle = false;

			var tempProcTime = "";
			var proc = Process.GetProcessById(id);

			for (int count = 0, cycle = 0; count < 200; count++, cycle++)
			{
				if (tempProcTime != proc.TotalProcessorTime.ToString())
				{
					tempProcTime = proc.TotalProcessorTime.ToString();
					cycle = -1;

					if (!firstProcessorIdle && idle)
					{
						idle = false;
					}
				}
				else
				{
					if (cycle == 15 && !idle)
					{
						if (firstProcessorIdle)
						{
							firstProcessorIdle = false;
							idle = true;
						}
						else
						{
							return;
						}
					}
				}

				Thread.Sleep(50);
			}
		}

		/// <summary>
		/// Ввод данных в лаунчере
		/// </summary>
		/// <param name="acc">Требуемый аккаунт</param>
		private void EnterAccountData(Account acc)
		{
			if (token.IsCancellationRequested) return;

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
		/// Ожидание игрового клиента
		/// </summary>
		private void WaitGameClient()
		{
			if (token.IsCancellationRequested) return;

			for (var i = 0; i < 90; i++)
			{
				foreach (var p in Process.GetProcesses())
				{
					if (p.MainWindowTitle == "Forsaken World II")
					{
						return;
					}
				}

				Thread.Sleep(1000);
			}

			cts.Cancel();
			throw new Exception("Игровой клиент не найден.");
		}

		private void KillProcByName(string procName)
		{
			var processes = Process.GetProcessesByName(procName);

			if (processes.Length == 0) return;

			try
			{
				processes.First().Kill();

				for (var i = 0; i < 10; i++)
				{
					if (processes.First().HasExited)
					{
						return;
					}

					Thread.Sleep(1000);
				}
			}
			catch (Exception ex)
			{
				ShowError($"{ex.Message}\r{ex.StackTrace}");
			}
		}

		private void KillProcById(int id)
		{
			try
			{
				var proc = Process.GetProcessById(id);

				proc.Kill();

				for (var i = 0; i < 10; i++)
				{
					if (proc.HasExited)
					{
						return;
					}

					Thread.Sleep(1000);
				}
			}
			catch (Exception ex)
			{
				ShowError($"{ex.Message}\r{ex.StackTrace}");
			}
		}

		private void ShowError(string error)
		{
			WindowState = WindowState.Normal;
			Application.Current.Dispatcher.Invoke(
				() => wm.ShowDialog(
					new DialogViewModel(error, DialogViewModel.DialogType.ERROR)));
		}

		/// <summary>
		/// Ожидание запуска самой игры
		/// </summary>
		/// <returns></returns>
		private void EnterToGame()
		{
			if (token.IsCancellationRequested) return;

			sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LMENU, VirtualKeyCode.F4);

			Thread.Sleep(3000);

			for (var i = 0; i < 1; i++)
			{
				sim.Keyboard.Sleep(500);
				sim.Keyboard.KeyPress(VirtualKeyCode.TAB);
			}

			sim.Keyboard.Sleep(500);
			sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

			for (var i = 0; i < 30; i++)
			{
				foreach (var p in Process.GetProcesses())
				{
					if (p.ProcessName == "ArcOSOverlay" && !OverlayIds.Contains(p.Id))
					{
						OverlayIds.Add(p.Id);

						if (OverlayIds.Count == PemIds.Count * 2 + 2)
						{
							sim.Keyboard.Sleep(1000);
							sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

							foreach (var proc in Process.GetProcessesByName("pem"))
							{
								if (PemIds.Contains(proc.Id)) continue;

								PemIds.Add(proc.Id);
							}

							return;
						}
					}
				}

				Thread.Sleep(1000);
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
