﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using org.ortc;
using org.ortc.adapter;
using PeerConnectionClient.Signalling;
using PeerConnectionClient.Utilities;
using RtcPeerConnection = org.ortc.adapter.RTCPeerConnection;

namespace PeerConnectionClient.Win10.Shared
{
    public enum RTCStatsValueName
    {
        StatsValueNameActiveConnection = 0,
        StatsValueNameAudioInputLevel = 1,
        StatsValueNameAudioOutputLevel = 2,
        StatsValueNameBytesReceived = 3,
        StatsValueNameBytesSent = 4,
        StatsValueNameDataChannelId = 5,
        StatsValueNamePacketsLost = 6,
        StatsValueNamePacketsReceived = 7,
        StatsValueNamePacketsSent = 8,
        StatsValueNameProtocol = 9,
        StatsValueNameReceiving = 10,
        StatsValueNameSelectedCandidatePairId = 11,
        StatsValueNameSsrc = 12,
        StatsValueNameState = 13,
        StatsValueNameTransportId = 14,
        StatsValueNameAccelerateRate = 15,
        StatsValueNameActualEncBitrate = 16,
        StatsValueNameAdaptationChanges = 17,
        StatsValueNameAvailableReceiveBandwidth = 18,
        StatsValueNameAvailableSendBandwidth = 19,
        StatsValueNameAvgEncodeMs = 20,
        StatsValueNameBandwidthLimitedResolution = 21,
        StatsValueNameBucketDelay = 22,
        StatsValueNameCaptureStartNtpTimeMs = 23,
        StatsValueNameCandidateIPAddress = 24,
        StatsValueNameCandidateNetworkType = 25,
        StatsValueNameCandidatePortNumber = 26,
        StatsValueNameCandidatePriority = 27,
        StatsValueNameCandidateTransportType = 28,
        StatsValueNameCandidateType = 29,
        StatsValueNameChannelId = 30,
        StatsValueNameCodecName = 31,
        StatsValueNameComponent = 32,
        StatsValueNameContentName = 33,
        StatsValueNameCpuLimitedResolution = 34,
        StatsValueNameCurrentDelayMs = 35,
        StatsValueNameDecodeMs = 36,
        StatsValueNameDecodingCNG = 37,
        StatsValueNameDecodingCTN = 38,
        StatsValueNameDecodingCTSG = 39,
        StatsValueNameDecodingNormal = 40,
        StatsValueNameDecodingPLC = 41,
        StatsValueNameDecodingPLCCNG = 42,
        StatsValueNameDer = 43,
        StatsValueNameDtlsCipher = 44,
        StatsValueNameEchoCancellationQualityMin = 45,
        StatsValueNameEchoDelayMedian = 46,
        StatsValueNameEchoDelayStdDev = 47,
        StatsValueNameEchoReturnLoss = 48,
        StatsValueNameEchoReturnLossEnhancement = 49,
        StatsValueNameEncodeUsagePercent = 50,
        StatsValueNameExpandRate = 51,
        StatsValueNameFingerprint = 52,
        StatsValueNameFingerprintAlgorithm = 53,
        StatsValueNameFirsReceived = 54,
        StatsValueNameFirsSent = 55,
        StatsValueNameFrameHeightInput = 56,
        StatsValueNameFrameHeightReceived = 57,
        StatsValueNameFrameHeightSent = 58,
        StatsValueNameFrameRateDecoded = 59,
        StatsValueNameFrameRateInput = 60,
        StatsValueNameFrameRateOutput = 61,
        StatsValueNameFrameRateReceived = 62,
        StatsValueNameFrameRateSent = 63,
        StatsValueNameFrameWidthInput = 64,
        StatsValueNameFrameWidthReceived = 65,
        StatsValueNameFrameWidthSent = 66,
        StatsValueNameInitiator = 67,
        StatsValueNameIssuerId = 68,
        StatsValueNameJitterBufferMs = 69,
        StatsValueNameJitterReceived = 70,
        StatsValueNameLabel = 71,
        StatsValueNameLocalAddress = 72,
        StatsValueNameLocalCandidateId = 73,
        StatsValueNameLocalCandidateType = 74,
        StatsValueNameLocalCertificateId = 75,
        StatsValueNameMaxDecodeMs = 76,
        StatsValueNameMinPlayoutDelayMs = 77,
        StatsValueNameNacksReceived = 78,
        StatsValueNameNacksSent = 79,
        StatsValueNamePlisReceived = 80,
        StatsValueNamePlisSent = 81,
        StatsValueNamePreemptiveExpandRate = 82,
        StatsValueNamePreferredJitterBufferMs = 83,
        StatsValueNameRemoteAddress = 84,
        StatsValueNameRemoteCandidateId = 85,
        StatsValueNameRemoteCandidateType = 86,
        StatsValueNameRemoteCertificateId = 87,
        StatsValueNameRenderDelayMs = 88,
        StatsValueNameRetransmitBitrate = 89,
        StatsValueNameRtt = 90,
        StatsValueNameSecondaryDecodedRate = 91,
        StatsValueNameSendPacketsDiscarded = 92,
        StatsValueNameSpeechExpandRate = 93,
        StatsValueNameSrtpCipher = 94,
        StatsValueNameTargetDelayMs = 95,
        StatsValueNameTargetEncBitrate = 96,
        StatsValueNameTrackId = 97,
        StatsValueNameTransmitBitrate = 98,
        StatsValueNameTransportType = 99,
        StatsValueNameTypingNoiseState = 100,
        StatsValueNameViewLimitedResolution = 101,
        StatsValueNameWritable = 102,
        StatsValueNameCurrentEndToEndDelayMs = 103
    }

