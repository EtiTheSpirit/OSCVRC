﻿using OSCVRC.DataUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OSCVRC {

	/// <summary>
	/// An OSC client designed explicitly for VRChat. This is used to send and receive data of compatible types.
	/// </summary>
	public class VRCAvatarParameterOSCInterface : IDisposable {

		private const string AVATAR_PARAMETER_PATH_PREFIX = "/avatar/parameters";

		/// <summary>
		/// This matches incoming OSC data of integer or float type.
		/// </summary>
		//language=regex
		private const string REGEX_MATCH_INCOMING_OSC = @"([^,]+),([fi])\x00{2}([\x00-\xFF]{4})";

		/// <summary>
		/// This matches incoming OSC data of string type.
		/// </summary>
		//language=regex
		private const string REGEX_MATCH_INCOMING_OSC_STRING = @"([^,]+),(s)\x00{2}([^\x00]+)\x00{1,4}";

		/// <summary>
		/// This matches incoming OSC data of boolean type.
		/// </summary>
		//language=regex
		private const string REGEX_MATCH_INCOMING_OSC_BOOL = @"([^,]+),([TF])\x00{2}";

		private static readonly byte[] TAG_FLOAT = (new byte[] { (byte)',', (byte)'f', 0, 0 });
		private static readonly byte[] TAG_INT = (new byte[] { (byte)',', (byte)'i', 0, 0 });
		private static readonly byte[] TAG_TRUE = (new byte[] { (byte)',', (byte)'T', 0, 0 });
		private static readonly byte[] TAG_FALSE = (new byte[] { (byte)',', (byte)'F', 0, 0 });


		private bool _disposed;
		private readonly Socket _sender;
		private readonly Socket _receiver;
		private readonly IPEndPoint _sendToIP;
		private readonly IPEndPoint _receiveFromIP;
		private CancellationTokenSource _receiverCanceler;

		/// <summary>
		/// If true, actions will be logged, including sent and received parameters.
		/// </summary>
		public bool LogActions { get; set; } = false;

		/// <summary>
		/// Stores all received avatar parameters. The values in this dictionary contain the boxed value, and then the byte indicating the type (int = 0, float = 1, bool = 2).
		/// Stored values should be in little endian.
		/// </summary>
		private readonly Dictionary<string, Variant<int, float, bool>> _dataCache = new Dictionary<string, Variant<int, float, bool>>();

		/// <summary>
		/// This list contains bytes that have yet to be read due to the packet being incomplete.
		/// </summary>
		private readonly List<byte> _overflowBuffer = new List<byte>();

		/// <summary>
		/// Prepares to send and receive data from the provided addresses. Leaving them <see langword="null"/> uses VRChat's default.
		/// </summary>
		/// <param name="sendTo">The IP to send to. Uses VRChat's default if this is null.</param>
		/// <param name="receiveFrom">The IP to receive from. Uses VRChat's default if this is null.</param>
		public VRCAvatarParameterOSCInterface(IPEndPoint sendTo = null, IPEndPoint receiveFrom = null) {
			_sendToIP = sendTo ?? new IPEndPoint(IPAddress.Loopback, 9000);
			_receiveFromIP = receiveFrom ?? new IPEndPoint(IPAddress.Loopback, 9001);
			_sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_sender.Connect(_sendToIP);
			_receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_receiver.ReceiveTimeout = int.MaxValue;
			_receiver.SendTimeout = int.MaxValue;
			_receiver.Ttl = byte.MaxValue;
			_receiver.Bind(_receiveFromIP);
			_receiverCanceler = new CancellationTokenSource();

			try {
				Task.Run(ReceiveAllParameters, _receiverCanceler.Token);
			} catch (TaskCanceledException) { }
		}

		/// <inheritdoc/>
		~VRCAvatarParameterOSCInterface() { Dispose(); }

		#region Sender Helpers

		/// <summary>
		/// Constructs a new OSC packet. <see cref="int"/> and <see cref="float"/> types require the value to be placed in by the caller. <see cref="bool"/> types require the tag to be placed in by the caller.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="parameterName"></param>
		/// <returns></returns>
		/// <exception cref="NotSupportedException"></exception>
		private static byte[] NewOSCPacket<T>(string parameterName) where T : struct {
			byte[] paramNameBytes = OSCDataUtil.GetOSCString($"{AVATAR_PARAMETER_PATH_PREFIX}/{parameterName}");
			int orgLen = paramNameBytes.Length;

			if (typeof(T) == typeof(int)) {
				Array.Resize(ref paramNameBytes, orgLen + 4 + 4);
				Array.Copy(TAG_INT.ToArray(), 0, paramNameBytes, orgLen, 4);
			} else if (typeof(T) == typeof(float)) {
				Array.Resize(ref paramNameBytes, orgLen + 4 + 4);
				Array.Copy(TAG_FLOAT.ToArray(), 0, paramNameBytes, orgLen, 4);
			} else if (typeof(T) == typeof(bool)) {
				Array.Resize(ref paramNameBytes, orgLen + 4 + 0);
			} else {
				throw new NotSupportedException($"The provided type parameter ({typeof(T).FullName}) is not a VRChat-supported OSC type.");
			}
			return paramNameBytes;
		}

		private static void AppendToEnd(byte[] packet, byte[] valueBytes) {
			Array.Copy(valueBytes, 0, packet, packet.Length - valueBytes.Length, valueBytes.Length);
		}

		#endregion

		#region Senders

		/// <summary>
		/// Sets the provided parameter to the provided floating point value.
		/// </summary>
		/// <remarks>
		/// There are a few quirks to this method's behavior.
		/// <list type="bullet">
		/// <item>If the given parameter is not the appropriate type, the behavior is undefined and this might cause problems.</item>
		/// <item>Networked floats are fixed point, allowing units of 1/127th. This will still locally write the entire single precision float, however, and so reading it back will yield the same accurate value even though this may not be what is on the network.</item>
		/// </list>
		/// </remarks>
		/// <param name="parameterName">The name of the avatar parameter. This is case sensitive and must match exactly.</param>
		/// <param name="value">The value to set this parameter to.</param>
		/// <exception cref="ArgumentOutOfRangeException">If the input value is less than -1.0f or greater than 1.0f.</exception>
		public void SetAvatarParameter(string parameterName, float value) {
			if (_disposed) throw new ObjectDisposedException(GetType().Name);
			if (value < -1f || value > 1f) throw new ArgumentOutOfRangeException(nameof(value), "VRChat Float parameters only support values from -1.0f to +1.0f.");
			_dataCache[parameterName] = value;
			byte[] packet = NewOSCPacket<float>(parameterName);
			byte[] valueBytes = OSCDataUtil.GetBigEndianBytesOf(value);
			AppendToEnd(packet, valueBytes);
			OSCDataUtil.Pad(ref packet);
			_sender.Send(packet);
			if (LogActions) Logger.LogSend(parameterName, value);
		}

		/// <summary>
		/// Sets the provided parameter to the provided integer value.
		/// </summary>
		/// <remarks>
		/// If the given parameter is not the appropriate type, the behavior is undefined and this might cause problems.
		/// </remarks>
		/// <param name="parameterName">The name of the avatar parameter. This is case sensitive and must match exactly.</param>
		/// <param name="value">The value to set this parameter to.</param>
		/// <exception cref="ArgumentOutOfRangeException">If the input value is less than zero or greater than 255.</exception>
		public void SetAvatarParameter(string parameterName, int value) {
			if (_disposed) throw new ObjectDisposedException(GetType().Name);
			if (value < 0 || value > 255) throw new ArgumentOutOfRangeException(nameof(value), "VRChat Integer parameters only support values from 0 to 255.");
			SetAvatarParameter(parameterName, (byte)value);
		}

		/// <inheritdoc cref="SetAvatarParameter(string, int)"/>
		public void SetAvatarParameter(string parameterName, byte value) {
			if (_disposed) throw new ObjectDisposedException(GetType().Name);
			_dataCache[parameterName] = value;
			byte[] packet = NewOSCPacket<int>(parameterName);
			byte[] valueBytes = OSCDataUtil.GetBigEndianBytesOf(value);
			AppendToEnd(packet, valueBytes);
			OSCDataUtil.Pad(ref packet);
			_sender.Send(packet);
			if (LogActions) Logger.LogSend(parameterName, value);
		}

		/// <summary>
		/// Sets the provided parameter to the provided boolean value. This can be set to true to fire triggers. I think. I forgot to be honest.
		/// </summary>
		/// <remarks>
		/// If the given parameter is not the appropriate type, the behavior is undefined and this might cause problems.
		/// </remarks>
		/// <param name="parameterName">The name of the avatar parameter. This is case sensitive and must match exactly.</param>
		/// <param name="value">The value to set this parameter to.</param>
		public void SetAvatarParameter(string parameterName, bool value) {
			if (_disposed) throw new ObjectDisposedException(GetType().Name);
			_dataCache[parameterName] = value;
			byte[] packet = NewOSCPacket<bool>(parameterName); // Bool has no args and thus it has no tag assigned here because the tag is the value.
			byte[] valueBytes = value ? TAG_TRUE.ToArray() : TAG_FALSE.ToArray();
			AppendToEnd(packet, valueBytes);
			OSCDataUtil.Pad(ref packet);
			_sender.Send(packet);
			if (LogActions) Logger.LogSend(parameterName, value);
		}

		#endregion

		#region Receive Helpers

		private Task ReceiveAllParameters() {
			while (true) {
				byte[] buf = new byte[128];
				//EndPoint ep = new IPEndPoint(_receiveFromIP.Address, _receiveFromIP.Port);;
				//int amount = _receiver.ReceiveFrom(buf, ref ep);
				//int amount = await _receiver.ReceiveAsync(buf);
				int amount = _receiver.Receive(buf);

				Array.Resize(ref buf, amount);
				HandleParameters(buf);
			}
		}

		private bool PutIfChanged(string name, Variant<int, float, bool> value) {
			if (_dataCache.TryGetValue(name, out Variant<int, float, bool> existing)) {
				if (existing == value) return false;
			}
			_dataCache[name] = value;
			return true;
		}

		private void HandleParameters(byte[] buf) {
			string newReceivedInfo = OSCDataUtil.RawByteArrayToString(buf);
			newReceivedInfo = OSCDataUtil.RawByteArrayToString(_overflowBuffer.ToArray()) + newReceivedInfo; // Put anything missing onto the end here.

			MatchCollection @params = Regex.Matches(newReceivedInfo, REGEX_MATCH_INCOMING_OSC);
			foreach (Match match in @params) {
				if (!HandleMatch(match, ref newReceivedInfo)) break;
			}
			if (@params.Count == 0) {
				@params = Regex.Matches(newReceivedInfo, REGEX_MATCH_INCOMING_OSC_BOOL);
				foreach (Match match in @params) {
					if (!HandleMatch(match, ref newReceivedInfo)) break;
				}
			}
			if (@params.Count == 0) {
				@params = Regex.Matches(newReceivedInfo, REGEX_MATCH_INCOMING_OSC_STRING);
				foreach (Match match in @params) {
					if (!HandleMatch(match, ref newReceivedInfo)) break;
				}
			}

			_overflowBuffer.Clear();
			_overflowBuffer.AddRange(OSCDataUtil.StringToRawByteArray(newReceivedInfo));
		}

		private bool HandleMatch(Match match, ref string newReceivedInfo) {
			string parameterName = match.Groups[1].Value.Replace("\0", ""); // FULL PATH
			string type = match.Groups[2].Value;
			string rawValueStr = match.Groups[3].Value;
			byte[] rawValue = OSCDataUtil.StringToRawByteArray(rawValueStr);
			if (LogActions) Console.WriteLine(match.Value);


			if (parameterName.StartsWithGetAfter("/avatar/parameters/", out parameterName)) {
				return HandleParameterReceived(match.Length, ref newReceivedInfo, parameterName, type, rawValue);
			} else if (parameterName.StartsWithGetAfter("/avatar/change/", out string avatarId) && type == "s") {
				return HandleAvatarChanged(match.Length, ref newReceivedInfo, avatarId);
			} else {
				TruncateReceivedInfo(match.Length, ref newReceivedInfo);
			}
			
			return true;
		}

		private bool HandleAvatarChanged(int matchLength, ref string newReceivedInfo, string avatarId) {
			if (avatarId.StartsWithGetAfter("avtr_", out string id)) {
				if (Guid.TryParse(id, out Guid guid)) {
					AvatarChanged?.Invoke(guid);
					TruncateReceivedInfo(matchLength, ref newReceivedInfo);
				}
			}
			return false;
		}

		private bool HandleParameterReceived(int matchLength, ref string newReceivedInfo, string parameterName, string type, byte[] rawValue) {
			if (type == "T" || type == "F") {
				bool value = type == "T";
				bool changed = PutIfChanged(parameterName, new Variant<int, float, bool>(value));
				if (changed) {
					if (LogActions) Logger.LogReceiveParameter(parameterName, value);
					ParameterChanged?.Invoke(parameterName, value);
					BoolParameterChanged?.Invoke(parameterName, value);
				}
			} else if (type == "i") {
				if (rawValue.Length != 4) {
					return false; // Exit early. This parameter isn't yet completely sent over the network
				}
				int value = OSCDataUtil.GetIntFromBigEndian(rawValue);
				bool changed = PutIfChanged(parameterName, new Variant<int, float, bool>(value));
				if (changed) {
					if (LogActions) Logger.LogReceiveParameter(parameterName, value);
					ParameterChanged?.Invoke(parameterName, value);
					IntParameterChanged?.Invoke(parameterName, value);
				}
			} else if (type == "f") {
				if (rawValue.Length != 4) {
					return false; // Exit early. This parameter isn't yet completely sent over the network.
				}
				float value = OSCDataUtil.GetFloatFromBigEndian(rawValue);
				bool changed = PutIfChanged(parameterName, new Variant<int, float, bool>(value));
				if (changed) {
					if (LogActions) Logger.LogReceiveParameter(parameterName, value);
					ParameterChanged?.Invoke(parameterName, value);
					FloatParameterChanged?.Invoke(parameterName, value);
				}
			}

			TruncateReceivedInfo(matchLength, ref newReceivedInfo);
			return true;
		}

		private void TruncateReceivedInfo(int matchLength, ref string newReceivedInfo) {
			if (matchLength == newReceivedInfo.Length) {
				newReceivedInfo = string.Empty;
			} else {
				newReceivedInfo = newReceivedInfo.Substring(matchLength); // Skip ahead
			}
		}


		#endregion

		#region Receivers

		/// <summary>
		/// Returns the parameter with the provided name. Returns whether or not the action yielded a result.
		/// </summary>
		/// <param name="parameterName"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public bool TryGetAvatarParameter(string parameterName, out int value) {
			if (_dataCache.TryGetValue(parameterName, out Variant<int, float, bool> info)) {
				if (info.index == 1) {
					value = info.valueType1;
					return true;
				}
			}
			value = default;
			return false;
		}

		/// <inheritdoc cref="TryGetAvatarParameter(string, out int)"/>
		public bool TryGetAvatarParameter(string parameterName, out float value) {
			if (_dataCache.TryGetValue(parameterName, out Variant<int, float, bool> info)) {
				if (info.index == 2) {
					value = info.valueType2;
					return true;
				}
			}
			value = default;
			return false;
		}

		/// <inheritdoc cref="TryGetAvatarParameter(string, out int)"/>
		public bool TryGetAvatarParameter(string parameterName, out bool value) {
			if (_dataCache.TryGetValue(parameterName, out Variant<int, float, bool> info)) {
				if (info.index == 3) {
					value = info.valueType3;
					return true;
				}
			}
			value = default;
			return false;
		}

		/// <summary>
		/// Returns all known parameters.
		/// </summary>
		/// <returns></returns>
		public IReadOnlyDictionary<string, Variant<int, float, bool>> GetAllParameters() {
			return _dataCache;
		}

		#endregion

		#region Events

		#region Parameter Events
		/// <summary>
		/// Fired when a <see cref="float"/> parameter is changed.
		/// </summary>
		public event ParameterChangedDelegate<float> FloatParameterChanged;

		/// <summary>
		/// Fired when an <see cref="int"/> parameter is changed.
		/// </summary>
		public event ParameterChangedDelegate<int> IntParameterChanged;

		/// <summary>
		/// Fired when a <see cref="bool"/> parameter is changed.
		/// </summary>
		public event ParameterChangedDelegate<bool> BoolParameterChanged;

		/// <summary>
		/// Fired when any parameter is changed.
		/// </summary>
		public event ParameterChangedDelegate<Variant<int, float, bool>> ParameterChanged;
		#endregion

		#region Avatar Events
		/// <summary>
		/// Fired when your avatar changes.
		/// </summary>
		public event AvatarChangeDelegate AvatarChanged;
		#endregion

		/// <summary>
		/// A delegate for when a parameter changes.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <param name="value"></param>
		public delegate void ParameterChangedDelegate<T>(string name, T value);

		/// <summary>
		/// A delegate for when the player's avatar changes.
		/// </summary>
		/// <param name="avatarID"></param>
		public delegate void AvatarChangeDelegate(Guid avatarID);

		#endregion

		/// <summary>
		/// Close this client, preventing it from sending any data.
		/// </summary>
		public void Dispose() {
			if (_disposed) return;
			_disposed = true;
			_receiverCanceler.Cancel();
			GC.SuppressFinalize(this);
			_sender.Dispose();
			_dataCache.Clear();
		}
	}
}