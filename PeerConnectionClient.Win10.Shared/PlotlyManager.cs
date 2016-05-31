﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using PeerConnectionClient.Signalling;
using PeerConnectionClient.Utilities;

namespace PeerConnectionClient.Win10.Shared
{
    class PlotlyManager
    {
        private static volatile PlotlyManager _instance;
        private static readonly object SyncRoot = new object();

        private readonly string username = "hookflash";
        private readonly string password = "QGC4s8XjbPmZoq";
        private readonly string key = "hei6r5e6rd";
        private readonly string email = "erik@hookflash.com";
        private readonly string restAPIUrl = "https://plot.ly/clientresp";
        public static PlotlyManager Instance
        {
            get
            {
                if (_instance != null) return _instance;
                lock (SyncRoot)
                {
                    if (_instance == null) _instance = new PlotlyManager();
                }

                return _instance;
            }
        }

        //public async void SendData(List<object> dict)
        public async void SendData(IList<object> data, IList<object> timestamps, string name)
        {
            var dataStr = "[" + String.Join(",", data.ToArray()) + "]";
            var timestampsStr = "[" + String.Join(",", timestamps.ToArray()) + "]";
            var argsStr = "\"[" + dataStr + "," + timestampsStr + "]\"";
            using (var client = new HttpClient())
            {
                var values = new Dictionary<string, string>
                {
                   { "un", username },
                   { "key", key },
                   {"origin", "plot" },
                    {"platform","lisp"},
                    { "args",argsStr},
                    {"kwargs","{\"filename\": \"" + name + "\",\"fileopt\": \"new\",\"style\": {\"type\": \"scatter\"},\"layout\": {\"title\": \"experimental data\"},\"world_readable\": true}"},
                };
                /*var values = new Dictionary<string, string>
                {
                   { "un", username },
                   { "key", key },
                   {"origin", "plot" },
                    {"platform","lisp"},
                    { "args","[[0, 1, 2], [3, 1, 6]],[{\"x\": [0, 1, 2], \"y\": [3, 1, 6]}],[{\"x\": [0, 1, 2], \"y\": [3, 1, 6], \"name\": \"Experimental\", \"marker\": {\"symbol\": \"square\", \"color\": \"purple\"}}, {\"x\": [1, 2, 3], \"y\": [3, 4, 5], \"name\": \"Control\"}]"},
                    {"kwargs","{\"filename\": \"plot from api\",\"fileopt\": \"overwrite\",\"style\": {\"type\": \"bar\"}, \"traces\": [0,3,5],\"layout\": {\"title\": \"experimental data\"},\"world_readable\": true}"},
                };*/

                var content = new FormUrlEncodedContent(values);

                var response = await client.PostAsync(restAPIUrl, content);

                var responseString = await response.Content.ReadAsStringAsync();
                if (responseString != null && responseString.Length > 0)
                {
                    Debug.Write(responseString);
                }
            }
        }

        public string GetTitle(RTCStatsValueName valueName)
        {
            switch (valueName)
            {
                case RTCStatsValueName.StatsValueNameBytesReceived:
                    return "Received Bytes";
                    break;
                case RTCStatsValueName.StatsValueNamePacketsReceived:
                    return "Received Packets";
                    break;
                case RTCStatsValueName.StatsValueNamePacketsLost:
                    return "Lost Packets";
                    break;
                case RTCStatsValueName.StatsValueNameCurrentEndToEndDelayMs:
                    return "End To End Delay";
                    break;
                case RTCStatsValueName.StatsValueNameBytesSent:
                    return "Sent Bytes";
                    break;
                case RTCStatsValueName.StatsValueNamePacketsSent:
                    return "Sent Packets";
                    break;
                case RTCStatsValueName.StatsValueNameFrameRateReceived:
                    return "Frame Rate Received";
                    break;
                case RTCStatsValueName.StatsValueNameFrameWidthReceived:
                    return "Frame Width Received";
                    break;
                case RTCStatsValueName.StatsValueNameFrameHeightReceived:
                    return "Frame Heigh Received";
                    break;
                case RTCStatsValueName.StatsValueNameFrameRateSent:
                    return "Frame Rate Sent";
                    break;
                case RTCStatsValueName.StatsValueNameFrameWidthSent:
                    return "Frame Width Sent";
                    break;
                case RTCStatsValueName.StatsValueNameFrameHeightSent:
                    return "Frame Height Sent";
                    break;
            }
            return "";
        }


