using CHCNetSDK;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Drawing;             // Podstawowa obsługa Bitmap
using System.Drawing.Imaging;     // Obsługa formatów pikseli (np. Format24bppRgb)
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Runtime.InteropServices; // Do pracy z pamięcią (Marshal, IntPtr)
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static CHCNetSDK.CHCNet;
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

        public int CentralCamera_Id = -1;
        public int LeftCamera_Id = -1;
        public int RigthCamera_Id = -1;

        public IntPtr pUserCentral = new IntPtr();
        public IntPtr pUserLeft = new IntPtr();
        public IntPtr pUserRigth = new IntPtr();
        public IntPtr pUserThermoVision = new IntPtr();
        public IntPtr pUser1_HD = new IntPtr();
        public IntPtr pUser2_HD = new IntPtr();
        public IntPtr pUser3_HD = new IntPtr();
        public IntPtr pUserThermoVision_HD = new IntPtr();

        public int play_handle_Central = 0;
        public int play_handleLeft = 0;
        public int play_handleRigth = 0;
        public int play_handle_ThermoVision = 0;
        public int play_handle_Central_HD = 0;
        public int play_handle_Left_HD = 0;
        public int play_handle_Rigth_HD = 0;
        public int play_handle_ThermoVision_HD = 0;

        int m_Port_Center_Camera = -1;
        int m_Port_Left_Camera = -1;
        int m_Port_Right_Camera = -1;
        int m_Port_Center_Camera_ThermoVision = -1;
        int m_Port_Center_Camera_HD = -1;
        int m_Port_Left_Camera_HD = -1;
        int m_Port_Right_Camera_HD = -1;
        int m_Port_Center_Camera_ThermoVision_HD = -1;

        private CHCNetSDK.CHCNet.REALDATACALLBACK RealDataCentralCamera; // Przechowuj delegata, by GC go nie usunął
        private CHCNetSDK.CHCNet.REALDATACALLBACK RealDataLeftCamera; // Przechowuj delegata, by GC go nie usunął
        private CHCNetSDK.CHCNet.REALDATACALLBACK RealDataRigthCamera; // Przechowuj delegata, by GC go nie usunął
        private CHCNetSDK.CHCNet.REALDATACALLBACK RealDataThermoVision; // Przechowuj delegata, by GC go nie usunął
        private CHCNetSDK.CHCNet.REALDATACALLBACK RealDataCentralCamera_HD; // Przechowuj delegata, by GC go nie usunął
        private CHCNetSDK.CHCNet.REALDATACALLBACK RealDataLeftCamera_HD; // Przechowuj delegata, by GC go nie usunął
        private CHCNetSDK.CHCNet.REALDATACALLBACK RealDataRigthCamera_HD; // Przechowuj delegata, by GC go nie usunął
        private CHCNetSDK.CHCNet.REALDATACALLBACK RealDataThermoVision_HD; // Przechowuj delegata, by GC go nie usunął

        private DECCBFUN m_DecCentralCameraCallback; // Przechowuj delegata callbacku dekodowania
        private DECCBFUN m_DecLeftCameraCallback; // Przechowuj delegata callbacku dekodowania
        private DECCBFUN m_DecRigthCameraCallback; // Przechowuj delegata callbacku dekodowania
        private DECCBFUN m_DecCallbackThermoVision; // Przechowuj delegata callbacku dekodowania
        private DECCBFUN m_DecCentralCameraCallback_HD; // Przechowuj delegata callbacku dekodowania
        private DECCBFUN m_DecLeftCameraCallback_HD; // Przechowuj delegata callbacku dekodowania
        private DECCBFUN m_DecRigthCameraCallback_HD; // Przechowuj delegata callbacku dekodowania
        private DECCBFUN m_DecCallbackThermoVision_HD; // Przechowuj delegata callbacku dekodowania

        WriteableBitmap CentralCameraWBMP = new WriteableBitmap(704, 576, 96, 96, PixelFormats.Bgr24, null);  // Kamera centralna
        WriteableBitmap LeftCameraWBMP = new WriteableBitmap(640, 512, 96, 96, PixelFormats.Bgr24, null);  // Kamera Lewa
        WriteableBitmap RigthCameraWBMP = new WriteableBitmap(640, 512, 96, 96, PixelFormats.Bgr24, null);  // Kamera Prawa
        WriteableBitmap ThermoVisionWBMP = new WriteableBitmap(704, 576, 96, 96, PixelFormats.Bgr24, null);    // Termowizja
        WriteableBitmap CentralCameraWBMP_HD = new WriteableBitmap(1920, 1080, 96, 96, PixelFormats.Bgr24, null);  // Kamera centralna
        WriteableBitmap LeftCameraWBMP_HD = new WriteableBitmap(1920, 1080, 96, 96, PixelFormats.Bgr24, null);  // Kamera Lewa
        WriteableBitmap RigthCameraWBMP_HD = new WriteableBitmap(1920, 1080, 96, 96, PixelFormats.Bgr24, null);  // Kamera Prawa
        WriteableBitmap ThermoVisionWBMP_HD = new WriteableBitmap(1280, 720, 96, 96, PixelFormats.Bgr24, null);    // Termowizja

        //bool isCentralCameraWBMP_HD_Big = false;
        //bool isLeftCameraWBMP_HD_Big = false;
        //bool isRigthCameraWBMP_HD_Big = false;
        //bool isThermoVisionWBMP_HD_Big = false;

        private byte[] rawBuffer1;
        private byte[] rawBuffer2;
        private byte[] rawBuffer3;
        private byte[] rawBufferThermo;

        private byte[] rawBuffer2_HD;
        private byte[] rawBuffer3_HD;
        private byte[] rawBufferThermo_HD;
        private byte[] rawBufferCentralCamera_HD;

        private DirectInput directInput;
        private Joystick joystick;
        private CancellationTokenSource cts;

        private bool isListening = true;

        string DVRIPAddressCentralCamera = "192.168.1.64";
        string DVRIPAddressLeftCamera = "192.168.1.65";
        string DVRIPAddressRigthCamera = "192.168.1.66";
        Int16 DVRPortNumber = Int16.Parse("8000");
        string MyIPAddress = "192.168.1.105";
        Int16 MyPortNumber = Int16.Parse("13000");
        string DriverIPAddress = "192.168.1.105";
        Int16 DriverPortNumber = Int16.Parse("13001");
        string DVRUserName = "admin";
        string DVRPassword = "1qaz2wsx";


        CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO lpPreviewInfoCentralCamera_HD = new CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO() // Kamera centralna
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

        CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO lpPreviewInfoLeftCamera_HD = new CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO() // Kamera lewa
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

        CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO lpPreviewInfoRightCamera_HD = new CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO() // Kamera prawa
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

        CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO lpPreviewInfoThermoVision_HD = new CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO() // Kamera centralna - termowizja
        {
            hPlayWnd = IntPtr.Zero,
            lChannel = Int16.Parse("2"),
            dwStreamType = 0,   // 0-główny, 1-podstrumień
            dwLinkMode = 0,
            bBlocked = true,
            dwDisplayBufNum = 1,
            byProtoType = 0,
            byPreviewMode = 0
        };

        //public delegate void MSGExceptionCallBack(uint dwType, int lUserID, int lHandle, IntPtr pUser);
        //private CHCNetSDK.CHCNet.EXCEPYIONCALLBACK m_ExceptionCB = null;
        public MainWindow()
        {
            InitializeComponent();
            CHCNetSDK.CHCNet.NET_DVR_Init();
            // Uruchomienie po załadowaniu okna
            this.Loaded += (s, e) => StartJoystickService();
            // Bezpieczne zamknięcie przy wyjściu
            this.Closing += (s, e) => StopJoystickService();

            Task.Run(() => StartTcpServer()); // Uruchomienie w tle

            this.StateChanged += MainWindow_StateChanged;

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

            RealDataCentralCamera_HD = new CHCNetSDK.CHCNet.REALDATACALLBACK(RealDataCentralCameraCallback_HD);
            RealDataLeftCamera_HD = new CHCNetSDK.CHCNet.REALDATACALLBACK(RealDataLeftCameraCallback_HD);
            RealDataRigthCamera_HD = new CHCNetSDK.CHCNet.REALDATACALLBACK(RealDataRigthCameraCallback_HD);
            RealDataThermoVision_HD = new CHCNetSDK.CHCNet.REALDATACALLBACK(RealDataCallbackThermoVision_HD);

            m_DecCentralCameraCallback = new DECCBFUN(DecCentralCameraCallback);
            m_DecLeftCameraCallback = new DECCBFUN(DecLeftCameraCallback);
            m_DecRigthCameraCallback = new DECCBFUN(DecRigthCameraCallback);
            m_DecCallbackThermoVision = new DECCBFUN(DecCallbackThermoVision);

            m_DecCentralCameraCallback_HD = new DECCBFUN(DecCentralCameraCallback_HD);
            m_DecLeftCameraCallback_HD = new DECCBFUN(DecLeftCameraCallback_HD);
            m_DecRigthCameraCallback_HD = new DECCBFUN(DecRigthCameraCallback_HD);
            m_DecCallbackThermoVision_HD = new DECCBFUN(DecCallbackThermoVision_HD);
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

                PlayM4_Stop(m_Port_Center_Camera_HD);
                PlayM4_CloseStream(m_Port_Center_Camera_HD);
                CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handle_Central_HD);

                PlayM4_Stop(m_Port_Left_Camera_HD);
                PlayM4_CloseStream(m_Port_Left_Camera_HD);
                CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handle_Left_HD);

                PlayM4_Stop(m_Port_Right_Camera_HD);
                PlayM4_CloseStream(m_Port_Right_Camera_HD);
                CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handle_Rigth_HD);

                PlayM4_Stop(m_Port_Center_Camera_ThermoVision_HD);
                PlayM4_CloseStream(m_Port_Center_Camera_ThermoVision_HD);
                CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(m_Port_Center_Camera_ThermoVision_HD);


                CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handle_ThermoVision);
                CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handle_Central);
                CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handleLeft);
                CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handleRigth);

                MainCamera.Visibility = Visibility.Collapsed;
                CentralCameraSmall.Visibility = Visibility.Collapsed;
                RightCameraSmall.Visibility = Visibility.Collapsed;
                LeftCameraSmall.Visibility = Visibility.Collapsed;

                BigMainCamera.Visibility = Visibility.Hidden;

                state_playing = false;
                ButtonStart.Content = "Start";
                ButtonStart.Background = System.Windows.Media.Brushes.Green;
            }
            else
            {
                ButtonStart.Content = "Stop";
                ButtonStart.Background = System.Windows.Media.Brushes.Red;

                CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO lpPreviewInfoCentralCamera = new CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO() // Kamera centralna
                {
                    hPlayWnd = IntPtr.Zero,
                    lChannel = Int16.Parse("1"),
                    dwStreamType = 1,   // 0-główny, 1-podstrumień
                    dwLinkMode = 0,
                    bBlocked = true,
                    dwDisplayBufNum = 1,
                    byProtoType = 0,
                    byPreviewMode = 0
                };

                CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO lpPreviewInfoLeftCamera = new CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO() // Kamera lewa
                {
                    hPlayWnd = IntPtr.Zero,
                    lChannel = Int16.Parse("1"),
                    dwStreamType = 1,   // 0-główny, 1-podstrumień
                    dwLinkMode = 0,
                    bBlocked = true,
                    dwDisplayBufNum = 1,
                    byProtoType = 0,
                    byPreviewMode = 0
                };

                CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO lpPreviewInfoRightCamera = new CHCNetSDK.CHCNet.NET_DVR_PREVIEWINFO() // Kamera prawa
                {
                    hPlayWnd = IntPtr.Zero,
                    lChannel = Int16.Parse("1"),
                    dwStreamType = 1,   // 0-główny, 1-podstrumień
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
                    dwStreamType = 1,   // 0-główny, 1-podstrumień
                    dwLinkMode = 1,
                    bBlocked = true,
                    dwDisplayBufNum = 1,
                    byProtoType = 0,
                    byPreviewMode = 0
                };

                if (CentralCamera_Id >= 0)
                {
                    play_handle_ThermoVision = CHCNetSDK.CHCNet.NET_DVR_RealPlay_V40(CentralCamera_Id, ref lpPreviewInfoThermoVision, RealDataThermoVision, pUserThermoVision);
                    MainCamera.Source = ThermoVisionWBMP;
                    MainCamera.Visibility = Visibility.Visible;
                    Task.Run(() =>
                    {
                        // Praca w tle
                        CameraPresenceControl(CentralCamera_Id, "Kamera Centralna");
                    });
                }
                
                if (CentralCamera_Id >= 0)
                {
                    play_handle_Central = CHCNetSDK.CHCNet.NET_DVR_RealPlay_V40(CentralCamera_Id, ref lpPreviewInfoCentralCamera, RealDataCentralCamera, pUserCentral);
                    CentralCameraSmall.Source = CentralCameraWBMP;
                    CentralCameraSmall.Visibility = Visibility.Visible;
                }

                if (LeftCamera_Id >= 0)
                {
                    play_handleLeft = CHCNetSDK.CHCNet.NET_DVR_RealPlay_V40(LeftCamera_Id, ref lpPreviewInfoLeftCamera, RealDataLeftCamera, pUserLeft);
                    LeftCameraSmall.Source = RigthCameraWBMP;
                    LeftCameraSmall.Visibility = Visibility.Visible;
                    Task.Run(() =>
                    {
                        // Praca w tle
                        CameraPresenceControl(LeftCamera_Id, "Kamera Lewa");
                    });
                    RightCameraSmall.Visibility = Visibility.Visible;
                }
                if (RigthCamera_Id >= 0)
                {
                    play_handleRigth = CHCNetSDK.CHCNet.NET_DVR_RealPlay_V40(RigthCamera_Id, ref lpPreviewInfoRightCamera, RealDataRigthCamera, pUserRigth);
                    RightCameraSmall.Source = RigthCameraWBMP;
                    RightCameraSmall.Visibility = Visibility.Visible;
                    Task.Run(() =>
                    {
                        // Praca w tle
                        CameraPresenceControl(RigthCamera_Id, "Kamera Prawa");
                    });
                }
                // Rozpoczęcie podglądu

                state_playing = true;
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
            PlayM4_Stop(m_Port_Center_Camera_ThermoVision_HD);
            PlayM4_Stop(m_Port_Left_Camera_HD);
            PlayM4_Stop(m_Port_Right_Camera_HD);
            PlayM4_Stop(m_Port_Center_Camera_HD);

            PlayM4_CloseStream(m_Port_Center_Camera_ThermoVision);
            PlayM4_CloseStream(m_Port_Center_Camera);
            PlayM4_CloseStream(m_Port_Left_Camera);
            PlayM4_CloseStream(m_Port_Right_Camera);
            PlayM4_CloseStream(m_Port_Center_Camera_ThermoVision_HD);
            PlayM4_CloseStream(m_Port_Left_Camera_HD);
            PlayM4_CloseStream(m_Port_Right_Camera_HD);
            PlayM4_CloseStream(m_Port_Center_Camera_HD);

            CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handle_ThermoVision);
            CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handle_Central);
            CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handleLeft);
            CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handleRigth);
            CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handle_ThermoVision_HD);
            CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handle_Central_HD);
            CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handle_Left_HD);
            CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handle_Rigth_HD);

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
            play_handle_Left_HD = CHCNetSDK.CHCNet.NET_DVR_RealPlay_V40(LeftCamera_Id, ref lpPreviewInfoLeftCamera_HD, RealDataLeftCamera_HD, pUser3_HD);
            BigMainCamera.Source = LeftCameraWBMP_HD;
            LeftCameraSmall.Visibility = Visibility.Collapsed;
            RightCameraSmall.Visibility = Visibility.Collapsed;
            CentralCameraSmall.Visibility = Visibility.Collapsed;
            MainCamera.Visibility = Visibility.Collapsed;
            BigMainCamera.Visibility = Visibility.Visible;
        }
        private void Image_RightCameraMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            play_handle_Central_HD = CHCNetSDK.CHCNet.NET_DVR_RealPlay_V40(RigthCamera_Id, ref lpPreviewInfoRightCamera_HD, RealDataRigthCamera_HD, pUser2_HD);
            BigMainCamera.Source = RigthCameraWBMP_HD;
            LeftCameraSmall.Visibility = Visibility.Collapsed;
            RightCameraSmall.Visibility = Visibility.Collapsed;
            CentralCameraSmall.Visibility = Visibility.Collapsed;
            MainCamera.Visibility = Visibility.Collapsed;
            BigMainCamera.Visibility = Visibility.Visible;
        }
        private void Image_CentralCameraMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            play_handle_Rigth_HD = CHCNetSDK.CHCNet.NET_DVR_RealPlay_V40(CentralCamera_Id, ref lpPreviewInfoCentralCamera_HD, RealDataCentralCamera_HD, pUser1_HD);
            BigMainCamera.Source = CentralCameraWBMP_HD;
            LeftCameraSmall.Visibility = Visibility.Collapsed;
            RightCameraSmall.Visibility = Visibility.Collapsed;
            CentralCameraSmall.Visibility = Visibility.Collapsed;
            MainCamera.Visibility = Visibility.Collapsed;
            BigMainCamera.Visibility = Visibility.Visible;
        }
        private void Image_CentralCameraTermovisionMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            play_handle_ThermoVision_HD = CHCNetSDK.CHCNet.NET_DVR_RealPlay_V40(CentralCamera_Id, ref lpPreviewInfoThermoVision_HD, RealDataThermoVision_HD, pUserThermoVision_HD);
            BigMainCamera.Source = ThermoVisionWBMP_HD;
            LeftCameraSmall.Visibility = Visibility.Collapsed;
            RightCameraSmall.Visibility = Visibility.Collapsed;
            CentralCameraSmall.Visibility = Visibility.Collapsed;
            MainCamera.Visibility = Visibility.Collapsed;
            BigMainCamera.Visibility = Visibility.Visible;
        }
        private void Image_BigMainCameraTermovisionMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Rozpoczęcie podglądu
            MainCamera.Source = ThermoVisionWBMP;
            CentralCameraSmall.Source = CentralCameraWBMP;
            RightCameraSmall.Source = RigthCameraWBMP;
            LeftCameraSmall.Source = LeftCameraWBMP;

            LeftCameraSmall.Visibility = Visibility.Visible;
            RightCameraSmall.Visibility = Visibility.Visible;
            CentralCameraSmall.Visibility = Visibility.Visible;
            MainCamera.Visibility = Visibility.Visible;

            PlayM4_Stop(m_Port_Center_Camera_HD);
            PlayM4_CloseStream(m_Port_Center_Camera_HD);
            CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handle_Central_HD);

            PlayM4_Stop(m_Port_Left_Camera_HD);
            PlayM4_CloseStream(m_Port_Left_Camera_HD);
            CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handle_Left_HD);

            PlayM4_Stop(m_Port_Right_Camera_HD);
            PlayM4_CloseStream(m_Port_Right_Camera_HD);
            CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handle_Rigth_HD);

            PlayM4_Stop(m_Port_Center_Camera_ThermoVision_HD);
            PlayM4_CloseStream(m_Port_Center_Camera_ThermoVision_HD);
            CHCNetSDK.CHCNet.NET_DVR_StopRealPlay(play_handle_ThermoVision_HD);

            BigMainCamera.Visibility = Visibility.Collapsed;
            BigMainCamera.Source = null;
            state_playing = true;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(HwndMessageHook);

            RegisterHidNotification(); // To "budzi" system do wysyłania powiadomień
        }

        private IntPtr HwndMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_DEVICECHANGE = 0x0219;
            const int DBT_DEVICEARRIVAL = 0x8000;       // Podłączono urządzenie
            const int DBT_DEVICEREMOVECOMPLETE = 0x8004; // Odłączono urządzenie

            if (msg == WM_DEVICECHANGE)
            {
                System.Diagnostics.Debug.WriteLine("Wykryto zmianę sprzętową! wParam: " + wParam);
                int eventCode = wParam.ToInt32();
                if (eventCode == DBT_DEVICEARRIVAL || eventCode == DBT_DEVICEREMOVECOMPLETE)
                {
                    // System wykrył zmianę w USB - restartujemy usługę joysticka
                    RestartJoystickService();
                }
            }
            return IntPtr.Zero;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            public Guid dbcc_classguid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string dbcc_name;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]

        static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, uint Flags);

        private void RegisterHidNotification()
        {
            Guid hidGuid = new Guid("4d1e55b2-f16f-11cf-88cb-001111000030"); // GUID dla urządzeń HID
            DEV_BROADCAST_DEVICEINTERFACE notificationFilter = new DEV_BROADCAST_DEVICEINTERFACE();
            notificationFilter.dbcc_size = Marshal.SizeOf(notificationFilter);
            notificationFilter.dbcc_devicetype = 0x00000005; // DBT_DEVTYP_DEVICEINTERFACE
            notificationFilter.dbcc_classguid = hidGuid;

            IntPtr buffer = Marshal.AllocHGlobal(notificationFilter.dbcc_size);
            Marshal.StructureToPtr(notificationFilter, buffer, true);

            RegisterDeviceNotification(new WindowInteropHelper(this).Handle, buffer, 0);
        }

        private void UpdateUI(JoystickState state)
        {
        }

        private void ResetUI()
        {

        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            //byte[] DataToSend = { 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39};
            ////            TcpClientSend(Encoding.UTF8.GetBytes(TextBoxToSend.Text));
            JoystickState stateIn = new JoystickState();
            
            SetJoy(ref stateIn);
            UpdateSendTCPIP(stateIn);
        }

    }
}

