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
using Jurassic;

//This is a part of a large solution (>200 projects) 
//developed by Zakolodyazhny Volodymyr
//This partial class expands functionality of a Usdm01sSB2 device. It extents StateOne.cs


namespace Argon.Agr.Usdm01sSB2
{
    public partial class StateOne
    {
        //2019
        string strIAvrUserName = IavrLogin.iAvrUserName;
        DateTime dtBlockIavrTime = DateTime.Now;       //час початку блокування іАВР від повторної роботи
        TimeSpan dtBlockIavrTimeInterval = TimeSpan.FromSeconds(2);         //витримка блокування іАВР від повторної роботи
        DateTime dtChangePosCheckTime = DateTime.Now;       //час початку блокування іАВР при зміні положення вимикача
        TimeSpan dtChangePosTimeInterval = TimeSpan.FromSeconds(2); //витримка блокування іАВР від повторної роботи
        DateTime dtIavrRememberBlockedTime = DateTime.Now;          //час початку паузи про нагадування, що іАВР по цьому вимикачу - блоковане
        TimeSpan dtIavrRememberBlockedTimeInterval = TimeSpan.FromSeconds(10); //.FromMinutes(60); //change to 10-60 //інтервал нагадування, що іАВР по цьому вимикачу - блоковане
        DateTime dtIavrUnblockTime = DateTime.Now;          //час початку блокування АВР по захисту
        TimeSpan dtIavrUnblockTimeInterval = TimeSpan.FromSeconds(5); //change to 10-60 min //витримка часу, коли зкидати блокування
        System.Threading.Timer UnblockTimer; 
        //int unBlockInterval = 5000; //(int)dtIavrUnblockTimeInterval.TotalMilliseconds;
        bool blockIavr = false;
        bool BlockIavr
        {
            get
            {
                return blockIavr;
            }
            set
            {
                //Trace.WriteLine(objectName + " blockIavr = " + blockIavr.ToString());
                if (blockIavr != value)
                {
                    Trace.WriteLine(objectName + " BlockIavr value != blockIavr ");
                    blockIavr = value;
                    OnBlockIavrChanged(blockIavr);
                }
                if (blockIavr == true)
                {
                    IavrReady = false;
                }
            }
        }

        bool iavrReady = false;
        bool IavrReady
        {
            get { return iavrReady; }
            set
            {
                if (iavrReady != value)
                {
                    Trace.WriteLine(objectName + String.Format("New iavrReady value != iavrReady({0}) ", iavrReady));
                    //if (!BlockIavr && !iavrReady)
                    {
                        iavrReady = value;
                        OnIavrReadyChanged(iavrReady);
                    }
                }
            }
        }

        void OnBlockIavrChanged(bool blockValue)
        {
            Trace.WriteLine(objectName + " OnBlockIavrChanged. blockValue = " + blockValue.ToString());
            Worker cmdWorker = this.InternalWorker as Worker;
            switch (blockValue)
            {
                case true:
                    {
                        cmdWorker.WriteMessage(Messages.StartIavrBlocked, "On");
                        ptRememberUserIavrBlockedTimer = new PeriodicTimer(dtIavrRememberBlockedTimeInterval, false);
                        break;
                    }
                case false:
                    {
                        cmdWorker.WriteMessage(Messages.StartIavrBlocked, "Off");
                        cmdWorker.WriteMessage(Messages.StartIavrUnBlocked, "On");

                        ptRememberUserIavrBlockedTimer = null;
                        break;
                    }
            };
        }

        void OnIavrReadyChanged(bool readyValue)
        {
            Trace.WriteLine(objectName + " OnIavrReadyChanged readyValue = " + readyValue.ToString());
            Worker cmdWorker = this.InternalWorker as Worker;
            switch (readyValue)
            {
                case true:
                    {
                        cmdWorker.WriteMessage(Messages.StartIavrReady, "On");
                        break;
                    }
                case false:
                    {
                        cmdWorker.WriteMessage(Messages.StartIavrReady, "Off");
                        break;
                    }
            };
        }

        void OnInfoMessageChanged(string value)
        {
            Trace.WriteLine(objectName + " OnInfoMessageChanged value = " + value);
            Worker cmdWorker = this.InternalWorker as Worker;
            cmdWorker.WriteMessage(Messages.InfoMessage, value);
        }

