$ErrorActionPreference='Stop'
$root='D:\code_da\TK-Sport-Community'
$conn=[System.Data.SqlClient.SqlConnection]::new('Server=localhost;Database=Sep490_Import_20261110;Integrated Security=True;TrustServerCertificate=True')
$conn.Open()
function Query($sql){$cmd=$conn.CreateCommand();$cmd.CommandText=$sql;$dt=[System.Data.DataTable]::new();$dt.Load($cmd.ExecuteReader());return ,$dt}
$tables=Query 'SELECT name FROM sys.tables ORDER BY name'
$cols=Query 'SELECT t.name tbl,c.name,col_type=ty.name,c.is_identity,c.is_computed,c.column_id FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id ORDER BY t.name,c.column_id'
$fks=Query 'SELECT t.name tbl,c.name col,rt.name parent,rc.name parentcol FROM sys.foreign_key_columns f JOIN sys.tables t ON t.object_id=f.parent_object_id JOIN sys.columns c ON c.object_id=t.object_id AND c.column_id=f.parent_column_id JOIN sys.tables rt ON rt.object_id=f.referenced_object_id JOIN sys.columns rc ON rc.object_id=rt.object_id AND rc.column_id=f.referenced_column_id'
$conn.Close()
$keys=@{
roles=@('code');sports=@('code');venues=@('name');users=@('email');players=@('user_id');player_sport_profiles=@('player_id','sport_id');rubrics=@('sport_id','version');rubric_dimensions=@('rubric_id','code');courts=@('venue_id','name');time_slots=@('start_time','end_time');sessions=@('court_id','session_date','time_slot_id');session_referees=@('session_id','referee_id');session_staff=@('session_id','staff_id');social_booking_orders=@('order_code');tickets=@('ticket_code');orders=@('order_code');payments=@('reference_code');bank_transactions=@('external_id');products=@('sku');product_units=@('product_id','unit_name');stock_items=@('product_id');order_items=@('order_id','product_id');assessment_scores=@('assessment_id','dimension_id');friendships=@('requester_id','addressee_id');policies=@('code');user_fcm_tokens=@('fcm_token');late_entry_offers=@('session_id');late_entry_tickets=@('ticket_id')
}
$ordered=[System.Collections.Generic.List[string]]::new()
$pending=[System.Collections.Generic.List[string]]::new()
foreach($r in $tables.Rows){$pending.Add($r.name)}
while($pending.Count){$progress=$false;foreach($t in @($pending.ToArray())){$deps=@($fks.Select("tbl='$t'")|ForEach-Object {$_.parent}|Select-Object -Unique);if(@($deps|Where-Object {$_ -notin $ordered}).Count -eq 0){$ordered.Add($t);$pending.Remove($t)|Out-Null;$progress=$true}};if(!$progress){throw 'Cyclic schema'}}
$sql=[System.Text.StringBuilder]::new()
[void]$sql.AppendLine(@'
SET NOCOUNT ON; SET XACT_ABORT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SET ANSI_WARNINGS ON; SET ANSI_PADDING ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON; SET NUMERIC_ROUNDABORT OFF;
IF DB_NAME()<>'Sep490' THROW 51000,'Wrong target',1;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service';
IF EXISTS(SELECT 1 FROM dbo.audit_logs WHERE action='merge_backup_20261110') THROW 51000,'This backup has already been merged',1;
BEGIN TRANSACTION;
GO
'@)
[void]$sql.AppendLine((Get-Content "$root/db/upgrades/014-late-entry-sales.sql" -Raw))
[void]$sql.AppendLine(@'
GO
DISABLE TRIGGER dbo.TR_payments_revenue ON dbo.payments;
DISABLE TRIGGER dbo.TR_refunds_revenue ON dbo.refunds;
UPDATE dbo.venues SET name=N'TKSC Vĩnh Phúc' WHERE id=1 AND name=N'TK Sport Community Center';
UPDATE dbo.courts SET venue_id=1 WHERE venue_id=2 AND name IN(N'Sân Demo Cầu lông',N'Sân Demo Pickleball');
GO
'@)
foreach($t in $ordered){
 $tc=@($cols.Select("tbl='$t'"));$pk=if($t -eq 'late_entry_offers'){'session_id'}elseif($t -eq 'late_entry_tickets'){'ticket_id'}else{'id'}
 $writable=@($tc|Where-Object {!$_.is_identity -and !$_.is_computed})
 $select=[System.Collections.Generic.List[string]]::new();$joins=[System.Collections.Generic.List[string]]::new()
 $select.Add("s.[$pk] AS _old_id")
 foreach($c in $writable){$fk=@($fks.Select("tbl='$t' AND col='$($c.name)'"));if($fk.Count){$alias='m_'+$c.name;$select.Add("$alias.new_id AS [$($c.name)]");$joins.Add("LEFT JOIN #map_$($fk[0].parent) $alias ON $alias.old_id=s.[$($c.name)]")}else{$select.Add("s.[$($c.name)]")}}
 $on='1=0';if($keys.ContainsKey($t)){$parts=foreach($k in $keys[$t]){$ct=($tc|Where-Object name -eq $k).col_type;if($ct -match 'char|text'){"t.[$k]=s.[$k] COLLATE DATABASE_DEFAULT"}else{"t.[$k]=s.[$k]"}};$on=$parts -join ' AND '}
 $updates=''
 if($t -eq 'users'){$updates='WHEN MATCHED THEN UPDATE SET venue_id=s.venue_id'}
 if($t -eq 'player_sport_profiles'){$updates='WHEN MATCHED THEN UPDATE SET current_score=COALESCE(s.current_score,t.current_score),current_band=COALESCE(s.current_band,t.current_band),rating_status=CASE WHEN s.current_score IS NOT NULL THEN s.rating_status ELSE t.rating_status END'}
 if($t -eq 'stock_items'){$updates='WHEN MATCHED THEN UPDATE SET quantity_on_hand=s.quantity_on_hand,reorder_level=s.reorder_level,venue_id=s.venue_id'}
 if($t -eq 'orders'){$updates='WHEN MATCHED THEN UPDATE SET total_amount=s.total_amount,status=s.status,payment_state=s.payment_state,handover_state=s.handover_state,venue_id=s.venue_id,staff_id=s.staff_id,buyer_id=s.buyer_id'}
 if($t -eq 'order_items'){$updates='WHEN MATCHED THEN UPDATE SET quantity=s.quantity,unit_price_vnd=s.unit_price_vnd,subtotal_vnd=s.subtotal_vnd'}
 $names=($writable|ForEach-Object {"[$($_.name)]"}) -join ','
 $values=($writable|ForEach-Object {"s.[$($_.name)]"}) -join ','
 [void]$sql.AppendLine("CREATE TABLE #map_$t (old_id bigint PRIMARY KEY,new_id bigint NOT NULL);")
 [void]$sql.AppendLine("MERGE dbo.[$t] AS t USING (SELECT $($select -join ',') FROM Sep490_Import_20261110.dbo.[$t] s $($joins -join ' ')) s ON $on $updates WHEN NOT MATCHED THEN INSERT ($names) VALUES ($values) OUTPUT s._old_id,inserted.[$pk] INTO #map_$t;")
 if(!$updates -and $keys.ContainsKey($t)){
   $matchParts=foreach($k in $keys[$t]){$fk=@($fks.Select("tbl='$t' AND col='$k'"));$expr=if($fk.Count){"(SELECT new_id FROM #map_$($fk[0].parent) WHERE old_id=s.[$k])"}else{"s.[$k]"};$ct=($tc|Where-Object name -eq $k).col_type;if($ct -match 'char|text'){$expr+=' COLLATE DATABASE_DEFAULT'};"t.[$k]=$expr"}
   [void]$sql.AppendLine("INSERT #map_$t SELECT s.[$pk],t.[$pk] FROM Sep490_Import_20261110.dbo.[$t] s JOIN dbo.[$t] t ON $($matchParts -join ' AND ') WHERE NOT EXISTS(SELECT 1 FROM #map_$t m WHERE m.old_id=s.[$pk]);")
 }
 [void]$sql.AppendLine("IF (SELECT COUNT(*) FROM #map_$t)<>(SELECT COUNT(*) FROM Sep490_Import_20261110.dbo.[$t]) THROW 51000,'Incomplete mapping: $t',1;")
}
[void]$sql.AppendLine(@'
UPDATE a SET entity_id=COALESCE(m.new_id,a.entity_id) FROM dbo.audit_logs a JOIN #map_audit_logs imported ON imported.new_id=a.id OUTER APPLY (
 SELECT new_id FROM #map_assessments WHERE old_id=a.entity_id AND a.entity_type='Assessment'
 UNION ALL SELECT new_id FROM #map_orders WHERE old_id=a.entity_id AND a.entity_type='GoodsOrder'
 UNION ALL SELECT new_id FROM #map_payments WHERE old_id=a.entity_id AND a.entity_type='Payment'
 UNION ALL SELECT new_id FROM #map_rating_appeals WHERE old_id=a.entity_id AND a.entity_type='RatingAppeal'
 UNION ALL SELECT new_id FROM #map_sessions WHERE old_id=a.entity_id AND a.entity_type IN('Session','SessionSupply')
 UNION ALL SELECT new_id FROM #map_session_exceptions WHERE old_id=a.entity_id AND a.entity_type='SessionException'
 UNION ALL SELECT new_id FROM #map_tickets WHERE old_id=a.entity_id AND a.entity_type='Ticket') m;
UPDATE n SET reference_id=m.new_id FROM dbo.notifications n JOIN #map_notifications i ON i.new_id=n.id JOIN #map_tickets m ON m.old_id=n.reference_id WHERE n.reference_type LIKE 'ticket_%';
UPDATE sm SET reference_id=COALESCE(m.new_id,sm.reference_id) FROM dbo.stock_movements sm JOIN #map_stock_movements i ON i.new_id=sm.id OUTER APPLY (
 SELECT new_id FROM #map_products WHERE old_id=sm.reference_id AND sm.reference_type='Product'
 UNION ALL SELECT new_id FROM #map_sessions WHERE old_id=sm.reference_id AND sm.reference_type='Session') m;
DELETE sm FROM dbo.stock_movements sm JOIN #map_stock_items m ON m.new_id=sm.stock_item_id WHERE sm.reference_type='test_reset';
ENABLE TRIGGER dbo.TR_payments_revenue ON dbo.payments;
ENABLE TRIGGER dbo.TR_refunds_revenue ON dbo.refunds;
SELECT p.id INTO #receipts FROM dbo.payments p JOIN #map_payments m ON m.new_id=p.id WHERE p.status='paid' AND NOT EXISTS(SELECT 1 FROM dbo.revenue_entries e WHERE e.payment_id=p.id AND e.refund_id IS NULL);
UPDATE p SET status='pending' FROM dbo.payments p JOIN #receipts r ON r.id=p.id;
UPDATE p SET status='paid' FROM dbo.payments p JOIN #receipts r ON r.id=p.id;
-- Keep one assignment per referee/time, preferring sessions with checked-in players.
CREATE TABLE #kept_ref(referee_id bigint,session_id bigint);
DECLARE @assignment bigint,@ref bigint,@session bigint;
DECLARE assignments CURSOR LOCAL FAST_FORWARD FOR
SELECT sr.id,sr.referee_id,sr.session_id FROM dbo.session_referees sr JOIN dbo.sessions s ON s.id=sr.session_id
WHERE s.session_date>='20261110'
ORDER BY (SELECT COUNT(*) FROM dbo.tickets t WHERE t.session_id=s.id AND t.status='checkedin') DESC,
 (SELECT COUNT(*) FROM dbo.tickets t WHERE t.session_id=s.id AND t.status='confirmed') DESC,
 CASE WHEN EXISTS(SELECT 1 FROM #map_session_referees m WHERE m.new_id=sr.id) THEN 0 ELSE 1 END,s.id;
OPEN assignments; FETCH NEXT FROM assignments INTO @assignment,@ref,@session;
WHILE @@FETCH_STATUS=0
BEGIN
 IF EXISTS(SELECT 1 FROM #kept_ref k JOIN dbo.sessions a ON a.id=k.session_id JOIN dbo.time_slots ta ON ta.id=a.time_slot_id
 CROSS JOIN dbo.sessions b JOIN dbo.time_slots tb ON tb.id=b.time_slot_id
 WHERE k.referee_id=@ref AND b.id=@session AND a.session_date=b.session_date AND ta.start_time<tb.end_time AND tb.start_time<ta.end_time)
 BEGIN
  DELETE dbo.session_referees WHERE id=@assignment;
  PRINT CONCAT('Resolved referee overlap: assignment ',@assignment,', session ',@session);
 END
 ELSE INSERT #kept_ref VALUES(@ref,@session);
 FETCH NEXT FROM assignments INTO @assignment,@ref,@session;
END;
CLOSE assignments; DEALLOCATE assignments;
IF EXISTS(SELECT 1 FROM dbo.sessions s JOIN dbo.tickets t ON t.session_id=s.id WHERE s.session_date>='20261110' AND t.status IN('confirmed','checkedin','reserved') GROUP BY s.id,s.capacity HAVING COUNT(*)>s.capacity) THROW 51000,'Future session exceeds capacity',1;
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1) THROW 51000,'Invalid FK',1;
INSERT dbo.audit_logs(actor_id,action,entity_type,new_value) SELECT MIN(id),'merge_backup_20261110','Database',N'Merged verified backup SHA256 6d0b25777734f7f275a5b1d9de9f7b562df779ebf8ed8a8a440fff377ff1739a; preserved local test data; remapped foreign keys.' FROM dbo.users WHERE google_subject IS NOT NULL;
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;
SELECT 'MERGE TRIAL OK' result;
ROLLBACK;
'@)
[System.IO.File]::WriteAllText("$root/db/merge-backup-20261110.sql",$sql.ToString(),[System.Text.UTF8Encoding]::new($true))
Write-Output "Generated merge SQL for $($ordered.Count) tables"
