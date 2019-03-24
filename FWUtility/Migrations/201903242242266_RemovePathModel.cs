namespace FWUtility.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovePathModel : DbMigration
    {
        public override void Up()
        {
            DropTable("dbo.Paths");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.Paths",
                c => new
                    {
                        PathId = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Directory = c.String(),
                        Parameter = c.String(),
                    })
                .PrimaryKey(t => t.PathId);
            
        }
    }
}