        PeriodicTimer ptRememberUserIavrBlockedTimer;

        string infoMessage = "";
        string InfoMessage
        {
            get
            {
                return infoMessage;
            }
            set
            {
                if (infoMessage != value)
                {
                    infoMessage = value;
                    Trace.WriteLine(objectName + " infoMessage value changed. infoMessage = " + infoMessage);
                    if (infoMessage != "") OnInfoMessageChanged(infoMessage);
                }
            }
        }

        private void DoIavrWork(object stateData, Worker cmdWorker, UInt16 uiNew)
        {
            //UnBlockTimerWork(uiNew);

            if (IavrVariables.Iavr_key_state == 2)
            {
                Trace.WriteLine(objectName + ". DoIavrWork Iavr_key_state: " + IavrVariables.Iavr_key_state + ", so do next check.");
                if (DateTime.Now - dtChangePosTimeInterval > dtChangePosCheckTime) //не пускати іАВР одразу після зміни положення
                {
                    Trace.WriteLine(objectName + ". DoIavrWork Switch pos changed more than Interval ago. Do next check.");
                    if ((GetSwitchPos(uiNew) == (ushort)SwitchPos.Off) || (GetSwitchPos(uiNew) == (ushort)SwitchPos.On))
                    {
                        if (((DateTime.Now - dtBlockIavrTime) > dtBlockIavrTimeInterval) & !BlockIavr)
                        {
                            Trace.WriteLine(objectName + ". DoIavrWork Set IavrReady = true");
                            IavrReady = true;
                        }
                    }
                    CheckStartingAndGo(uiNew, cmdWorker, (Device)stateData);
                    Trace.WriteLine(objectName + ". DoIavrWork CheckIavrBlockedTimer. No CheckStarting");
                    //CheckIavrBlockedTimer(uiNew, cmdWorker, (Device)stateData);
                    CheckIavrBlockedRememberTimer(cmdWorker);
                }
                else  //не виконувати перевірки
                {
                    Trace.WriteLine(objectName + ". DoIavrWork Switch pos changed time Interval hasn't gone. No CheckStarting");
                    IavrReady = false;
                }
            }
            else
            {
                Trace.WriteLine(objectName + ". DoIavrWork Iavr_key_state: " + IavrVariables.Iavr_key_state + ", so nothing to do.");
                IavrReady = false;
            }
        }

        
        ////2018 new test AVR 
        private void CheckStartingAndGo(UInt16 ui, Worker cmdWorker, Device device)
        {
            Trace.WriteLine(objectName + " CheckStartingAndGo");

            //if (BlockIavr == false)
            {
                bool checkStartingsRes = false;
                checkStartingsRes = CheckStartings();
                Trace.WriteLine(objectName + " CheckStartingAndGo. CheckStarting()= " + checkStartingsRes.ToString());
                if ((checkStartingsRes == true))//&&iavrReady)
                {
                    Trace.WriteLine(objectName + " CheckStartingAndGo. Check dtBlockIavrTimeInterval");
                    if ((DateTime.Now - dtBlockIavrTime) > dtBlockIavrTimeInterval) //блокування повторної роботи
                    {
                        //OnInfoMessageChanged(infoMessage);
                        Trace.WriteLine(objectName + " CheckStartingAndGo. dtBlockIavrTimeInterval has gone");
                        SendSwitchOnCommand(ui, cmdWorker, device, checkStartingsRes);
                    }
                    else
                    {
                        Trace.WriteLine(objectName + " CheckStartingAndGo. dtBlockIavrTimeInterval hasn't gone.");
                        IavrReady = false;
                    }
                    dtBlockIavrTime = DateTime.Now;
                }
                else
                {
                    //jseStrt = new ScriptEngine(); //free memory ?
                    Trace.WriteLine(objectName + " CheckStartingAndGo. CheckStarting= false");
                }
            }
            //else
            {
                //Trace.WriteLine(objectName + " CheckStartingAndGo. BlockIavr= true, so NO Checkstartings");
            }
        }


