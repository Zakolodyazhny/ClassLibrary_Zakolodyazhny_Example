using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Argon.Agr.ComUsdm01.Protocol;
using Argon.StateMachine;
using Argon.Api;
using Argon.Base;
using Argon.IEC104.Interface;
using Argon.IEC104;
using Argon.Eval;
using Argon.Iavr;

//This is a part of a large solution (>200 projects) 
//This class is partially developed by Zakolodyazhny Volodymyr
//My task was to add a new funtionality to existing projects (like this Usdm01sSB2)
//New functionality, added by me, is partially here, but mostly - in StateOneIavrPart.cs
//Usdm01sSB2 device is a thread who talks to hardware and writes data to DB.
//Usdm01sSB2 device is one thread of many in our Argon Windows Service Application.

namespace Argon.Agr.Usdm01sSB2
{
    public partial class StateOne : Usdm01WorkerState 
    {
        Packet sendCmdOn;
        Packet sendCmdOff;


        public StateOne(Usdm01WorkerState next)
            : base(next) {
        }

        UInt16 uiOld = AgrUsdm01sSB2.UnknState;
        string objectName = String.Empty;
        

        private void ExecuteCommand(Device device, int cmd) {
            Trace.WriteLine(objectName + " ExecuteCommand. CMD: " + cmd);
            SetCommands();
            switch((Commands)cmd) {
                case Commands.SwitchOn:
                    ExecuteSwitchOn(device);
                    break;
                case Commands.SwitchOff:
                    ExecuteSwitchOff(device);
                    break;
                default:
                    this.Worker.WriteMessage(Messages.CmdErr, "Невідома команда №" + cmd);
                    Thread.Sleep(1000);
                    break;
            }
        }

        private ushort ReadState(Device device) {
            UInt16 uiValue = AgrUsdm01sSB2.UnknState;
            try {
                if(device.Write(device.ReadDiscreteInputPaket)) {
                    Thread.Sleep(50);
                    Packet p = device.Read();
                    if(p == null) {
                        return AgrUsdm01sSB2.UnknState;
                    } else if(!Packet.TestPackets(device.ReadDiscreteInputPaket, p)) {
                        return AgrUsdm01sSB2.UnknState;
                    } else {
                        uiValue = p.GetWord(0);
                        uiValue = this.InternalWorker.Convert(uiValue);
                    }
                }
            } catch {
                return AgrUsdm01sSB2.UnknState;
            }
            return uiValue;
        }


        static ushort EnableSwitchOn {
            get {
                return (ushort)SwitchPos.Off
                     | (ushort)StateBits.Trolley
                     | (ushort)StateBits.ControlKey;
            }
        }

        static ushort EnableSwitchOn_Switching {
            get {
                return (ushort)SwitchPos.Switching
                     | (ushort)StateBits.Trolley
                     | (ushort)StateBits.ControlKey;
            }
        }

        private bool CanSwitchOn(UInt16 ui) {
            return (ui == StateOne.EnableSwitchOn ||
                    ui == StateOne.EnableSwitchOn_Switching) &&
                    CheckBlocking() && !blockIavr;
        }