        public string GetColor(RTCStatsValueName valueName)
        {
            switch (valueName)
            {
                case RTCStatsValueName.StatsValueNameBytesReceived:
                case RTCStatsValueName.StatsValueNameBytesSent:
                    return "blue";
                    break;
                case RTCStatsValueName.StatsValueNamePacketsReceived:
                case RTCStatsValueName.StatsValueNamePacketsSent:
                    return "green";
                    break;
                case RTCStatsValueName.StatsValueNamePacketsLost:
                    return "red";
                    break;
                case RTCStatsValueName.StatsValueNameCurrentEndToEndDelayMs:
                    return "yellow";
                    break;
                case RTCStatsValueName.StatsValueNameFrameRateReceived:
                case RTCStatsValueName.StatsValueNameFrameRateSent:
                    return "black";
                    break;
                case RTCStatsValueName.StatsValueNameFrameWidthReceived:
                case RTCStatsValueName.StatsValueNameFrameWidthSent:
                    return "grey";
                    break;
                case RTCStatsValueName.StatsValueNameFrameHeightReceived:
                case RTCStatsValueName.StatsValueNameFrameHeightSent:
                    return "orange";
                    break;
            }
            return "purple";
        }

        private string GetYAxisTitle(RTCStatsValueName valueName)
        {
            switch (valueName)
            {
                case RTCStatsValueName.StatsValueNameBytesSent:
                case RTCStatsValueName.StatsValueNameBytesReceived:
                    return "Bytes";
                    break;
                case RTCStatsValueName.StatsValueNamePacketsSent:
                case RTCStatsValueName.StatsValueNamePacketsReceived:
                case RTCStatsValueName.StatsValueNamePacketsLost:
                    return "Packets";
                    break;

                case RTCStatsValueName.StatsValueNameCurrentEndToEndDelayMs:
                    return "Delay (ms)";
                    break;

                case RTCStatsValueName.StatsValueNameFrameRateSent:
                case RTCStatsValueName.StatsValueNameFrameRateReceived:
                    return "Frames";
                    break;
                case RTCStatsValueName.StatsValueNameFrameHeightReceived:
                case RTCStatsValueName.StatsValueNameFrameWidthReceived:
                case RTCStatsValueName.StatsValueNameFrameWidthSent:
                case RTCStatsValueName.StatsValueNameFrameHeightSent:
                    return "Pixels";
                    break;
            }
            return "";
        }
        //{\"x\": [0, 1, 2], \"y\": [3, 11, 16],\"line\": {\"color\":\"purple\"},\"name\":\"Frame Rates\"}
        private async Task FormatDataForSending(OrtcStatsManager.TrackStatsData trackStatsData, string formatedTimestamps, string id, string trackId)
        {
            List<string> datasets= new List<string>();
            //foreach (var values in trackStatsData.Data.Values)
            foreach (var valueNames in trackStatsData.Data.Keys)
            {
                var values = trackStatsData.Data[valueNames];
                var valuesStr = "[" + String.Join(",", values.ToArray()) + "]";
                string graphTitle = GetTitle(valueNames);
                string color = GetColor(valueNames);

                string formated = "[" + "{\"x\": " + formatedTimestamps + ", \"y\": " + valuesStr + ",\"line\": " + "{\"color\":\"" + color + "\"}" + ",\"name\":\"" + graphTitle + "\"}" + "]";
                //a-wait SendToPlotly(formated, id, graphTitle);
                datasets.Add(formated);
            }
            string ret = "[" + String.Join(",", datasets.ToArray()) + "]";
            //return ret;
        }

