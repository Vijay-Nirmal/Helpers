--EXAMPLE OF LONGEST RUNNING QUERIES (MORE THAN 5 SECONDS) AND ADDING COLUMNS AND SOME COMMON DMVs

CREATE EVENT SESSION [Long-Running Queries] ON SERVER
ADD EVENT sqlserver.rpc_completed (
ACTION ( sqlserver.client_app_name, sqlserver.client_hostname,
sqlserver.database_id, sqlserver.database_name, sqlserver.nt_username,
sqlserver.query_hash, sqlserver.server_principal_name,
sqlserver.session_id, sqlserver.sql_text )
WHERE 
( duration >= ( 500000 ) ) ),   --<< GREATER OR EQUAL TOO 5 SECONDS

ADD EVENT sqlserver.sql_batch_completed (SET collect_batch_text = ( 1 )
ACTION ( sqlserver.client_app_name, sqlserver.database_id,
sqlserver.query_hash, sqlserver.session_id )
WHERE ( duration >= ( 500000 ) )) 

ADD TARGET package0.event_file(SET filename=N'Long-Running Queries')

WITH ( MAX_MEMORY = 4096 KB
, EVENT_RETENTION_MODE = ALLOW_SINGLE_EVENT_LOSS
, MAX_DISPATCH_LATENCY = 1 SECONDS
, MAX_EVENT_SIZE = 0 KB
, MEMORY_PARTITION_MODE = NONE
, TRACK_CAUSALITY = ON
, STARTUP_STATE = ON );
GO

