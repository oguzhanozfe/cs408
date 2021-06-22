using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace client
{
    public partial class Form1 : Form
    {

        bool terminating = false;
        bool connected = false;
        Socket clientSocket;
        string cName;

        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();
        }

        private void button_connect_Click(object sender, EventArgs e)
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string IP = textBox_ip.Text;
            cName = textBox_name.Text;

            int portNum;
            if(Int32.TryParse(textBox_port.Text, out portNum))
            {
                try
                {
                    clientSocket.Connect(IP, portNum);
                    button_connect.Enabled = false;
                    textBox_ip.Enabled = false;
                    textBox_name.Enabled = false;
                    textBox_port.Enabled = false;
                    textBox_message.Enabled = true;
                    button_send.Enabled = true;
                    button_disconnect.Enabled = true;
                    connected = true;

                    Byte[] buffer = new Byte[64];
                    string identity = "N:" + cName;
                    buffer = Encoding.Default.GetBytes(identity);
                    clientSocket.Send(buffer);

                    Thread receiveThread = new Thread(Receive);
                    receiveThread.Start();

                }
                catch
                {
                    logs.AppendText("Could not connect to the server!\n");
                }
            }
            else
            {
                logs.AppendText("Check the port\n");
            }

        }

        private void Receive()
        {
            while(connected)
            {
                try
                {
                    Byte[] buffer = new Byte[64];
                    clientSocket.Receive(buffer);

                    string incomingMessage = Encoding.Default.GetString(buffer);
                    incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));

                    logs.AppendText(incomingMessage + "\n");

                    if (incomingMessage == "Connection not established...User " + cName + " rejected...\n")
                    {
                        connected = false;
                        button_connect.Enabled = true;
                        textBox_ip.Enabled = true;
                        textBox_name.Enabled = true;
                        textBox_port.Enabled = true;
                        textBox_message.Enabled = false;
                        button_send.Enabled = false;
                        button_disconnect.Enabled = false;
                    }
                }
                catch
                {
                    if (!terminating)
                    {
                        logs.AppendText("The server has disconnected\n");
                        button_connect.Enabled = true;
                        textBox_ip.Enabled = true;
                        textBox_name.Enabled = true;
                        textBox_port.Enabled = true;
                        textBox_message.Enabled = false;
                        button_send.Enabled = false;
                        button_disconnect.Enabled = false;
                    }

                    clientSocket.Close();
                    connected = false;
                }

            }
        }

        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            connected = false;
            terminating = true;
            Environment.Exit(0);
        }

        private void button_send_Click(object sender, EventArgs e)
        {
            string message = cName + ": " + textBox_message.Text;

            if(message != cName + ": " && message.Length <= 64)
            {
                Byte[] buffer = new Byte[64];
                buffer = Encoding.Default.GetBytes(message);
                clientSocket.Send(buffer);
                textBox_message.Text = "";
            }
        }

        private void button_disconnect_Click(object sender, EventArgs e)
        {
            //Let the server know
            string message = "Disconnect";
            Byte[] buffer = new Byte[64];
            buffer = Encoding.Default.GetBytes(message);
            clientSocket.Send(buffer);

            connected = false;
            button_connect.Enabled = true;
            textBox_ip.Enabled = true;
            textBox_name.Enabled = true;
            textBox_port.Enabled = true;
            textBox_message.Enabled = false;
            button_send.Enabled = false;
            button_disconnect.Enabled = false;
        }
    }
}