        private void ExecuteSwitchOn(Device device) {
            UInt16 ui = ReadState(device);
            if(ui == AgrUsdm01sSB2.UnknState) {
                throw new Exception("Відсутній зв'язок");  
            }
            Worker cmdWorker = this.InternalWorker as Worker;

            //new 2019
            AgrUsdm01sSB2 ao = this.Worker.ActiveObject as AgrUsdm01sSB2;
            if (ao == null) throw new Exception("помилка в AgrUsdm01sSB2!");

            if(cmdWorker.PrevState != -1 && ui != cmdWorker.PrevState) {
                throw new Exception("Стан змінився");
            }

            cmdWorker.WriteMessage((ushort)(~(int)ui), ui);

            if (!CanSwitchOn (ui)) {

                int iVal = ui & (ushort)StateBits.Switch;
                string str = String.Empty;
                if (iVal != (int)SwitchPos.Off) {
                    switch ((SwitchPos)iVal) {
                        case SwitchPos.On: str = " Вже увімкнено"; break;
                        case SwitchPos.Switching: str = " Йде перемикання"; break;
                        case SwitchPos.Unknown: str = " Невідомий стан"; break;
                        case SwitchPos.Off: str = " Інші причини"; break;
                    }
                    cmdWorker.WriteMessage (Messages.CmdErr, Messages.CommandNumToText (cmdWorker.NeedCommand) + " : " + str);
                }

                //new 2019
                //AgrUsdm01sSB2 ao = this.Worker.ActiveObject as AgrUsdm01sSB2;

                //if (ao == null) throw new Exception("помилка в AgrUsdm01sSB2!");
                
                Trace.WriteLine(objectName + " IgnoreTrolleyState: " + ao.IgnoreTrolleyState);

                iVal = ui & (ushort)StateBits.Trolley;
                if ((iVal == 0) && !ao.IgnoreTrolleyState) //new
                {
                    cmdWorker.WriteMessage (Messages.CmdErr,
                        Messages.CommandNumToText (cmdWorker.NeedCommand) +
                        "Возик - викочено.");
                }

                iVal = ui & (ushort)StateBits.ControlKey;
                if (iVal == 0) {
                    cmdWorker.WriteMessage (Messages.CmdErr,
                        Messages.CommandNumToText (cmdWorker.NeedCommand) +
                        "Ключ керування в положенні - місцевий.");
                }
                iVal = ui & (ushort)StateBits.Fault;
                if (iVal != 0) {
                    cmdWorker.WriteMessage (Messages.CmdErr,
                        Messages.CommandNumToText (cmdWorker.NeedCommand) +
                        "Пристрій захисту/керування - несправний.");
                }

                Trace.WriteLine(objectName + " ExecuteSwitchOn: Не всі умови керування виконані");
                throw new Exception ("Не всі умови керування виконані");
            }

            // Надіслати команду, та переконатися, що надійшла
            if (ao.ExternalExecute)
            {
                SendExternalExecute(cmdWorker, (byte)Commands.SwitchOn);
                Thread.Sleep(1);
            }
            else
            {
                if (this.sendCmdOn == null) throw new Exception("Не призначено команду");
                device.Write(this.sendCmdOn);
                Thread.Sleep(50);
                Packet p = device.Read();
                if (p == null || !Packet.TestPackets(this.sendCmdOn, p))
                {
                    throw new Exception("Не було підтвердження команди по Modbus");
                }
                cmdWorker.WriteMessage(Messages.CmdOk,
                    Messages.CommandNumToText((int)Commands.SwitchOn) + " Користувач: " + cmdWorker.CurrentUser);
            }
            WaitCmdExec(device, cmdWorker, (int)Commands.SwitchOn, (int)SwitchPos.On);
            if (ao.ExternalExecute)
            {
                SendExternalExecute(cmdWorker, 0);
            }
        }

        void WaitCmdExec(Device device, Worker cmdWorker, int cmdNumber, int cmdResult) {
            // Очікування підтвердження
            Thread.Sleep(10);
            int iCount = 0; //UInt16 uii = 0;
            Trace.WriteLine(objectName + " WaitCmdExec Begin cmdNumber: " + cmdNumber + ", cmdResult: " + cmdResult);
            Trace.WriteLine(objectName + " WaitCmdExec CmdTimeLimit: " + this.InternalWorker.CmdTimeLimit);

            DateTime dtStart = DateTime.Now;
            UInt16 ui = 0;
            //for (UInt16 ui = ReadState(device); (ui & (ushort)StateBits.Switch) != cmdResult; iCount++) {
            for (ui = ReadState(device); GetSwitchPos(ui) != cmdResult; iCount++)
            {
                if ((iCount > 100) || (dtStart + this.InternalWorker.CmdTimeLimit < DateTime.Now))
                {
                    Trace.WriteLine(objectName + " WaitCmdExec Command not executed.");
                    cmdWorker.WriteMessage(Messages.CmdLim,
                    Messages.CommandNumToText(cmdNumber) + " Користувач: " + cmdWorker.CurrentUser);
                    if (GetSwitchPos(ui) == (UInt16)SwitchPos.Off)    BlockIavr = true; //?
                    break;
                }
                else
                {
                    Trace.WriteLine(objectName + " WaitCmdExec Command in 'for' cycle . Current SwitchPos(): " + GetSwitchPos(ui));
                }
                Thread.Sleep(this.InternalWorker.IdleSleepInterval.Add(TimeSpan.FromMilliseconds(50)));
                ui = ReadState(device);
            }
            if (GetSwitchPos(ui) == cmdResult)
            {
                Trace.WriteLine(objectName + " WaitCmdExec Command Executed ok. Current SwitchPos(): " + GetSwitchPos(ui));
                cmdWorker.WriteMessage(Messages.CmdExecuted,
                    Messages.CommandNumToText(cmdNumber) + " Користувач: " + cmdWorker.CurrentUser);
                
                //cmdWorker.Send104Message(Messages.NeedCommand, DateTime.Now, 0); //Зкидання останньої команди
            }
            else
            {
                Trace.WriteLine(objectName + " WaitCmdExec Command Not Executed. Current SwitchPos(): " + GetSwitchPos(ui));
                cmdWorker.WriteMessage(Messages.CmdNotExecuted,
                    Messages.CommandNumToText(cmdNumber) + " Користувач: " + cmdWorker.CurrentUser);
                
            }
            cmdWorker.ClearCommand();
            Trace.WriteLine(objectName + " WaitCmdExec End.");
        }

