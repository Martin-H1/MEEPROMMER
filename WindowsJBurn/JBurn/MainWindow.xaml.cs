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

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JBurn
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const String revision = "$Revision: 1.5 $";
        private const String date = "$Date: 2013/07/19 05:44:46 $";

        MySerial mySerial = new MySerial();
        String selectedComPort;
        byte[] data = new byte[32768];
        byte[] eeprom = new byte[32768];
        int maxAddress = 8192;
        int offset = 0;
        long filesize = 0;
        int serialSpeed = 115200;
        //int serialSpeed = 57600;

        Stopwatch _stopwatch = new Stopwatch();

        /// <summary>
        /// Initialzes the serial combo box, and disables eeprom buttons.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Add the list of com ports to the drop down.
            foreach (String port in MySerial.getAvailableSerialPorts())
            {
                this._serial.Items.Add(port);
            }
        }

        /// <summary>
        /// User pressed load from file, so chose a file and read it into the buffer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLoad(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = ".bin"; // Default file extension
            dlg.Filter = "Intel image files (.bin)|*.bin"; // Filter files by extension 

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process input if the user clicked OK.
            if (result == true)
            {
                try
                {
                    byte[] temp = File.ReadAllBytes(dlg.FileName);
                    filesize = temp.Length;
                    for (int idx = 0; idx < temp.Length && idx < data.Length; idx++)
                        data[idx] = temp[idx];
                    appendToLog(filesize + " bytes loaded from \"" + dlg.FileName + "\"\n");
                }
                catch (Exception ex)
                {
                    appendToLog("Error: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// User wants to save the current eeprom image to a file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSave(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = ".bin"; // Default file extension
            dlg.Filter = "Intel image files (.bin)|*.bin"; // Filter files by extension 

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process input if the user clicked OK.
            if (result == true)
            {
                try
                {
                    File.WriteAllBytes(dlg.FileName, this.data);

                    appendToLog("Saving: " + dlg.FileName + "\n");
                }
                catch (Exception ex)
                {
                    appendToLog("Error: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Send the version request and display the response in the callback.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnVersion(object sender, RoutedEventArgs e)
        {
            appendToLog("Simple JBurn - Revision : " + revision + ", " + date + "\n");
            if (mySerial.isConnected())
            {
                mySerial.Write("V\n", '\n', OnVersionCallback);
            }
            else
            {
                appendToLog("Error: Not connected to any Programmer!\n");
            }
        }

        /// <summary>
        /// Callback from above which contains the version text.
        /// </summary>
        /// <param name="context"></param>
        private void OnVersionCallback(MySerial.AsyncContext context)
        {
            appendToLog(context.ResponseText);
        }

        /// <summary>
        /// Displays the differences as requested.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDiff(object sender, RoutedEventArgs e)
        {
            /*
                         clearButton.setEnabled(true);
            writeButton.setEnabled(true);
            readButton.setEnabled(true);
            setCursor(null); //turn off the wait cursor

            textPane.setCaretPosition(textPane.getDocument().getLength());
            if (this.diff) {
                log.insertString(log.getLength(), "Checking difference between loaded ROM file and data on EEPROM"
                        + "\n",null);
                int byteCount = 0;
                //this.readEEPROM();
                for (int i = 0; i < filesize; i++) {
                    if (data[i] != eeprom[i + offset]) {
                        byteCount++;
                    }
                }
                log.insertString(log.getLength(), filesize + " bytes checked from 0x" + Utility.wordToHex(offset)
                        + " to 0x" + Utility.wordToHex(offset + (int) filesize - 1) + ", " + byteCount
                        + " byte are different." + "\n",null);
                textPane.setCaretPosition(textPane.getDocument().getLength());
            }
        } catch (BadLocationException e) {
            System.err.println("Output Error");
        }

        }
    }*/
        }

        /// <summary>
        /// Displays the data buffer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnData(object sender, RoutedEventArgs e)
        {
            String line = "";
            for (int i = 0; i < maxAddress; i++)
            {
                if (i % 32 == 0)
                {
                    line = line + "0x" + Utility.wordToHex(i) + "  ";
                }
                line = line + Utility.byteToHex(eeprom[i]) + " ";
                if (i % 32 == 31)
                {
                    appendToLog(line + "\n");
                    line = "";
                }
            }
        }

        /// <summary>
        /// Displays the image to the text box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnImage(object sender, RoutedEventArgs e)
        {
            String line = "";
            for (int i = 0; i < maxAddress; i++)
            {
                if (i % 32 == 0)
                {
                    line = line + "0x" + Utility.wordToHex(i) + "  ";
                }
                line = line + Utility.byteToHex(data[i]) + " ";
                if (i % 32 == 31)
                {
                    appendToLog(line + "\n");
                    line = "";
                }
            }
        }

        /// <summary>
        /// The userselected a serial port, so open it and enable the buttons.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _serial_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object temp = _serial.Items.GetItemAt(_serial.SelectedIndex);
            selectedComPort = temp != null ? temp.ToString() : "";
            appendToLog("now selected: " + selectedComPort + "\n");

            try
            {
                mySerial.disconnect();
                mySerial.connect(selectedComPort, serialSpeed);
                appendToLog(selectedComPort + " is now connected.\n");
            }
            catch (Exception ex)
            {
                appendToLog("Error : " + ex.Message + "\n");
            }
        }

        /// <summary>
        /// The user changed the eepomr size, so update state variables.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _eepromType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (_eepromType.SelectedIndex)
            {
                case 0:
                    maxAddress = 8192;
                    break;
                case 1:
                    maxAddress = 16384;
                    break;
                case 2:
                    maxAddress = 32768;
                    break;
                default:
                    maxAddress = 8192;
                    break;
            }

            appendToLog("now selected: " +
                        ((ComboBoxItem)_eepromType.Items.GetItemAt(_eepromType.SelectedIndex)).Content +
                        ", address range = 0x0000 to 0x" +
                        Utility.wordToHex(maxAddress - 1) + "\n");
        }

        /// <summary>
        /// User changedthe base location to start writing to the eeprom.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _offset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            offset = _offset.SelectedIndex * 1024;

            appendToLog("Offset is now set to : " + _offset.SelectedIndex + "k\n");
            appendToLog("data will be written from 0x" + Utility.wordToHex(offset) + "\n");

            if (offset + filesize > maxAddress)
            {
                appendToLog("WARNING!! The offset you choose will cause the current file not to fit in the choosen EEPROM anymore\n");
            }
        }

        /// <summary>
        /// clears the in memory image and writes it to the eeprom.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClear(object sender, RoutedEventArgs e)
        {
            appendToLog("Clearing EEPROM. setting " + maxAddress + " bytes to 0xff\n");
            for (int i = 0; i < maxAddress; i++)
            {
                data[i] = (byte)(0xff);
            }

            OnWrite(sender, e);         
        }

        /// <summary>
        /// User pushed the read button, so load the image from the eeprom.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRead(object sender, RoutedEventArgs e)
        {
            this.mySerial.DiscardInBuffer();

            // Time the eeprom read.
            _stopwatch.Start();

            String command = "r,0000," + Utility.wordToHex(maxAddress-1) + ",20\n";
            appendToLog("sending command. " + command);
            mySerial.Write(command, maxAddress, OnReadCallback);
            appendToLog("command sent.\n");
        }

        /// <summary>
        /// Callback from the above, data in response is processed.
        /// </summary>
        /// <param name="context"></param>
        private void OnReadCallback(MySerial.AsyncContext context)
        {
            appendToLog("trying to read.\n");
            context.ResponseRaw.CopyTo(eeprom);
            _stopwatch.Stop();
            appendToLog(maxAddress + " bytes read in " +  _stopwatch.ElapsedMilliseconds / 1000 + " seconds\n");
        }

        /// <summary>
        /// Called in response to the user pushing the write eeprom button.
        /// It sends the initial write comand, then the first block of data.
        /// </summary>
        /// <param name="sender">the button that sent the message</param>
        /// <param name="e">context information that is unneeded.</param>
        private void OnWrite(object sender, RoutedEventArgs e)
        {
            // disable buttons while we're writing.
            _clear.IsEnabled = false;

            appendToLog("sending write command.\n");

            mySerial.Write("w," + Utility.wordToHex(offset) + "," + Utility.wordToHex(1024) + "\n", '%', null);
            mySerial.Write(data, offset, 1024, '%', maxAddress, OnWriteCallback);
            appendToLog("wrote data from 0x" + Utility.wordToHex(offset) + " to 0x" + Utility.wordToHex(offset + 1023) + "\n");
        }

        /// <summary>
        /// After writing a binary block this is the callback indicating success.
        /// It then writes the next blockm or declares success if no more blocks
        /// need to be written.
        /// </summary>
        /// <param name="context">an argument block for tracking status of the writes.</param>
        private void OnWriteCallback(MySerial.AsyncContext context)
        {
            // index into the block by the amount written.
            int addr = context.Offset + context.Count;
            if (addr < this.maxAddress)
            {
                int newCount = (addr + context.Count) < maxAddress ? context.Count : (maxAddress - addr);
                mySerial.Write("w," + Utility.wordToHex(addr) + "," + Utility.wordToHex(newCount) + "\n", '%', null);
                mySerial.Write(data, addr, newCount, '%', 0, OnWriteCallback);
                appendToLog("wrote data from 0x" + Utility.wordToHex(addr) + " to 0x" + Utility.wordToHex(addr + 1023) + "\n");
            }
            else
            {
                _clear.IsEnabled = true;
            }
        }
 
        /* 
        public void done() {
            Toolkit.getDefaultToolkit().beep();
            clearButton.setEnabled(true);
            writeButton.setEnabled(true);
            readButton.setEnabled(true);
            setCursor(null); //turn off the wait cursor
            try{
            log.insertString(log.getLength(), "data sent." + "\n",null);

            log.insertString(log.getLength(), "wrote " + len + " bytes from 0x"
                    + Utility.wordToHex(address) + " to 0x"
                    + Utility.wordToHex(address + (int) len - 1) + " in "
                    + (float) (end - start) / 1000
                    + " seconds " + "\n",null);
            textPane.setCaretPosition(textPane.getDocument().getLength());
         */

        /// <summary>
        /// Writes the message to the text box at the bottom.
        /// </summary>
        /// <param name="text">the message to write</param>
        private void appendToLog(String text)
        {
            if (_messages != null)
                this._messages.Text += text;
        }
    }
}
