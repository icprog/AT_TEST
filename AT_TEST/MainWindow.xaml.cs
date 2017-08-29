using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AT_TEST
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private string receiveData;
        public static SerialPort serial = new SerialPort();
        private DispatcherTimer autoDetectionTimer = new DispatcherTimer();
        static UInt32 receiveBytesCount = 0;
        static UInt32 sendBytesCount = 0;
        public MainWindow()
        {
            InitializeComponent();
           
            GetValuablePortName();

            // 设置自动检测1秒1次
            autoDetectionTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            autoDetectionTimer.Tick += new EventHandler(AutoDectionTimer_Tick);
            //开启定时器
            autoDetectionTimer.Start();

            //设置状态栏提示
            statusTextBlock.Text = "准备就绪";

        }
        #region 自动更新串口号
        //自动检测串口名
        private void GetValuablePortName()
        {
            //检测有效的串口并添加到combobox
            string[] serialPortName = System.IO.Ports.SerialPort.GetPortNames();

            foreach (string name in serialPortName)
            {
                portNamesCombobox.Items.Add(name);
            }
        }

        //自动检测串口时间到
        private void AutoDectionTimer_Tick(object sender, EventArgs e)
        {

            string[] serialPortName = System.IO.Ports.SerialPort.GetPortNames();

            if (turnOnButton.IsChecked == true)
            {
                //在找到的有效串口号中遍历当前打开的串口号
                foreach (string name in serialPortName)
                {
                    if (serial.PortName == name)
                        return;                 //找到，则返回，不操作               
                }
                if (serial.IsOpen == false)
                {
                    serial.Close();
                    portNamesCombobox.Items.Remove(serial.PortName);
                    portNamesCombobox.SelectedIndex = 0;
                } 
                //若找不到已打开的串口:表示当前打开的串口已失效
                //按钮回弹
                turnOnButton.IsChecked = false;
                //删除combobox中的名字
                portNamesCombobox.Items.Remove(serial.PortName);
                portNamesCombobox.SelectedIndex = 0;
                //提示消息
                statusTextBlock.Text = serial.PortName.ToString() + "已失效！";
            }
            else
            {
                //检查有效串口和combobox中的串口号个数是否不同
                if (portNamesCombobox.Items.Count != serialPortName.Length)
                {
                    //串口数不同，清空combobox
                    portNamesCombobox.Items.Clear();

                    //重新添加有效串口
                    foreach (string name in serialPortName)
                    {
                        portNamesCombobox.Items.Add(name);
                    }
                    portNamesCombobox.SelectedIndex = 0;

                    statusTextBlock.Text = "串口列表已更新！";

                }
            }
        }
        #endregion


        #region 串口配置面板

        //使能或关闭串口配置相关的控件
        private void serialSettingControlState(bool state)
        {

        }

        //打开串口
        private void TurnOnButton_Checked(object sender, RoutedEventArgs e)
        {

            try
            {

                serial.PortName = portNamesCombobox.Text;
                serial.BaudRate = Convert.ToInt32(baudRateCombobox.Text);
                serial.Parity = (System.IO.Ports.Parity)Enum.Parse(typeof(System.IO.Ports.Parity), parityCombobox.Text);
                serial.DataBits = Convert.ToInt16(dataBitsCombobox.Text);
                serial.StopBits = (System.IO.Ports.StopBits)Enum.Parse(typeof(System.IO.Ports.StopBits), stopBitsCombobox.Text);
                serial.ReadTimeout = 500;
                serial.WriteTimeout = 500;
                //设置串口编码为default：获取操作系统的当前 ANSI 代码页的编码。
                serial.Encoding = Encoding.Default;

                //添加串口事件处理
                serial.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(ReceiveData);

                //开启串口
                serial.Open();

                //关闭串口配置面板

                BitmapImage image = new BitmapImage(new Uri(@"Resources\on.bmp", UriKind.Relative));
                imagestate.Source = image;
                statusTextBlock.Text = portNamesCombobox.SelectedItem.ToString() + "已开启";
                turnOnButton.Content = "关闭串口";
                portNamesCombobox.IsEnabled = false;
                baudRateCombobox.IsEnabled = false;
                parityCombobox.IsEnabled = false;
                dataBitsCombobox.IsEnabled = false;
                stopBitsCombobox.IsEnabled = false;



            }
            catch
            {
                statusTextBlock.Text = "串口连接失败";
                portNamesCombobox.Items.Remove(portNamesCombobox.SelectedItem);
                serial.Close();
                
                
                
                turnOnButton.Unchecked -= TurnOnButton_Unchecked;
                turnOnButton.IsChecked = false;
                turnOnButton.Unchecked += TurnOnButton_Unchecked;

            }

        }


        //关闭串口
        private void TurnOnButton_Unchecked(object sender, RoutedEventArgs e)
        {

            try
            {
               
                serial.Close();
                serial.Dispose();
                //关闭定时器

                BitmapImage image = new BitmapImage(new Uri(@"Resources\off.bmp", UriKind.Relative));
                imagestate.Source = image;
                //使能串口配置面板


                statusTextBlock.Text = portNamesCombobox.SelectedItem.ToString() + "已关闭";
                turnOnButton.Content = "打开串口";
                portNamesCombobox.IsEnabled = true;
                baudRateCombobox.IsEnabled = true;
                parityCombobox.IsEnabled = true;
                dataBitsCombobox.IsEnabled = true;
                stopBitsCombobox.IsEnabled = true;

            }
            catch
            {

            }

        }

        #endregion
        //接收数据
        private delegate void UpdateUiTextDelegate(string text);
        private void ReceiveData(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            receiveData = serial.ReadExisting();
            Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(ShowData), receiveData);
        }

        //显示数据
        private void ShowData(string text)
        {
            string receiveText = text;

            //更新接收字节数
            receiveBytesCount += (UInt32)receiveText.Length;
            statusReceiveByteTextBlock.Text = receiveBytesCount.ToString();

            //没有关闭数据显示
            
                //字符串显示
                if (hexadecimalDisplayCheckBox.IsChecked == false)
                {
                Rec_TxtBox.AppendText(receiveText);
                Rec_TxtBox.ScrollToEnd();

            }
                else //16进制显示
                {
                    byte[] recData = System.Text.Encoding.Default.GetBytes(receiveText);// 将接受到的字符串据转化成数组；  

                    foreach (byte str in recData)
                    {
                        Rec_TxtBox.AppendText(string.Format("{0:X2} ", str));
                        Rec_TxtBox.ScrollToEnd();
                    }
                
            }

        }
        private void SendData(SerialPort serial, String text)
        {
            try
            {
                if (serial != null)
                {
                    if (serial.IsOpen)
                    {
                        serial.Write(text);
                        sendBytesCount += (UInt32)text.Length;
                        statusSendByteTextBlock.Text = sendBytesCount.ToString();
                        SendView_TxtBox.AppendText(text);
                        SendView_TxtBox.ScrollToEnd();
                    }
                }
            }
            catch
            {

            }
            
        }

        private void Enter_Test_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=TEST_MODE\r\n";
            SendData(serial, sendData); 
            
        }

        private void Exti_Test_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=EXIT_TEST_MODE\r\n";
            SendData(serial, sendData);
            
        }

        private void Read_BAT_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=BATTERY_LEVEL\r\n";
            SendData(serial, sendData);
            
        }

        private void Charge_Test_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=POWER_MODE\r\n";
            SendData(serial, sendData);
            
        }

        private void Speaker_Test_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=SPEAKER\r\n";
            SendData(serial, sendData);
            
        }

        private void MIC_Test_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=MIC\r\n";
            SendData(serial, sendData);
            
        }

        private void MODE_Trcking_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=MODE_TRACKING\r\n";
            SendData(serial, sendData);
            
        }

        private void MODE_Flight_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=MODE_SMART\r\n";
            SendData(serial, sendData);
            
        }

        private void Power_Off_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=SHUTDOWN\r\n";
            SendData(serial, sendData);
            
        }

        private void Reboot_Test_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=REBOOT\r\n";
            SendData(serial, sendData);
            
        }

        private void SW_Version_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=SW_VERSION\r\n";
            SendData(serial, sendData);
            
        }

        private void Read_IMEI_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=IMEI\r\n";
            SendData(serial, sendData);
            
        }

        private void Gsensor_Test_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=GSENSOR\r\n";
            SendData(serial, sendData);
            
        }

        private void GPS_OTP_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=HW_VERSION\r\n";
            SendData(serial, sendData);
            
        }

        private void GPS_On_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=GPS_ON\r\n";
            SendData(serial, sendData);
            
        }

        private void GPS_Off_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=GPS_OFF\r\n";
            SendData(serial, sendData);
            
        }

        private void Delet_File_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=DEL_TEST_FILE\r\n";
            SendData(serial, sendData);
            
        }

        private void Read_SN_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=SN\r\n";
            SendData(serial, sendData);
            
        }

        private void GSM_Standby_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=MODE_GSM_STANDBY\r\n";
            SendData(serial, sendData);
            
        }

        private void LED_Test_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=LED\r\n";
            SendData(serial, sendData);
            
        }

        private void GPS_WIFI_Off_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=WIFI_OFF\r\n";
            SendData(serial, sendData);
            
        }

        private void GPS_BT_Off_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=BT_OFF\r\n";
            SendData(serial, sendData);
            
        }

        private void WIFI_Scan_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=WIFI_SCAN\r\n";
            SendData(serial, sendData);
            
        }

        private void BT_Scan_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=BT_SCAN\r\n";
            SendData(serial, sendData);
            
        }

        

        private void IMEI_W_Click(object sender, RoutedEventArgs e)
        {
            //String sendData = "AT+TEST=SET_IMEI,";
            //if(Write_IMEI.Text.Length!=16)
            //{
            //    MessageBox.Show("IMEI号码长度不正确，请检查后重新输入！","警告");
            //}
            //else
            //{
            //    try
            //    {
            //        if (Write_IMEI.Text != "" && Regex.IsMatch(Write_IMEI.Text, @"^\d{16}$"))
            //        {
            //            sendData += Write_IMEI.Text + "\r\n";
            //            SendData(serial, sendData);
            //        }
            //        else MessageBox.Show("IMEI号码输入不正确，请检查后重新输入！", "警告");

            //    }
            //    catch
            //    {
                    
            //    }
               
            //}
            
        }

        private void SN_W_Click(object sender, RoutedEventArgs e)
        {
            //String sendData = "AT+TEST=SET_SN,";
            //if (Write_SN.Text.Length != 20)
            //{
            //    MessageBox.Show("SN号码长度不正确，请检查后重新输入！", "警告");
            //}
            //else
            //{
            //    try
            //    {
            //        if (Write_SN.Text != "" && Regex.IsMatch(Write_SN.Text, @"^\d{20}$"))
            //        {
            //            sendData += Write_SN.Text + "\r\n";
            //            SendData(serial, sendData);
            //        }
            //        else MessageBox.Show("SN号码输入不正确，请检查后重新输入！", "警告");
                   
            //    }
            //    catch
            //    {
                    
            //    }

            //}
        }

        private void Read_Phone_Num_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=SIM_ICCID\r\n";
            SendData(serial, sendData);
        }

        private void Read_SIM_Ready_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=SIM_READY\r\n";
            SendData(serial, sendData);
        }

        private void Read_SIM_IMSI_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=SIM_IMSI\r\n";
            SendData(serial, sendData);
        }

        private void Call_In_Test_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=CALL_IN,";
            if (Phone_Num_In.Text.Length != 11)
            {
                MessageBox.Show("电话号码长度不正确，请检查后重新输入！", "警告");
            }
            else
            {
                try
                {
                    if (Phone_Num_In.Text != "" && Regex.IsMatch(Phone_Num_In.Text, @"^\d{11}$"))
                    {
                        sendData += Phone_Num_In.Text + "\r\n";
                        SendData(serial, sendData);
                    }
                    else MessageBox.Show("电话号码输入不正确，请检查后重新输入！", "警告");


                }
                catch
                {
                    
                }

            }
        }

        private void Call_Out_Test_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=CALL_OUT,";
            if (Phone_Num_Out.Text.Length != 11)
            {
                MessageBox.Show("电话号码长度不正确，请检查后重新输入！", "警告");
            }
            else
            {
                try
                {
                    if (Phone_Num_Out.Text != "" && Regex.IsMatch(Phone_Num_Out.Text, @"^\d{11}$"))
                    {
                        sendData += Phone_Num_Out.Text + "\r\n";
                        SendData(serial, sendData);
                    }
                    else MessageBox.Show("电话号码输入不正确，请检查后重新输入！", "警告");

                }
                catch
                {
                    
                }

            }
        }

        private void Phone_Num_In_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            TextChange[] change = new TextChange[e.Changes.Count];
            e.Changes.CopyTo(change, 0);
            int offset = change[0].Offset;
            if (change[0].AddedLength > 0)
            {
                double num = 0;
                if (!Double.TryParse(textBox.Text, out num))
                {
                    textBox.Text = textBox.Text.Remove(offset, change[0].AddedLength);
                    textBox.Select(offset, 0);
                }
            }
        }

        private void Phone_Num_Out_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            TextChange[] change = new TextChange[e.Changes.Count];
            e.Changes.CopyTo(change, 0);
            int offset = change[0].Offset;
            if (change[0].AddedLength > 0)
            {
                double num = 0;
                if (!Double.TryParse(textBox.Text, out num))
                {
                    textBox.Text = textBox.Text.Remove(offset, change[0].AddedLength);
                    textBox.Select(offset, 0);
                }
            }
        }

        private void Write_IMEI_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            TextChange[] change = new TextChange[e.Changes.Count];
            e.Changes.CopyTo(change, 0);
            int offset = change[0].Offset;
            if (change[0].AddedLength > 0)
            {
                double num = 0;
                if (!Double.TryParse(textBox.Text, out num))
                {
                    textBox.Text = textBox.Text.Remove(offset, change[0].AddedLength);
                    textBox.Select(offset, 0);
                }
            }
        }

        private void Write_SN_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            TextChange[] change = new TextChange[e.Changes.Count];
            e.Changes.CopyTo(change, 0);
            int offset = change[0].Offset;
            if (change[0].AddedLength > 0)
            {
                double num = 0;
                if (!Double.TryParse(textBox.Text, out num))
                {
                    textBox.Text = textBox.Text.Remove(offset, change[0].AddedLength);
                    textBox.Select(offset, 0);
                }
            }
        }

        private void Phone_Num_Read_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            TextChange[] change = new TextChange[e.Changes.Count];
            e.Changes.CopyTo(change, 0);
            int offset = change[0].Offset;
            if (change[0].AddedLength > 0)
            {
                double num = 0;
                if (!Double.TryParse(textBox.Text, out num))
                {
                    textBox.Text = textBox.Text.Remove(offset, change[0].AddedLength);
                    textBox.Select(offset, 0);
                }
            }
        }

       
        private void baudRateCombobox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e )
        {
           if( e.Command == ApplicationCommands.Paste)
            {
                try
                {
                    IDataObject iData = Clipboard.GetDataObject();
                    if (iData.GetDataPresent(DataFormats.Text))
                    {
                        string buff = (string)iData.GetData(DataFormats.UnicodeText);
                       

                        if (Regex.IsMatch(buff, @"^\d+$"))
                        {
                            e.Handled = false;
                        }
                        else
                            e.Handled = true;

                        




                }
                }
                catch
                {
                    e.Handled = false;
                }
            }
            


        }

        private void baudRateCombobox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Key.ToString();
            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
                || (e.Key >= Key.D0 && e.Key <= Key.D9)
                || (e.Key == Key.Back)
                || (e.Key == Key.Left)
                || (e.Key==Key.Right)
                || (e.Key == Key.Delete)
                || ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.C)
                || ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.V)
                || ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.Z)
                || ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.X))
            {

                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void SendDat_Button_Click(object sender, RoutedEventArgs e)
        {
            if (hexadecimalSendCheckBox.IsChecked==true)
            {
            }
            else
            {
                String sendData = SendData_TxtBox.Text+"\r\n";
                SendData(serial, sendData);
            }
        }

        private void Clear_Send_Click(object sender, RoutedEventArgs e)
        {
            SendView_TxtBox.Text = "";
            sendBytesCount = 0;
            statusSendByteTextBlock.Text = sendBytesCount.ToString();
        }

        private void Clear_Read_Click(object sender, RoutedEventArgs e)
        {
            Rec_TxtBox.Text = "";
            receiveBytesCount = 0;
            statusReceiveByteTextBlock.Text = receiveBytesCount.ToString();
        }

        private void GSM_ON_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=GSM_CHK_ON\r\n";
            SendData(serial, sendData);
        }

        private void GSM_OFF_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=GSM_CHK_OFF\r\n";
            SendData(serial, sendData);
        }

        private void LBS_ON_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=LBS_ON\r\n";
            SendData(serial, sendData);
        }

        private void LBS_OFF_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=LBS_OFF\r\n";
            SendData(serial, sendData);
        }

        private void WRITE_TEST_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=WRITE_FLAG\r\n";
            SendData(serial, sendData);
        }

        private void READ_TEST_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=READ_FLAG\r\n";
            SendData(serial, sendData);
        }

        private void CLEAR_TEST_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=CLEAR_FLAG\r\n";
            SendData(serial, sendData);
        }

        private void AGING_TEST_Click(object sender, RoutedEventArgs e)
        {
            String sendData = "AT+TEST=AGING\r\n";
            SendData(serial, sendData);
        }

        private void READ_MODE_Click(object sender, RoutedEventArgs e)
        {
            
            String sendData = "AT+TEST=GET_MODE\r\n";
            SendData(serial, sendData);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(serial.IsOpen)
            {
                serial.Close();
                serial.Dispose();
            }
        }
    }
}
