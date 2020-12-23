create or replace 
PROCEDURE TEST_JOB 
(
  JOBNAME IN VARCHAR2
) 
AS 
   time_limit INTERVAL DAY(3) TO SECOND(2) := '+000 00:10:00';
   time_filter INTERVAL DAY(3) TO SECOND(2) := '+000 00:01:00';
   elapsedtime INTERVAL DAY(3) TO SECOND(2);
   job_count VARCHAR2(30);
      
BEGIN
  dbms_output.put_line(' ');
  dbms_output.put_line('TEST_JOB '||JOBNAME||' Begin ');
    
  execute immediate('select count (job_name) into :job_count from DBA_SCHEDULER_RUNNING_JOBS where job_name like :1 and elapsed_time > :2') INTO job_count USING JOBNAME, time_filter;  
  dbms_output.put_line('TEST_JOB '||JOBNAME||' running more than '||time_filter||' count: '||job_count);
  
  IF job_count > 0
  THEN
    execute immediate('select elapsed_time into :elapsedtime from DBA_SCHEDULER_RUNNING_JOBS where job_name like :1') INTO elapsedtime USING JOBNAME;
    if elapsedtime > time_limit
      then 
        dbms_output.put_line('TEST_JOB '||JOBNAME||' elapsed_time '||elapsedtime||' > : '||time_limit);
        begin
          DBMS_SCHEDULER.STOP_JOB(job_name=>'"ADMIN"."'||JOBNAME||'"', force => TRUE);
          dbms_output.put_line('TEST_JOB '||JOBNAME||' stop job '||JOBNAME);
        end;
        else
        dbms_output.put_line('TEST_JOB '||JOBNAME||' elapsed_time '||elapsedtime||' < time_limit : '||time_limit);
    end if;
  ELSE 
  dbms_output.put_line('TEST_JOB '||JOBNAME||' Not running.');
  END IF;
   
  dbms_output.put_line('TEST_JOB '||JOBNAME||' End. ');
  dbms_output.put_line(' ');
  
  EXCEPTION
  When Others Then
      DECLARE
         Error_Code Number := Sqlcode;
      BEGIN
        dbms_output.put_line(' TEST_JOB '||JOBNAME||' error: '||Error_Code||' ');
        Raise_Application_Error(-20003, ' TEST_JOB '||JOBNAME||' error: '||Error_Code||' ');
      END;
  
END TEST_JOB;