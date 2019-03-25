namespace FWUtility.ViewModels
{
	using System.Collections.Generic;
	using System.Data.Entity;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using Caliburn.Micro;
	using Database;
	using Model;
	using WindowsInput;
	using WindowsInput.Native;
	using static Helpers.Helper;
	using Screen = Caliburn.Micro.Screen;

	public class MainViewModel : Conductor<Screen>.Collection.OneActive
	{
		private WindowState _windowState;

		public WindowState WindowState
		{
			get => _windowState;
			set
			{
				_windowState = value;
				NotifyOfPropertyChange(() => WindowState);
			}
		}



		private BindableCollection<Account> _accounts = new BindableCollection<Account>();
		private Account _selectedAccount;
		private string _arcPath;

		public string ArcPath
		{
			get => _arcPath;
			set
			{
				_arcPath = value;
				NotifyOfPropertyChange(() => ArcPath);
			}
		}

		public MainViewModel()
		{
			LoadData();
		}

		protected override void OnInitialize()
		{
			LoadPathData();
			base.OnInitialize();
		}

		public async void LoadData()
		{
			Accounts.Clear();
			using (var ctx = new FWUDbContext())
			{
				await ctx.Accounts.LoadAsync();
				Accounts.AddRange(ctx.Accounts.Local.OrderBy(a => a.Name));
			}

			if (Accounts.All(a => a.Name != CreateName))
			{
				Accounts.Add(CreatingAccount);
			}
		}

		public async void LoadPathData()
		{
			if (!File.Exists(ArcPathDirectory))
			{
				using (var fs = new FileStream(ArcPathDirectory, FileMode.Create))
				{
					fs.Dispose();
				}
				//File.Create(ArcPathDirectory);


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

			var temp = Accounts.Where(a => a.IsChecked);

			foreach (var acc in temp)
			{
				await StartingGame(acc);
			}

		}

		private Task StartingGame(Account currAcc)
		{
			var sim = new InputSimulator();

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

			

			Process.Start($"{ArcPath}{ArcEndPath}");

			var working = true;
			var flag = false;
			var arcId = 0;
			var test = new List<int>();
			var q = new List<int>();

			while (working)
			{
				foreach (var p in Process.GetProcesses())
				{
					if (p.ProcessName == "Arc")
					{

						if (!test.Contains(p.Id))
						{
							test.Add(p.Id);

						}
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

				q = test;

				Thread.Sleep(500);
			}

			var proc = Process.GetProcessById(arcId);

			Thread.Sleep(5000);

			sim.Keyboard.KeyPress(VirtualKeyCode.TAB);
			Thread.Sleep(500);
			sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.SHIFT, VirtualKeyCode.END);
			Thread.Sleep(500);
			sim.Keyboard.TextEntry(currAcc.Email);
			Thread.Sleep(500);
			sim.Keyboard.KeyPress(VirtualKeyCode.TAB);
			Thread.Sleep(500);
			sim.Keyboard.TextEntry(currAcc.Password);
			Thread.Sleep(500);
			sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

			working = true;

			while (working)
			{
				foreach (var p in Process.GetProcesses())
				{
					if (p.ProcessName == "ArcChat")
					{
						working = false;
						break;
					}
				}

				Thread.Sleep(1000);
			}


			Process.Start($"{ArcPath}{LauncherEndPath}", LauncherParameter);

			working = true;

			while (working)
			{
				foreach (var p in Process.GetProcesses())
				{
					if (p.ProcessName == "patcher")
					{
						working = false;
						break;
					}
				}
				Thread.Sleep(1000);
			}

			for (var i = 0; i < 11; i++)
			{
				sim.Keyboard.KeyPress(VirtualKeyCode.TAB);
				Thread.Sleep(500);
			}

			Thread.Sleep(2000);

			working = true;

			while (working)
			{
				foreach (var p in Process.GetProcesses())
				{
					if (p.ProcessName == "pem")
					{
						working = false;
						break;
					}
				}

				if (working)
				{
					sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
				}

				Thread.Sleep(2000);
			}

			Thread.Sleep(5000);

			proc.Kill();

			return Task.CompletedTask;
		}

		public void CheckedValidation()
		{
			NotifyOfPropertyChange(() => CanStart);
		}

		public void UnCheckedValidation()
		{
			NotifyOfPropertyChange(() => CanStart);
		}

		public bool CanStart
		{
			get
			{
				if (Accounts.Any(a => a.IsChecked))
				{
					return Accounts.Count(a => a.IsChecked) <= 3;
				}
				else
				{
					return false;
				}
			}
		}

		public void SelectionChanged()
		{
			ActiveItem?.TryClose();

			if (_selectedAccount == null)
			{
				return;
			}

			ActiveItem = _selectedAccount.Name == CreateName
				? new EditableViewModel(new Account(), EditingState.CREATING)
				: new EditableViewModel(new Account
				{
					Name = _selectedAccount.Name,
					Email = _selectedAccount.Email,
					Password = _selectedAccount.Password
				}, EditingState.EDITING);
		}

		public void Settings()
		{
			SelectedAccount = null;
			ActiveItem = new SettingsViewModel(ArcPath);
		}

		public Account SelectedAccount
		{
			get => _selectedAccount;
			set
			{
				_selectedAccount = value;
				NotifyOfPropertyChange(() => SelectedAccount);
			}
		}

		public BindableCollection<Account> Accounts
		{
			get => _accounts;
			set
			{
				_accounts = value;
				NotifyOfPropertyChange(() => Accounts);
			}
		}

		public Account CreatingAccount => new Account
		{
			Name = "Добавить",
			Visibility = Visibility.Hidden
		};


		private Screen _editableScreen;

		public Screen EditableScreen
		{
			get => _editableScreen;
			set
			{
				_editableScreen = value;
				NotifyOfPropertyChange(() => EditableScreen);
			}
		}
	}
}
