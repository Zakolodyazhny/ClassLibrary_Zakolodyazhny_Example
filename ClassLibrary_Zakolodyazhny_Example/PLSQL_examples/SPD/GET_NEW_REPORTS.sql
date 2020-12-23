create or replace 
PROCEDURE GET_NEW_REPORTS 
(
  node_number IN VARCHAR2,
  node_name IN VARCHAR2,
  row_count_limit IN INTEGER)
AS  
  report_count Number(20,0);
  max_local_rid Number(20,0);
  max_remote_rid Number(20,0);
  sql_stmt VARCHAR2(500);
  controller_changed_count integer;
  Curr_time VARCHAR2(20);
  
BEGIN
  --//for test only
  select TO_CHAR(SYSTIMESTAMP, 'hh24:mi:ss.ff3') into Curr_time From sys.Dual; dbms_output.put_line('Curr_time_1  '||Curr_time);
  
  execute immediate('select 1 from dual@'||node_name); -- test of connection
  
  --//for test only
  --select TO_CHAR(SYSTIMESTAMP, 'hh24:mi:ss.ff3') into Curr_time From sys.Dual; dbms_output.put_line('Curr_time_2  '||Curr_time);
  
  execute immediate('select count (*) into :controller_changed_count from (select C.last_change from controller C, controller@'||node_name||' R where C.rid_controller = R.rid_controller and C.last_change < R.last_change and C.Rid_Controller like :1)')
    INTO controller_changed_count USING node_number;
    
  IF controller_changed_count > 0
  THEN 
    dbms_output.put_line('Changed controller count= '||controller_changed_count||'. '||node_name||' controller changed.');
    MERGE_STATION(node_name);
  END IF;
  
   --//for test only
  --select TO_CHAR(SYSTIMESTAMP, 'hh24:mi:ss.ff3') into Curr_time From sys.Dual; dbms_output.put_line('Curr_time_4  '||Curr_time);
  
  select count(*) into report_count from report where rid_report like node_number;
  --dbms_output.put_line('local db report_count= '||report_count);
  
  IF report_count = 0
  THEN 
    --execute immediate('insert into report select * from report@'||node_name||' where rid_report = (select min(rid_report) from report@'||node_name||' where rid_report like  '|| node_number||')');
    execute immediate('insert into report select * from report@'||node_name||' where rid_report = (select min(rid_report) from report@'||node_name||' where rid_report like :1)') USING node_number;
    --end if; - можно закончить это условие и выйти из него.
  ELSE
     --//for test only
    --select TO_CHAR(SYSTIMESTAMP, 'hh24:mi:ss.ff3') into Curr_time From sys.Dual;     --dbms_output.put_line('Curr_time_5  '||Curr_time);
  
    execute immediate('select max(rid_report) into :max_local_rid from report where rid_report like :1') INTO max_local_rid USING node_number;
    
    --//for test only
    --dbms_output.put_line('max_local_rid= '||max_local_rid);
    --select TO_CHAR(SYSTIMESTAMP, 'hh24:mi:ss.ff3') into Curr_time From sys.Dual;    dbms_output.put_line('Curr_time_6  '||Curr_time);
    
    --//new
    execute immediate('select max(rid_report) into :max_remote_rid from report@'||node_name||' where rid_report like :1') INTO max_remote_rid USING node_number;
    --dbms_output.put_line('max_remote_rid= '||max_remote_rid);
    
     --//for test only/
    --select TO_CHAR(SYSTIMESTAMP, 'hh24:mi:ss.ff3') into Curr_time From sys.Dual;    dbms_output.put_line('Curr_time_7  '||Curr_time);
    
    IF max_local_rid < max_remote_rid
    THEN
      dbms_output.put_line('Execute insert into report.');
      sql_stmt := 'insert into report (select * from report@'||node_name||' where rid_report in 
      (select * from (select rid_report from report@'||node_name||' where rid_report like :1 and rid_report > :2 order by rid_report) where rownum <= :3 ))';
      execute immediate sql_stmt using node_number, max_local_rid, row_count_limit;
    END IF;
    
  END IF;
  
   --//for test only
  select TO_CHAR(SYSTIMESTAMP, 'hh24:mi:ss.ff3') into Curr_time From sys.Dual;   dbms_output.put_line('Curr_time_8 '||Curr_time||'. END.');
  
EXCEPTION
  When Others Then
      DECLARE
         Error_Code Number := Sqlcode;
      Begin
         IF error_code = -2291 --/* Parent key not found. */
         THEN
            Insert_NewTypes(node_name);     
         /*ELSE for test mode
            Sys.Dbms_Lock.Sleep(60);*/
         End If;
      END;
      Raise_Application_Error(-20005, ' GET_NEW_REPORTS error: '||Sqlerrm||' ');
      
END GET_NEW_REPORTS;