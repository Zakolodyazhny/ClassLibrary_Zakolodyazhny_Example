create or replace 
PROCEDURE TEST_ALL_DTS_JOBS AS 
BEGIN
  dbms_output.put_line('TEST_ALL_DTS_JOBS Begin ');
    
  TEST_JOB('dts_arblcer_job');
  TEST_JOB('dts_arbrov_job');
  TEST_JOB('dts_archerk_job');
  TEST_JOB('dts_archern_job');
  TEST_JOB('dts_arjovtne_job');
  TEST_JOB('dts_arles_job');
  TEST_JOB('dts_arnegin_job');
  TEST_JOB('dts_arnkiev_job');
  TEST_JOB('dts_arpivn_job');
  TEST_JOB('dts_arpol_job');
  TEST_JOB('dts_arslav_job');
  TEST_JOB('dts_arzhit_job');
  
  dbms_output.put_line('TEST_ALL_DTS_JOBS End. ');
END TEST_ALL_DTS_JOBS;