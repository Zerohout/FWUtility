namespace FWUtility.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ModifyPathModel : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Paths", "Parameter", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Paths", "Parameter");
        }
    }
}