        private void CheckIavrBlockedRememberTimer(Worker cmdWorker)
        {
            if (ptRememberUserIavrBlockedTimer != null) //(!blockIavr)
            {
                if (ptRememberUserIavrBlockedTimer.TimerTriggered) //нагадати користувачеві
                {
                    cmdWorker.WriteMessage(Messages.StartIavrBlocked, "On (нагадування)"); //нагадування
                    ptRememberUserIavrBlockedTimer.TimerTriggered = false;
                }
            }
            else
            {
                ptRememberUserIavrBlockedTimer = null; //unrichable code?
            }
        }

        private void doUnblock(System.Object state)
        {
            Trace.WriteLine(objectName + ". doUnblock");
            Trace.WriteLine(objectName + ". doUnblock Object State " + (UInt16)state);
            BlockIavr = false;
        }


        //CheckMetods

        #region CheckMethods

        void CheckProtectionSignals(Worker cmdWorker, UInt16 ui)
        {
            Trace.WriteLine(objectName + String.Format(" CheckProtectionSignals. "));
            bool checkBlockingsWithTriggerRes = CheckBlockingWithTrigger();
            Trace.WriteLine(objectName + String.Format(" CheckProtectionSignals. checkBlockingsWithTriggerRes({0}), blockIavr({1}).", checkBlockingsWithTriggerRes, blockIavr));
            if ((checkBlockingsWithTriggerRes == true) && (blockIavr == false)) // || (blockIavr == true))
            {
                Trace.WriteLine(objectName + " CheckProtectionSignals. Protection Work.");
                cmdWorker.WriteMessage(Messages.StartIavrBlockedByProtection, "On");
                BlockIavr = true;
            }
            else
            {
                if ((checkBlockingsWithTriggerRes == false) && (blockIavr == true))
                {
                    //Trace.WriteLine(objectName + String.Format(" CheckProtectionSignals. (checkBlockingsWithTriggerRes({0})", checkBlockingsWithTriggerRes));
                    //Trace.WriteLine(objectName + String.Format(" CheckProtectionSignals. (blockIavr({0})", blockIavr));
                    if (GetSwitchPos(ui) == (ushort)SwitchPos.On)
                    {
                        Trace.WriteLine(objectName + String.Format(" GetSwitchPos = On "));
                    }
                    else
                    {
                        Trace.WriteLine(objectName + String.Format(" GetSwitchPos = Off "));
                    }

                    if ((DateTime.Now - dtIavrUnblockTime) > dtIavrUnblockTimeInterval)
                    {
                        if (SwitchIsOn(ui)) doUnblock(ui);
                    }
                }
            }
        }


