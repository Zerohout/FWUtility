﻿namespace FWUtility.Database
{
	using System;
	using System.Data.Entity;
	using Models;

	public class FWUDbContext : DbContext
	{
		public FWUDbContext() : base("FWUDb")
		{
			AppDomain.CurrentDomain.SetData("DataDirectory", AppDomain.CurrentDomain.BaseDirectory);
			
			Database.CreateIfNotExists();
			
			//Database.SetInitializer(new DropCreateDatabaseAlways<FWUDbContext>());
			Database.SetInitializer(new DropCreateDatabaseIfModelChanges<FWUDbContext>());
			
		}

		public DbSet<Account> Accounts { get; set; }
	}
}