        private void ExecuteSwitchOff(Device device) {
             
            UInt16 ui = ReadState(device);
            if(ui == AgrUsdm01sSB2.UnknState) {
                ui = ReadState(device);
                throw new Exception("Відсутній зв'язок");
            }
            Worker cmdWorker = this.InternalWorker as Worker;

            //new 2019
            AgrUsdm01sSB2 ao = this.Worker.ActiveObject as AgrUsdm01sSB2;
            if (ao == null) throw new Exception("помилка в AgrUsdm01sSB2!");

            if(cmdWorker.PrevState != -1 && ui != cmdWorker.PrevState) {
                throw new Exception("Стан змінився");
            }

            cmdWorker.WriteMessage((ushort)(~(int)ui), ui);

            // Надіслати команду, та переконатися, що надійшла
            if (ao.ExternalExecute)
            {
                SendExternalExecute(cmdWorker, (byte)Commands.SwitchOff);
                Thread.Sleep(1);
            }
            else
            {
                if (this.sendCmdOn == null) throw new Exception("Не призначено команду");
                device.Write(this.sendCmdOff);
                Thread.Sleep(50);
                Packet p = device.Read();
                if (p == null || !Packet.TestPackets(this.sendCmdOff, p))
                {
                    throw new Exception("Не було підтвердження команди по Modbus");
                }

                cmdWorker.WriteMessage(Messages.CmdOk,
                    Messages.CommandNumToText((int)Commands.SwitchOff) + " Користувач: " +
                    cmdWorker.CurrentUser
                    );
            }
            WaitCmdExec(device, cmdWorker, (int)Commands.SwitchOff, (int)SwitchPos.Off);
            if (ao.ExternalExecute)
            {
                SendExternalExecute(cmdWorker, 0);
            }
        }

        private void SetCommands() {
            if(sendCmdOn == null || sendCmdOff == null) {
                try {
                    ISlaveObject so = this.InternalWorker.ActiveObject as ISlaveObject;
                    if(so != null) {
                        if(so.BitCmd1 > -1 && so.BitCmd1 < 8) {
                            //{ 0x01, 0x05, 0x03, 0x01, 0xFF, 0x00 }
                            sendCmdOn = new Packet(this.InternalWorker.Address, 5, (ushort)(0x300 + so.BitCmd1), 0xFF00);
                        }
                        if(so.BitCmd2 > -1 && so.BitCmd2 < 8) {
                            //{ 0x01, 0x05, 0x03, 0x01, 0xFF, 0x00 }
                            sendCmdOff = new Packet(this.InternalWorker.Address, 5, (ushort)(0x300 + so.BitCmd2), 0xFF00);
                        }
                    }
                } catch {
                }
            }
        }

