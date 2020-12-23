create or replace 
PROCEDURE MonitorReconDaily AS 
  node_number varchar2(4); 
  curr_time varchar2(8);
  textstr varchar2(1024); 
  resstr  varchar2(1024); 
  time_interval INTERVAL DAY(3) TO SECOND(0) := '001 0:00:00';  --// '001 0:00:00' = 1 day;
-- file params
  log_file utl_file.file_type;
  log_file_name varchar2(1024) := 'argon_log.txt';
  log_dir_oracle_name varchar2(1024) := 'LOG_DIR';
 
BEGIN
  -- file part
  log_file := utl_file.fopen(log_dir_oracle_name, log_file_name, 'w');
  --  
  
  textstr := 'Today is: '||sysdate;
  dbms_output.put_line(textstr);
  utl_file.put_line(log_file, textstr);
  utl_file.fflush(log_file);
  --utl_file.fclose(log_file);
  
  select TO_CHAR(sysdate, 'HH24:MI:SS') into curr_time From sys.Dual;
  textstr := 'Current time:  '||curr_time;
  dbms_output.put_line(textstr);
  --log_file := utl_file.fopen('LOG_DIR', log_file_name, 'a');
  utl_file.put_line(log_file, textstr);
  utl_file.fflush(log_file);
  
  textstr := 'Пошук в БД файлів DAILY за останню добу (від '||(sysdate - time_interval)||' і новіших)';
  dbms_output.put_line(textstr);
  log_file := utl_file.fopen('LOG_DIR', log_file_name, 'a');
  utl_file.put_line(log_file, convert(textstr, 'CL8MSWIN1251'));
  utl_file.fflush(log_file);
  --utl_file.fclose(log_file);

  node_number := '601%';
  select TESTFILESMONITOR(node_number, log_file_name, time_interval) into resstr from dual;
  --dbms_output.put_line(resstr);
  --utl_file.put_line(log_file, convert(resstr, 'CL8MSWIN1251'));
  utl_file.FFLUSH(log_file);
  
  node_number := '602%';
  select TESTFILESMONITOR(node_number, log_file_name, time_interval) into resstr from dual;
  --dbms_output.put_line(resstr);
  --utl_file.put_line(log_file, convert(resstr, 'CL8MSWIN1251'));
  utl_file.FFLUSH(log_file);
  
  node_number := '603%';
  select TESTFILESMONITOR(node_number, log_file_name, time_interval) into resstr from dual;
  --dbms_output.put_line(resstr);
  --utl_file.put_line(log_file, convert(resstr, 'CL8MSWIN1251'));
  utl_file.FFLUSH(log_file);
  
  node_number := '604%';
  select TESTFILESMONITOR(node_number, log_file_name, time_interval) into resstr from dual;
  --dbms_output.put_line(resstr);
  --utl_file.put_line(log_file, convert(resstr, 'CL8MSWIN1251'));
  utl_file.FFLUSH(log_file);
  
  node_number := '605%';
  select TESTFILESMONITOR(node_number, log_file_name, time_interval) into resstr from dual;
  --dbms_output.put_line(resstr);
  --utl_file.put_line(log_file, convert(resstr, 'CL8MSWIN1251'));
  utl_file.FFLUSH(log_file);
  
  node_number := '606%';
  select TESTFILESMONITOR(node_number, log_file_name, time_interval) into resstr from dual;
  --dbms_output.put_line(resstr);
  --utl_file.put_line(log_file, convert(resstr, 'CL8MSWIN1251'));
  utl_file.FFLUSH(log_file);
  
  node_number := '607%';
  select TESTFILESMONITOR(node_number, log_file_name, time_interval) into resstr from dual;
  --dbms_output.put_line(resstr);
  --utl_file.put_line(log_file, convert(resstr, 'CL8MSWIN1251'));
  utl_file.FFLUSH(log_file);
  
  node_number := '608%';
  select TESTFILESMONITOR(node_number, log_file_name, time_interval) into resstr from dual;
  --dbms_output.put_line(resstr);
  --utl_file.put_line(log_file, convert(resstr, 'CL8MSWIN1251'));
  utl_file.FFLUSH(log_file);
  
  node_number := '609%';
  select TESTFILESMONITOR(node_number, log_file_name, time_interval) into resstr from dual;
  --dbms_output.put_line(resstr);
  --utl_file.put_line(log_file, convert(resstr, 'CL8MSWIN1251'));
  utl_file.FFLUSH(log_file);
  
  node_number := '610%';
  select TESTFILESMONITOR(node_number, log_file_name, time_interval) into resstr from dual;
  --dbms_output.put_line(resstr);
  --utl_file.put_line(log_file, convert(resstr, 'CL8MSWIN1251'));
  utl_file.FFLUSH(log_file);
  
  node_number := '611%';
  select TESTFILESMONITOR(node_number, log_file_name, time_interval) into resstr from dual;
  --dbms_output.put_line(resstr);
  --utl_file.put_line(log_file, convert(resstr, 'CL8MSWIN1251'));
  utl_file.FFLUSH(log_file);
  
  node_number := '612%';
  select TESTFILESMONITOR(node_number, log_file_name, time_interval) into resstr from dual;
  --dbms_output.put_line(resstr);
  --utl_file.put_line(log_file, convert(resstr, 'CL8MSWIN1251'));
  utl_file.FFLUSH(log_file);
  
  textstr := ' ';
  dbms_output.put_line(textstr);
  utl_file.put_line(log_file, textstr);
  
  select TO_CHAR(sysdate, 'HH24:MI:SS') into curr_time From sys.Dual;
  textstr := 'Current time: '||curr_time;
  dbms_output.put_line(textstr);
  utl_file.put_line(log_file, textstr);
  utl_file.fflush(log_file);
  
  textstr := 'End of log.';
  dbms_output.put_line(textstr);
  utl_file.put_line(log_file, textstr);
  utl_file.fflush(log_file);
  
  utl_file.fclose(log_file);
  
  EXCEPTION
  When Others Then
      DECLARE
         Error_Code Number := Sqlcode;
      BEGIN
        dbms_output.put_line(' Error_Code '||Error_Code);
        utl_file.put_line(log_file, Error_Code);
        utl_file.fclose(log_file);
      END;
      Raise_Application_Error(-22001, Sqlerrm);
 
 --utl_file.fclose(log_file);
END MonitorReconDaily;