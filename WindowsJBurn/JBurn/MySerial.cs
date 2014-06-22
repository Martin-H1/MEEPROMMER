/*
 * Native Windows GUI for Arduino EEPROM programmer
 *
 * Initial author mario, C# port by Martin Heermance. While Mario didn't
 * state a specific license, his text makes clear he was writing OSS, so
 * I'm making if official by using the MIT license.
 * 
 * The MIT License (MIT)
 * Copyright (c) 2014 Mario and Martin Heermance
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;

namespace JBurn
{
    public class MySerial
    {
        // Set of serial port properties kept as data members
        private SerialPort _serialPort = new SerialPort();
        private int _dataBits = 8;
        private Handshake _handshake = Handshake.None;
        private Parity _parity = Parity.None;
        private StopBits _stopBits = StopBits.One;

        /// <summary>
        /// This class is an argument block used to marshall context from the callback thread to the UI.
        /// </summary>
        public class AsyncContext
        {
            // Set of properties used to marshall anyc context back to the caller.
            public byte[] Data { get; set; }
            public int Offset { get; set; }
            public int Count { get; set; }
            public char Terminator { get; set; }
            public String ResponseText { get; set; }
            public List<byte> ResponseRaw { get; set; }
            public int ResponseSize { get; set; }
            public OnDataRecievedDelegate CallerCallback { get; set; }
        }

        // I hate doing this, but the serial API doesn't allow a way to marshall this.
        // So I have to use this to marshall across thread. It should work because
        // a serial port is inherently FIFO and we manage writes by turning off buttons.
        AsyncContext _context;

        /// <summary> 
        /// Holds data received until we get a terminator. 
        /// </summary> 
        private string tString = string.Empty;

        /// <summary>
        /// static which allows the consumer to populate a dropdown with a list of serial ports.
        /// </summary>
        /// <returns>a string array of serial port names</returns>
        public static string[] getAvailableSerialPorts()
        {
            return System.IO.Ports.SerialPort.GetPortNames();
        }

        /// <summary>
        /// A constructor with nothing special about it, but it captures the UI thread
        /// callback context since this object is constructed by the UI thread.
        /// </summary>
        public MySerial()
        {
            syncContext = System.Windows.Threading.DispatcherSynchronizationContext.Current;
        }

        /// <summary>
        /// Connects to the named port at the desired baud rate.
        /// </summary>
        /// <param name="portName">the serial port name (e.g. COM4)</param>
        /// <param name="speed">the baud rate</param>
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

        /// <summary>
        /// closes the serial port
        /// </summary>
        public void disconnect()
        {
            if (_serialPort != null)
            {
                _serialPort.Close(); // close the port
            }
        }

        /// <summary>
        /// useful state accessor
        /// </summary>
        /// <returns>true if connected</returns>
        public bool isConnected()
        {
            return (_serialPort != null) && _serialPort.IsOpen;
        }

        /// <summary>
        /// Drops any leftover data in order to get a clean read from the port.
        /// </summary>
        public void DiscardInBuffer()
        {
            tString = string.Empty;
            _serialPort.DiscardInBuffer();
        }

        /// <summary>
        /// Write accepts text and callsback when a response is received. This is useful
        /// for one shot writing of small blocks of text data.
        /// </summary>
        /// <param name="text">the data to send to the serial port</param>
        /// <param name="terminator">the character which signifies the end of the response</param>
        /// <param name="callback">called when a line of response is recieved</param>
        public void Write(String text, char terminator, OnDataRecievedDelegate callback)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);
            Write(data, 0, data.Length, terminator, 0, callback);
        }

        /// <summary>
        /// Write accepts text and callsback when a response is received. It differs from the
        /// above in that there's no terminator to the response. It is fixed length.
        /// </summary>
        /// <param name="text">the data to send to the serial port</param>
        /// <param name="responseSize">the number of bytes in the response</param>
        /// <param name="callback">called when a line of response is recieved</param>
        public void Write(String text, int responseSize, OnDataRecievedDelegate callback)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);
            Write(data, 0, data.Length, '\0', responseSize, callback);
        }

        /// <summary>
        /// Write accepts data and calls back when a response is received. This is useful for
        /// writing a large block in chuncks. The user can specify an ofset and count and write
        /// ranges of the data.
        /// </summary>
        /// <param name="data">the data to send to the serial port</param>
        /// <param name="offset">the offset within the buffer to start writing from</param>
        /// <param name="count">the number of bytes to write</param>
        /// <param name="terminator">the character which signifies the end of the response.</param>
        /// <param name="responseSize">the expected size of the response in bytes if there's no terminator.</param>
        /// <param name="callback">called when a line of response is recieved</param>
        public void Write(Byte[] data, int offset, int count, char terminator,
            int responseSize, OnDataRecievedDelegate callback)
        {
            // If the user expects a response, then save all the context parameters.
            if (callback != null)
            {
                _context = new AsyncContext();
                _context.Data = data;
                _context.Offset = offset;
                _context.Count = count;
                _context.Terminator = terminator;
                _context.ResponseText = null;
                _context.ResponseRaw = new List<byte>();
                _context.ResponseSize = responseSize;
                _context.CallerCallback = callback;
            }

            _serialPort.Write(data, offset, count);
        }

        // Everything below this is gritty Windows async callback stuff.
        // Basically I/O is non-blocking and calls back on a worker thread
        // But it's up to the programmer to marshall the data back to the caller.

        /// <summary>
        /// This holds the UI context to enable post message back to the UI thread
        /// </summary>
        private static SynchronizationContext syncContext;
        private static SynchronizationContext Sync
        {
            get { return syncContext; }
        }

        /// <summary>
        /// Consumers implement this delegate to be called when their response is available.
        /// Their initial arguments are reflected back to them to allow them to callback with
        /// the next block. This allows chaining a sequence of calls to together to write
        /// large payloads.
        /// </summary>
        public delegate void OnDataRecievedDelegate(AsyncContext context);

        /// <summary>
        /// I could use a lambda function to handle this thread break, but I find the
        /// syntax a bear to get correct.
        /// </summary>
        /// <param name="postPayload">the caller's callback method and response</param>
        private static void SimpleDelegateMethod(object postPayload)
        {
            System.Diagnostics.Debug.Assert(postPayload is AsyncContext);
            AsyncContext context = (AsyncContext)postPayload;
            context.CallerCallback(context);
        }

        /// <summary>
        /// Called in response to the data write.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // Initialize a buffer to hold the received data 
            byte[] buffer = new byte[_serialPort.BytesToRead];

            // There is no accurate method for checking how many bytes are read 
            // unless you check the return from the Read method 
            int bytesRead = _serialPort.Read(buffer, 0, buffer.Length);

            // if the user passed a terminator, then convert to a string.
            if (_context.Terminator != '\0')
            {
                // Assume the data we are received is ASCII data. 
                tString += Encoding.ASCII.GetString(buffer, 0, bytesRead);

                if (tString.IndexOf(_context.Terminator) > -1)
                {
                    // Split the string at the terminator.
                    string[] fields = tString.Split(_context.Terminator);

                    // The payload is before the terminator.
                    _context.ResponseText = fields[0];

                    // The rest of the string is after the terminator.
                    tString = fields[1];

                    // If desired Post the message back to this object to break to the UI thread.
                    if (_context.CallerCallback != null)
                    {
                        Sync.Post(SimpleDelegateMethod, _context);
                    }
                }
            }
            else
            {
                _context.ResponseRaw.AddRange(buffer);
                if (_context.ResponseRaw.Count >= _context.ResponseSize)
                {
                    Sync.Post(SimpleDelegateMethod, _context);
                }
            }
        }
    }
}