        public override State DoStateWork(object stateData) {           
            // Обробка команди
            Worker cmdWorker = this.InternalWorker as Worker;
            if(cmdWorker != null && cmdWorker.NeedCommand != 0) {
                try {
                    Trace.WriteLine(objectName + " DoStateWork ExecuteCommand OOO. cmd: " + cmdWorker.NeedCommand);
                    ExecuteCommand((Device)stateData, cmdWorker.NeedCommand);
                } catch(Exception ex) {
                    cmdWorker.WriteMessage(Messages.CmdErr, Messages.CommandNumToText(cmdWorker.NeedCommand) + " : " + ex.Message);
                    Trace.WriteLine(objectName + " DoStateWork Command Error " + ex.Message);
                } finally {
                    cmdWorker.ClearCommand();
                }
                return base.GetNext(); //uncomment, 2019
            }
            // Перевірка значень
            UInt16 uiNew = Convert.ToUInt16(this.InternalWorker.ActiveObject.Measurements["StateValue"].Value);
            try {
                SourceObject so = this.InternalWorker.SourceObjects.CurrentObject as SourceObject;
                foreach(ActiveObject ao in so.ActiveObjects) {
                    if(ao.Worker is Usdm01Worker) {
                        Usdm01Worker usdmWorker = ao.Worker as Usdm01Worker;
                        if(usdmWorker.Master
                            && usdmWorker.Address == this.InternalWorker.Address
                            && (usdmWorker.DeviceState == DeviceState.Stopped || usdmWorker.DeviceState == DeviceState.Unknown)) {

                            if(uiNew != AgrUsdm01sSB2.UnknState && this.InternalWorker.ActiveObject is ISlaveObject) {
                                ((ISlaveObject)(this.InternalWorker.ActiveObject)).SetUnknStateValue();
                                uiNew = AgrUsdm01sSB2.UnknState;
                                break;
                            }
                        }
                    }
                }
                objectName = this.Worker.ActiveObject.Name;
            } catch(Exception ex) {
                Trace.WriteLine(objectName + " DoStateWork Exception: ", ex.Message);
            }
            Trace.WriteLine(objectName + " DoStateWork value " + uiNew);
            
            //2018 new test iAVR 
            //2019 copy to all others

            if (uiNew != uiOld)
            {
                if (cmdWorker != null) cmdWorker.WriteMessage(uiOld, uiNew);
                uiOld = uiNew;

                //new 2019
                dtChangePosCheckTime = DateTime.Now;
                Trace.WriteLine(objectName + " DoStateWork value changed: " + uiNew + ". New switch pos: " + GetSwitchPos(uiNew));
                if (SwitchIsOn(uiNew) && BlockIavr) dtIavrUnblockTime = DateTime.Now;
            }
            if (!BlockIavr)
            {
                dtIavrUnblockTime = DateTime.Now;
            }

            CheckProtectionSignals(cmdWorker, uiNew);

            if (uiNew <= 32)
            {
                DoIavrWork(stateData, cmdWorker, uiNew);
            }

            return base.GetNext();
        }

        void SendExternalExecute(Worker cmdWorker, byte bneedCommand)
        {
            Trace.WriteLine(objectName + " DoStateWork SendExternalExecute NEED_COMMAND value: " + bneedCommand);
            //UInt16 uiNew1 = Convert.ToUInt16(this.InternalWorker.ActiveObject.Measurements["StateValue"].Value);
            cmdWorker.Send104Message(Messages.NeedCommand, DateTime.Now, bneedCommand);
        }

        private bool CheckBlocking()
        {
            Worker cmdWorker = this.InternalWorker as Worker;
            Trace.WriteLine(objectName + " CheckBlocking. Worker " + this.Worker.ActiveObject.Name);
            try
            {
                AgrUsdm01sSB2 ao = this.Worker.ActiveObject as AgrUsdm01sSB2;
                if (ao == null) throw new Exception("помилка в AgrUsdm01sSB2");
                foreach (IEC104Variable var in ao.IEC104Variables)
                {
                    if (!var.Get104Value(ao.Processor104))
                    {
                        throw new Exception(objectName + "Неможливо отримати змінну по протоколу IEC104:" + var.Name);
                    }
                    else
                    {
                        Trace.WriteLine(objectName + " CheckBlocking. IEC104Variable " + var.Name + ", Value: " + var.Value);
                    }
                }
                if (ao.Blockings == null) throw new Exception(objectName + "Не задані блокування");
                bool boFlag = false;
                foreach (Blocking blk in ao.Blockings)
                {
                    if (String.IsNullOrEmpty(blk.Expression)) continue;
                    try
                    {
                        //if (JScript.Eval(blk, ao.IEC104Variables)) {//throw new Exception(blk.Message);
                        if (Eval(blk, ao.IEC104Variables))
                        {
                            cmdWorker.WriteMessage(Messages.CmdErr, Messages.CommandNumToText(cmdWorker.NeedCommand) + " : " + blk.Message);
                            boFlag = true;
                            InfoMessage = blk.Message;
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine(objectName + " CheckBlocking. Exception1 " + e.Message);
                        cmdWorker.WriteMessage(Messages.CmdErr, "Помилка у виразі блокування" + " : " + blk.Name + ". " + e.Message);
                        return false;
                    }
                }
                if (boFlag) return false; //там, де воно використовується - це норм (інверсія)
                return true;
            }
            catch (Exception e)
            {
                Trace.WriteLine(objectName + " CheckBlocking. Exception2 " + e.Message);
                cmdWorker.WriteMessage(Messages.CmdErr, Messages.CommandNumToText(cmdWorker.NeedCommand) + " : " + e.Message);
                return false;
            }
        }
    }
}
