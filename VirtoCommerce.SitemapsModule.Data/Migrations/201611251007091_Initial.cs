namespace VirtoCommerce.SitemapsModule.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Sitemap",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Filename = c.String(nullable: false, maxLength: 256),
                        StoreId = c.String(nullable: false, maxLength: 64),
                        CreatedDate = c.DateTime(nullable: false),
                        ModifiedDate = c.DateTime(),
                        CreatedBy = c.String(maxLength: 64),
                        ModifiedBy = c.String(maxLength: 64),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.SitemapItem",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Title = c.String(nullable: false, maxLength: 256),
                        ImageUrl = c.String(maxLength: 512),
                        ObjectId = c.String(nullable: false, maxLength: 128),
                        ObjectType = c.String(nullable: false, maxLength: 128),
                        SitemapId = c.String(nullable: false, maxLength: 128),
                        CreatedDate = c.DateTime(nullable: false),
                        ModifiedDate = c.DateTime(),
                        CreatedBy = c.String(maxLength: 64),
                        ModifiedBy = c.String(maxLength: 64),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Sitemap", t => t.SitemapId, cascadeDelete: true)
                .Index(t => t.SitemapId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.SitemapItem", "SitemapId", "dbo.Sitemap");
            DropIndex("dbo.SitemapItem", new[] { "SitemapId" });
            DropTable("dbo.SitemapItem");
            DropTable("dbo.Sitemap");
        }
    }
}
