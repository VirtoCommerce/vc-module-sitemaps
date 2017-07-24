namespace VirtoCommerce.SitemapsModule.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AcceptedStaticContentFilenameExtensions : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Sitemap", "AcceptedFilenameExtensions", c => c.String(maxLength: 256));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Sitemap", "AcceptedFilenameExtensions");
        }
    }
}