        private string FormatDataForSending(OrtcStatsManager.TrackStatsData trackStatsData, string formatedTimestamps, RTCStatsValueName valueNames)
        {
            var values = trackStatsData.Data[valueNames];
            var valuesStr = "[" + String.Join(",", values.ToArray()) + "]";
            string graphTitle = GetTitle(valueNames);
            string color = GetColor(valueNames);

            string formated = "[" + "{\"x\": " + formatedTimestamps + ", \"y\": " + valuesStr + ",\"line\": " + "{\"color\":\"" + color + "\"}" + ",\"name\":\"" + graphTitle + "\"}" + "]";

            return formated;
        }

        private Dictionary<string, string> FormatDataForSending2(OrtcStatsManager.TrackStatsData trackStatsData, string formatedTimestamps, RTCStatsValueName valueNames)
        {
            var values = trackStatsData.Data[valueNames];
            var valuesStr = "[" + String.Join(",", values.ToArray()) + "]";
            string graphTitle = GetTitle(valueNames);
            string color = GetColor(valueNames);

            //string formated = "[" + "{\"x\": " + formatedTimestamps + ", \"y\": " + valuesStr + ",\"line\": " + "{\"color\":\"" + color + "\"}" + ",\"name\":\"" + graphTitle + "\"}" + "]";

            string formated = "{\"x\": " + formatedTimestamps + ", \"y\": " + valuesStr + ",\"line\": " + "{\"color\":\"" + color + "\"}" + ",\"name\":\"" + graphTitle + "\"}";

            Dictionary<string,string> dictionary = new Dictionary<string, string>
            {
                 { "args",formated },
                 { "title",graphTitle },
            };

            return dictionary;
        }

        public async Task SendSummary(List<Dictionary<string, string>> datasets, string path, string trackId)
        {
            //string ret = datasets.Aggregate("[", (current, dict) => current.Length==1?current:(current+",") + dict["args"]);

            string ret = "[";
            foreach (var dataset in datasets)
                ret = (ret.Length == 1 ? ret : (ret + ",")) + dataset["args"];
            ret += "]";
            string title = "Summary for the track " + trackId;
            await SendToPlotly(ret, path, trackId,title);
        }

        public async Task CreateCallSummary(OrtcStatsManager.StatsData statsData, string id, string path)
        {
            IList<string> argsCallItems = new List<string>();

            //string formated = "[" + "{\"x\": " + formatedTimestamps + ", \"y\": " + valuesStr + ",\"line\": " + "{\"color\":\"" + color + "\"}" + ",\"name\":\"" + graphTitle + "\"}" + "]";

            string formatedArgsStartTime = "{\"x\": " + statsData.StarTime + ", \"y\": " + "[0]" + ",\"line\": " + "{\"color\":\"" + "black" + "\"}" + ",\"name\":\"" + "Start time" + "\"}";
            argsCallItems.Add(formatedArgsStartTime);
            string formatedArgsTimeToSetupCall = "{\"x\": " + statsData.TimeToSetupCall.Milliseconds + ", \"y\": " + "[0]" + ",\"line\": " + "{\"color\":\"" + "black" + "\"}" + ",\"name\":\"" + "Time To Setup Call" + "\"}";
            argsCallItems.Add(formatedArgsTimeToSetupCall);

            foreach (var trackId in statsData.TrackStatsDictionary.Keys)
            {
                OrtcStatsManager.TrackStatsData trackStatsData = statsData.TrackStatsDictionary[trackId];
                IList<string> argsItems = new List<string>();
                foreach (var valueName in trackStatsData.Data.Keys)
                {
                    var values = trackStatsData.Data[valueName];
                    double averageValue = values.Average();
                    argsItems.Add("{\"x\": " + averageValue.ToString("0.##") + ", \"y\": " + "[0]" + ",\"line\": " + "{\"color\":\"" + (trackStatsData.outgoing ? "blue" : "green") + "\"}" + ",\"name\":\"" + "Average " + (trackStatsData.outgoing?"outgoing " : "incoming ") + GetTitle(valueName) + "\"}");
                }
                string argsTrack = String.Join(",", argsItems.ToArray());
                argsCallItems.Add(argsTrack);
            }
            string args = "[" + String.Join(",", argsCallItems.ToArray()) + "]";
            string title = "Call Summary";
            await SendToPlotly(args, path, "CallSummary", title);
        }
        public async Task SendData(OrtcStatsManager.StatsData statsData, string id)
        {
            if (statsData == null)
                return;

            var timestampsStr = "[" + String.Join(",", statsData.Timestamps.ToArray()) + "]";
            //List<string> datasets = new List<string>();
            //List<Dictionary<string, string>> datasets = new List<Dictionary<string, string>>();
            var hostname = NetworkInformation.GetHostNames().FirstOrDefault(h => h.Type == HostNameType.DomainName);
            string ret = hostname?.CanonicalName ?? "<unknown host>";
            var strCaller = statsData.IsCaller ? "caller_" : "callee_";
            var basePath = statsData.StarTime.ToString("yyyyMMdd") + "/" + id + "/" + strCaller + hostname;
            if (statsData.IsCaller)
                basePath += "_" + Conductor.Instance.AudioCodec.Name + "_" + Conductor.Instance.VideoCodec.Name;
            foreach (var trackId in statsData.TrackStatsDictionary.Keys)
            {
                List<Dictionary<string, string>> datasets = new List<Dictionary<string, string>>();
                    
                OrtcStatsManager.TrackStatsData trackStatsData = statsData.TrackStatsDictionary[trackId];
                foreach (var valueNames in trackStatsData.Data.Keys)
                {
                    //string formattedString = FormatDataForSending(trackStatsData, timestampsStr, valueNames);
                    Dictionary<string, string> dictionary = FormatDataForSending2(trackStatsData, timestampsStr, valueNames);
                    dictionary.Add("trackId",trackId);
                    dictionary.Add("yaxisTitle", GetYAxisTitle(valueNames));
                    datasets.Add(dictionary);
                    //datasets.Add(formattedString);
                }
                foreach (Dictionary<string, string> dict in datasets)
                {
                    await SendToPlotly(dict, basePath, trackStatsData.outgoing);
                }

                await SendSummary(datasets, basePath, trackId);
            }

            await CreateCallSummary(statsData, id, basePath);
        }

