using CHCNetSDK;
using System;
using System.Collections.Generic;
using System.Drawing;             // Podstawowa obsługa Bitmap
using System.Drawing.Imaging;     // Obsługa formatów pikseli (np. Format24bppRgb)
using System.Linq;
using System.Runtime.InteropServices; // Do pracy z pamięcią (Marshal, IntPtr)
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

//using static CHCNetSDK.CHCNet;
using static CHCNetSDK.PlayCtrl;
using static System.Diagnostics.Debug;

namespace Camera_WPF_HCNetSDK
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool state_playing;
        //private bool state_playing1;
        //private bool state_playing2;
        //private bool state_playing3;
        //private bool state_playingThermoVision;

        public int user_id_1 = -1;
        public int user_id_2 = -1;
        public int user_id_3 = -1;

        public IntPtr pUser1 = new IntPtr();
        public IntPtr pUser2 = new IntPtr();
        public IntPtr pUser3 = new IntPtr();
        public IntPtr pUserThermoVision = new IntPtr();

        public int play_handle_Central = 0;
        public int play_handleLeft = 0;
        public int play_handleRigth = 0;
        public int play_handle_thermovision = 0;

        int m_Port_Center_Camera = -1;
        int m_Port_Left_Camera = -1;
        int m_Port_Right_Camera = -1;
        int m_Port_Center_Camera_ThermoVision = -1;

        private CHCNetSDK.CHCNet.REALDATACALLBACK RealData1; // Przechowuj delegata, by GC go nie usunął
        private CHCNetSDK.CHCNet.REALDATACALLBACK RealData2; // Przechowuj delegata, by GC go nie usunął
        private CHCNetSDK.CHCNet.REALDATACALLBACK RealData3; // Przechowuj delegata, by GC go nie usunął
        private CHCNetSDK.CHCNet.REALDATACALLBACK RealDataThermoVision; // Przechowuj delegata, by GC go nie usunął

        private DECCBFUN m_DecCallback1; // Przechowuj delegata callbacku dekodowania
        private DECCBFUN m_DecCallback2; // Przechowuj delegata callbacku dekodowania
        private DECCBFUN m_DecCallback3; // Przechowuj delegata callbacku dekodowania
        private DECCBFUN m_DecCallbackThermoVision; // Przechowuj delegata callbacku dekodowania

        WriteableBitmap wbmp1 = new WriteableBitmap(1920, 1080, 96, 96, PixelFormats.Bgr24, null);  // Kamera centralna
        WriteableBitmap wbmp2 = new WriteableBitmap(1920, 1080, 96, 96, PixelFormats.Bgr24, null);  // Kamera Lewa
        WriteableBitmap wbmp3 = new WriteableBitmap(1920, 1080, 96, 96, PixelFormats.Bgr24, null);  // Kamera Prawa
        WriteableBitmap wbmpThermoVision = new WriteableBitmap(1280, 720, 96, 96, PixelFormats.Bgr24, null);    // Termowizja

        private byte[] rawBuffer1;
        private byte[] rawBuffer2;
        private byte[] rawBuffer3;
        private byte[] rawBufferThermo;

        //public delegate void MSGExceptionCallBack(uint dwType, int lUserID, int lHandle, IntPtr pUser);
        //private CHCNetSDK.CHCNet.EXCEPYIONCALLBACK m_ExceptionCB = null;
        public MainWindow()
        {
            InitializeComponent();
            System.Console.WriteLine("Log: Start Aplikacji " + DateTime.Now);
            CHCNetSDK.CHCNet.NET_DVR_Init();

            //m_ExceptionCB = new CHCNetSDK.CHCNet.EXCEPYIONCALLBACK(ExceptionCallBackDetails);
            //CHCNetSDK.CHCNet.NET_DVR_SetExceptionCallBack_V30(0, IntPtr.Zero, m_ExceptionCB, IntPtr.Zero);
            //CHCNetSDK.CHCNet.NET_DVR_SetConnectTime(5000, 1);
            //CHCNetSDK.CHCNet.NET_DVR_SetReconnect(5000, 1);

            this.StateChanged += MainWindow_StateChanged;

            string DVRIPAddress1 = "192.168.1.64";
            string DVRIPAddress2 = "192.168.1.65";
            string DVRIPAddress3 = "192.168.1.66";

            Int16 DVRPortNumber = Int16.Parse("8000");
            string DVRUserName = "admin";
            string DVRPassword = "1qaz2wsx";

            CHCNetSDK.CHCNet.NET_DVR_DEVICEINFO_V30 DeviceInfo1 = new CHCNetSDK.CHCNet.NET_DVR_DEVICEINFO_V30();
            CHCNetSDK.CHCNet.NET_DVR_DEVICEINFO_V30 DeviceInfo2 = new CHCNetSDK.CHCNet.NET_DVR_DEVICEINFO_V30();
            CHCNetSDK.CHCNet.NET_DVR_DEVICEINFO_V30 DeviceInfo3 = new CHCNetSDK.CHCNet.NET_DVR_DEVICEINFO_V30();

            user_id_1 = CHCNetSDK.CHCNet.NET_DVR_Login_V30(DVRIPAddress1, DVRPortNumber, DVRUserName, DVRPassword, ref DeviceInfo1);
            user_id_2 = CHCNetSDK.CHCNet.NET_DVR_Login_V30(DVRIPAddress2, DVRPortNumber, DVRUserName, DVRPassword, ref DeviceInfo2);
            user_id_3 = CHCNetSDK.CHCNet.NET_DVR_Login_V30(DVRIPAddress3, DVRPortNumber, DVRUserName, DVRPassword, ref DeviceInfo3);


            Task.Run(() =>
            {
                // Praca w tle
                System.Diagnostics.Debug.WriteLine($"Uruchomienie wątku");
                CameraPresenceControl(user_id_1, "Kamera Lewa");
            });

            RealData1 = new CHCNetSDK.CHCNet.REALDATACALLBACK(RealDataCallback1);
            RealData2 = new CHCNetSDK.CHCNet.REALDATACALLBACK(RealDataCallback2);
            RealData3 = new CHCNetSDK.CHCNet.REALDATACALLBACK(RealDataCallback3);
            RealDataThermoVision = new CHCNetSDK.CHCNet.REALDATACALLBACK(RealDataCallbackThermoVision);

            m_DecCallback1 = new DECCBFUN(DecCallback1);
            m_DecCallback2 = new DECCBFUN(DecCallback2);
            m_DecCallback3 = new DECCBFUN(DecCallback3);
            m_DecCallbackThermoVision = new DECCBFUN(DecCallbackThermoVision);
        }

        private async void CameraPresenceControl(int userId, string cameraName)
        {
            while (true)
            {
                // Próba pobrania statusu (lekkie zapytanie)
                System.Diagnostics.Debug.WriteLine($"Wątek {userId} {cameraName} RUN " + DateTime.Now);
                bool isAlive = CheckCameraStatus(userId);

                if (!isAlive)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Reakcja natychmiastowa!
                        System.Diagnostics.Debug.WriteLine($"Kamera {userId} {cameraName} rozłączona! " + DateTime.Now);
                    });
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Reakcja natychmiastowa!
                        System.Diagnostics.Debug.WriteLine($"Kamera {userId} {cameraName} podłączona!");
                    });
                }

                await Task.Delay(10000); // Sprawdzaj co 1 sekundę
            }
        }

        private bool CheckCameraStatus(int userId)
        {
            // Wywołujemy dowolną prostą funkcję informacyjną
            CHCNetSDK.CHCNet.NET_DVR_TIME time = new CHCNetSDK.CHCNet.NET_DVR_TIME();
            uint dwReturn = 0;
            int nSize = Marshal.SizeOf(time);
            IntPtr lpOutBuffer = Marshal.AllocHGlobal(nSize);
            IntPtr lpInBuffer = Marshal.AllocHGlobal(nSize);
            IntPtr lpStatusList = Marshal.AllocHGlobal(nSize);

            // Próba pobrania czasu kamery - jeśli zwróci false, kamera nie odpowiada
            //            bool result = CHCNetSDK.CHCNet.NET_DVR_GetConfig(userId, CHCNetSDK.NET_DVR_GET_TIMECFG, 1, lpOutBuffer, (uint)nSize, ref dwReturn);
            bool result = CHCNetSDK.CHCNet.NET_DVR_GetDeviceConfig(userId, CHCNetSDK.CHCNet.NET_DVR_GET_TIMECFG, 1, lpInBuffer, (uint)nSize, lpStatusList, lpOutBuffer, (uint)nSize);

            Marshal.FreeHGlobal(lpOutBuffer);
            Marshal.FreeHGlobal(lpInBuffer);
            Marshal.FreeHGlobal(lpStatusList);
            return result;
        }

        //private void ExceptionCallBackDetails(uint dwType, int lUserID, int lHandle, IntPtr pUser)
        //{
        //    if(lUserID == user_id_1)
        //    {
        //        switch (dwType)
        //        {
        //            case CHCNetSDK.CHCNet.EXCEPTION_RECONNECT: // Kamera się rozłączyła i próbuje nawiązać połączenie
        //                System.Diagnostics.Debug.WriteLine("Log1: Kamera Rozłączona - " + DateTime.Now);
        //                break;
        //            case CHCNetSDK.CHCNet.EXCEPTION_ALARMRECONNECT: // Połączenie przywrócone
        //                System.Diagnostics.Debug.WriteLine("Log1: Kamera Podłączona - " + DateTime.Now);
        //                break;
        //            default:
        //                System.Diagnostics.Debug.WriteLine("Log1: Iny Błąd - " + DateTime.Now);
        //                break;
        //        }
        //    }
        //    if (lUserID == user_id_2)
        //    {
        //        switch (dwType)
        //        {
        //            case CHCNetSDK.CHCNet.EXCEPTION_RECONNECT: // Kamera się rozłączyła i próbuje nawiązać połączenie
        //                System.Diagnostics.Debug.WriteLine("Log2: Kamera Rozłączona - " + DateTime.Now);
        //                break;
        //            case CHCNetSDK.CHCNet.EXCEPTION_ALARMRECONNECT: // Połączenie przywrócone
        //                System.Diagnostics.Debug.WriteLine("Log2: Kamera Podłączona - " + DateTime.Now);
        //                break;
        //            default:
        //                System.Diagnostics.Debug.WriteLine("Log2: Iny Błąd - " + DateTime.Now);
        //                break;
        //        }
        //    }
        //    if (lUserID == user_id_3)
        //    {
        //        switch (dwType)
        //        {
        //            case CHCNetSDK.CHCNet.EXCEPTION_RECONNECT: // Kamera się rozłączyła i próbuje nawiązać połączenie
        //                System.Diagnostics.Debug.WriteLine("Log3: Kamera Rozłączona - " + DateTime.Now);
        //                break;
        //            case CHCNetSDK.CHCNet.EXCEPTION_ALARMRECONNECT: // Połączenie przywrócone
        //                System.Diagnostics.Debug.WriteLine("Log3: Kamera Podłączona - " + DateTime.Now);
        //                break;
        //            default:
        //                System.Diagnostics.Debug.WriteLine("Log3: Iny Błąd - " + DateTime.Now);
        //                break;
        //        }
        //    }
        //}

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                // Dodajemy margines równy grubości niewidocznej ramki systemowej
                this.BorderThickness = new System.Windows.Thickness(8);
            }
            else
            {
                this.BorderThickness = new System.Windows.Thickness(0);
            }
        }
        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            if (state_playing)
            {
                PlayM4_Stop(m_Port_Center_Camera_ThermoVision);
                PlayM4_Stop(m_Port_Center_Camera);
                PlayM4_Stop(m_Port_Left_Camera);
                PlayM4_Stop(m_Port_Right_Camera);

                PlayM4_CloseStream(m_Port_Center_Camera_ThermoVision);
                PlayM4_CloseStream(m_Port_Center_Camera);
                PlayM4_CloseStream(m_Port_Left_Camera);
                PlayM4_CloseStream(m_Port_Right_Camera);

                CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handle_thermovision);
                CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handle_Central);
                CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handleLeft);
                CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handleRigth);

                state_playing = false;
                ButtonStart.Content = "Start";
                ButtonStart.Background = System.Windows.Media.Brushes.Green;
            }
            else
            {
                ButtonStart.Content = "Stop";
                ButtonStart.Background = System.Windows.Media.Brushes.Red;

                CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO lpPreviewInfo1 = new CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO() // Kamera centralna
                {
                    hPlayWnd = IntPtr.Zero,
                    lChannel = Int16.Parse("1"),
                    dwStreamType = 0,   // 0-główny, 1-podstrumień
                    dwLinkMode = 0,
                    bBlocked = true,
                    dwDisplayBufNum = 1,
                    byProtoType = 0,
                    byPreviewMode = 0
                };

                CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO lpPreviewInfo2 = new CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO() // Kamera lewa
                {
                    hPlayWnd = IntPtr.Zero,
                    lChannel = Int16.Parse("1"),
                    dwStreamType = 0,   // 0-główny, 1-podstrumień
                    dwLinkMode = 0,
                    bBlocked = true,
                    dwDisplayBufNum = 1,
                    byProtoType = 0,
                    byPreviewMode = 0
                };

                CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO lpPreviewInfo3 = new CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO() // Kamera prawa
                {
                    hPlayWnd = IntPtr.Zero,
                    lChannel = Int16.Parse("1"),
                    dwStreamType = 0,   // 0-główny, 1-podstrumień
                    dwLinkMode = 0,
                    bBlocked = true,
                    dwDisplayBufNum = 1,
                    byProtoType = 0,
                    byPreviewMode = 0
                };

                CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO lpPreviewInfoThermoVision = new CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO() // Kamera centralna - termowizja
                {
                    hPlayWnd = IntPtr.Zero,
                    lChannel = Int16.Parse("2"),
                    dwStreamType = 0,   // 0-główny, 1-podstrumień
                    dwLinkMode = 1,
                    bBlocked = true,
                    dwDisplayBufNum = 1,
                    byProtoType = 0,
                    byPreviewMode = 0
                };

                play_handle_thermovision = CHCNetSDK.CHCNet.NET_DVR_RealPlay_V40(user_id_1, ref lpPreviewInfoThermoVision, RealDataThermoVision, pUserThermoVision);
                play_handle_Central = CHCNetSDK.CHCNet.NET_DVR_RealPlay_V40(user_id_1, ref lpPreviewInfo1, RealData1, pUser1);
                play_handleLeft = CHCNetSDK.CHCNet.NET_DVR_RealPlay_V40(user_id_2, ref lpPreviewInfo2, RealData2, pUser2);
                play_handleRigth = CHCNetSDK.CHCNet.NET_DVR_RealPlay_V40(user_id_3, ref lpPreviewInfo3, RealData3, pUser3);

                // Rozpoczęcie podglądu
                MainCamera.Source = wbmpThermoVision;
                CenterCameraSmall.Source = wbmp1;
                RightCameraSmall.Source = wbmp2;
                LeftCameraSmall.Source = wbmp3;

                state_playing = true;
            }

        }
        private void RealDataCallback1(int lRealHandle, uint dwDataType, IntPtr pBuffer, uint dwBufSize, IntPtr pUser)
        {
            switch (dwDataType)
            {
                case CHCNetSDK.CHCNet.NET_DVR_SYSHEAD:
                    if (!PlayM4_GetPort(ref m_Port_Center_Camera)) 
                        return;

                    if (dwBufSize > 0)
                    {
                        if (PlayM4_OpenStream(m_Port_Center_Camera, pBuffer, dwBufSize, 1024 * 1024))
                        {
                            PlayM4_SetDecCallBack(m_Port_Center_Camera, m_DecCallback1);

                            try
                            {
                                PlayM4_Play(m_Port_Center_Camera, IntPtr.Zero);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Błąd: " + ex.Message);
                            }
                        }
                    }
                    break;

                case CHCNetSDK.CHCNet.NET_DVR_STREAMDATA: // Dane wideo
                    if (m_Port_Center_Camera != -1 && dwBufSize > 0)
                    {
                        //Console.WriteLine("RealDataCallback1 - STREAMDATA");
                        try
                        {
                            PlayM4_InputData(m_Port_Center_Camera, pBuffer, dwBufSize);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Błąd: " + ex.Message);
                        }
                    }
                    break;
            }
        }
        private void RealDataCallback2(int lRealHandle, uint dwDataType, IntPtr pBuffer, uint dwBufSize, IntPtr pUser)
        {
            switch (dwDataType)
            {
                case CHCNetSDK.CHCNet.NET_DVR_SYSHEAD:
                    if (!PlayM4_GetPort(ref m_Port_Left_Camera)) 
                        return;

                    if (dwBufSize > 0)
                    {
                        if (PlayM4_OpenStream(m_Port_Left_Camera, pBuffer, dwBufSize, 1024 * 1024))
                        {
                            PlayM4_SetDecCallBack(m_Port_Left_Camera, m_DecCallback2);

                            try
                            {
                                PlayM4_Play(m_Port_Left_Camera, IntPtr.Zero);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Błąd: " + ex.Message);
                            }
                        }
                    }
                    break;

                case CHCNetSDK.CHCNet.NET_DVR_STREAMDATA: // Dane wideo
                    if (m_Port_Left_Camera != -1 && dwBufSize > 0)
                    {
                        //Console.WriteLine("RealDataCallback2 - STREAMDATA");
                        try
                        {
                            PlayM4_InputData(m_Port_Left_Camera, pBuffer, dwBufSize);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Błąd: " + ex.Message);
                        }
                    }
                    break;
            }
        }
        private void RealDataCallback3(int lRealHandle, uint dwDataType, IntPtr pBuffer, uint dwBufSize, IntPtr pUser)
        {
            switch (dwDataType)
            {
                case CHCNetSDK.CHCNet.NET_DVR_SYSHEAD:
                    if (!PlayM4_GetPort(ref m_Port_Right_Camera)) 
                        return;

                    if (dwBufSize > 0)
                    {
                        if (PlayM4_OpenStream(m_Port_Right_Camera, pBuffer, dwBufSize, 1024 * 1024))
                        {
                            PlayM4_SetDecCallBack(m_Port_Right_Camera, m_DecCallback3);

                            try
                            {
                                PlayM4_Play(m_Port_Right_Camera, IntPtr.Zero);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Błąd: " + ex.Message);
                            }
                        }
                    }
                    break;

                case CHCNetSDK.CHCNet.NET_DVR_STREAMDATA: // Dane wideo
                    if (m_Port_Right_Camera != -1 && dwBufSize > 0)
                    {
                        //Console.WriteLine("RealDataCallback3 - STREAMDATA");
                        try
                        {
                            PlayM4_InputData(m_Port_Right_Camera, pBuffer, dwBufSize);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Błąd: " + ex.Message);
                        }
                    }
                    break;
            }
        }
        private void RealDataCallbackThermoVision(int lRealHandle, uint dwDataType, IntPtr pBuffer, uint dwBufSize, IntPtr pUser)
        {
            switch (dwDataType)
            {
                case CHCNetSDK.CHCNet.NET_DVR_SYSHEAD:
                    if (!PlayM4_GetPort(ref m_Port_Center_Camera_ThermoVision)) return;

                    if (dwBufSize > 0)
                    {
                        if (PlayM4_OpenStream(m_Port_Center_Camera_ThermoVision, pBuffer, dwBufSize, 1024 * 1024))
                        {
                            PlayM4_SetDecCallBack(m_Port_Center_Camera_ThermoVision, m_DecCallbackThermoVision);

                            try
                            {
                                PlayM4_Play(m_Port_Center_Camera_ThermoVision, IntPtr.Zero);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Błąd: " + ex.Message);
                            }
                        }
                    }
                    break;

                case CHCNetSDK.CHCNet.NET_DVR_STREAMDATA: // Dane wideo
                    if (m_Port_Center_Camera_ThermoVision != -1 && dwBufSize > 0)
                    {
                        //Console.WriteLine("RealDataCallbackThermoVision - STREAMDATA");
                        try
                        {
                            PlayM4_InputData(m_Port_Center_Camera_ThermoVision, pBuffer, dwBufSize);
                            //Console.WriteLine("RealDataCallbackThermoVision - PlayM4_InputData");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Błąd: " + ex.Message);
                        }
                    }
                    break;
            }
        }

        private void DecCallback1(int nPort, IntPtr pBuf, int nSize, ref PlayCtrl.FRAME_INFO pFrameInfo, int nUser, int nReserved2)
        {
            if (m_Port_Center_Camera == -1 || nSize <= 0 || pBuf == IntPtr.Zero)
                return;

            int width = pFrameInfo.nWidth;
            int height = pFrameInfo.nHeight;
            int expectedRgbSize = width * height * 3;

            if (rawBuffer1 == null || rawBuffer1.Length != nSize)
                rawBuffer1 = new byte[nSize];

            Marshal.Copy(pBuf, rawBuffer1, 0, nSize);
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!state_playing || wbmp1 == null)
                    return;

                try
                {
                    wbmp1.Lock();
                    // Kopiujemy już gotowe dane RGB do naszej bitmapy WPF
                    ConvertYV12ToRGB(rawBuffer1, wbmp1.BackBuffer, width, height);
                    wbmp1.AddDirtyRect(new Int32Rect(0, 0, width, height));
                    //Console.WriteLine("DecCallback1");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Błąd: " + ex.Message);
                }
                finally
                {
                    wbmp1.Unlock();
                }
            }));
        }
        private void DecCallback2(int nPort, IntPtr pBuf, int nSize, ref PlayCtrl.FRAME_INFO pFrameInfo, int nUser, int nReserved2)
        {
            if (m_Port_Left_Camera == -1 || nSize <= 0 || pBuf == IntPtr.Zero)
                return;

            int width = pFrameInfo.nWidth;
            int height = pFrameInfo.nHeight;
            int expectedRgbSize = width * height * 3;

            if (rawBuffer2 == null || rawBuffer2.Length != nSize)
                rawBuffer2 = new byte[nSize];

            Marshal.Copy(pBuf, rawBuffer2, 0, nSize);
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!state_playing || wbmp2 == null)
                    return;

                try
                {
                    wbmp2.Lock();
                    // Kopiujemy już gotowe dane RGB do naszej bitmapy WPF
                    ConvertYV12ToRGB(rawBuffer2, wbmp2.BackBuffer, width, height);
                    wbmp2.AddDirtyRect(new Int32Rect(0, 0, width, height));
                    //Console.WriteLine("DecCallback2");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Błąd: " + ex.Message);
                }
                finally
                {
                    wbmp2.Unlock();
                }
            }));
        }
        private void DecCallback3(int nPort, IntPtr pBuf, int nSize, ref PlayCtrl.FRAME_INFO pFrameInfo, int nUser, int nReserved2)
        {
            if (m_Port_Right_Camera == -1 || nSize <= 0 || pBuf == IntPtr.Zero)
                return;

            int width = pFrameInfo.nWidth;
            int height = pFrameInfo.nHeight;
            int expectedRgbSize = width * height * 3;

            if (rawBuffer3 == null || rawBuffer3.Length != nSize)
                rawBuffer3 = new byte[nSize];

            Marshal.Copy(pBuf, rawBuffer3, 0, nSize);
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!state_playing || wbmp3 == null)
                    return;

                try
                {
                    wbmp3.Lock();
                    // Kopiujemy już gotowe dane RGB do naszej bitmapy WPF
                    ConvertYV12ToRGB(rawBuffer3, wbmp3.BackBuffer, width, height);
                    wbmp3.AddDirtyRect(new Int32Rect(0, 0, width, height));
                    //Console.WriteLine("DecCallback3");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Błąd: " + ex.Message);
                }
                finally
                {
                    wbmp3.Unlock();
                }
            }));
        }
        private void DecCallbackThermoVision(int nPort, IntPtr pBuf, int nSize, ref PlayCtrl.FRAME_INFO pFrameInfo, int nUser, int nReserved2)
        {
            if (m_Port_Center_Camera_ThermoVision == -1 || nSize <= 0 || pBuf == IntPtr.Zero)
            {
                Console.WriteLine("DecCallbackTV Begin");
                return;
            }

            int width = pFrameInfo.nWidth;
            int height = pFrameInfo.nHeight;
            int expectedRgbSize = width * height * 3;

            if (rawBufferThermo == null || rawBufferThermo.Length != nSize)
                rawBufferThermo = new byte[nSize];

            Marshal.Copy(pBuf, rawBufferThermo, 0, nSize);
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!state_playing || wbmpThermoVision == null)
                {
                    Console.WriteLine("DecCallbackTV Return");
                    return;
                }

                try
                {
                    wbmpThermoVision.Lock();
                    // Kopiujemy już gotowe dane RGB do naszej bitmapy WPF
                    ConvertYV12ToRGB(rawBufferThermo, wbmpThermoVision.BackBuffer, width, height);
                    wbmpThermoVision.AddDirtyRect(new Int32Rect(0, 0, width, height));
                    //Console.WriteLine("DecCallbackTV");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Błąd: " + ex.Message);
                }
                finally
                {
                    //Console.WriteLine("DecCallbackTV Unlock");
                    wbmpThermoVision.Unlock();
                }
            }));
        }
        private void ConvertYV12ToRGB(byte[] pBuf, IntPtr pDest, int width, int height)
        {
            int frameSize = width * height;
            int chromaSize = frameSize >> 2;

            unsafe
            {
                fixed (byte* pSrc = pBuf) // Blokujemy tablicę w pamięci na czas konwersji
                {
                    byte* yPtr = pSrc;
                    byte* vPtr = yPtr + frameSize;
                    byte* uPtr = vPtr + chromaSize;
                    byte* dPtr = (byte*)pDest;

                    // Przetwarzanie równoległe na wielu rdzeniach
                    Parallel.For(0, height, i =>
                    {
                        int yRowOffset = i * width;
                        int uvRowOffset = (i >> 1) * (width >> 1);
                        int destRowOffset = i * width * 3;

                        for (int j = 0; j < width; j++)
                        {
                            int Y = yPtr[yRowOffset + j];
                            int uvIdx = uvRowOffset + (j >> 1);
                            int V = vPtr[uvIdx] - 128;
                            int U = uPtr[uvIdx] - 128;

                            // Szybka konwersja na liczbach całkowitych (Shift zamiast Float)
                            int r = Y + ((V * 1436) >> 10);
                            int g = Y - ((U * 352 + V * 731) >> 10);
                            int b = Y + ((U * 1814) >> 10);

                            int pixelIdx = destRowOffset + (j * 3);

                            // Super szybki Clamp
                            dPtr[pixelIdx] = (byte)(b < 0 ? 0 : (b > 255 ? 255 : b)); // B
                            dPtr[pixelIdx + 1] = (byte)(g < 0 ? 0 : (g > 255 ? 255 : g)); // G
                            dPtr[pixelIdx + 2] = (byte)(r < 0 ? 0 : (r > 255 ? 255 : r)); // R
                        }
                    });
                }
            }
        }
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        // Zamykanie okna
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            PlayM4_Stop(m_Port_Center_Camera_ThermoVision);
            PlayM4_Stop(m_Port_Center_Camera);
            PlayM4_Stop(m_Port_Left_Camera);
            PlayM4_Stop(m_Port_Right_Camera);

            PlayM4_CloseStream(m_Port_Center_Camera_ThermoVision);
            PlayM4_CloseStream(m_Port_Center_Camera);
            PlayM4_CloseStream(m_Port_Left_Camera);
            PlayM4_CloseStream(m_Port_Right_Camera);

            CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(m_Port_Center_Camera_ThermoVision);
            CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handle_Central);
            CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handleLeft);
            CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handleRigth);

            CHCNetSDK.CHCNet.NET_DVR_Logout(user_id_1);
            CHCNetSDK.CHCNet.NET_DVR_Logout(user_id_2);
            CHCNetSDK.CHCNet.NET_DVR_Logout(user_id_3);

            CHCNetSDK.CHCNet.NET_DVR_Cleanup();
            
            this.Close();
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                // Jeśli okno jest już zmaksymalizowane, przywróć je do normalnego rozmiaru
                this.WindowState = WindowState.Normal;
            }
            else
            {
                // W przeciwnym razie zmaksymalizuj
                this.WindowState = WindowState.Maximized;
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                // Jeśli okno jest już zmaksymalizowane, przywróć je do normalnego rozmiaru
                this.WindowState = WindowState.Normal;
            }
            else
            {
                // W przeciwnym razie zmaksymalizuj
                this.WindowState = WindowState.Minimized;
            }
        }

        private void Image_LeftCameraMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LeftCameraSmall.Visibility = Visibility.Collapsed;
            RightCameraSmall.Visibility = Visibility.Collapsed;
            CenterCameraSmall.Visibility = Visibility.Collapsed;
            MainCamera.Visibility = Visibility.Collapsed;
            BigMainCamera.Visibility = Visibility.Visible;
            BigMainCamera.Source = wbmp3;
        }
        private void Image_RightCameraMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LeftCameraSmall.Visibility = Visibility.Collapsed;
            RightCameraSmall.Visibility = Visibility.Collapsed;
            CenterCameraSmall.Visibility = Visibility.Collapsed;
            MainCamera.Visibility = Visibility.Collapsed;
            BigMainCamera.Visibility = Visibility.Visible;
            BigMainCamera.Source = wbmp2;
        }
        private void Image_CenterCameraMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LeftCameraSmall.Visibility = Visibility.Collapsed;
            RightCameraSmall.Visibility = Visibility.Collapsed;
            CenterCameraSmall.Visibility = Visibility.Collapsed;
            MainCamera.Visibility = Visibility.Collapsed;
            BigMainCamera.Visibility = Visibility.Visible;
            BigMainCamera.Source = wbmp1;
        }
        private void Image_CenterCameraTermovisionMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LeftCameraSmall.Visibility = Visibility.Collapsed;
            RightCameraSmall.Visibility = Visibility.Collapsed;
            CenterCameraSmall.Visibility = Visibility.Collapsed;
            MainCamera.Visibility = Visibility.Collapsed;
            BigMainCamera.Visibility = Visibility.Visible;
            BigMainCamera.Source = wbmpThermoVision;
        }
        private void Image_BigMainCameraTermovisionMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Rozpoczęcie podglądu
            MainCamera.Source = wbmpThermoVision;
            CenterCameraSmall.Source = wbmp1;
            RightCameraSmall.Source = wbmp2;
            LeftCameraSmall.Source = wbmp3;

            LeftCameraSmall.Visibility = Visibility.Visible;
            RightCameraSmall.Visibility = Visibility.Visible;
            CenterCameraSmall.Visibility = Visibility.Visible;
            MainCamera.Visibility = Visibility.Visible;

            BigMainCamera.Visibility = Visibility.Collapsed;
            BigMainCamera.Source = null;
            state_playing = true;
        }
    }
}

