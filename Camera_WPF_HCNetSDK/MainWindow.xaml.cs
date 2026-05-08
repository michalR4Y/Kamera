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
        public int user_id_1 = -1;
        public int user_id_2 = -1;
        public int user_id_3 = -1;

        public IntPtr pUser1 = new IntPtr();
        public IntPtr pUser2 = new IntPtr();
        public IntPtr pUser3 = new IntPtr();
        public IntPtr pUser4 = new IntPtr();

        public int play_handle1 = 0;
        public int play_handle2 = 0;
        public int play_handle3 = 0;
        public int play_handle4 = 0;

        int m_lPort = -1;
        private REALDATACALLBACK RealData; // Przechowuj delegata, by GC go nie usunął
        private DECCBFUN m_DecCallback; // Przechowuj delegata callbacku dekodowania
        private byte[] rgbBuffer; // Globalny bufor, by nie tworzyć go co klatkę

        WriteableBitmap wbmp = new WriteableBitmap(2688, 1520, 96, 96, PixelFormats.Bgr24, null);

        public MainWindow()
        {
            InitializeComponent();
            NET_DVR_Init();
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            if (state_playing)
            {
                NET_DVR_StopRealPlay(play_handle1);
                NET_DVR_StopRealPlay(play_handle2);
                NET_DVR_StopRealPlay(play_handle3);
                NET_DVR_StopRealPlay(play_handle4);

                NET_DVR_Logout(user_id_1);
                NET_DVR_Logout(user_id_2);
                NET_DVR_Logout(user_id_3);

                PlayM4_Stop(m_lPort);
                PlayM4_CloseStream(m_lPort);
                ButtonStart.Content = "Start";

                state_playing = false;
            }
            else
            {
                string DVRIPAddress1 = "192.168.1.64";
                //string DVRIPAddress2 = "192.168.1.65";
                //string DVRIPAddress3 = "192.168.1.66";

                Int16 DVRPortNumber = Int16.Parse("8000");
                string DVRUserName = "admin";
                string DVRPassword = "1qaz2wsx";

                NET_DVR_DEVICEINFO_V30 DeviceInfo1 = new NET_DVR_DEVICEINFO_V30();
                //NET_DVR_DEVICEINFO_V30 DeviceInfo2 = new NET_DVR_DEVICEINFO_V30();
                //NET_DVR_DEVICEINFO_V30 DeviceInfo3 = new NET_DVR_DEVICEINFO_V30();

                ButtonStart.Content = "Stop";

                user_id_1 = NET_DVR_Login_V30(DVRIPAddress1, DVRPortNumber, DVRUserName, DVRPassword, ref DeviceInfo1);

                NET_DVR_PREVIEWINFO lpPreviewInfo1 = new NET_DVR_PREVIEWINFO()
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

                RealData = new REALDATACALLBACK(RealDataCallback);
                play_handle1 = NET_DVR_RealPlay_V40(user_id_1, ref lpPreviewInfo1, RealData, pUser1);
                // Rozpoczęcie podglądu
                    MainCamera.Source = wbmp;
                    RightCameraSmall.Source = wbmp;
                    LeftCameraSmall.Source = wbmp;
                    CenterCameraSmall.Source = wbmp;

                state_playing = true;
            }

        }
        private void DecCallback(int nPort, IntPtr pBuf, int nSize, ref PlayCtrl.FRAME_INFO pFrameInfo, int nUser, int nReserved2)
        {
            if (m_lPort == -1 || nSize <= 0 || pBuf == IntPtr.Zero)
                return;

            int width = pFrameInfo.nWidth;
            int height = pFrameInfo.nHeight;
            int expectedRgbSize = width * height * 3;

            // Inicjalizacja bufora raz, przy zmianie rozdzielczości
            if (rgbBuffer == null || rgbBuffer.Length != expectedRgbSize)
            {
                rgbBuffer = new byte[expectedRgbSize];
            }
            // Przesyłamy do wątku UI
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!state_playing || wbmp == null)
                    return;

                try
                {
                    wbmp.Lock();
                    // Kopiujemy już gotowe dane RGB do naszej bitmapy WPF
                    ConvertYV12ToRGB(pBuf, wbmp.BackBuffer, width, height);
                    wbmp.AddDirtyRect(new Int32Rect(0, 0, width, height));
                    Console.WriteLine("test");
                }
                catch (Exception ex)
                {
                    WriteLine("Błąd: " + ex.Message);
                }
                finally
                {
                    wbmp.Unlock();
                }
            }));
        }

        private void RealDataCallback(int lRealHandle, uint dwDataType, IntPtr pBuffer, uint dwBufSize, IntPtr pUser)
        {
            switch (dwDataType)
            {
                case NET_DVR_SYSHEAD:
                    if (!PlayM4_GetPort(ref m_lPort)) return;
                    if (dwBufSize > 0)
                    {
                        if (PlayM4_OpenStream(m_lPort, pBuffer, dwBufSize, 1024 * 1024))
                        {
                            PlayM4_SetDisplayType(m_lPort, 0); // Format RGB
                            m_DecCallback = new DECCBFUN(DecCallback);
                            PlayM4_SetDecCallBack(m_lPort, m_DecCallback);

                            try
                            {
                                PlayM4_Play(m_lPort, IntPtr.Zero);
                            }
                            catch (Exception ex)
                            {
                                WriteLine("Błąd: " + ex.Message);
                            }
                        }
                    }
                    break;

                case NET_DVR_STREAMDATA: // Dane wideo
                    if (m_lPort != -1 && dwBufSize > 0)
                    {
                        // Przekazanie danych do dekodera
                        if (!PlayM4_InputData(m_lPort, pBuffer, dwBufSize))
                        {
                            // Ewentualna obsługa błędu przepełnienia bufora
                        }
                    }
                    break;
            }
        }

        private void ConvertYV12ToRGB(IntPtr pBuf, IntPtr pDest, int width, int height)
        {
            int frameSize = width * height;
            int chromaSize = frameSize >> 2;

            unsafe
            {
                byte* yPtr = (byte*)pBuf;
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
}
