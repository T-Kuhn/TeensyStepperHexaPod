using System.Threading;
using UnityEngine;
using System.IO.Ports;
using System.Linq;

namespace MachineSimulator.SerialCommunication
{
    public class SerialInterface : MonoBehaviour
    {
        [SerializeField] private string[] _availablePorts;
        [SerializeField] private string _portName = "";

        private SerialPort _port;
        Thread _receiveDataThread;
        private bool _isOpen => _port != null && _port.IsOpen;

        private void Awake()
        {
            _availablePorts = SerialPort.GetPortNames();
            // NOTE: There is of course no guarantee that the last port is the one we want,
            //       but the last port here happens to BE the want I want on my PC.
            //       That's why we're getting the last portName here.
            _portName = _availablePorts.Last();
        }

        private void Open()
        {
            _port = new SerialPort(_portName, Constants.BaudRate, Parity.None, 8, StopBits.One);
            _port.Handshake = Handshake.None;
            _port.Open();

            _receiveDataThread = new Thread(RecieveData);
            _receiveDataThread.Start();
        }

        public void Send(string s)
        {
            if (!_isOpen) Open();

            Debug.Log("Sending: " + s);

            _port.Write(s);
        }

        private void RecieveData()
        {
            while (_port.IsOpen)
            {
                var str = _port.ReadLine();
                if (!string.IsNullOrEmpty(str))
                {
                    Debug.Log("Received: " + str);
                }
            }
        }

        private void OnDestroy()
        {
            if (_port != null && _port.IsOpen)
            {
                _port.Close();
            }

            if (_receiveDataThread != null && _receiveDataThread.IsAlive)
            {
                _receiveDataThread.Abort();
            }
        }
    }
}