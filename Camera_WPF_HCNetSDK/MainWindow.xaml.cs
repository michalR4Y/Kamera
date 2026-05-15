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
using static CHCNetSDK.CHCNet;


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

        public int CentralCamera_Id = -1;
        public int LeftCamera_Id = -1;
        public int RigthCamera_Id = -1;

        public IntPtr pUser1 = new IntPtr();
        public IntPtr pUser2 = new IntPtr();
        public IntPtr pUser3 = new IntPtr();
        public IntPtr pUserThermoVision = new IntPtr();

        public int play_handle_Central = 0;
        public int play_handleLeft = 0;
        public int play_handleRigth = 0;
        public int play_handle_ThermoVision = 0;

        int m_Port_Center_Camera = -1;
        int m_Port_Left_Camera = -1;
        int m_Port_Right_Camera = -1;
        int m_Port_Center_Camera_ThermoVision = -1;

        private CHCNetSDK.CHCNet.REALDATACALLBACK RealDataCentralCamera; // Przechowuj delegata, by GC go nie usunął
        private CHCNetSDK.CHCNet.REALDATACALLBACK RealDataLeftCamera; // Przechowuj delegata, by GC go nie usunął
        private CHCNetSDK.CHCNet.REALDATACALLBACK RealDataRigthCamera; // Przechowuj delegata, by GC go nie usunął
        private CHCNetSDK.CHCNet.REALDATACALLBACK RealDataThermoVision; // Przechowuj delegata, by GC go nie usunął

        private DECCBFUN m_DecCentralCameraCallback; // Przechowuj delegata callbacku dekodowania
        private DECCBFUN m_DecLeftCameraCallback; // Przechowuj delegata callbacku dekodowania
        private DECCBFUN m_DecRigthCameraCallback; // Przechowuj delegata callbacku dekodowania
        private DECCBFUN m_DecCallbackThermoVision; // Przechowuj delegata callbacku dekodowania

        WriteableBitmap CentralCameraWBMP = new WriteableBitmap(1920, 1080, 96, 96, PixelFormats.Bgr24, null);  // Kamera centralna
        WriteableBitmap LeftCameraWBMP = new WriteableBitmap(1920, 1080, 96, 96, PixelFormats.Bgr24, null);  // Kamera Lewa
        WriteableBitmap RigthCameraWBMP = new WriteableBitmap(1920, 1080, 96, 96, PixelFormats.Bgr24, null);  // Kamera Prawa
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

            string DVRIPAddressCentralCamera = "192.168.1.64";
            string DVRIPAddressLeftCamera    = "192.168.1.65";
            string DVRIPAddressRigthCamera   = "192.168.1.66";

            Int16 DVRPortNumber = Int16.Parse("8000");
            string DVRUserName  = "admin";
            string DVRPassword  = "1qaz2wsx";

            CHCNetSDK.CHCNet.NET_DVR_DEVICEINFO_V30 CentralCameraInfo = new CHCNetSDK.CHCNet.NET_DVR_DEVICEINFO_V30();
            CHCNetSDK.CHCNet.NET_DVR_DEVICEINFO_V30 LeftCameraInfo = new CHCNetSDK.CHCNet.NET_DVR_DEVICEINFO_V30();
            CHCNetSDK.CHCNet.NET_DVR_DEVICEINFO_V30 RigthCameraInfo = new CHCNetSDK.CHCNet.NET_DVR_DEVICEINFO_V30();

            CentralCamera_Id = CHCNetSDK.CHCNet.NET_DVR_Login_V30(DVRIPAddressCentralCamera, DVRPortNumber, DVRUserName, DVRPassword, ref CentralCameraInfo);
            LeftCamera_Id = CHCNetSDK.CHCNet.NET_DVR_Login_V30(DVRIPAddressLeftCamera, DVRPortNumber, DVRUserName, DVRPassword, ref LeftCameraInfo);
            RigthCamera_Id = CHCNetSDK.CHCNet.NET_DVR_Login_V30(DVRIPAddressRigthCamera, DVRPortNumber, DVRUserName, DVRPassword, ref RigthCameraInfo);

            RealDataCentralCamera = new CHCNetSDK.CHCNet.REALDATACALLBACK(RealDataCentralCameraCallback);
            RealDataLeftCamera = new CHCNetSDK.CHCNet.REALDATACALLBACK(RealDataLeftCameraCallback);
            RealDataRigthCamera = new CHCNetSDK.CHCNet.REALDATACALLBACK(RealDataRigthCameraCallback);
            RealDataThermoVision = new CHCNetSDK.CHCNet.REALDATACALLBACK(RealDataCallbackThermoVision);

            m_DecCentralCameraCallback = new DECCBFUN(DecCentralCameraCallback);
            m_DecLeftCameraCallback = new DECCBFUN(DecLeftCameraCallback);
            m_DecRigthCameraCallback = new DECCBFUN(DecRigthCameraCallback);
            m_DecCallbackThermoVision = new DECCBFUN(DecCallbackThermoVision);
        }

        private async void CameraPresenceControl(int cameraId, string cameraName)
        {
            while (true)
            {
                await Task.Delay(1000); // Sprawdzaj co 1 sekundę

                CHCNetSDK.CHCNet.NET_DVR_WORKSTATE_V30 struWorkState = new CHCNetSDK.CHCNet.NET_DVR_WORKSTATE_V30();
                int nSize = Marshal.SizeOf(struWorkState);
                IntPtr ptrWorkState = Marshal.AllocHGlobal(nSize);
                Marshal.StructureToPtr(struWorkState, ptrWorkState, false);

                bool result = CHCNetSDK.CHCNet.NET_DVR_GetDVRWorkState_V30(cameraId, ptrWorkState);
                uint errorCode = CHCNetSDK.CHCNet.NET_DVR_GetLastError();

                if (cameraId == RigthCamera_Id)
                {
                    if (result)
                    {
                        Console.WriteLine(cameraName + ": " + cameraId + " działa");
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            RightCameraError.Visibility = Visibility.Hidden;
                            RightCameraSmall.Visibility = Visibility.Visible;
                        }));
                    }
                    else
                    {
                        Console.WriteLine(cameraName + ": " + cameraId + " nie działa. Błąd: + " + errorCode);
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            RightCameraError.Visibility = Visibility.Visible;
                            RightCameraSmall.Visibility = Visibility.Hidden;
                        }));
                    }
                }

                if (cameraId == CentralCamera_Id)
                {
                    if (result)
                    {
                        Console.WriteLine(cameraName + ": " + cameraId + " działa");
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            CentralCameraError.Visibility = Visibility.Hidden;
                        }));
                    }
                    else
                    {
                        Console.WriteLine(cameraName + ": " + cameraId + " nie działa. Błąd: + " + errorCode);
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            CentralCameraError.Visibility = Visibility.Visible;
                        }));
                    }
                }

                if (cameraId == LeftCamera_Id)
                {
                    if (result)
                    {
                        Console.WriteLine(cameraName + ": " + cameraId + " działa");
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            LeftCameraError.Visibility = Visibility.Hidden;
                        }));
                    }
                    else
                    {
                        Console.WriteLine(cameraName + ": " + cameraId + " nie działa. Błąd: + " + errorCode);
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            LeftCameraError.Visibility = Visibility.Visible;
                        }));
                    }
                }
            }
        }

        private bool CheckCameraStatus(int userId)
        {
            // Wywołujemy dowolną prostą funkcję informacyjną
            CHCNetSDK.CHCNet.NET_DVR_TIME time = new CHCNetSDK.CHCNet.NET_DVR_TIME();
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

                CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handle_ThermoVision);
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

                play_handle_ThermoVision = CHCNetSDK.CHCNet.NET_DVR_RealPlay_V40(CentralCamera_Id, ref lpPreviewInfoThermoVision, RealDataThermoVision, pUserThermoVision);
                play_handle_Central = CHCNetSDK.CHCNet.NET_DVR_RealPlay_V40(CentralCamera_Id, ref lpPreviewInfo1, RealDataCentralCamera, pUser1);
                play_handleLeft = CHCNetSDK.CHCNet.NET_DVR_RealPlay_V40(LeftCamera_Id, ref lpPreviewInfo2, RealDataLeftCamera, pUser2);
                play_handleRigth = CHCNetSDK.CHCNet.NET_DVR_RealPlay_V40(RigthCamera_Id, ref lpPreviewInfo3, RealDataRigthCamera, pUser3);

                // Rozpoczęcie podglądu
                MainCamera.Source = wbmpThermoVision;
                CentralCameraSmall.Source = CentralCameraWBMP;
                RightCameraSmall.Source = RigthCameraWBMP;
                LeftCameraSmall.Source = LeftCameraWBMP;

                Task.Run(() =>
                {
                    // Praca w tle
                    CameraPresenceControl(CentralCamera_Id, "Kamera Centralna");
                });
                Task.Run(() =>
                {
                    // Praca w tle
                    CameraPresenceControl(LeftCamera_Id, "Kamera Lewa");
                });
                Task.Run(() =>
                {
                    // Praca w tle
                    CameraPresenceControl(RigthCamera_Id, "Kamera Prawa");
                });

                state_playing = true;
            }

        }
        private void RealDataCentralCameraCallback(int lRealHandle, uint dwDataType, IntPtr pBuffer, uint dwBufSize, IntPtr pUser)
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
                            PlayM4_SetDecCallBack(m_Port_Center_Camera, m_DecCentralCameraCallback);

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
        private void RealDataLeftCameraCallback(int lRealHandle, uint dwDataType, IntPtr pBuffer, uint dwBufSize, IntPtr pUser)
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
                            PlayM4_SetDecCallBack(m_Port_Left_Camera, m_DecLeftCameraCallback);

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
        private void RealDataRigthCameraCallback(int lRealHandle, uint dwDataType, IntPtr pBuffer, uint dwBufSize, IntPtr pUser)
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
                            PlayM4_SetDecCallBack(m_Port_Right_Camera, m_DecRigthCameraCallback);

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

        private void DecCentralCameraCallback(int nPort, IntPtr pBuf, int nSize, ref PlayCtrl.FRAME_INFO pFrameInfo, int nUser, int nReserved2)
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
                if (!state_playing || CentralCameraWBMP == null)
                    return;

                try
                {
                    CentralCameraWBMP.Lock();
                    // Kopiujemy już gotowe dane RGB do naszej bitmapy WPF
                    ConvertYV12ToRGB(rawBuffer1, CentralCameraWBMP.BackBuffer, width, height);
                    CentralCameraWBMP.AddDirtyRect(new Int32Rect(0, 0, width, height));
                    //Console.WriteLine("DecCallback1");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Błąd: " + ex.Message);
                }
                finally
                {
                    CentralCameraWBMP.Unlock();
                }
            }));
        }
        private void DecLeftCameraCallback(int nPort, IntPtr pBuf, int nSize, ref PlayCtrl.FRAME_INFO pFrameInfo, int nUser, int nReserved2)
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
                if (!state_playing || LeftCameraWBMP == null)
                    return;

                try
                {
                    LeftCameraWBMP.Lock();
                    // Kopiujemy już gotowe dane RGB do naszej bitmapy WPF
                    ConvertYV12ToRGB(rawBuffer2, LeftCameraWBMP.BackBuffer, width, height);
                    LeftCameraWBMP.AddDirtyRect(new Int32Rect(0, 0, width, height));
                    //Console.WriteLine("DecCallback2");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Błąd: " + ex.Message);
                }
                finally
                {
                    LeftCameraWBMP.Unlock();
                }
            }));
        }
        private void DecRigthCameraCallback(int nPort, IntPtr pBuf, int nSize, ref PlayCtrl.FRAME_INFO pFrameInfo, int nUser, int nReserved2)
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
                if (!state_playing || RigthCameraWBMP == null)
                    return;

                try
                {
                    RigthCameraWBMP.Lock();
                    // Kopiujemy już gotowe dane RGB do naszej bitmapy WPF
                    ConvertYV12ToRGB(rawBuffer3, RigthCameraWBMP.BackBuffer, width, height);
                    RigthCameraWBMP.AddDirtyRect(new Int32Rect(0, 0, width, height));
                    //Console.WriteLine("DecCallback3");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Błąd: " + ex.Message);
                }
                finally
                {
                    RigthCameraWBMP.Unlock();
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

            CHCNetSDK.CHCNet.NET_DVR_Logout(CentralCamera_Id);
            CHCNetSDK.CHCNet.NET_DVR_Logout(LeftCamera_Id);
            CHCNetSDK.CHCNet.NET_DVR_Logout(RigthCamera_Id);

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
            CentralCameraSmall.Visibility = Visibility.Collapsed;
            MainCamera.Visibility = Visibility.Collapsed;
            BigMainCamera.Visibility = Visibility.Visible;
            BigMainCamera.Source = RigthCameraWBMP;
        }
        private void Image_RightCameraMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LeftCameraSmall.Visibility = Visibility.Collapsed;
            RightCameraSmall.Visibility = Visibility.Collapsed;
            CentralCameraSmall.Visibility = Visibility.Collapsed;
            MainCamera.Visibility = Visibility.Collapsed;
            BigMainCamera.Visibility = Visibility.Visible;
            BigMainCamera.Source = LeftCameraWBMP;
        }
        private void Image_CentralCameraMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LeftCameraSmall.Visibility = Visibility.Collapsed;
            RightCameraSmall.Visibility = Visibility.Collapsed;
            CentralCameraSmall.Visibility = Visibility.Collapsed;
            MainCamera.Visibility = Visibility.Collapsed;
            BigMainCamera.Visibility = Visibility.Visible;
            BigMainCamera.Source = CentralCameraWBMP;
        }
        private void Image_CentralCameraTermovisionMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LeftCameraSmall.Visibility = Visibility.Collapsed;
            RightCameraSmall.Visibility = Visibility.Collapsed;
            CentralCameraSmall.Visibility = Visibility.Collapsed;
            MainCamera.Visibility = Visibility.Collapsed;
            BigMainCamera.Visibility = Visibility.Visible;
            BigMainCamera.Source = wbmpThermoVision;
        }
        private void Image_BigMainCameraTermovisionMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Rozpoczęcie podglądu
            MainCamera.Source = wbmpThermoVision;
            CentralCameraSmall.Source = CentralCameraWBMP;
            RightCameraSmall.Source = LeftCameraWBMP;
            LeftCameraSmall.Source = RigthCameraWBMP;

            LeftCameraSmall.Visibility = Visibility.Visible;
            RightCameraSmall.Visibility = Visibility.Visible;
            CentralCameraSmall.Visibility = Visibility.Visible;
            MainCamera.Visibility = Visibility.Visible;

            BigMainCamera.Visibility = Visibility.Collapsed;
            BigMainCamera.Source = null;
            state_playing = true;
        }
    }
}

