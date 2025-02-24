using Microsoft.AspNetCore.SignalR.Client;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace HJ_Connect
{
    public enum ConnectType
    {
        SerialPort,
        TCP,
        UDP,
        Socket,
        WebSocket,
        RabbitMQ
    }
    public class MessageReceivedEventArgs : EventArgs
    {
        public string Message { get; private set; }
        public string MachineName { get; private set; }
        public Guid MachineId { get; private set; }
        public DateTime ReveivedTime { get; private set; }
        public MessageReceivedEventArgs(string message, string machineName, Guid machineId)
        {
            Message = message;
            MachineId = machineId;
            MachineName = machineName;
            ReveivedTime = DateTime.Now;
        }
    }

    public interface IConnect : IDisposable
    {
        public string MachineName { get; set; }
        public Guid MachineId { get; set; }
        public bool IsConnected { get; }
        public ConnectType ConnectType { get; }
        public string EndPoints { get; set; }
        //断开重连间隔  单位 ms
        public long ReconnectInterval { get; set; }
        //断开重连
        public void Reconnect(long interval);
        public void Connect();
        public void SendMessage(string message);
        public void Disconnect();
        //读取单条
        public string ReceiveMessage();
        //循环读取
        public void StartReceiveMessage();

        public delegate void OnMessageReceived(MessageReceivedEventArgs args);
        public OnMessageReceived MessageReceived { get; set; }
    }

    public abstract class ConnectBase : IConnect
    {
        public string MachineName { get; set; }
        public Guid MachineId { get; set; }
        public virtual bool IsConnected { get; }
        public ConnectType ConnectType { get; private set; }
        public string EndPoints { get; set; }
        public long ReconnectInterval { get; set; }
        public IConnect.OnMessageReceived MessageReceived { get; set; }


        protected string? HostName { get; set; }
        protected int? Port { get; set; }
        protected string? QueueName { get; set; }
        protected string? UserName { get; set; }
        protected string? Password { get; set; }

        public ConnectBase(Guid machineId, string machineName, ConnectType connectType, string endPoints)
        {
            ConnectType = connectType;
            EndPoints = endPoints;
            MachineId = machineId;
            MachineName = machineName;
            ParseEndPoints(EndPoints);
        }

        // 根据 ConnectType 解析 EndPoints  输入格式类似 "HoseName:{};Port:{};Passwora:{};UserName:{}"
        private void ParseEndPoints(string endPoints)
        {
            var parameters = endPoints.Split(';');
            if (parameters.FirstOrDefault(_ => _.ToLower().Contains("hostname")) is string StrHostName)
            {
                var parts = StrHostName.Split(':');
                if (parts.Length == 2)
                {
                    HostName = parts[1];
                }
            }
            if (parameters.FirstOrDefault(_ => _.ToLower().Contains("port")) is string StrPort)
            {
                var parts = StrPort.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out int port))
                {
                    Port = port;
                }
            }
            if (parameters.FirstOrDefault(_ => _.ToLower().Contains("username")) is string StrUserName)
            {
                var parts = StrUserName.Split(':');
                if (parts.Length == 2)
                {
                    UserName = parts[1];
                }
            }
            if (parameters.FirstOrDefault(_ => _.ToLower().Contains("password")) is string StrPassword)
            {
                var parts = StrPassword.Split(':');
                if (parts.Length == 2)
                {
                    Password = parts[1];
                }
            }
            if (parameters.FirstOrDefault(_ => _.ToLower().Contains("queuename")) is string StrQueueName)
            {
                var parts = StrQueueName.Split(':');
                if (parts.Length == 2)
                {
                    QueueName = parts[1];
                }
            }
        }


        public virtual void Reconnect(long interval)
        {
            //IsConnected = true;
        }
        public virtual void Connect()
        {
            //IsConnected = true;
        }

        public virtual void Disconnect()
        {
            //IsConnected = false;
        }

        public virtual string ReceiveMessage()
        {
            throw new NotImplementedException();
        }
        public virtual void StartReceiveMessage()
        {
            throw new NotImplementedException();
        }

        public virtual void SendMessage(string message) { }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
    #region Serial Port
    public sealed class SerialPortConnect : ConnectBase
    {
        private SerialPort serialPort;
        private const int DefaultBaudRate = 9600;
        internal SerialPortConnect(Guid machineId, string machineName, string endpoints) : base(machineId, machineName, ConnectType.SerialPort, endpoints)
        {
            //(string portName, int baudRate, System.IO.Ports.Parity parity, int dataBits, System.IO.Ports.StopBits stopBits)
            //ConnectType = ConnectType.SerialPort;
            //serialPort = new SerialPort();
            //serialPort.PortName = endpoints;
            //serialPort.BaudRate = 9600;


            serialPort = new SerialPort
            {
                PortName = HostName,  // HostName is the COM port (e.g., COM1)
                BaudRate = DefaultBaudRate       // Default baud rate, can be customized later
            };
            //serialPort.DataReceived += SerialPort_DataReceived;
        }

        public override void Connect()
        {
            base.Connect();
            serialPort.Open();
        }

        public override void Disconnect()
        {
            base.Disconnect();
            serialPort.Close();
        }
        public override void SendMessage(string message)
        {
            if (serialPort.IsOpen)
            {
                serialPort.WriteLine(message);
            }
        }

        public override void StartReceiveMessage()
        {
            serialPort.DataReceived += SerialPort_DataReceived;
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (sender is SerialPort _serialPort)
            {
                string message = _serialPort.ReadExisting();
                MessageReceived?.Invoke(new(message, MachineName, MachineId));
            }
        }
    }
    #endregion

    #region SignalR
    public sealed class SignalRConnect : ConnectBase
    {
        private HubConnection? hubConnection;

        public override bool IsConnected => hubConnection?.State == HubConnectionState.Connected;

        internal SignalRConnect(Guid machineId, string machineName, string endpoints)
            : base(machineId, machineName, ConnectType.Socket, endpoints)
        {
        }

        public override void Connect()
        {
            base.Connect();
            if (hubConnection != null)
            {
                return;
            }

            if (HostName is string host)
            {
                hubConnection = new HubConnectionBuilder()
                    .WithUrl($"http://{host}/hjChathub")
                    .Build();

                hubConnection.On<string>("HJChat", (message) =>
                {
                    MessageReceived?.Invoke(new MessageReceivedEventArgs(message, MachineName, MachineId));
                });

                hubConnection.StartAsync().Wait();
            }
        }

        public override void Disconnect()
        {
            base.Disconnect();
            hubConnection?.StopAsync().Wait();
            hubConnection = null;
        }

        public override void SendMessage(string message)
        {
            if (hubConnection != null && hubConnection.State == HubConnectionState.Connected)
            {
                hubConnection.SendAsync("HJChat", MachineName, message).Wait();
            }
        }

        public override string ReceiveMessage()
        {
            throw new NotImplementedException("Use asynchronous receiving instead.");
        }

        public override void StartReceiveMessage()
        {
            // SignalR uses a push model, so no need to implement this method
        }
    }
    #endregion
    #region Web Socket
    public sealed class WebSocketConnect : ConnectBase
    {
        private ClientWebSocket? webSocket;
        private CancellationTokenSource? cancellationTokenSource;

        public override bool IsConnected => webSocket?.State == WebSocketState.Open;

        internal WebSocketConnect(Guid machineId, string machineName, string endpoints)
            : base(machineId, machineName, ConnectType.WebSocket, endpoints)
        {
        }

        public override void Connect()
        {
            base.Connect();
            if (webSocket != null)
            {
                return;
            }

            webSocket = new ClientWebSocket();
            cancellationTokenSource = new CancellationTokenSource();

            if (HostName is string host)
            {
                var uri = new Uri($"ws://{host}");
                webSocket.ConnectAsync(uri, cancellationTokenSource.Token).Wait();
            }
        }

        public override void Disconnect()
        {
            base.Disconnect();
            cancellationTokenSource?.Cancel();
            webSocket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by user", CancellationToken.None).Wait();
            webSocket = null;
        }
        public override void SendMessage(string message)
        {
            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                var buffer = new ArraySegment<byte>(bytes);
                webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationTokenSource!.Token).Wait();
            }
        }

        public override string ReceiveMessage()
        {
            throw new NotImplementedException("Use asynchronous receiving instead.");
        }

        public override void StartReceiveMessage()
        {
            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                Task.Run(ReceiveLoop);
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[1024 * 4];
            while (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationTokenSource!.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", cancellationTokenSource.Token);
                }
                else
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    MessageReceived?.Invoke(new MessageReceivedEventArgs(message, MachineName, MachineId));
                }
            }
        }
    }
    #endregion

    #region Socket
    /// <summary>
    /// 套接字连接 不可持续
    /// </summary>
    public sealed class SocketConnect : ConnectBase
    {
        private Socket? socket;
        private NetworkStream? networkStream;
        private StreamReader? reader;
        private StreamWriter? writer;

        public override bool IsConnected => socket?.Connected ?? false;

        internal SocketConnect(Guid machineId, string machineName, string endpoints) : base(machineId, machineName, ConnectType.Socket, endpoints)
        {
        }

        public override void Reconnect(long interval)
        {
            //socket.BeginConnect
            base.Reconnect(interval);
            Disconnect();
            Task.Delay(TimeSpan.FromMilliseconds(interval)).Wait();
            Connect();
        }

        public override void Connect()
        {
            base.Connect();
            if (socket != null)
            {
                return;
            }
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (HostName is string host && Port is int port)
            {
                socket.Connect(host, port);
                networkStream = new NetworkStream(socket);
                reader = new StreamReader(networkStream, Encoding.UTF8);
                writer = new StreamWriter(networkStream, Encoding.UTF8) { AutoFlush = true };

                // 开始异步读取数据
                //BeginReceive();
            }
        }

        public override void Disconnect()
        {
            base.Disconnect();
            reader?.Close();
            writer?.Close();
            networkStream?.Close();
            socket?.Close();
            socket = null;
        }

        public override void SendMessage(string message)
        {
            if (writer != null)
            {
                writer.WriteLine(message);
            }
        }

        public override string ReceiveMessage()
        {
            if (reader != null)
            {
                string message = reader.ReadLine();
                if (message != null)
                {
                    MessageReceived?.Invoke(new MessageReceivedEventArgs(message, MachineName, MachineId));
                    return message;
                }
            }
            return string.Empty;
        }
        public override void StartReceiveMessage()
        {
            if (reader != null)
            {
                BeginReceive();
            }
        }

        private void BeginReceive()
        {
            byte[] buffer = new byte[1024];
            networkStream?.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(ReceiveCallback), buffer);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                if (networkStream != null)
                {
                    int bytesRead = networkStream.EndRead(ar);
                    if (bytesRead > 0)
                    {
                        byte[] buffer = (byte[])ar.AsyncState!;
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        MessageReceived?.Invoke(new MessageReceivedEventArgs(message, MachineName, MachineId));

                        // 继续异步读取数据
                        BeginReceive();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接收数据失败: {ex.Message}");
            }
        }
    }
    #endregion

    #region TCP
    public sealed class TCPConnect : ConnectBase
    {
        private TcpClient? tcpClient;
        private NetworkStream? networkStream;
        private StreamReader? reader;
        private StreamWriter? writer;
        private Thread? receiveThread;
        private bool isReceiving;

        internal TCPConnect(Guid machineId, string machineName, string endpoints) : base(machineId, machineName, ConnectType.TCP, endpoints)
        {
            tcpClient = new TcpClient();
        }

        public override void Connect()
        {
            base.Connect();
            if (tcpClient != null && HostName is string host && Port is int port)
            {
                tcpClient.Connect(host, port);
                networkStream = tcpClient.GetStream();
                reader = new StreamReader(networkStream, Encoding.UTF8);
                writer = new StreamWriter(networkStream, Encoding.UTF8) { AutoFlush = true };

                // 开始异步读取数据
                StartReceiving();
            }
        }

        public override void Disconnect()
        {
            base.Disconnect();
            isReceiving = false;
            receiveThread?.Join();
            reader?.Close();
            writer?.Close();
            networkStream?.Close();
            tcpClient?.Close();
            tcpClient = null;
        }

        public override void SendMessage(string message)
        {
            if (writer != null)
            {
                writer.WriteLine(message);
            }
        }

        public override string ReceiveMessage()
        {
            throw new NotImplementedException("Use asynchronous receiving instead.");
        }

        public override void StartReceiveMessage()
        {
            if (reader != null)
            {
                StartReceiving();
            }
        }

        private void StartReceiving()
        {
            isReceiving = true;
            receiveThread = new Thread(ReceiveLoop);
            receiveThread.Start();
        }

        private void ReceiveLoop()
        {
            try
            {
                while (isReceiving && networkStream != null)
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        MessageReceived?.Invoke(new MessageReceivedEventArgs(message, MachineName, MachineId));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接收数据失败: {ex.Message}");
            }
        }
    }
    #endregion

    #region UDP
    public sealed class UDPConnect : ConnectBase
    {
        private UdpClient udpClient;
        private IPEndPoint remoteEndPoint;
        //不固定
        public override bool IsConnected => true;

        internal UDPConnect(Guid machineId, string machineName, string endpoints) : base(machineId, machineName, ConnectType.UDP, endpoints)
        {
            udpClient = new UdpClient();
            if (HostName is string host && Port is int port)
            {
                remoteEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
            }
        }

        public override void Connect()
        {
            // UDP 是无连接的，所以这里不需要实现连接逻辑
        }

        public override void Disconnect()
        {
            udpClient.Close();
        }

        public override void SendMessage(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length, remoteEndPoint);
        }

        public override string ReceiveMessage()
        {
            var remoteEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = udpClient.Receive(ref remoteEP);
            string message = Encoding.UTF8.GetString(data);
            MessageReceived?.Invoke(new MessageReceivedEventArgs(message, MachineName, MachineId));
            return message;
        }
    }
    #endregion

    #region RabbitMQ
    public sealed class RabbitMQConnect : ConnectBase
    {
        private IConnection connection;
        private ConnectionFactory factory;
        private IModel channel;
        public override bool IsConnected => connection.IsOpen;
        internal RabbitMQConnect(Guid machineId, string machineName, string endpoints) : base(machineId, machineName, ConnectType.RabbitMQ, endpoints)
        {
            factory = new ConnectionFactory();
            //另一种写法   格式
            var endpoint = $"amqp://{UserName}:{Password}@{HostName}";
            if (Port != null)
            {
                endpoint += $":{Port}";
            }


            factory.SetUri(new Uri(endpoint));
            //factory.HostName = HostName;
            //factory.UserName = UserName;
            //factory.Password = UserName;
            //factory.VirtualHost = "";
            //factory.Port = 12312;
            //端口列表   写法如下

            //          var endpoints = new System.Collections.Generic.List<AmqpTcpEndpoint> {
            //new AmqpTcpEndpoint("hostname"),
            //new AmqpTcpEndpoint("localhost")
            //};
            //IConnection conn = factory.CreateConnection(endpoints);
            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            channel.QueueDeclare(QueueName, false, false, false, null);


            var _consumer = new EventingBasicConsumer(channel);
            _consumer.Received += OnMessageReceived;
            channel.BasicConsume(QueueName, true, _consumer);
        }

        public override void SendMessage(string message)
        {
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "",
                                 routingKey: QueueName,
                                 basicProperties: null,
                                 body: body);
        }

        /// <summary>
        /// 仅接受一条
        /// </summary>
        /// <returns></returns>
        public override string ReceiveMessage()
        {
            //return base.ReceiveMessage();
            var consumer = new QueueingBasicConsumer(channel);
            channel.BasicConsume(QueueName, true, consumer);

            //while (true)
            {
                var ea = (BasicDeliverEventArgs)consumer.Queue.Dequeue();

                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);

                //int dots = message.Split('.').Length - 1;
                //Thread.Sleep(dots * 1000);

                return message;

                //Console.WriteLine("Received {0}", message);
                //Console.WriteLine("Done");
            }
        }

        EventingBasicConsumer? _consumer;
        ///// <summary>
        ///// 持续监听
        ///// </summary>
        ///// <returns></returns>
        //public void StartReceiveMessage()
        //{
        //    _consumer = new EventingBasicConsumer(channel);
        //    _consumer.Received += OnMessageReceives;
        //    // this consumer tag identifies the subscription
        //    // when it has to be cancelled
        //    string consumerTag = channel.BasicConsume(QueueName, false, _consumer);
        //}
        ///// <summary>
        ///// 持续监听
        ///// </summary>
        ///// <returns></returns>
        //public void StopReceiveMessage()
        //{
        //    _consumer!.Received -= OnMessageReceives;
        //    _consumer = null;
        //}

        private void OnMessageReceived(object? obj, BasicDeliverEventArgs args)
        {

            var body = args.Body.ToArray();

            var message = Encoding.UTF8.GetString(body);
            MessageReceived?.Invoke(new MessageReceivedEventArgs(message, MachineName, MachineId));
            // copy or deserialise the payload
            // and process the message
            // ...
            //channel.BasicAck(args.DeliveryTag, false);
        }
    }
    #endregion

    public partial class ConnectFactory
    {
        //(Guid machineId, string machineName, string endpoints) : base(machineId,machineName, ConnectType.SerialPort, endpoints)
        public static IConnect CreateConnect(Guid machineId, string machineName, string connectType, string endpoints)
        {
            switch (connectType.ToLower())
            {
                case "serialport":
                    return new SerialPortConnect(machineId, machineName, endpoints);
                case "httpclient":
                case "tcp":
                    return new TCPConnect(machineId, machineName, endpoints);
                case "udp":
                    return new UDPConnect(machineId, machineName, endpoints);
                case "socket":
                    return new SocketConnect(machineId, machineName, endpoints);
                case "websocket":
                    return new WebSocketConnect(machineId, machineName, endpoints);
                case "rabbitmq":
                case "mq":
                    return new RabbitMQConnect(machineId, machineName, endpoints);
                case "signalr":
                    return new SignalRConnect(machineId, machineName, endpoints);
                default:
                    throw new Exception($"invaid messageType{connectType},endpoints{endpoints}");
            }
        }
    }
}
