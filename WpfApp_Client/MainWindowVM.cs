using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Net.Http;
using System.Text;
using HJ212;
using HJ_Connect;
using StackExchange.Redis;
using Timer = System.Timers.Timer;
using HJData = HJ212.HJ212Protocol.Data;
using RabbitMQ.Client;
using System;
using System.Windows;
using Data;
using System.Text.Json;
using System.Diagnostics;
using System.Net.Sockets;
namespace WpfApp_Client;



public class MainWindowVM : ObservableObject
{
    //local db
    private readonly HJ_DbContext _dbContext;
    //Redis
    private ConnectionMultiplexer _redis;
    private IDatabase _redisDB;


    public IConnect LocalSerialPort;
    public IConnect Connect;
    private Random _random;
    private Timer? _timer;

    #region Url Property

    private bool _isUrlPost;
    public bool IsUrlPost
    {
        get => _isUrlPost;
        set => SetProperty(ref _isUrlPost, value);
    }

    string UrlAddress;

    private string _tempUrlAddress = "https://localhost:7018/api/SerialPort";

    private string _urlPortStatus;
    public string UrlPortStatus
    {
        get => _urlPortStatus;
        set => SetProperty(ref _urlPortStatus, value);
    }

    #endregion

    private string _message;
    private string _serialPortStatus;
    private string _serialPortName;
    private string _serialPortEndpoints;
    private int _serialPortBaudRate = 9600;

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    private ObservableCollection<IConnect> _connects;
    public ObservableCollection<IConnect> Connects
    {
        get => _connects;
        set => SetProperty(ref _connects, value);
    }
    private ObservableCollection<KeyValuePair<string, string>> _data;
    public ObservableCollection<KeyValuePair<string, string>> Data
    {
        get => _data;
        set => SetProperty(ref _data, value);
    }

    private ObservableCollection<string> _output;
    public ObservableCollection<string> Output
    {
        get => _output;
        set => SetProperty(ref _output, value);
    }
    private int _messagesSendedCount = 0;
    public int MessagesSendedCount
    {
        get => _messagesSendedCount;
        set => SetProperty(ref _messagesSendedCount, value);
    }
    private int _messagesReceivedCount = 0;
    public int MessagesReceivedCount
    {
        get => _messagesReceivedCount;
        set => SetProperty(ref _messagesReceivedCount, value);
    }
    private int _messagesReceivedFaildCount = 0;
    public int MessagesReceivedFaildCount
    {
        get => _messagesReceivedFaildCount;
        set => SetProperty(ref _messagesReceivedFaildCount, value);
    }

    private ObservableCollection<Message> _messagesSended;
    public ObservableCollection<Message> MessagesSended
    {
        get => _messagesSended;
        set => SetProperty(ref _messagesSended, value);
    }

    private ObservableCollection<Message> _messagesReceived;
    public ObservableCollection<Message> MessagesReceived
    {
        get => _messagesReceived;
        set => SetProperty(ref _messagesReceived, value);
    }

    private ObservableCollection<string> _serialPortNames;
    public ObservableCollection<string> SerialPortNames
    {
        get => _serialPortNames;
        set => SetProperty(ref _serialPortNames, value);
    }

    public string SerialPortStatus
    {
        get => _serialPortStatus;
        set => SetProperty(ref _serialPortStatus, value);
    }

    public string SerialPortName
    {
        get => _serialPortName;
        set => SetProperty(ref _serialPortName, value);
    }

    public string SerialPortEndpoints
    {
        get => _serialPortEndpoints;
        set => SetProperty(ref _serialPortEndpoints, value);
    }

    public int TempSerialPortBaudRate
    {
        get => _serialPortBaudRate;
        set => SetProperty(ref _serialPortBaudRate, value);
    }

