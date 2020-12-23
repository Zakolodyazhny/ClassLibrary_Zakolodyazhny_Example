create or replace 
PROCEDURE MERGE_STATION(node_name IN VARCHAR2) AS 
BEGIN
  --EXECUTE immediate('insert into station (select * from station@' || node_name || ' where rid_station not in (select rid_station from station))');
  Execute Immediate('Merge Into station C Using (Select * From station@' || Node_Name ||') R ON (C.rid_station = R.rid_station) 
    When Matched Then UPDATE SET C.STATION_NAME = R.STATION_NAME       
    When Not Matched Then Insert (RID_STATION, RID_STATION_UP, STATION_NAME, STATION_LOCATION, REC_INFO, REC_ACTIVE) Values (R.RID_STATION, R.RID_STATION_UP, R.STATION_NAME, R.STATION_LOCATION, R.REC_INFO, R.REC_ACTIVE)');
  COMMIT;
 
  Execute Immediate('Merge Into controller C Using (Select * From controller@' || Node_Name ||') R ON (C.rid_controller = R.rid_controller) 
    When Matched Then UPDATE SET C.RID_STATION = R.RID_STATION, C.HOST_NAME = R.HOST_NAME, C.HOST_IP = R.HOST_IP, C.CONTROLLER_DATA = R.CONTROLLER_DATA, C.LAST_CHANGE = R.LAST_CHANGE      
    When Not Matched Then Insert (RID_CONTROLLER, RID_STATION, HOST_NAME, HOST_IP, CONTROLLER_NAME, REC_INFO, REC_ACTIVE, CONTROLLER_DATA, LAST_CHANGE)    
    Values (R.RID_CONTROLLER, R.RID_STATION, R.HOST_NAME, R.HOST_IP, R.CONTROLLER_NAME, R.REC_INFO, R.REC_ACTIVE, R.CONTROLLER_DATA, R.LAST_CHANGE)');
  COMMIT;
  
  Execute Immediate('Merge Into Connection C Using (Select * From Connection@' || Node_Name ||') R ON (C.rid_connection = R.rid_connection) 
    When Matched Then UPDATE SET C.Connection_Name = R.Connection_Name       
    When Not Matched Then Insert (Rid_Connection, Rid_Controller, Connection_Name, Rec_Info, Rec_Active) Values (R.Rid_Connection, R.Rid_Controller, R.Connection_Name, R.Rec_Info, R.Rec_Active)');
  COMMIT;
  
  Execute Immediate('Merge Into Device D Using (Select * From Device@' || Node_Name ||') R ON (D.rid_device = R.rid_device) 
    When Matched Then UPDATE SET D.RID_CONNECTION = R.RID_CONNECTION, D.DEVICE_NAME = R.DEVICE_NAME      
    When Not Matched Then Insert (RID_DEVICE, RID_DEVICE_TYPE, RID_CONNECTION, DEVICE_NAME, REC_INFO, REC_ACTIVE) Values (R.RID_DEVICE, R.RID_DEVICE_TYPE, R.RID_CONNECTION, R.DEVICE_NAME, R.REC_INFO, R.REC_ACTIVE)');
  COMMIT;
  
EXCEPTION
WHEN OTHERS THEN
  Raise_Application_Error(-20003, ' MERGE_STATION Error: '||Sqlerrm||' ');
END MERGE_STATION;