        public async Task SendToPlotly(Dictionary<string, string> dictionary, string path, bool outgoing)
        {
            //string filename = path + "/" + dictionary["trackId"] + "/" + dictionary["title"];
            string filename = path + "/" + dictionary["trackId"] + "/" + dictionary["title"];

            using (var client = new HttpClient())
            {
                var values = new Dictionary<string, string>
                {
                   { "un", username },
                   { "key", key },
                   {"origin", "plot" },
                    {"platform","lisp"},
                    { "args","[" + dictionary["args"] + "]"},
                    {"kwargs","{\"filename\": \"" + filename + "\",\"fileopt\": \"append\",\"style\": {\"type\": \"scatter\"},\"layout\": {\"title\": \"" + dictionary["trackId"] + "_" + (outgoing?"outgoing":"incoming") + " (" + dictionary["title"] + ")"+ "\",\"xaxis\": {\"title\": \"Time (s)\"},\"yaxis\": {\"title\":" + dictionary["yaxisTitle"]+"\"}},\"world_readable\": true}"}
                };

                var content = new FormUrlEncodedContent(values);

                var response = await client.PostAsync(restAPIUrl, content);

                var responseString = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseString))
                {
                    Debug.Write(responseString);
                }
            }
        }
        public async Task SendToPlotly(string args, string path, string trackId, string title=null)
        {
            string filename = path + "/" + trackId;
            string plotTitle = title == null ? trackId : title;
            using (var client = new HttpClient())
            {
                var values = new Dictionary<string, string>
                {
                   { "un", username },
                   { "key", key },
                   {"origin", "plot" },
                    {"platform","lisp"},
                    { "args",args},
                    {"kwargs","{\"filename\": \"" + filename + "\",\"fileopt\": \"new\",\"style\": {\"type\": \"scatter\"},\"layout\": {\"title\": \"" + plotTitle + "\"},\"world_readable\": true}"},
                };

                var content = new FormUrlEncodedContent(values);

                var response = await client.PostAsync(restAPIUrl, content);

                var responseString = await response.Content.ReadAsStringAsync();
                if (responseString != null && responseString.Length > 0)
                {
                    Debug.Write(responseString);
                }
            }
        }

        
    }
}