    /*public class CallMatrics
    {
        public IDictionary<RTCStatsValueName, IDictionary<string, CallMatricsData>> Data { get; set; }

        public CallMatrics()
        {
            Data = new Dictionary<RTCStatsValueName, IDictionary<string, CallMatricsData>>();
        }
    }

    public class CallMatricsData
    {
        public string Id { get; set; }
        public IList<object> DataList { get; set; }
        public IList<object> Timestamps { get; set; }

        public CallMatricsData()
        {
            DataList = new List<object>();
            Timestamps = new List<object>();
        }
    }*/
    class OrtcStatsManager
    {
        private static volatile OrtcStatsManager _instance;
        private static readonly object SyncRoot = new object();

        private RtcPeerConnection _peerConnection;
        private string _currentId;
        private bool _isStatsCollectionEnabled;
        private Timer _callMetricsTimer;
        private const int scheduleTimeInSeconds = 2;
        private int CallDuration { get; set; }
        private RTCStatsProvider StatsProviderPeerConnection { get; set; }
        private RTCStatsProvider StatsProviderPeerConnectionCall { get; set; }
        private IList<RTCStatsReport> callMetricsStatsReportList { get; set; }
        //private Dictionary<string,IDictionary<RTCStatsValueName,IList<object>>> callsMetricsDictionary {get; set;}
        //private Dictionary<string, CallMatrics> callsMetricsDictionary { get; set; }
        private Dictionary<string, StatsData> callsStatsDictionary { get; set; }
        private Dictionary<string, IList<RTCStatsReport>> callsStatsReportDictionary { get; set; }
        // /tester/peercc/device/codec/<notes-if-present>/<YYYMMDD-HHMM>/dataset 
        private OrtcStatsManager()
        {
            //this.callMetricsStatsReportList = new List<RTCStatsReport>();
            //callsMetricsDictionary = new Dictionary<string, IDictionary<RTCStatsValueName, IList<object>>>();
            //callsMetricsDictionary = new Dictionary<string, CallMatrics>();
            callsStatsReportDictionary = new Dictionary<string, IList<RTCStatsReport>>();
            callsStatsDictionary = new Dictionary<string, StatsData>();
        }

        public static OrtcStatsManager Instance
        {
            get
            {
                if (_instance != null) return _instance;
                lock (SyncRoot)
                {
                    if (_instance == null) _instance = new OrtcStatsManager();
                }

                return _instance;
            }
        }

