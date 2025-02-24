using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace HJ212;


public class HJ212Protocol
{
	public class Header
	{
		public string StartFlag { get; set; } = "7E";  // 开始标志
		public string ProtocolVersion { get; set; } = "01";  // 协议版本
		public string Command { get; set; }  // 命令字
		public string DataLength { get; set; }  // 数据长度
		public string Checksum { get; set; }  // 校验和
		public string EndFlag { get; set; } = "7E";  // 结束标志
	}

	public class Data
	{
		public Dictionary<string, string> Measurements { get; set; } = new Dictionary<string, string>();
	}

	public static string Encode(Header header, Data data, Dictionary<string, string> fieldMappings)
	{
		StringBuilder sb = new StringBuilder();
		sb.Append(header.StartFlag);
		sb.Append(header.ProtocolVersion);
		sb.Append(header.Command);

		// 拼接数据区并计算数据区长度
		StringBuilder dataSection = new StringBuilder();
		foreach (var measurement in data.Measurements)
		{
			string mappedKey = fieldMappings.ContainsKey(measurement.Key) ? fieldMappings[measurement.Key] : measurement.Key;
			dataSection.Append(mappedKey);

			dataSection.Append(int.Parse(measurement.Value).ToString("X2"));
		}
		// 打印调试信息
		Console.WriteLine("Measurements:");
		foreach (var measurement in data.Measurements)
		{
			Console.WriteLine($"{measurement.Key}: {measurement.Value}");
		}
		Console.WriteLine($"DataSection: {dataSection.ToString()}");

		int dataLength = dataSection.Length;
		header.DataLength = dataLength.ToString("D4"); // 转换为4位十进制字符串
		sb.Append(header.DataLength);

		// 添加数据区
		sb.Append(dataSection.ToString());

		// 计算校验和
		string checksum = CalculateChecksum(sb.ToString());
		sb.Append(checksum);

		sb.Append(header.EndFlag);

		return sb.ToString();
	}

	public static (Header, Data) Decode(string rawData, Dictionary<string, string> fieldMappings)
	{
		Header header = new Header();
		Data data = new Data();

		if (rawData.Length < 16) throw new ArgumentException("Data is too short to be a valid HJ212 packet.");

		header.StartFlag = rawData.Substring(0, 2);
		header.ProtocolVersion = rawData.Substring(2, 2);
		header.Command = rawData.Substring(4, 2);
		header.DataLength = rawData.Substring(6, 4);
		int dataLength = Convert.ToInt32(header.DataLength); // 解析为十进制

		// 解码数据区
		string dataSection = rawData.Substring(10, dataLength);
		int index = 0;
		while (index < dataSection.Length)
		{
			string key = dataSection.Substring(index, 2);
			index += 2;
			string strValue = dataSection.Substring(index, 2);
			string value = Convert.ToInt32(strValue, 16).ToString();
			index += 2;

			// 根据映射字典转换原始键
			string mappedKey = fieldMappings.FirstOrDefault(x => x.Value == key).Key ?? key;
			data.Measurements.Add(mappedKey, value);
		}

		// 校验和验证
		string checksum = rawData.Substring(rawData.Length - 4, 2);
		if (checksum != CalculateChecksum(rawData.Substring(0, rawData.Length - 4)))
		{
			throw new ArgumentException("Checksum mismatch.");
		}

		return (header, data);
	}


	private static string CalculateChecksum(string data)
	{
		int sum = 0;
		foreach (char c in data)
		{
			sum += c;
		}
		return (sum & 0xFF).ToString("X2");  // 取低字节
	}
}



