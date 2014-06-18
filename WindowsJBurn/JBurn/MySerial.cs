using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace JBurn
{
    public class MySerial
    {
        private SerialPort _serialPort = new SerialPort();
        private int _dataBits = 8;
        private Handshake _handshake = Handshake.None;
        private Parity _parity = Parity.None;
        private StopBits _stopBits = StopBits.One;

        /// <summary> 
        /// Holds data received until we get a terminator. 
        /// </summary> 
        private string tString = string.Empty;

        public MySerial()
        {
        }

        void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Console.WriteLine("In Serial received");
            //Initialize a buffer to hold the received data 
            byte[] buffer = new byte[_serialPort.ReadBufferSize];

            //There is no accurate method for checking how many bytes are read 
            //unless you check the return from the Read method 
            int bytesRead = _serialPort.Read(buffer, 0, buffer.Length);

            Console.WriteLine("bytesRead = " + bytesRead);

            //For the example assume the data we are received is ASCII data. 
            tString += Encoding.ASCII.GetString(buffer, 0, bytesRead);
            //Check if string contains the terminator  
            if (tString.IndexOf('\n') > -1)
            {
                //If tString does contain terminator we cannot assume that it is the last character received 
                string workingString = tString.Substring(0, tString.IndexOf('\n'));
                //Remove the data up to the terminator from tString 
                tString = tString.Substring(tString.IndexOf('\n'));
                //Do something with workingString 
                Console.WriteLine(workingString);
            }
        }

        public static string[] getAvailableSerialPorts()
        {
            return System.IO.Ports.SerialPort.GetPortNames();
        }

        public void connect(String portName, int speed)
        {
            _serialPort.BaudRate = speed;
            _serialPort.DataBits = _dataBits;
            _serialPort.Handshake = _handshake;
            _serialPort.Parity = _parity;
            _serialPort.PortName = portName;
            _serialPort.StopBits = _stopBits;
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(_serialPort_DataReceived);
            _serialPort.Open();
        }


        public void disconnect()
        {
            if (_serialPort != null)
            {
                _serialPort.Close(); // close the port
            }
        }

        public bool isConnected()
        {
            return (_serialPort != null) && _serialPort.IsOpen;
        }
    }
}