        /// <summary>
        /// For each call create unique id and a list where will be stored stats data.
        /// </summary>
        private void PrepareForStats()
        {
            _currentId = /*Conductor.Instance.Peer.Name + "/" +*/ Helper.ProductName() + "/" + Helper.DeviceName() + "/" + Helper.OsVersion() + "/" + Conductor.Instance.AudioCodec.Name + "_" + Conductor.Instance.VideoCodec.Name + "/" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "/dataset/Plot1";

            StatsData statsData = new StatsData();
            //All call stats will be stored in this dict, so it can be safely uploaded to server, while other call can be in progress
            callsStatsDictionary.Add(_currentId,statsData);
            

            callMetricsStatsReportList = new List<RTCStatsReport>();
        }
        public void Initialize(RtcPeerConnection pc)
        {
            if (pc != null)
            {
                _peerConnection = pc;
                PrepareForStats();
                RTCStatsProviderOptions options =
                    new RTCStatsProviderOptions(new List<RTCStatsType>
                    {
                        RTCStatsType.IceGatherer,
                        RTCStatsType.Codec,
                        RTCStatsType.DtlsTransport
                    });

                StatsProviderPeerConnection = new RTCStatsProvider(pc, options);
            }
            else
            {
                Debug.WriteLine("ORTCStatsManager: Cannot initialize peer connection by null pointer");
            }
        }

        public void Reset()
        {
            _peerConnection = null;
            callMetricsStatsReportList = null;
            //callMetricsStatsReportList = new List<RTCStatsReport>();
            //callMetricsStatsReportList.Clear();
            StatsProviderPeerConnectionCall = null;
        }

        public bool IsStatsCollectionEnabled
        {
            get { return _isStatsCollectionEnabled; }
            set
            {
                _isStatsCollectionEnabled = value;
                if (_peerConnection != null && StatsProviderPeerConnectionCall == null)
                {
                    if (_isStatsCollectionEnabled)
                    {
                        StartCallWatch();
                    }
                    else
                    {
                        Reset();
                    }
                }
                else
                {
                    Debug.WriteLine("StatsManager: Stats are not toggled as manager is not initialized yet.");
                }
            }
        }

        

        public void StartCallWatch()
        {
            if (_peerConnection != null)
            {
                RTCStatsProviderOptions options =
                    new RTCStatsProviderOptions(new List<RTCStatsType>
                    {
                        RTCStatsType.InboundRtp,
                        RTCStatsType.OutboundRtp,
                        RTCStatsType.Track
                    });
                StatsProviderPeerConnectionCall = new RTCStatsProvider(_peerConnection, options);
                CallDuration = 0;
                AutoResetEvent autoEvent = new AutoResetEvent(false);
                TimerCallback tcb = CollectCallMetrics3;
                _callMetricsTimer = new Timer(tcb, autoEvent, 0, scheduleTimeInSeconds*1000);
            }
            else
            {
                Debug.WriteLine("ORTCStatsManager: Cannot create stats provider if peer connection is null pointer");
            }
        }

        public async void CallEnded()
        {
            StopCallWatch();
            
            if (callsStatsDictionary != null && callsStatsDictionary.ContainsKey(_currentId))
            {
                await PlotlyManager.Instance.SendData(callsStatsDictionary[_currentId], _currentId);
            }
        }
        
        public static int counter = 0;
        public static bool send = true;
        public void StopCallWatch()
        {
            if (_callMetricsTimer != null)
            {
                _callMetricsTimer.Dispose();
            }
        }

        /*private async void CollectCallMetrics2(object state)
        {
            RTCStatsReport report = await StatsProviderPeerConnectionCall.GetStats();
            CallMatrics callMatrics = callsMetricsDictionary[_currentId];

            if (report != null)
            {
                callMetricsStatsReportList.Add(report);
            }
        }*/

        private StatsData currentStatsData;
        internal class StatsData
        {
            public IList<int> Timestamps { get; } 
            public Dictionary<string,TrackStatsData> TrackStatsDictionary { get; }

            public StatsData()
            {
                Timestamps = new List<int>();
                TrackStatsDictionary = new Dictionary<string, TrackStatsData>();
            }

            public TrackStatsData GetTrackStatsData(string trackId)
            {
                TrackStatsData ret = null;
                if (trackId != null && TrackStatsDictionary.ContainsKey(trackId))
                    ret = TrackStatsDictionary[trackId];
                else
                {
                    ret = new TrackStatsData();
                    TrackStatsDictionary.Add(trackId,ret);
                }
                return ret;
            }
        }
        internal class TrackStatsData
        {
            public string MediaTrackId { get; set; }
            public Dictionary<RTCStatsValueName,IList<object>> Data{ get; }