        private bool CheckBlockingWithTrigger()
        {
#warning Можливо, замінити на простий перебір змінних (==1), без обчислення виразу
            bool res = false;
            Worker cmdWorker = this.InternalWorker as Worker;
            Trace.WriteLine(objectName + " CheckBlockingWithTrigger.");
            try
            {
                AgrUsdm01sSB2 ao = this.Worker.ActiveObject as AgrUsdm01sSB2;
                if (ao == null) throw new Exception("помилка в AgrUsdm01sSB2!");
                foreach (IEC104Variable var in ao.IEC104Variables)
                {
                    if (!var.Get104Value(ao.Processor104)) {
                        //Thread.Sleep(1000);
                        throw new Exception("Неможливо отримати змінну IEC104: " + var.Name); 
                    }
                    else
                    {
                        Trace.WriteLine(objectName + " CheckBlockingWithTrigger. IEC104Variable " + var.Name + ", Value: " + var.Value);
                    }
                }
                if (ao.BlockingsWithTrigger == null) throw new Exception("Не задані блокування з тригером!");

                foreach (BlockingWithTrigger blcTr in ao.BlockingsWithTrigger)
                {
                    if (String.IsNullOrEmpty(blcTr.Expression)) continue;
                    try
                    {
                        bool evalres = Eval(blcTr, ao.IEC104Variables);
                        res = res || evalres;
                        Trace.WriteLine(objectName + " CheckBlockingWithTrigger. Blocking: " + blcTr.Name + ". Expression: " + blcTr.Expression + ". Eval= " + res);
                        if (evalres)
                        {
                            Trace.WriteLine(objectName + " CheckBlockingWithTrigger. Blocking.Message: " + blcTr.Message);
                            if (!BlockIavr) InfoMessage = blcTr.Message;
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine(objectName + " CheckBlockingWithTrigger. Помилка у виразі: " + blcTr.Name + ". " + e.Message);
                        cmdWorker.WriteMessage(Messages.InfoMessage, "Помилка у виразі: " + blcTr.Name + ". " + e.Message);
                        Thread.Sleep(1000);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(objectName + " CheckBlockingWithTrigger. Помилка: " + e.Message);
                cmdWorker.WriteMessage(Messages.InfoMessage, e.Message);
                Thread.Sleep(1000);
                return false;
            }
            Trace.WriteLine(objectName + " CheckBlockingWithTrigger. Overall result: " + res.ToString());
            return res;
        }

        //new 2018 test
        private bool CheckStartings()
        {
            bool res = false;
            Worker cmdWorker = this.InternalWorker as Worker;
            Trace.WriteLine(objectName + " CheckStarting");
            try
            {
                AgrUsdm01sSB2 ao = this.Worker.ActiveObject as AgrUsdm01sSB2;
                if (ao == null) throw new Exception("помилка в AgrUsdm01sSB2!");
                foreach (IEC104Variable var in ao.IEC104Variables)
                {
                    if (!var.Get104Value(ao.Processor104))
                    {
                        throw new Exception("Неможливо отримати змінну IEC104: " + var.Name);
                    }
                    else
                    {
                        Trace.WriteLine(objectName + " CheckStarting. IEC104Variable " + var.Name + ", Value: " + var.Value);
                    }
                }
                if (ao.Startings == null) throw new Exception("Не задані пуски!");

                foreach (Starting strt in ao.Startings)
                {
                    if (String.IsNullOrEmpty(strt.Expression)) continue;
                    try
                    {
                        Trace.WriteLine(objectName + " CheckStarting. Starting: " + strt.Name + ". Expression: " + strt.Expression);
                        bool evalres = Eval(strt, ao.IEC104Variables);
                        res = res || evalres;
                        if (evalres)
                        {
                            Trace.WriteLine(objectName + " CheckStarting. Starting.Message " + strt.Message);
                            if (!BlockIavr) InfoMessage = strt.Message;
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine(objectName + " CheckStarting. Помилка у виразі пуску: " + strt.Name + ". " + e.Message);
                        cmdWorker.WriteMessage(Messages.CmdErr, "Помилка у виразі пуску: " + strt.Name + ". " + e.Message);
                        Thread.Sleep(1000);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(objectName + " CheckStarting. Помилка: " + e.Message);
                cmdWorker.WriteMessage(Messages.CmdErr, Messages.CommandNumToText(cmdWorker.NeedCommand) + " : " + e.Message);
                Thread.Sleep(1000);
                return false;
            }

            Trace.WriteLine(objectName + " CheckStarting. Overall result: " + res);
            return res;
        }



        #endregion //Check Methods

        UInt16 GetSwitchPos(UInt16 ui)
        {
            UInt16 res = (UInt16)(ui & (ushort)StateBits.Switch);
            Trace.WriteLine(objectName + " GetSwitchPos Switch pos: " + res.ToString());
            return res; //(UInt16)(ui & (ushort)StateBits.Switch);
        }

        bool SwitchIsOn(UInt16 ui)
        {
            bool res = (UInt16)(ui & (ushort)StateBits.Switch) == (UInt16)SwitchPos.On;
            Trace.WriteLine(objectName + " GetSwitchPos SwitchIsOn: " + res);
            return res; 
        }

        private void SendSwitchOnCommand(UInt16 ui, Worker cmdWorker, Device device, bool checkStartingsRes)
        {
            if (cmdWorker != null && cmdWorker.NeedCommand == 0) //no command now
            {
                Trace.WriteLine(objectName + " SendSwitchOnCommand. NeedCommand == 0.");
                try
                {
                    Trace.WriteLine(objectName + " SendSwitchOnCommand. Send message " + Messages.StartIavr + " true.");
                    cmdWorker.WriteMessage(Messages.StartIavr, checkStartingsRes.ToString());

                    Trace.WriteLine(objectName + " SendSwitchOnCommand. iAvrUserActivated(?): " + IavrLogin.iAvrUserActivated);
                    ActivateIavrUser(cmdWorker);

                    //Посилаємо команду
                    Trace.WriteLine(objectName + " SendSwitchOnCommand. SendCommand.");
                    int errcode = 0; string errmessage = "";
                    //new 2019
                    cmdWorker.SendIavrCommand(strIAvrUserName, (int)Commands.SwitchOn, ui, out errcode, out errmessage);
                    Trace.WriteLine(objectName + " SendSwitchOnCommand. SendCommand SwitchOn (" + (int)Commands.SwitchOn + ").");

                    //тут треба ExecuteCommand
                    Trace.WriteLine(objectName + " SendSwitchOnCommand. ExecuteCommand.");
                    ExecuteCommand(device, cmdWorker.NeedCommand);
                    //Trace.WriteLine("SB2 StartTest point4. ExecuteCommand (" + (int)Commands.SwitchOn + ").");
                }
                catch (Exception ex)
                {
                    cmdWorker.WriteMessage(Messages.CmdErr, Messages.CommandNumToText(cmdWorker.NeedCommand) + " : " + ex.Message);
                    Trace.WriteLine(objectName + " SendSwitchOnCommand Command Error. cmdWorker.NeedCommand: " + cmdWorker.NeedCommand);
                    Trace.WriteLine(objectName + " SendSwitchOnCommand Command Error: " + ex.Message);
                }
                finally
                {
                    cmdWorker.ClearCommand();
                }
            }
            else
            {
                Trace.WriteLine(objectName + " SendSwitchOnCommand. NeedCommand: " + cmdWorker.NeedCommand + " Not Executed?");
            }
        }

        private void ActivateIavrUser(Worker cmdWorker)
        {
            string login = IavrLogin.iAvrUserName;
            string pass = IavrLogin.iAvrUserPassword;
            int ErrCode; string ErrMsg;
            Trace.WriteLine(objectName + " ActivateIavrUser WriteMessage Try to Activate iAVR user.");
            bool bTestUser = cmdWorker.TestUser(login, pass);
            bool activateRes = cmdWorker.Activate(login, pass, out ErrCode, out ErrMsg);
            Trace.WriteLine(objectName + " ActivateIavrUser WriteMessage Activate " + activateRes.ToString());
        }


        //region ScriptEngine

        #region Script Engine Part
        //Script Engine Part
        ScriptEngine jseBwT = new ScriptEngine();
        ScriptEngine jseStrt = new ScriptEngine();
        ScriptEngine jseBlk = new ScriptEngine();

        bool Eval(BlockingWithTrigger blockingTr, IEC104VariableList list) 
        {
            bool result = false;
            //string expression = String.Copy(blockingTr.Expression);
            foreach (IEC104Variable var in list)
            {
                if (!var.Valid) throw new ArgumentException("Неможливо отримати змінну IEC104: " + var.Name);
                jseBwT.SetGlobalValue(var.Name, var.Value);
            }
            //result = jseBwT.Evaluate<bool>(expression);
            result = jseBwT.Evaluate<bool>(blockingTr.Expression);
            //Trace.WriteLine("BlockingWithTrigger res = " + result.ToString());

            return result;
        }

        bool Eval(Starting starting, IEC104VariableList list)
        {
            bool result = false;
            //string expression = String.Copy(starting.Expression);
            foreach (IEC104Variable var in list)
            {
                if (!var.Valid) throw new ArgumentException("Неможливо отримати змінну IEC104: " + var.Name);
                jseStrt.SetGlobalValue(var.Name, var.Value);
            }
            //Trace.WriteLine("Starting expression: " + expression);
            //result = jseStrt.Evaluate<bool>(expression);
            result = jseStrt.Evaluate<bool>(starting.Expression);
            //Trace.WriteLine("Starting res = " + result.ToString());
            
            return result;
        }

        bool Eval(Blocking blocking, IEC104VariableList list)
        {
            bool result = false;
            string expression = String.Copy(blocking.Expression);
            foreach (IEC104Variable var in list)
            {
                if (!var.Valid) throw new ArgumentException("Неможливо отримати змінну IEC104: " + var.Name);
                jseBlk.SetGlobalValue(var.Name, var.Value);
            }
            Trace.WriteLine("Blocking expression: " + expression);

            //for example, this expression will work: "(1==1)"
            result = jseBlk.Evaluate<bool>(expression);
            Trace.WriteLine("Blocking res = " + result.ToString());

            return result;
        }
        #endregion

    }
}