    public IRelayCommand SendCommand { get; }
    public IRelayCommand StartSendCommand { get; }
    public IRelayCommand StopSendCommand { get; }
    public IRelayCommand SavePostSettingsCommand { get; }
    public IRelayCommand StartReceiveFromSerialPortCommand { get; }
    public IRelayCommand StopReceiveFromSerialPortCommand { get; }
    public IRelayCommand OpenLocalSenderCommand { get; }
    public IRelayCommand CloseLocalSenderCommand { get; }


    //编码映射
    private readonly Dictionary<string, string> _fieldMappings = new Dictionary<string, string>
            {
                { "Temperature", "01" },
                { "Humidity", "02" },
                { "Pressure", "03" },
                { "WindSpeed", "04" },
                { "Rainfall", "05" }
            };
    public MainWindowVM(HJ_DbContext dbContext)
    {
        Connects = new ObservableCollection<IConnect>();
        _dbContext = dbContext;
        var machines = _dbContext.Machines.ToList();
        var receivers = machines.Where(_ => _.MachineType == MachineType.Receiver);
        var senders = machines.Where(_ => _.MachineType == MachineType.Sender);
        foreach (var sender in senders.Select(_ => ConnectFactory.CreateConnect(_.Id, _.Name, _.StrConnectType, _.Endpoints)))
        {
            Connects.Add(sender);
        }
        var serialPort = machines.FirstOrDefault(m => m.StrConnectType == "SerialPort");
        _redis = ConnectionMultiplexer.Connect("localhost:6379"); // 请根据实际情况修改Redis连接字符串
        _redisDB = _redis.GetDatabase();


        // 使用 SignalRConnect
        //Connect = ConnectFactory.CreateConnect(Guid.NewGuid(), "SignalRMachine", "signalr", "localhost:5000");
        //可以接收client发过来的消息
        //Connect.MessageReceived += OnMessageReceivedFromSignalR;


        Connect = Connects.FirstOrDefault(_ => _.ConnectType == ConnectType.Socket);//ConnectFactory.CreateConnect("Socket", "HostName:127.0.0.1;Port:12345");
        LocalSerialPort = ConnectFactory.CreateConnect(serialPort.Id, serialPort.Name, serialPort.StrConnectType, serialPort.Endpoints);

        _serialPortName = serialPort.Name;
        _serialPortEndpoints = serialPort.Endpoints;
        //Connect = ConnectFactory.CreateConnect("SerialPort", "HostName:COM4");
        //Connect = ConnectFactory.CreateConnect("RabbitMQ", "amqp://{guest }:{guest}@{localhost}:{port}/vhost");
        //Connect = ConnectFactory.CreateConnect("RabbitMQ", "HostName:localhost;UserName:guest;Password:guest;QueueName:hello");
        _serialPortStatus = "Closed";
        _output = new();
        _messagesSended = new();
        _messagesReceived = new();
        _data = new();
        //
        //SerialPort.GetPortNames()
        _serialPortNames = new ObservableCollection<string>(receivers.Select(_ => _.Endpoints));
        _random = new Random();
        SendCommand = new RelayCommand(async () => await SendData());
        StartSendCommand = new RelayCommand(StartSendingData);
        StopSendCommand = new RelayCommand(StopSendingData);
        SavePostSettingsCommand = new RelayCommand(SavePostSettings);
        StartReceiveFromSerialPortCommand = new RelayCommand(StartReceivingDataFromSerialPort);
        StopReceiveFromSerialPortCommand = new RelayCommand(StopReceivingDataFromSerialPort);
        OpenLocalSenderCommand = new RelayCommand(OpenLocalSender);
        CloseLocalSenderCommand = new RelayCommand(CloseLocalSender);
    }

    private void StartSendingData()
    {
        try
        {
            Connect.Connect();
            LogOutput("连接成功");
            //OpenSerialPort();
            _timer = new Timer(10000);
            _timer.Elapsed += async (sender, e) => await SendData();
            _timer.Start();
            LogOutput("Started sending data every minute.");
        }
        catch(Exception e)
        {
            LogOutput(e.Message);
        }
    }

