using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
        //used to end a line in the output window
        private const String newline = "\n";

        MySerial mySerial = new MySerial();
        String selectedComPort;
        byte[] data = new byte[32768];
        byte[] eeprom = new byte[32768];
        int maxAddress = 8192;
        int offset = 0;
        long filesize = 0;
        int readwrite = 0;
        int serialSpeed = 115200;
        //int serialSpeed = 57600;

        public MainWindow()
        {
            InitializeComponent();

            // Add the list of com ports to the drop down.
            foreach (String port in MySerial.getAvailableSerialPorts())
            {
                this._serial.Items.Add(port);
            }
        }

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
                    appendToLog(filesize + " bytes loaded from \"" + dlg.FileName + "\"" + newline);
                }
                catch (Exception ex)
                {
                    appendToLog("Error: " + ex.Message);
                }
            }
        }

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

                    appendToLog("Saving: " + dlg.FileName + newline);
                }
                catch (Exception ex)
                {
                    appendToLog("Error: " + ex.Message);
                }
            }
        }

        private void OnVersion(object sender, RoutedEventArgs e)
        {

        }

        private void OnDiff(object sender, RoutedEventArgs e)
        {

        }

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
                    appendToLog(line + newline);
                    line = "";
                }
            }
        }

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
                    appendToLog(line + newline);
                    line = "";
                }
            }
        }

        private void _serial_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object temp = _serial.Items.GetItemAt(_serial.SelectedIndex);
            selectedComPort = temp != null ? temp.ToString() : "";
            appendToLog("now selected: " + selectedComPort + newline);

            try
            {
                mySerial.disconnect();
                mySerial.connect(selectedComPort, serialSpeed);
                appendToLog(selectedComPort + " is now connected." + newline);
            }
            catch (Exception ex)
            {
                appendToLog("Error : " + ex.Message + newline);
            }
        }

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
                        Utility.wordToHex(maxAddress - 1) + newline);
        }

        private void _offset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            offset = _offset.SelectedIndex * 1024;

            appendToLog("Offset is now set to : " + _offset.SelectedIndex + "k" + newline);
            appendToLog("data will be written from 0x" + Utility.wordToHex(offset) + newline);

            if (offset + filesize > maxAddress)
            {
                appendToLog("WARNING!! The offset you choose will cause the current file not to fit in the choosen EEPROM anymore " + newline);
            }
        }

        private void OnClear(object sender, RoutedEventArgs e)
        {
            appendToLog("Clearing EEPROM. setting " + maxAddress + " bytes to 0xff" + newline);
            for (int i = 0; i < maxAddress; i++)
            {
                data[i] = (byte)(0xff);
            }
            /*
            clearButton.setEnabled(false);
            readwrite = 0;
            progressBar.setValue(0);
            setCursor(Cursor.getPredefinedCursor(Cursor.WAIT_CURSOR));
            //Instances of javax.swing.SwingWorker are not reusuable, so
            //we create new instances as needed.
            writeTask = new WriteTask(0, maxAddress);
            writeTask.addPropertyChangeListener(this);
            writeTask.execute();
             */
        }

        private void OnRead(object sender, RoutedEventArgs e)
        {

        }

        private void OnWrite(object sender, RoutedEventArgs e)
        {

        }

        /*
    class ReadTask extends SwingWorker<Void, Void> {

        public int done = 0;
        boolean diff = false;
        long start, end = 0;
        int readProgress = 0;

        public ReadTask(boolean d) {
            this.diff = d;
        }

 
        @Override
        public Void doInBackground() {
            //check if eeprom should be read or written
            try {
            try {
                //remove old data from input stream to prevent them "poisening" our
                //data
                mySerial.in.skip(mySerial.in.available());
                //take time to read the eeprom
                start = System.currentTimeMillis();
                BufferedWriter bw = new BufferedWriter(new OutputStreamWriter(mySerial.out));
                String line = "";


                log.insertString(log.getLength(), "sending command." + newline,null);
                bw.write("r,0000," + Utility.wordToHex(maxAddress) + ",20" + newline);
                bw.flush();
                log.insertString(log.getLength(), "command sent." + newline,null);
                log.insertString(log.getLength(), "trying to read." + newline,null);
                int counter = 0;
                byte c = ' ';
                do {
                    eeprom[counter++] = (byte) mySerial.in.read();
                    if (counter % 100 == 0) {
                        readProgress = 100 * counter / maxAddress;
                        setProgress(readProgress);
                    }
                } while (counter < maxAddress);
                end = System.currentTimeMillis();
                setProgress(100);


            } catch (Exception e) {
                log.insertString(log.getLength(), "Error: " + e.getMessage() + newline,null);
            }
        } catch (BadLocationException e) {
            System.err.println("Output Error");
        }
        }

        /*
         * Executed in event dispatching thread
         *
        @Override
        public void done() {
            try{ 
                Toolkit.getDefaultToolkit().beep();
            clearButton.setEnabled(true);
            writeButton.setEnabled(true);
            readButton.setEnabled(true);
            setCursor(null); //turn off the wait cursor
            log.insertString(log.getLength(), maxAddress + " bytes read in " + (float) (end - start) / 1000
                    + " seconds " + newline,null);
            textPane.setCaretPosition(textPane.getDocument().getLength());
            if (this.diff) {
                log.insertString(log.getLength(), "Checking difference between loaded ROM file and data on EEPROM"
                        + newline,null);
                int byteCount = 0;
                //this.readEEPROM();
                for (int i = 0; i < filesize; i++) {
                    if (data[i] != eeprom[i + offset]) {
                        byteCount++;
                    }
                }
                log.insertString(log.getLength(), filesize + " bytes checked from 0x" + Utility.wordToHex(offset)
                        + " to 0x" + Utility.wordToHex(offset + (int) filesize - 1) + ", " + byteCount
                        + " byte are different." + newline,null);
                textPane.setCaretPosition(textPane.getDocument().getLength());
            }
        } catch (BadLocationException e) {
            System.err.println("Output Error");
        }

        }
    }

    class WriteTask extends SwingWorker<Void, Void> {

        public int done = 0;
        int len;
        int address;
        long start, end = 0;
        int writeProgress = 0;

        public WriteTask(int a, int l) {
            this.len = l;
            this.address = a;
        }
        /*
         * Main task. Executed in background thread.
         *

        @Override
        public Void doInBackground() {
            try{
            try {
                //take time to read the eeprom
                start = System.currentTimeMillis();
                BufferedWriter bw = new BufferedWriter(new OutputStreamWriter(mySerial.out));
                String line = "";
                log.insertString(log.getLength(), "sending command." + newline,null);
                for (int i = 0; i < len; i += 1024) {
                    bw.write("w," + Utility.wordToHex(address + i) + "," + Utility.wordToHex(1024) + newline);
                    bw.flush();
                    writeProgress = i * 100 / len;
                    setProgress(writeProgress);
                    mySerial.out.write(data, i, 1024);
                    log.insertString(log.getLength(), "wrote data from 0x" + Utility.wordToHex(address + i)
                            + " to 0x" + Utility.wordToHex(address + i + 1023) + newline,null);
                    textPane.setCaretPosition(textPane.getDocument().getLength());

                    byte c = ' ';
                    do {
                        c = (byte) mySerial.in.read();
                    } while (c != '%');

                }
                end = System.currentTimeMillis();
                setProgress(100);
            } catch (Exception e) {
                log.insertString(log.getLength(), "Error: " + e.getMessage() + newline,null);
            }

        } catch (BadLocationException e) {
            System.err.println("Output Error");
        }
            return null;
            
        }

        /*
         * Executed in event dispatching thread
         *
        @Override
        public void done() {
            Toolkit.getDefaultToolkit().beep();
            clearButton.setEnabled(true);
            writeButton.setEnabled(true);
            readButton.setEnabled(true);
            setCursor(null); //turn off the wait cursor
            try{
            log.insertString(log.getLength(), "data sent." + newline,null);

            log.insertString(log.getLength(), "wrote " + len + " bytes from 0x"
                    + Utility.wordToHex(address) + " to 0x"
                    + Utility.wordToHex(address + (int) len - 1) + " in "
                    + (float) (end - start) / 1000
                    + " seconds " + newline,null);
            textPane.setCaretPosition(textPane.getDocument().getLength());
        } catch (BadLocationException e) {
            System.err.println("Output Error");
        }

        }
    }

    private void writeButtonActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_writeButtonActionPerformed
        writeButton.setEnabled(false);
        progressBar.setValue(0);
        readwrite = 1;
        setCursor(Cursor.getPredefinedCursor(Cursor.WAIT_CURSOR));
        //Instances of javax.swing.SwingWorker are not reusuable, so
        //we create new instances as needed.
        writeTask = new WriteTask(offset, (int) filesize);
        writeTask.addPropertyChangeListener(this);
        writeTask.execute();

    }//GEN-LAST:event_writeButtonActionPerformed

    private void versionButtonActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_versionButtonActionPerformed
            appendToLog("Simple JBurn - Revision : " + revision + ", " + date + newline);
        if (mySerial.isConnected()) {
            try {
                mySerial.out.write('V');
                mySerial.out.write('\n');
                String line = "";
                byte c = ' ';
                do {
                    c = (byte) mySerial.in.read();
                    line = line + (char) c;
                    if (c == '\n') {
                        appendToLog( line);
                        line = "";
                    }
                } while (c != '\n');
            } catch (Exception e) {
                appendToLog("Error: " + e.getMessage() + newline);
            }
        } else {
            appendToLog("Error: Not connected to any Programmer!" + newline);
        }
    }//GEN-LAST:event_versionButtonActionPerformed

    private void showDiffButtonActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_showDiffButtonActionPerformed
        readButton.setEnabled(false);
        readwrite = 2;
        progressBar.setValue(0);
        setCursor(Cursor.getPredefinedCursor(Cursor.WAIT_CURSOR));
        //Instances of javax.swing.SwingWorker are not reusuable, so
        //we create new instances as needed.
        readTask = new ReadTask(true);
        readTask.addPropertyChangeListener(this);
        readTask.execute();
    }//GEN-LAST:event_showDiffButtonActionPerformed

    private void readButtonActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_readButtonActionPerformed
        readButton.setEnabled(false);
        readwrite = 2;
        progressBar.setValue(0);
        setCursor(Cursor.getPredefinedCursor(Cursor.WAIT_CURSOR));
        //Instances of javax.swing.SwingWorker are not reusuable, so
        //we create new instances as needed.
        readTask = new ReadTask(false);
        readTask.addPropertyChangeListener(this);
        readTask.execute();
    }//GEN-LAST:event_readButtonActionPerformed
         */
        
        private void appendToLog(String text)
        {
            if (_messages != null)
                this._messages.Text += text;
        }
    }
}
