using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace ClientServer
{
    class ServerClass
    {
        private static Timer timer;
        private static bool motorState = false;
        private static bool stepSignalFlag;
        private static int LEDStatus = 0;
        private static int MS1 = 22;
        private static int MS2 = 5;
        private static int RST = 6;
        private static int SLP = 13;
        private static int STEP = 19;
        private static int DIR = 26;
        private static GpioPin pin_MS1, pin_MS2, pin_RST, pin_SLP, pin_STEP, pin_DIR;
        private static int timerTime = 10;

        private static void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                pin_MS1 = null;
                pin_MS2 = null;
                pin_RST = null;
                pin_SLP = null;
                pin_STEP = null;
                pin_DIR = null;
                Debug.Write("There is no GPIO controller on this device.");
                return;
            }

            pin_MS1 = gpio.OpenPin(MS1);
            pin_MS2 = gpio.OpenPin(MS2);
            pin_RST = gpio.OpenPin(RST);
            pin_SLP = gpio.OpenPin(SLP);
            pin_STEP = gpio.OpenPin(STEP);
            pin_DIR = gpio.OpenPin(DIR);

            pin_MS1.Write(GpioPinValue.High);
            pin_MS1.SetDriveMode(GpioPinDriveMode.Output);

            pin_MS2.Write(GpioPinValue.Low);
            pin_MS2.SetDriveMode(GpioPinDriveMode.Output);

            pin_RST.Write(GpioPinValue.High);
            pin_RST.SetDriveMode(GpioPinDriveMode.Output);

            pin_SLP.Write(GpioPinValue.High);
            pin_SLP.SetDriveMode(GpioPinDriveMode.Output);

            pin_STEP.Write(GpioPinValue.Low);
            stepSignalFlag = false;
            pin_STEP.SetDriveMode(GpioPinDriveMode.Output);

            pin_DIR.Write(GpioPinValue.Low);
            pin_DIR.SetDriveMode(GpioPinDriveMode.Output);

            Debug.Write("GPIO pin initialized correctly. \n");
        }

        public static StreamSocketListener Listener { get; set; }

        // This is the static method used to start listening for connections.

        public static async Task<bool> StartServer()
        {
            InitGPIO();
            timer = new Timer(TimerCallback, null, 0, timerTime);

            Listener = new StreamSocketListener();
            // Removes binding first in case it was already bound previously.
            Listener.ConnectionReceived -= Listener_ConnectionReceived;
            Listener.ConnectionReceived += Listener_ConnectionReceived;
            try
            {
                await Listener.BindServiceNameAsync("4555"); // Your port goes here.
                Debug.Write("Server started \n");
                return true;
            }
            catch (Exception ex)
            {
                Listener.ConnectionReceived -= Listener_ConnectionReceived;
                Listener.Dispose();
                return false;
            }
        }

        private static async void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            var remoteAddress = args.Socket.Information.RemoteAddress.ToString();
            //var reader = new DataReader(args.Socket.InputStream);
            //var writer = new DataWriter(args.Socket.OutputStream);
            while (true)
            {
                try
                {
                    Debug.Write("Receive from client \n");
                    //Read line from the remote client.
                    Stream inStream = args.Socket.InputStream.AsStreamForRead();
                    StreamReader reader = new StreamReader(inStream);
                    string request = await reader.ReadLineAsync();
                    Debug.Write("Received data: " + request + " \n");

                    if (request.Equals("1"))
                    {
                        setMotorOn();
                    }
                    if (request.Equals("0"))
                    {
                        setMotorOff();
                    }

                    //Send the line back to the remote client.
                    //Stream outStream = args.Socket.OutputStream.AsStreamForWrite();
                    //StreamWriter writer = new StreamWriter(outStream);
                    //await writer.WriteLineAsync(request);
                    //await writer.FlushAsync();
                }
                catch (Exception ex)
                {
                    //writer.DetachStream();
                    // reader.DetachStream();
                    return;
                }
            }
        }

        public static void setMotorOn()
        {
            Debug.Write("Set motorState on \n");
            motorState = true;
        }
        public static void setMotorOff()
        {
            Debug.Write("Set motorState off \n");
            motorState = false;
        }

        private static void TimerCallback(object state)
        {
            if (motorState)
            {
                // Move motor
                if (!stepSignalFlag)
                {
                    pin_STEP.Write(GpioPinValue.High);
                    stepSignalFlag = true;
                }
                else
                {
                    pin_STEP.Write(GpioPinValue.Low);
                    stepSignalFlag = false;
                }
            }
            else
            {
                // Do nothing for 5 seconds, then start motor again
            }
        }

    }
}