    private void StopSendingData()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Dispose();
            _timer = null;
        }

        Connect.Disconnect();
        LogOutput("断开连接");
        CloseSerialPort();
    }

    private void OnMessageReceivedFromSerialPort(MessageReceivedEventArgs args)
    {
        var message = args.Message;
        // 拆分数据
        var messages = message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var msg in messages)
        {
            // 反转数据
            HJData reversedData;
            var flag = TryReverseHJData(msg, out reversedData);
            //此时id没有设置 将会导致redis中的内容会合并
            var messageModel = new Message { Id = Guid.NewGuid(),Content = msg, MachineId = args.MachineId, Time = args.ReveivedTime };
            // 存储消息到Redis
            _redisDB.ListRightPush("ReceivedMessages", JsonSerializer.Serialize(messageModel));

            //CacheMessageAsync(messageModel);
            _dbContext.Messages.Add(messageModel);

            // 更新界面
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (flag && reversedData != null)
                {
                    MessagesReceivedCount++;
                    MessagesReceived.Insert(0, messageModel);
                    //MessagesReceived.Add(messageModel);
                    Data.Clear();
                    foreach (var measurement in reversedData.Measurements)
                    {
                        Data.Add(new KeyValuePair<string, string>(measurement.Key, measurement.Value));
                    }
                }
                else
                {
                    MessagesReceivedFaildCount++;
                    LogOutput($"异常数据: {msg}");
                }
            });
        }
        _dbContext.SaveChanges();
    }

    private void StartReceivingDataFromSerialPort()
    {
        LocalSerialPort.Connect();
        LogOutput("连接Serial Port成功");
        LocalSerialPort.StartReceiveMessage();
        LocalSerialPort.MessageReceived += OnMessageReceivedFromSerialPort;
    }

    private void StopReceivingDataFromSerialPort()
    {
        if (LocalSerialPort != null)
        {
            LocalSerialPort.MessageReceived -= OnMessageReceivedFromSerialPort;
        }
        LocalSerialPort.Disconnect();
        LogOutput("断开Serial Port连接");
    }

    private void LogOutput(string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Application.Current.Dispatcher.Invoke(() =>
        {
            Output.Add($"[{timestamp}] {message}");
        });
    }

    private bool TryReverseHJData(string encodedData, out HJData? data)
    {
        data = null;
        // 假设HJ212Protocol.Decode是一个解码方法
        try
        {
            var temp = HJ212Protocol.Decode(encodedData, _fieldMappings);
            data = temp.Item2;
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }

    }

    private HJData GetHJData(out string encodedData)
    {
        var header = new HJ212Protocol.Header
        {
            Command = "01",
            DataLength = "0A"
        };

        var data = new HJ212Protocol.Data();
        data.Measurements.Add("Temperature", _random.Next(20, 30).ToString());
        data.Measurements.Add("Humidity", _random.Next(40, 60).ToString());
        data.Measurements.Add("Pressure", _random.Next(990, 1020).ToString());
        data.Measurements.Add("WindSpeed", _random.Next(0, 10).ToString());
        data.Measurements.Add("Rainfall", _random.Next(0, 100).ToString());

        encodedData = HJ212Protocol.Encode(header, data, _fieldMappings);
        return data;
    }

    /// <summary>
    /// 将缓存中的数据发送到远端
    /// </summary>
    /// <returns></returns>
    private async Task SendData()
    {

        var transaction = _redisDB.CreateTransaction();
        try
        {
            var message = _redisDB.ListLeftPop("ReceivedMessages");
            //var message = await transaction.ListLeftPopAsync("ReceivedMessages");
            //需要先检查连接
            if (!string.IsNullOrEmpty(message))
            {
                //var hj_data = GetHJData(out string encodedData);
                //Data = new ObservableCollection<KeyValuePair<string, string>>(hj_data.Measurements);

                //Message = "Data Sent: " + encodedData;
                //var messageModel = JsonSerializer.Deserialize<Message>(message);
                //LogOutput(Message);
                Connect.SendMessage(message);
                MessagesSendedCount++;

                //MessagesSended.Insert(0, messageModel);
                //MessagesSended.Add(messageModel);
            }
            else
            {
                LogOutput("No data to send.");
            }

            //await transaction.ExecuteAsync();
        }
        catch (SocketException ex)
        {
            //Connect.
            LogOutput(ex.Message);
        }
        catch (Exception ex)
        {
            LogOutput(ex.Message);
        }
        // 从Redis中读取消息
    }

    //private async Task SendToWebApi(string encodedData)
    //{
    //	using (var client = new HttpClient())
    //	{
    //		var content = new StringContent(encodedData, Encoding.UTF8, "application/json");
    //		var response = await client.PostAsync(UrlAddress ?? TempUrlAddress, content);
    //		if (response.IsSuccessStatusCode)
    //		{
    //			LogOutput("Data successfully sent to API.");
    //		}
    //		else
    //		{
    //			LogOutput("Failed to send data to API.");
    //		}
    //	}
    //}

    private void SavePostSettings()
    {
        //if (IsUrlPost)
        //{
        //	UrlAddress = TempUrlAddress;
        //}
        //if (string.IsNullOrWhiteSpace(SerialPortName))
        //{
        //	LogOutput($"非法串口{SerialPortName}");
        //	return;
        //}
        //if (_serialPort.IsOpen)
        //{
        //	_serialPort.Close();
        //}
        //_serialPort.PortName = SerialPortName;
        //_serialPort.BaudRate = TempSerialPortBaudRate;
        //LogOutput($"保存串口{SerialPortName}参数成功");
    }

    public void OpenSerialPort()
    {
        if (string.IsNullOrWhiteSpace(SerialPortName))
        {
            Message = "非法串口";
            Output.Add(Message);
            return;
        }
        if (!LocalSerialPort.IsConnected)
        {
            TempSerialPortBaudRate = 9600;
            LocalSerialPort.EndPoints = $"HostName:{SerialPortName};BaudRate:{TempSerialPortBaudRate}";
            LocalSerialPort.Connect();
            LocalSerialPort.StartReceiveMessage();
            SerialPortStatus = "Opened";
            Message = $"Opened serial port {SerialPortName}";
            Output.Add($"Opened serial port {SerialPortName}");
        }
    }

    public void CloseSerialPort()
    {
        if (LocalSerialPort.IsConnected)
        {
            LocalSerialPort.Disconnect();
            SerialPortStatus = "Closed";
            Output.Add($"Closed serial port {SerialPortName}");
        }
    }

    #region Redis

    // 获取订单
    public async Task<Message> GetOrderFromCacheAsync(string orderId)
    {
        var orderJson = await _redisDB.StringGetAsync(orderId);
        if (!string.IsNullOrEmpty(orderJson))
        {
            return JsonSerializer.Deserialize<Message>(orderJson);
        }
        return null;
    }

    // 添加或更新订单
    public async Task CacheMessageAsync(Message message)
    {
        var orderJson = JsonSerializer.Serialize(message);
        await _redisDB.StringSetAsync(message.Id.ToString(), orderJson, TimeSpan.FromMinutes(10)); // 缓存10分钟
    }

    // 从缓存中删除订单
    public async Task RemoveMessageFromCacheAsync(string orderId)
    {
        await _redisDB.KeyDeleteAsync(orderId);
    }
    #endregion

    Process _process;
    private void OpenLocalSender()
    {
        _process = new Process();
        _process.StartInfo.UseShellExecute = false;
        // You can start any process, HelloWorld is a do-nothing example.
        //_process.StartInfo.FileName = "C:\\HelloWorld.exe";
        //_process.StartInfo.cre = true;
        _process.StartInfo.FileName = "C:\\Users\\ijyy\\source\\repos\\SerialPortSender\\SerialPortSender\\bin\\Debug\\net8.0\\SerialPortSender.exe";
        //_process.StartInfo.FileName = "SerialPortSender.exe";
        _process.Start();
    }
    private void CloseLocalSender()
    {
        _process!.Kill();
    }
}

