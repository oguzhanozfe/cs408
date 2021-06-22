using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace server
{
    public partial class Form1 : Form
    {

        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        List<Socket> clientSockets = new List<Socket>();
        List<string> clientNames = new List<string>();
        List<string> connectedClients = new List<string>();

        bool terminating = false;
        bool listening = false;

        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();
            string line;
            //create a list of user names from the user database
            System.IO.StreamReader file = new System.IO.StreamReader("user_db.txt");
            while ((line = file.ReadLine()) != null)
            {
                clientNames.Add(line);
            }
        }

        private void button_listen_Click(object sender, EventArgs e)
        {
            int serverPort;

            if(Int32.TryParse(textBox_port.Text, out serverPort))
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);
                serverSocket.Bind(endPoint);
                serverSocket.Listen(3);

                listening = true;
                button_listen.Enabled = false;

                Thread acceptThread = new Thread(Accept);
                acceptThread.Start();

                logs.AppendText("Started listening on port: " + serverPort + "\n");

            }
            else
            {
                logs.AppendText("Please check port number \n");
            }
        }

        private void Accept()
        {
            while(listening)
            {
                try
                {
                    Socket newClient = serverSocket.Accept();
                    clientSockets.Add(newClient);
                    logs.AppendText("A client is connected.\n");

                    Thread receiveThread = new Thread(Receive);
                    receiveThread.Start();
                }
                catch
                {
                    if (terminating)
                    {
                        listening = false;
                    }
                    else
                    {
                        logs.AppendText("The socket stopped working.\n");
                    }

                }
            }
        }

        private void Receive()
        {
            Socket thisClient = clientSockets[clientSockets.Count() - 1];
            bool connected = true;

            while(connected && !terminating)
            {
                try
                {
                    Byte[] buffer = new Byte[64];
                    thisClient.Receive(buffer);

                    string incomingMessage = Encoding.Default.GetString(buffer);
                    incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));
                    //Check the lists to determine if the user name is valid
                    if(incomingMessage.Substring(0,2) == "N:")
                    {
                        string message;
                        incomingMessage = incomingMessage.Substring(2, incomingMessage.Length-2);
                        if (clientNames.Contains(incomingMessage) && !connectedClients.Contains(incomingMessage))
                        {
                            Byte[] buffer2 = new Byte[64];
                            buffer2 = Encoding.Default.GetBytes("Connection established... Welcome " + incomingMessage + "\n");
                            thisClient.Send(buffer2);
                            connectedClients.Add(incomingMessage);
                            message = "Client " + incomingMessage + " accepted\n";
                        }
                        else
                        {
                            Byte[] buffer2 = new Byte[64];
                            buffer2 = Encoding.Default.GetBytes("Connection not established...User " + incomingMessage + " rejected...\n");
                            thisClient.Send(buffer2);
                            message = "Client " + incomingMessage + " rejected\n";
                            thisClient.Close();
                            clientSockets.Remove(thisClient);
                            connected = false;
                        }
                        logs.AppendText(message);
                    }
                    else if(incomingMessage == "Disconnect")
                    {
                        logs.AppendText(connectedClients[clientSockets.Count() - 1] + " has disconnected\n");
                        Byte[] buffer2 = new Byte[64];
                        buffer2 = Encoding.Default.GetBytes("User " + connectedClients[clientSockets.Count() - 1] + " disconnected...\n");
                        thisClient.Send(buffer2);
                        connectedClients.Remove(connectedClients[clientSockets.Count() - 1]);
                        thisClient.Close();
                        clientSockets.Remove(thisClient);
                        connected = false;
                    }
                    else
                    {
                        logs.AppendText(incomingMessage + "\n");
                        foreach(Socket oClient in clientSockets)
                        {
                            if (oClient != thisClient)
                                oClient.Send(buffer);
                        }
                    }

                }
                catch
                {
                    if(!terminating)
                    {
                        logs.AppendText(connectedClients[clientSockets.Count() - 1] + " has disconnected\n");
                    }
                    connectedClients.Remove(connectedClients[clientSockets.Count() - 1]);
                    thisClient.Close();
                    clientSockets.Remove(thisClient);
                    connected = false;
                }
            }
        }

        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            listening = false;
            terminating = true;
            Environment.Exit(0);
        }

    }
}
