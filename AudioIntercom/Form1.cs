using NAudio.Wave;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AudioIntercom
{
    public partial class Form1 : Form
    {
        private UdpClient _udpClient;

        private Thread _receiveThread;

        private WaveInEvent _waveIn;

        private WaveOutEvent _waveOut;

        private BufferedWaveProvider _waveProvider;

        private string _remoteIp = "192.168.0.162";

        private int _remotePort = 12000;

        private int _localPort = 11000;

        public Form1()
        {
            InitializeComponent();
            ChangeConnectionStatus(false);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            /*if (String.IsNullOrEmpty(RemoteIpTextBox.Text) || RemoteIpTextBox.Text.Count(c => c.Equals('.')) < 3)
            { 
                MessageBox.Show("Enter valid ip for remote PC!");
                return;
            }

            if (String.IsNullOrEmpty(RemotePortTextBox.Text) || !Int32.TryParse(RemotePortTextBox.Text, out int remotePort))
            {
                MessageBox.Show("Enter valid port for remote PC!");
                return;
            }

            _remoteIp = RemoteIpTextBox.Text;
            _remotePort = remotePort;*/

            try
            {
                StartReceiving();
                ChangeConnectionStatus(true);
                MessageBox.Show($"Connected to {_remoteIp}:{_remotePort}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: \n{ex.Message}");
                ChangeConnectionStatus(false);
            }
        }

        public void StartReceiving()
        {
            _udpClient = new UdpClient(_localPort);

            // Настройка захвата аудио
            _waveIn = new WaveInEvent();
            _waveIn.WaveFormat = new WaveFormat(48000, 24, 2); // Формат аудио
            _waveIn.DataAvailable += WaveIn_DataAvailable;
            _waveIn.StartRecording();

            // Настройка воспроизведения аудио
            _waveProvider = new BufferedWaveProvider(_waveIn.WaveFormat);
            _waveOut = new WaveOutEvent();
            _waveOut.Init(_waveProvider);
            _waveOut.Play();

            // Запуск асинхронного приема данных
            Task.Run(() => ReceiveData());
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            try
            {
                // Отправка аудио данных через UDP
                _udpClient.Send(e.Buffer, e.BytesRecorded, _remoteIp, _remotePort);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending audio: " + ex.Message);
            }
        }

        private async Task ReceiveData()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, _remotePort);
            while (true)
            {
                try
                {
                    // Прием аудио данных через UDP
                    var result = await _udpClient.ReceiveAsync();
                    if (result.Buffer.Length > 0)
                    {
                        _waveProvider.AddSamples(result.Buffer, 0, result.Buffer.Length);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error receiving audio: " + ex.Message);
                }
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _waveIn.StopRecording();
            _udpClient.Close();
            _receiveThread.Abort();
            MessageBox.Show("Disconnected!");
        }

        private void ChangeConnectionStatus(bool connected)
        {
            if(connected)
            {
                StatusLabel.Text = "Connected";
                StatusLabel.ForeColor = Color.Green;
            }
            else
            {
                StatusLabel.Text = "Disconnected";
                StatusLabel.ForeColor = Color.Red;
            }
        }
    }
}
