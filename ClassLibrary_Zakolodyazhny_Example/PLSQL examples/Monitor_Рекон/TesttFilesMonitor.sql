create or replace 
Function TestFilesMonitor
   ( node_number IN VARCHAR2, 
     file_name IN VARCHAR2,
     time_interval IN INTERVAL DAY TO SECOND := '001 0:00:00') 
   RETURN varchar2
IS
    return_value varchar2(1024);
    daily_count number(10);
    station_daily_count number(10);
    tmpstr varchar2(1024);
    stationName varchar2(1024);
    reconCount number(10);
    tmpNumber number(10);
 -- file params
    f_out utl_file.file_type;
   
BEGIN
-- file part
   f_out := utl_file.fopen('LOG_DIR', file_name, 'a');
--  

  dbms_output.put_line(' ');
  utl_file.put_line(f_out, ' ');
  utl_file.FFLUSH(f_out);
  
  return_value := 'FALSE';
  daily_count := 0;
  station_daily_count :=0;
  stationName := ' ';
  
  tmpstr := 'node_number: '||node_number;
  dbms_output.put_line(tmpstr); 
  --utl_file.put_line(f_out, tmpstr);
  --utl_file.FFLUSH(f_out);
  
  BEGIN
  select count(*) into tmpNumber from station where RID_STATION like node_number;
  
  if (tmpNumber) > 0 then select station_name INTO stationName from station where RID_STATION like node_number;
  else stationName := ' There isn''t such station';   end if;
  
  EXCEPTION
    WHEN ACCESS_INTO_NULL THEN return_value :=  ' There isn''t such station';
    WHEN OTHERS THEN return_value := 'Unknown Error'; --RAISE_APPLICATION_ERROR(-20109,'Unknown Error');   
  END;
  
  select count(*) into reconCount from DEVICE where RID_DEVICE like node_number and REC_ACTIVE = 'TRUE';
  
  tmpstr := 'Станція: '||stationName||'. Кількість реконів: '||reconCount;
  dbms_output.put_line(tmpstr); 
  utl_file.put_line(f_out, convert(tmpstr, 'CL8MSWIN1251'));
  utl_file.FFLUSH(f_out);
  
  For cur in (select rid_device, device_name from device where RID_DEVICE_TYPE = 10001000000000000066 and RID_DEVICE like node_number and REC_ACTIVE = 'TRUE') loop
   begin
      --здесь пишем что делать
    select count(*) INTO daily_count from report 
    where rid_report_type = 10001000000000002915
    and sc_number is not null 
    and RID_DEVICE = cur.rid_device
    and DEVICE_DATE_TIME >= sysdate - time_interval
    ;
    station_daily_count := station_daily_count + daily_count;
    
    tmpstr := 'Рекон: '||cur.device_name||', '|| chr(9) ||'DAILY-файли: '||daily_count;
    dbms_output.put_line(tmpstr); 
    utl_file.put_line(f_out, convert(tmpstr, 'CL8MSWIN1251'));
    utl_file.FFLUSH(f_out);
   end;
  end loop;
  
  tmpstr :='Загалом по '||stationName||' DAILY-файлів: '||station_daily_count||'. ';
  dbms_output.put(tmpstr); 
  utl_file.put(f_out, convert(tmpstr, 'CL8MSWIN1251'));
  utl_file.FFLUSH(f_out);
  
  IF (reconCount = station_daily_count) and (reconCount > 0) THEN begin
    tmpstr := ' Все ОК. ';
    return_value := 'TRUE'; end;
  ELSE begin
    tmpstr := ' Відсутні файли ! ';
    return_value := 'FALSE'; end;
  END IF;
  dbms_output.put_line(tmpstr); 
  utl_file.put_line(f_out, convert(tmpstr, 'CL8MSWIN1251'));
  utl_file.FFLUSH(f_out);

  utl_file.fclose(f_out);

RETURN return_value;

  EXCEPTION  
   WHEN UTL_FILE.INVALID_PATH THEN return_value := 'Invalid Path'; --RAISE_APPLICATION_ERROR(-20100,'Invalid Path');
   WHEN UTL_FILE.INVALID_MODE THEN return_value := 'Invalid Mode'; --RAISE_APPLICATION_ERROR(-20101,'Invalid Mode');
   WHEN UTL_FILE.INVALID_FILEHANDLE THEN return_value := 'Invalid Filehandle'; --RAISE_APPLICATION_ERROR(-20102,'Invalid Filehandle');  
   WHEN UTL_FILE.INVALID_OPERATION THEN return_value := 'Invalid Operation -- May signal a file locked by the OS'; --RAISE_APPLICATION_ERROR(-20103,'Invalid Operation -- May signal a file locked by the OS');   
   WHEN UTL_FILE.READ_ERROR THEN return_value := 'Read Error'; --RAISE_APPLICATION_ERROR(-20104,'Read Error');   
   WHEN UTL_FILE.WRITE_ERROR THEN return_value := 'Write Error'; --RAISE_APPLICATION_ERROR(-20105,'Write Error');   
   WHEN UTL_FILE.INTERNAL_ERROR THEN return_value := 'Internal Error'; --RAISE_APPLICATION_ERROR(-20106,'Internal Error');   
   WHEN NO_DATA_FOUND THEN return_value := 'No Data Found';  --RAISE_APPLICATION_ERROR(-20107,'No Data Found');   
   WHEN VALUE_ERROR THEN return_value := 'Value Error'; -- RAISE_APPLICATION_ERROR(-20108,'Value Error'); 
   WHEN OTHERS THEN return_value := 'Unknown Error'; --RAISE_APPLICATION_ERROR(-20109,'Unknown Error');  
      /*DECLARE
        Error_Code Number := Sqlcode;
      --BEGIN
        --dbms_output.put_line(' Error_Code '||Error_Code);
        --RAISE_APPLICATION_ERROR(-20109,'Unknown  Error');  
      END;*/
  /*    
   dbms_output.put_line(' Error_Code '||return_value);
   utl_file.put_line(f_out, return_value);
   utl_file.FFLUSH(f_out);
   utl_file.fclose(f_out);
  */
   RETURN return_value;
END;