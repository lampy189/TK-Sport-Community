SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.ticket_support_messages','U') IS NULL
BEGIN
 CREATE TABLE dbo.ticket_support_messages (
  id BIGINT IDENTITY PRIMARY KEY,
  ticket_id BIGINT NOT NULL REFERENCES dbo.tickets(id),
  sender_id BIGINT NOT NULL REFERENCES dbo.users(id),
  is_staff BIT NOT NULL,
  content NVARCHAR(2000) NOT NULL,
  image_data VARBINARY(MAX) NULL,
  image_type VARCHAR(20) NULL,
  created_at DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
  CONSTRAINT CK_ticket_support_image CHECK ((image_data IS NULL AND image_type IS NULL) OR (image_data IS NOT NULL AND DATALENGTH(image_data) BETWEEN 1 AND 2097152 AND image_type IN ('image/png','image/jpeg')))
 );
 CREATE INDEX IX_ticket_support_ticket ON dbo.ticket_support_messages(ticket_id,id);
END;
COMMIT;
