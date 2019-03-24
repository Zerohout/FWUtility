namespace FWUtility.ViewModels
{
	using System;
	using System.Data.Entity;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Windows;
	using Caliburn.Micro;
	using Database;
	using Model;
	using static Helpers.Helper;

	public class MainViewModel : Conductor<Screen>.Collection.OneActive
	{
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
				using (var fs = new FileStream(ArcPathDirectory,FileMode.Create))
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

		public void Start()
		{
			var wm = new WindowManager();
			if (!Directory.Exists(ArcPath))
			{
				wm.ShowDialog(new DialogViewModel(
								  $"{ArcPathString} некорректный. Измените его в настройках",
								  DialogViewModel.DialogType.OK));
				return;
			}

			Process.Start($"{ArcPath}{ArcEndPath}");
			Thread.Sleep(5000);
			
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
