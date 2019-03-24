namespace FWUtility.Database
{
	using System;
	using System.Data.Entity;
	using Model;

	public class FWUDbContext : DbContext
	{
		public FWUDbContext() : base("FWUDb")
		{
			AppDomain.CurrentDomain.SetData("DataDirectory", AppDomain.CurrentDomain.BaseDirectory);

			Database.CreateIfNotExists();

			//Database.SetInitializer(new DropCreateDatabaseAlways<EF6DbContext>());
			Database.SetInitializer(new DropCreateDatabaseIfModelChanges<FWUDbContext>());
		}

		public DbSet<Account> Accounts { get; set; }
	}
}
