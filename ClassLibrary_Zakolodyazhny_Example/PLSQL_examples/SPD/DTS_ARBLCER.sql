create or replace 
PROCEDURE DTS_ARBLCER AS 
  node_number string(4) := '611%';
  node_name string(100) := 'arblcer';
  row_count_limit integer := 100;

  Time_busy_begin VARCHAR2(5):= '22:55'; 
  Time_busy_end VARCHAR2(5)  := '23:05';
  Curr_time VARCHAR2(5);
   
BEGIN
  
  select TO_CHAR(sysdate, 'HH24:MI') into curr_time From sys.Dual;
  dbms_output.put_line('curr_time  '||curr_time);
  
  IF curr_time > Time_busy_begin AND curr_time < Time_busy_end
  THEN 
    dbms_output.put_line('Its Time_busy ');
  ELSE
    GET_NEW_REPORTS(node_number, node_name, row_count_limit);
    --dbms_output.put_line('Its Time to work ');
  END IF;
  
  EXCEPTION
  When Others Then
      DECLARE
         Error_Code Number := Sqlcode;
      BEGIN
        dbms_output.put_line(' Error_Code '||Error_Code);
      END;
      Raise_Application_Error(-20001, Sqlerrm);
 
END DTS_ARBLCER;