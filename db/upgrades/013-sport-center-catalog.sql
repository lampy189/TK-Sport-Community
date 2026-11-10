SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service';
IF COL_LENGTH('dbo.products','venue_id') IS NULL
BEGIN
 ALTER TABLE dbo.products ADD venue_id bigint NULL CONSTRAINT FK_products_venue REFERENCES dbo.venues(id);
 ALTER TABLE dbo.products ADD CONSTRAINT DF_products_venue DEFAULT TRY_CONVERT(bigint,SESSION_CONTEXT(N'tksc_venue')) FOR venue_id;
END;
GO
-- Only derive a catalog's center from an already explicitly assigned stock row.
UPDATE p SET venue_id=s.venue_id FROM dbo.products p JOIN dbo.stock_items s ON s.product_id=p.id WHERE p.venue_id IS NULL AND s.venue_id IS NOT NULL;
GO
IF OBJECT_ID(N'dbo.sc_user',N'IF') IS NULL EXEC(N'CREATE FUNCTION dbo.sc_user(@id bigint,@venue bigint) RETURNS TABLE WITH SCHEMABINDING AS RETURN
SELECT 1 AS allowed WHERE EXISTS(SELECT 1 FROM dbo.sc_venue(@venue)) OR EXISTS(
 SELECT 1 FROM dbo.players p CROSS APPLY dbo.sc_player(p.id) a WHERE p.user_id=@id);');
GO
IF NOT EXISTS(SELECT 1 FROM sys.security_predicates WHERE target_object_id=OBJECT_ID('dbo.products'))
 ALTER SECURITY POLICY dbo.SportCenterIsolation
 ADD FILTER PREDICATE dbo.sc_venue(venue_id) ON dbo.products,
 ADD BLOCK PREDICATE dbo.sc_venue(venue_id) ON dbo.products AFTER INSERT,
 ADD BLOCK PREDICATE dbo.sc_venue(venue_id) ON dbo.products AFTER UPDATE;
IF NOT EXISTS(SELECT 1 FROM sys.security_predicates WHERE target_object_id=OBJECT_ID('dbo.users'))
 ALTER SECURITY POLICY dbo.SportCenterIsolation ADD FILTER PREDICATE dbo.sc_user(id,venue_id) ON dbo.users;
GO