            public TrackStatsData()
            {
                Data = new Dictionary<RTCStatsValueName, IList<object>>();
            }

            public void AddData(RTCStatsValueName valueName, object value)
            {
                IList<object> list;
                if (value != null && Data.ContainsKey(valueName))
                {
                    list = Data[valueName];
                }
                else
                {
                    list = new List<object>();
                    Data.Add(valueName,list);
                }
                list.Add(value);
            }
        }

        private StatsData activeStatsData;

        private void ParseStats(RTCStats stats, StatsData statsData)
        {
            switch (stats.StatsType)
            {
                case RTCStatsType.InboundRtp:
                    //Debug.WriteLine("RTCStatsType.InboundRtp:" + statId);
                    RTCInboundRtpStreamStats inboundRtpStreamStats = stats.ToInboundRtp();
                    if (inboundRtpStreamStats != null)
                    {
                        TrackStatsData tsd =
                            statsData.GetTrackStatsData(inboundRtpStreamStats.RtpStreamStats.MediaTrackId);

                        if (tsd != null)
                        {
                            tsd.AddData(RTCStatsValueName.StatsValueNameBytesReceived,
                                inboundRtpStreamStats.BytesReceived);

                            tsd.AddData(RTCStatsValueName.StatsValueNamePacketsReceived,
                                inboundRtpStreamStats.PacketsReceived);

                            tsd.AddData(RTCStatsValueName.StatsValueNamePacketsLost, inboundRtpStreamStats.PacketsLost);

                            tsd.AddData(RTCStatsValueName.StatsValueNameCurrentEndToEndDelayMs,
                                inboundRtpStreamStats.EndToEndDelay);
                        }
                    }
                    break;
                case RTCStatsType.OutboundRtp:
                    RTCOutboundRtpStreamStats outboundRtpStreamStats = stats.ToOutboundRtp();
                    if (outboundRtpStreamStats != null)
                    {
                        TrackStatsData tsd =
                            statsData.GetTrackStatsData(outboundRtpStreamStats.RtpStreamStats.MediaTrackId);

                        if (tsd != null)
                        {
                            tsd.AddData(RTCStatsValueName.StatsValueNameBytesSent, outboundRtpStreamStats.BytesSent);

                            tsd.AddData(RTCStatsValueName.StatsValueNamePacketsSent, outboundRtpStreamStats.PacketsSent);
                        }
                    }
                    break;
                case RTCStatsType.Track:
                    RTCMediaStreamTrackStats mediaStreamTrackStats = stats.ToTrack();
                    if (mediaStreamTrackStats != null)
                    {
                        TrackStatsData tsd =
                            statsData.GetTrackStatsData(mediaStreamTrackStats.TrackId);

                        if (tsd != null)
                        {
                            if (mediaStreamTrackStats.RemoteSource)
                            {
                                tsd.AddData(RTCStatsValueName.StatsValueNameFrameRateReceived,
                                    mediaStreamTrackStats.FramesPerSecond);
                                tsd.AddData(RTCStatsValueName.StatsValueNameFrameWidthReceived,
                                    mediaStreamTrackStats.FrameWidth);
                                tsd.AddData(RTCStatsValueName.StatsValueNameFrameHeightReceived,
                                    mediaStreamTrackStats.FrameHeight);
                            }
                            else
                            {
                                tsd.AddData(RTCStatsValueName.StatsValueNameFrameRateSent,
                                    mediaStreamTrackStats.FramesPerSecond);
                                tsd.AddData(RTCStatsValueName.StatsValueNameFrameWidthSent,
                                    mediaStreamTrackStats.FrameWidth);
                                tsd.AddData(RTCStatsValueName.StatsValueNameFrameHeightSent,
                                    mediaStreamTrackStats.FrameHeight);
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private async void CollectCallMetrics3(object state)
        {
            CallDuration += scheduleTimeInSeconds;
            StatsData statsData = callsStatsDictionary[_currentId];
            statsData.Timestamps.Add(CallDuration);
            RTCStatsReport report = await StatsProviderPeerConnectionCall.GetStats();
            if (report != null)
            {
                foreach (var statId in report.StatsIds)
                {
                    RTCStats stats = report.GetStats(statId);
                    ParseStats(stats, statsData);
                }
            }
        }

    }
}
