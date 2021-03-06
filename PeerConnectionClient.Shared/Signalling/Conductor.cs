﻿//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Linq;
using System.Collections.Generic;
using Windows.Networking.Connectivity;
using Windows.Networking;
using Windows.Data.Json;
using System.Diagnostics;
using System.Threading.Tasks;
using PeerConnectionClient.Model;
using System.Collections.ObjectModel;
using PeerConnectionClient.Utilities;
using System.Threading;
using org.ortc;
using org.ortc.adapter;
using RTCRtpCodecCapability = org.ortc.RTCRtpCodecCapability;
using PeerConnectionClient.Media_Extension;
using PeerConnectionClient.ViewModels;
using PeerConnectionClient.Win10.Shared;
using RTCIceCandidate = org.ortc.adapter.RTCIceCandidate;
using System.Text.RegularExpressions;

namespace PeerConnectionClient.Signalling
{
    /// <summary>
    /// A singleton conductor for WebRTC session.
    /// </summary>
    internal class Conductor
    {
        private static readonly object InstanceLock = new object();
        private readonly object _mediaLock = new object();
        private static Conductor _instance;
        private RTCPeerConnectionSignalingMode _signalingMode;

        /// <summary>
        ///  The single instance of the Conductor class.
        /// </summary>
        public static Conductor Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new Conductor();
                        }
                    }
                }
                return _instance;
            }
        }

        private readonly Signaller _signaller;

        /// <summary>
        /// The signaller property.
        /// Helps to pass WebRTC session signals between client and server.
        /// </summary>
        public Signaller Signaller => _signaller;

        /// <summary>
        /// Video codec used in WebRTC session.
        /// </summary>
        public RTCRtpCodecCapability VideoCodec { get; set; }

        /// <summary>
        /// Audio codec used in WebRTC session.
        /// </summary>
        public RTCRtpCodecCapability AudioCodec { get; set; }
        public CaptureCapability VideoCaptureProfile;

        // SDP negotiation attributes
        private static readonly string kCandidateSdpMidName = "sdpMid";
        private static readonly string kCandidateSdpMlineIndexName = "sdpMLineIndex";
        private static readonly string kCandidateSdpName = "candidate";
        private static readonly string kSessionDescriptionTypeName = "type";
        private static readonly string kSessionDescriptionSdpName = "sdp";
        private static readonly string kSessionDescriptionJsonName = "session";

        RTCPeerConnection _peerConnection;
        readonly Media _media;

        /// <summary>
        /// Media property to provide media details.
        /// </summary>
        public Media Media => _media;

        public ObservableCollection<Peer> Peers;
        public Peer Peer;
        MediaStream MediaStream { get; set; }
        //private IList<MediaStreamTrack> Tracks { get; set; }
        readonly List<RTCIceServer> _iceServers;

        private int _peerId = -1;
        protected bool _videoEnabled = true;
        protected bool _audioEnabled = true;
        protected String _sessionId;

        bool _etwStatsEnabled;

        /// <summary>
        /// Enable/Disable ETW stats used by WebRTCDiagHubTool Visual Studio plugin.
        /// If the ETW Stats are disabled, no data will be sent to the plugin.
        /// </summary>
        public bool ETWStatsEnabled
        {
            get
            {
                return _etwStatsEnabled;
            }
            set
            {
                _etwStatsEnabled = value;
                if (_peerConnection != null)
                {
                    //_peerConnection.EtwStatsEnabled = value;
                }
            }
        }

        bool _peerConnectionStatsEnabled;

        /// <summary>
        /// Enable/Disable connection health stats.
        /// Connection health stats are delivered by the OnConnectionHealthStats event. 
        /// </summary>
        public bool PeerConnectionStatsEnabled
        {
            get
            {
                return _peerConnectionStatsEnabled;
            }
            set
            {
                _peerConnectionStatsEnabled = value;
                if (_peerConnection != null)
                {
                    //_peerConnection.ConnectionHealthStatsEnabled = value;
                }
            }
        }

        bool _appInsightsEnabled;


        public bool AppInsightsEnabled
        {
            get
            {
                return _appInsightsEnabled;
            }
            set
            {
                _appInsightsEnabled = value;
                OrtcStatsManager.Instance.IsStatsCollectionEnabled = value;
            }
        }
        CancellationTokenSource connectToPeerCancelationTokenSource;
        Task<bool> connectToPeerTask;

        // Public events for adding and removing the local stream
        public event Action<MediaStreamEvent> OnAddLocalStream;

        // Public events to notify about connection status
        public event Action OnPeerConnectionCreated;
        public event Action OnPeerConnectionClosed;
        public event Action OnReadyToConnect;

        /// <summary>
        /// Updates the preferred video frame rate and resolution.
        /// </summary>
        public void updatePreferredFrameFormat()
        {
          if (VideoCaptureProfile != null)
          {
                _media.SetPreferredVideoCaptureFormat(
                    (int)VideoCaptureProfile.Width, (int)VideoCaptureProfile.Height, (int)VideoCaptureProfile.FrameRate);
/*#else
            WebRTC.SetPreferredVideoCaptureFormat(
              (int)VideoCaptureProfile.Width, (int)VideoCaptureProfile.Height, (int)VideoCaptureProfile.FrameRate);
#endif*/
          }
        }

        /// <summary>
        /// Creates a peer connection.
        /// </summary>
        /// <returns>True if connection to a peer is successfully created.</returns>
        private async Task<bool> CreatePeerConnection(CancellationToken cancelationToken)
        {
            Debug.Assert(_peerConnection == null);
            if(cancelationToken.IsCancellationRequested)
            {
                return false;
            }

            var config = new RTCConfiguration()
            {
                BundlePolicy = RTCPeerConnectionSignalingMode.Json == _signalingMode ? RTCBundlePolicy.MaxBundle : RTCBundlePolicy.MaxBundle,
                SignalingMode = _signalingMode,//RTCSessionDescriptionSignalingType.Json,
                //IceTransportPolicy = RTCIceTransportPolicy.All,
                GatherOptions = new RTCIceGatherOptions()
                { 
                    IceServers = new List<RTCIceServer>(_iceServers),
                }
            };

            Debug.WriteLine("Conductor: Creating peer connection.");
            _peerConnection = new RTCPeerConnection(config);
            //_peerConnection.EtwStatsEnabled = _etwStatsEnabled;
            //_peerConnection.ConnectionHealthStatsEnabled = _peerConnectionStatsEnabled;
            if (cancelationToken.IsCancellationRequested)
            {
                return false;
            }

            if (_peerConnection == null)
                throw new NullReferenceException("Peer connection is not created.");

            //if (AppInsightsEnabled)
            {
                OrtcStatsManager.Instance.Initialize(_peerConnection);
                //StatsManager.Instance.Initialize(_peerConnection);
                //StatsManager.Instance.IsStatsCollectionEnabled = true;
            }

            OnPeerConnectionCreated?.Invoke();

            _peerConnection.OnIceCandidate += PeerConnection_OnIceCandidate;
            _peerConnection.OnTrack += PeerConnection_OnAddTrack;
            _peerConnection.OnTrackGone += PeerConnection_OnRemoveTrack;
            _peerConnection.OnIceConnectionStateChange += () => { Debug.WriteLine("Conductor: Ice connection state change, state=" + (null != _peerConnection ? _peerConnection.IceConnectionState.ToString() : "closed")); };
            //_peerConnection.OnConnectionHealthStats += PeerConnection_OnConnectionHealthStats;

            Debug.WriteLine("Conductor: Getting user media.");
            RTCMediaStreamConstraints mediaStreamConstraints = new RTCMediaStreamConstraints
            {
                // Always include audio/video enabled in the media stream,
                // so it will be possible to enable/disable audio/video if 
                // the call was initiated without microphone/camera
                audioEnabled = true,
                videoEnabled = true
            };

            

            if (cancelationToken.IsCancellationRequested)
            {
                return false;
            }

            var tracks = await _media.GetUserMedia(mediaStreamConstraints);
            if (tracks != null)
            {
                RTCRtpCapabilities audioCapabilities = RTCRtpSender.GetCapabilities("audio");
                RTCRtpCapabilities videoCapabilities = RTCRtpSender.GetCapabilities("video");

                MediaStream = new MediaStream(tracks);
                Debug.WriteLine("Conductor: Adding local media stream.");
                IList<MediaStream> mediaStreamList = new List<MediaStream>();
                mediaStreamList.Add(MediaStream);
                foreach (var mediaStreamTrack in tracks)
                {
                    //Create stream track configuration based on capabilities
                    RTCMediaStreamTrackConfiguration configuration = null;
                    if (mediaStreamTrack.Kind == MediaStreamTrackKind.Audio && audioCapabilities != null)
                    {
                        configuration =
                            await Helper.GetTrackConfigurationForCapabilities(audioCapabilities, AudioCodec);
                    }
                    else if (mediaStreamTrack.Kind == MediaStreamTrackKind.Video && videoCapabilities != null)
                    {
                        configuration =
                            await Helper.GetTrackConfigurationForCapabilities(videoCapabilities, VideoCodec);
                    }
                    if (configuration != null)
                        _peerConnection.AddTrack(mediaStreamTrack, mediaStreamList, configuration);
                }
            }

            if (cancelationToken.IsCancellationRequested)
            {
                return false;
            }

            
            /*foreach (var mediaStreamTrack in _mediaStream.GetTracks())
            {
                _peerConnection.AddTrack(mediaStreamTrack, mediaStreamList);
            }*/
            OnAddLocalStream?.Invoke(new MediaStreamEvent() { Stream = MediaStream });

            if (cancelationToken.IsCancellationRequested)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Closes a peer connection.
        /// </summary>
        private void ClosePeerConnection()
        {
            lock (_mediaLock)
            {
                if (_peerConnection != null)
                {
                    _peerId = -1;
                    if (MediaStream != null)
                    {
                        foreach (var track in MediaStream.GetTracks())
                        {
                            track.Stop();
                            MediaStream.RemoveTrack(track);
                        }
                    }
                    MediaStream = null;

                    OnPeerConnectionClosed?.Invoke();

                    _peerConnection.Close(); // Slow, so do this after UI updated and camera turned off
                    _sessionId = null;
                    //if (AppInsightsEnabled)
                    {
                        OrtcStatsManager.Instance.CallEnded();
                    }
                        //StatsManager.Instance.TrackCallEnded();
                    _peerConnection = null;

                    OnReadyToConnect?.Invoke();

                    GC.Collect(); // Ensure all references are truly dropped.
                }
            }
        }

        /// <summary>
        /// Called when WebRTC detects another ICE candidate. 
        /// This candidate needs to be sent to the other peer.
        /// </summary>
        /// <param name="evt">Details about RTC Peer Connection Ice event.</param>
        private void PeerConnection_OnIceCandidate(RTCPeerConnectionIceEvent evt)
        {
            double index = null != evt.Candidate.SdpMLineIndex ? (double) evt.Candidate.SdpMLineIndex : -1;

            JsonObject json;

            String message;
            if (RTCPeerConnectionSignalingMode.Json == _signalingMode)
            {
                message = evt.Candidate.ToJsonString();
                json = JsonObject.Parse(message);
            }
            else
            {
                message = evt.Candidate.Candidate;
                json = new JsonObject
                {
                    {kCandidateSdpMidName, JsonValue.CreateStringValue(evt.Candidate.SdpMid)},
                    {kCandidateSdpMlineIndexName, JsonValue.CreateNumberValue(index)},
                    {kCandidateSdpName, JsonValue.CreateStringValue(message)}
                };
            }

            Debug.WriteLine("Conductor: Sending ice candidate.\n" + json.Stringify());
            SendMessage(json);
        }

        /// <summary>
        /// Invoked when the remote peer added a media stream to the peer connection.
        /// </summary>
        public event Action<RTCTrackEvent> OnAddRemoteTrack;
        private void PeerConnection_OnAddTrack(RTCTrackEvent evt)
        {
            OnAddRemoteTrack?.Invoke(evt);
        }

        /// <summary>
        /// Invoked when the remote peer removed a media stream from the peer connection.
        /// </summary>
        public event Action<RTCTrackEvent> OnRemoveTrack;
        private void PeerConnection_OnRemoveTrack(RTCTrackEvent evt)
        {
            OnRemoveTrack?.Invoke(evt);
        }

        /// <summary>
        /// Invoked when new connection health stats are available.
        /// Use ToggleConnectionHealthStats to turn on/of the connection health stats.
        /// </summary>
        public event Action<RTCPeerConnectionHealthStats> OnConnectionHealthStats;
        private void PeerConnection_OnConnectionHealthStats(RTCPeerConnectionHealthStats stats)
        {
            OnConnectionHealthStats?.Invoke(stats);
        }

        /// <summary>
        /// Private constructor for singleton class.
        /// </summary>
        private Conductor()
        {
            _signaller = new Signaller();
            _media = Media.CreateMedia();
            AppInsightsEnabled = false;
            Signaller.OnDisconnected += Signaller_OnDisconnected;
            Signaller.OnMessageFromPeer += Signaller_OnMessageFromPeer;
            Signaller.OnPeerConnected += Signaller_OnPeerConnected;
            Signaller.OnPeerHangup += Signaller_OnPeerHangup;
            Signaller.OnPeerDisconnected += Signaller_OnPeerDisconnected;
            Signaller.OnServerConnectionFailure += Signaller_OnServerConnectionFailure;
            Signaller.OnSignedIn += Signaller_OnSignedIn;

            _iceServers = new List<RTCIceServer>();
        }

        /// <summary>
        /// Handler for Signaller's OnPeerHangup event.
        /// </summary>
        /// <param name="peerId">ID of the peer to hung up the call with.</param>
        void Signaller_OnPeerHangup(int peerId)
        {
            if (peerId == _peerId)
            {
                Debug.WriteLine("Conductor: Our peer hung up.");
                ClosePeerConnection();
            }
        }

        /// <summary>
        /// Handler for Signaller's OnSignedIn event.
        /// </summary>
        private void Signaller_OnSignedIn()
        {
        }

        /// <summary>
        /// Handler for Signaller's OnServerConnectionFailure event.
        /// </summary>
        private void Signaller_OnServerConnectionFailure()
        {
            Debug.WriteLine("[Error]: Connection to server failed!");
        }

        /// <summary>
        /// Handler for Signaller's OnPeerDisconnected event.
        /// </summary>
        /// <param name="peerId">ID of disconnected peer.</param>
        private void Signaller_OnPeerDisconnected(int peerId)
        {
            // is the same peer or peer_id does not exist (0) in case of 500 Error
            if (peerId == _peerId || peerId == 0)
            {
                Debug.WriteLine("Conductor: Our peer disconnected.");
                ClosePeerConnection();
            }
        }

        /// <summary>
        /// Handler for Signaller's OnPeerConnected event.
        /// </summary>
        /// <param name="id">ID of the connected peer.</param>
        /// <param name="name">Name of the connected peer.</param>
        private void Signaller_OnPeerConnected(int id, string name)
        {
        }

        /// <summary>
        /// Handler for Signaller's OnMessageFromPeer event.
        /// </summary>
        /// <param name="peerId">ID of the peer.</param>
        /// <param name="message">Message from the peer.</param>
        private void Signaller_OnMessageFromPeer(int peerId, string message)
        {
            Task.Run(async () =>
            {
                Debug.Assert(_peerId == peerId || _peerId == -1);
                Debug.Assert(message.Length > 0);
                
                if (_peerId != peerId && _peerId != -1)
                {
                    Debug.WriteLine("[Error] Conductor: Received a message from unknown peer while already in a conversation with a different peer.");
                    return;
                }

                JsonObject jMessage;
                if (!JsonObject.TryParse(message, out jMessage))
                {
                    Debug.WriteLine("[Error] Conductor: Received unknown message." + message);
                    return;
                }

                string type = jMessage.ContainsKey(kSessionDescriptionTypeName) ? jMessage.GetNamedString(kSessionDescriptionTypeName) : null;

                bool created = false;

                if (_peerConnection == null)
                {
                    if (!String.IsNullOrEmpty(type))
                    {
                        // Create the peer connection only when call is
                        // about to get initiated. Otherwise ignore the
                        // messages from peers which could be a result
                        // of old (but not yet fully closed) connections.
                        if (type == "offer" || type == "answer" || type == "json")
                        {
                            Debug.Assert(_peerId == -1);
                            _peerId = peerId;

                            created = true;

                            IEnumerable<Peer> enumerablePeer = Peers.Where(x => x.Id == peerId);
                            Peer = enumerablePeer.First();
                            _signalingMode = Helper.SignalingModeForClientName(Peer.Name);

                            connectToPeerCancelationTokenSource = new CancellationTokenSource();
                            connectToPeerTask = CreatePeerConnection(connectToPeerCancelationTokenSource.Token);
                            bool connectResult = await connectToPeerTask;
                            connectToPeerTask = null;
                            connectToPeerCancelationTokenSource.Dispose();
                            if (!connectResult)
                            {
                                Debug.WriteLine("[Error] Conductor: Failed to initialize our PeerConnection instance");
                                await Signaller.SignOut();
                                return;
                            }
                            else if (_peerId != peerId)
                            {
                                Debug.WriteLine("[Error] Conductor: Received a message from unknown peer while already in a conversation with a different peer.");
                                return;
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("[Warn] Conductor: Received an untyped message after closing peer connection.");
                        return;
                    }
                }

                if (!String.IsNullOrEmpty(type))
                {
                    if (type == "offer-loopback")
                    {
                        // Loopback not supported
                        Debug.Assert(false);
                    }

                    string formatted = null;

                    if (jMessage.ContainsKey(kSessionDescriptionJsonName))
                    {
                        var containerObject = new JsonObject { { kSessionDescriptionJsonName, jMessage.GetNamedObject(kSessionDescriptionJsonName) } };
                        formatted = containerObject.Stringify();
                    }
                    else if (jMessage.ContainsKey(kSessionDescriptionSdpName))
                    {
                        formatted = jMessage.GetNamedString(kSessionDescriptionSdpName);
                    }

                    if (String.IsNullOrEmpty(formatted))
                    {
                        Debug.WriteLine("[Error] Conductor: Can't parse received session description message.");
                        return;
                    }

                    RTCSessionDescriptionSignalingType messageType = RTCSessionDescriptionSignalingType.SdpOffer;
                    switch (type)
                    {
                        case "json": messageType = RTCSessionDescriptionSignalingType.Json; break;
                        case "offer": messageType = RTCSessionDescriptionSignalingType.SdpOffer; break;
                        case "answer": messageType = RTCSessionDescriptionSignalingType.SdpAnswer; break;
                        case "pranswer": messageType = RTCSessionDescriptionSignalingType.SdpPranswer; break;
                        default: Debug.Assert(false, type); break;
                    }

                    Debug.WriteLine("Conductor: Received session description: " + messageType.ToString() + "\n" + formatted);
                    if (_peerConnection != null)
                    {
                        await _peerConnection.SetRemoteDescription(new RTCSessionDescription(messageType, formatted));

                        if ((messageType == RTCSessionDescriptionSignalingType.SdpOffer) ||
                            ((created) &&
                             (messageType == RTCSessionDescriptionSignalingType.Json)))
                        {
                            var answer = await _peerConnection.CreateAnswer();
                            await _peerConnection.SetLocalDescription(answer);
                            // Send answer
                            Debug.WriteLine("Conductor: Sending answer: " + answer.FormattedDescription);
                            SendSdp(answer);
                            //if (AppInsightsEnabled)
                                OrtcStatsManager.Instance.StartCallWatch(_sessionId, false);
                                //StatsManager.Instance.TrackCallStarted();
                        }
                    }
                }
                else
                {
                    RTCIceCandidate candidate = null;
                    if (RTCPeerConnectionSignalingMode.Json != _signalingMode)
                    {
                        var sdpMid = jMessage.ContainsKey(kCandidateSdpMidName) ? jMessage.GetNamedString(kCandidateSdpMidName) : null;
                        var sdpMlineIndex = jMessage.ContainsKey(kCandidateSdpMlineIndexName) ? jMessage.GetNamedNumber(kCandidateSdpMlineIndexName) : -1;
                        var sdp = jMessage.ContainsKey(kCandidateSdpName) ? jMessage.GetNamedString(kCandidateSdpName) : null;
                        if ((String.IsNullOrEmpty(sdpMid) && (sdpMlineIndex == -1)) || String.IsNullOrEmpty(sdp))
                        {
                            Debug.WriteLine("[Error] Conductor: Can't parse received message.\n" + message);
                            return;
                        }
                        candidate = String.IsNullOrEmpty(sdpMid) ? RTCIceCandidate.FromSdpStringWithMLineIndex(sdp, (ushort)sdpMlineIndex) : RTCIceCandidate.FromSdpStringWithMid(sdp, sdpMid);
                    }
                    else
                    {
                        candidate = RTCIceCandidate.FromJsonString(message);
                    }

                    _peerConnection?.AddIceCandidate(candidate);
                    Debug.WriteLine("Conductor: Received candidate : " + message);
                }
            }).Wait();
        }

        /// <summary>
        /// Handler for Signaller's OnDisconnected event handler.
        /// </summary>
        private void Signaller_OnDisconnected()
        {
            ClosePeerConnection();
        }

        /// <summary>
        /// Starts the login to server process.
        /// </summary>
        /// <param name="server">The host server.</param>
        /// <param name="port">The port to connect to.</param>
        public void StartLogin(string server, string port)
        {
            if (_signaller.IsConnected())
            {
                return;
            }
            _signaller.Connect(server, port, GetLocalPeerName());
        }
       
        /// <summary>
        /// Calls to disconnect the user from the server.
        /// </summary>
        public async Task DisconnectFromServer()
        {
            if (_signaller.IsConnected())
            {
                await _signaller.SignOut();
            }
        }

        /// <summary>
        /// Calls to connect to the selected peer.
        /// </summary>
        /// <param name="peer">Peer to connect to.</param>
        public async void ConnectToPeer(Peer peer)
        {
            Debug.Assert(peer != null);
            Debug.Assert(_peerId == -1);

            if (_peerConnection != null)
            {
                Debug.WriteLine("[Error] Conductor: We only support connecting to one peer at a time");
                return;
            }
            _signalingMode = Helper.SignalingModeForClientName(peer.Name);
            connectToPeerCancelationTokenSource = new CancellationTokenSource();
            connectToPeerTask = CreatePeerConnection(connectToPeerCancelationTokenSource.Token);
            bool connectResult = await connectToPeerTask;
            connectToPeerTask = null;
            connectToPeerCancelationTokenSource.Dispose();

            if (connectResult)
            {
                _peerId = peer.Id;
                if (_peerConnection != null)
                {
                    var offer = await _peerConnection.CreateOffer();
                    await _peerConnection.SetLocalDescription(offer);

                    Debug.WriteLine("Conductor: Sending offer: " + offer.FormattedDescription);
                    SendSdp(offer);
                    //if (AppInsightsEnabled)
                        OrtcStatsManager.Instance.StartCallWatch(_sessionId,true);
                    //StatsManager.Instance.TrackCallStarted();
                }
            }
        }

        /// <summary>
        /// Calls to disconnect from peer.
        /// </summary>
        public async Task DisconnectFromPeer()
        {
            await SendHangupMessage();
            ClosePeerConnection();
        }

        /// <summary>
        /// Constructs and returns the local peer name.
        /// </summary>
        /// <returns>The local peer name.</returns>
        private string GetLocalPeerName()
        {
            var hostname = NetworkInformation.GetHostNames().FirstOrDefault(h => h.Type == HostNameType.DomainName);
            string ret =  hostname?.CanonicalName ?? "<unknown host>";
            ret = ret +"-dual";
            return ret;
        }

        /// <summary>
        /// Sends SDP message.
        /// </summary>
        /// <param name="description">RTC session description.</param>
        private void SendSdp(RTCSessionDescription description)
        {
            var type = description.Type.ToString().ToLower();

            String formattedDescription = description.FormattedDescription;

            JsonObject json = null;
            if (description.Type == RTCSessionDescriptionSignalingType.Json)
            {
                if (String.IsNullOrEmpty(_sessionId))
                {
                    var match = Regex.Match(formattedDescription, "{\"username\":\"-*[a-zA-Z0-9]*\",\"id\":\"([0-9]+)\"");
                    if (match.Success)
                    {
                        _sessionId = match.Groups[1].Value;
                    }
                }
                var jsonDescription = JsonObject.Parse(formattedDescription);
                var sessionValue = jsonDescription.GetNamedObject(kSessionDescriptionJsonName);
                json = new JsonObject
                {
                    {kSessionDescriptionTypeName, JsonValue.CreateStringValue(type)},
                    {kSessionDescriptionJsonName,  sessionValue}
                };
            }
            else
            {
                var match = Regex.Match(formattedDescription, "o=[^ ]+ ([0-9]+) [0-9]+ [a-zA-Z]+ [a-zA-Z0-9]+ [0-9\\.]+");
                if (match.Success)
                {
                    _sessionId = match.Groups[1].Value;
                }

                var prefix = type.Substring(0, "sdp".Length);
                if (prefix == "sdp")
                {
                    type = type.Substring("sdp".Length);
                }

                json = new JsonObject
                {
                    {kSessionDescriptionTypeName, JsonValue.CreateStringValue(type)},
                    {kSessionDescriptionSdpName, JsonValue.CreateStringValue(formattedDescription)}
                };
            }
            SendMessage(json);
        }

        /// <summary>
        /// Helper method to send a message to a peer.
        /// </summary>
        /// <param name="json">Message body.</param>
        private void SendMessage(IJsonValue json)
        {
            // Don't await, send it async.
            var task = _signaller.SendToPeer(_peerId, json);
        }

        /// <summary>
        /// Helper method to send a hangup message to a peer.
        /// </summary>
        private async Task SendHangupMessage()
        {
            await _signaller.SendToPeer(_peerId, "BYE");
            //if (AppInsightsEnabled)
            {
                //OrtcStatsManager.Instance.CallEnded();
            }
                //StatsManager.Instance.TrackCallEnded();
        }

        /// <summary>
        /// Enables the local video stream.
        /// </summary>
        public void EnableLocalVideoStream()
        {
            lock (_mediaLock)
            {
                if (MediaStream != null)
                {
                    foreach (MediaStreamTrack videoTrack in MediaStream.GetVideoTracks())
                    {
                        videoTrack.Enabled = true;
                    }
                }
                _videoEnabled = true;
            }
        }

        /// <summary>
        /// Disables the local video stream.
        /// </summary>
        public void DisableLocalVideoStream()
        {
            lock (_mediaLock)
            {
                if (MediaStream != null)
                {
                    foreach (MediaStreamTrack videoTrack in MediaStream.GetVideoTracks())
                    {
                        videoTrack.Enabled = false;
                    }
                }
                _videoEnabled = false;
            }
        }

        /// <summary>
        /// Mutes the microphone.
        /// </summary>
        public void MuteMicrophone()
        {
            lock (_mediaLock)
            {
                if (MediaStream != null)
                {
                    foreach (MediaStreamTrack audioTrack in MediaStream.GetAudioTracks())
                    {
                        audioTrack.Enabled = false;
                    }
                }
                _audioEnabled = false;
            }
        }

        /// <summary>
        /// Unmutes the microphone.
        /// </summary>
        public void UnmuteMicrophone()
        {
            lock (_mediaLock)
            {
                if (MediaStream != null)
                {
                    foreach (MediaStreamTrack audioTrack in MediaStream.GetAudioTracks())
                    {
                        audioTrack.Enabled = true;
                    }
                }
                _audioEnabled = true;
            }
        }

        /// <summary>
        /// Receives a new list of Ice servers and updates the local list of servers.
        /// </summary>
        /// <param name="iceServers">List of Ice servers to configure.</param>
        public void ConfigureIceServers(Collection<IceServer> iceServers)
        {
            _iceServers.Clear();
            foreach(IceServer iceServer in iceServers)
            {
                //Url format: stun:stun.l.google.com:19302
                string url = "stun:";
                if (iceServer.Type == IceServer.ServerType.TURN)
                {
                    url = "turn:";
                }
                url += iceServer.Host.Value;
                RTCIceServer server = new RTCIceServer()
                {
                    Urls = new List<string>(),
                };
                server.Urls.Add(url);
                if (iceServer.Credential != null)
                {
                    server.Credential = iceServer.Credential;
                }
                if (iceServer.Username != null)
                {
                    server.UserName = iceServer.Username;
                }
                _iceServers.Add(server);
            }
        }

        /// <summary>
        /// If a connection to a peer is establishing, requests it's
        /// cancelation and wait the operation to cancel (blocks curren thread).
        /// </summary>
        public void CancelConnectingToPeer()
        {
            if(connectToPeerTask != null)
            {
                Debug.WriteLine("Conductor: Connecting to peer in progress, canceling");
                connectToPeerCancelationTokenSource.Cancel();
                connectToPeerTask.Wait();
                Debug.WriteLine("Conductor: Connecting to peer flow canceled");
            }
        }
    }
}
