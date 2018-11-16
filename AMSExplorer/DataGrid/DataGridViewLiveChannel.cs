﻿//----------------------------------------------------------------------------------------------
//    Copyright 2018 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//--------------------------------------------------------------------------------------------- 

using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Rest.Azure;
using Microsoft.Rest.Azure.OData;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.MediaServices.Client.DynamicEncryption;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AMSExplorer
{
    public class DataGridViewLiveChannel : DataGridView
    {
        public int ChannelsPerPage
        {
            get
            {
                return _channelsperpage;
            }
            set
            {
                _channelsperpage = value;
            }
        }
        public int PageCount
        {
            get
            {
                return _pagecount;
            }

        }
        public int CurrentPage
        {
            get
            {
                return _currentpage;
            }

        }

        public string FilterState
        {
            get
            {
                return _statefilter;
            }
            set
            {
                _statefilter = value;
            }

        }
        public SearchObject SearchInName
        {
            get
            {
                return _searchinname;
            }
            set
            {
                _searchinname = value;
            }

        }
        public bool Initialized
        {
            get
            {
                return _initialized;
            }
        }
        public string TimeFilter
        {
            get
            {
                return _timefilter;
            }
            set
            {
                _timefilter = value;
            }
        }
        public TimeRangeValue TimeFilterTimeRange
        {
            get
            {
                return _timefilterTimeRange;
            }
            set
            {
                _timefilterTimeRange = value;
            }
        }
        public int DisplayedCount
        {
            get
            {
                return _MyObservLiveEvent.Count();
            }
        }

        private List<StatusInfo> ListStatus = new List<StatusInfo>();
        static SortableBindingList<LiveEventEntry> _MyObservLiveEvent;

        static private int _channelsperpage = 50; //nb of items per page
        static private int _pagecount = 1;
        static private int _currentpage = 1;
        static private bool _initialized = false;
        static private bool _refreshedatleastonetime = false;
        static string _statefilter = "All";
        private CredentialsEntryV3 _credentialsV3;
        private AzureMediaServicesClient _client;
        static CloudMediaContext _context;
        static private CredentialsEntry _credentialsV2;
        static private SearchObject _searchinname = new SearchObject { SearchType = SearchIn.ChannelName, Text = "" };
        static private string _timefilter = FilterTime.LastWeek;
        static private TimeRangeValue _timefilterTimeRange = new TimeRangeValue(DateTime.Now.ToLocalTime().AddDays(-7).Date, null);
        static BackgroundWorker WorkerRefreshChannels;
        static Bitmap EncodingImage = Bitmaps.encoding;
        static Bitmap PremiumEncodingImage = Bitmaps.encodingPremium;
        public string _encoded = "Encoding";
        public string _encodedPreset = "EncodingPreset";

        private Bitmap ReturnChannelBitmap(LiveEvent channel)
        {
            switch (channel.Encoding.EncodingType)
            {
                case LiveEventEncodingType.None:
                    return null;

                case LiveEventEncodingType.Basic:
                    return EncodingImage;

                //case ChannelEncodingType.Premium:
                //    return PremiumEncodingImage;

                default:
                    return null;
            }
        }

        public void Init(AzureMediaServicesClient client, CredentialsEntryV3 credentials)
        {
            IEnumerable<LiveEventEntry> channelquery;
            _credentialsV3 = credentials;

            _client = client;

            var liveevents = _client.LiveEvents.List(_credentialsV3.ResourceGroup, _credentialsV3.AccountName);

            channelquery = from c in liveevents.Take(0)
                           orderby c.LastModified descending
                           select new LiveEventEntry
                           {
                               Name = c.Name,
                               Id = c.Id,
                               Description = c.Description,
                               InputProtocol = string.Format("{0} ({1})", c.Input.StreamingProtocol.ToString() /*Program.ReturnNameForProtocol(c.Input.StreamingProtocol)*/, c.Input.Endpoints.Count),
                               Encoding = ReturnChannelBitmap(c),
                               EncodingPreset = (c.Encoding != null && c.Encoding.EncodingType != LiveEventEncodingType.None) ? c.Encoding.PresetName : string.Empty,
                               InputUrl = c.Input.Endpoints.Count > 0 ? c.Input.Endpoints.FirstOrDefault().Url : string.Empty,
                               PreviewUrl = c.Preview.Endpoints.Count > 0 ? c.Preview.Endpoints.FirstOrDefault().Url : string.Empty,
                               State = c.ResourceState,
                               LastModified = c.LastModified != null ? (DateTime?)((DateTime)c.LastModified).ToLocalTime() : null
                           };
            /*
            channelquery = from c in _context.Channels.Take(0)
                           orderby c.LastModified descending
                           select new ChannelEntry
                           {
                               Name = c.Name,
                               Id = c.Id,
                               Description = c.Description,
                               InputProtocol = string.Format("{0} ({1})", Program.ReturnNameForProtocol(c.Input.StreamingProtocol), c.Input.Endpoints.Count),
                               Encoding = ReturnChannelBitmap(c),
                               EncodingPreset = (c.EncodingType != ChannelEncodingType.None && c.Encoding != null) ? c.Encoding.SystemPreset : string.Empty,
                               InputUrl = c.Input.Endpoints.FirstOrDefault().Url,
                               PreviewUrl = c.Preview.Endpoints.FirstOrDefault().Url,
                               State = c.State,
                               LastModified = c.LastModified.ToLocalTime()
                           };
*/

            DataGridViewCellStyle cellstyle = new DataGridViewCellStyle()
            {
                NullValue = null,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            DataGridViewImageColumn imageCol = new DataGridViewImageColumn()
            {
                DefaultCellStyle = cellstyle,
                Name = _encoded,
                DataPropertyName = _encoded,
            };
            this.Columns.Add(imageCol);

            SortableBindingList<LiveEventEntry> MyObservChannelsInPage = new SortableBindingList<LiveEventEntry>(channelquery.Take(0).ToList());
            this.DataSource = MyObservChannelsInPage;
            this.Columns["Id"].Visible = Properties.Settings.Default.DisplayLiveChannelIDinGrid;
            this.Columns["InputUrl"].HeaderText = "Primary Input Url";
            this.Columns["InputUrl"].Width = 140;
            this.Columns["InputUrl"].SortMode = DataGridViewColumnSortMode.NotSortable;
            this.Columns["InputProtocol"].HeaderText = "Input Protocol (input nb)";
            this.Columns["InputProtocol"].Width = 180;
            this.Columns["PreviewUrl"].Width = 120;
            this.Columns["PreviewUrl"].SortMode = DataGridViewColumnSortMode.NotSortable;

            this.Columns[_encoded].DisplayIndex = this.ColumnCount - 4;
            this.Columns[_encoded].DefaultCellStyle.NullValue = null;
            this.Columns[_encoded].HeaderText = "Cloud Encoding";
            this.Columns[_encoded].Width = 100;

            this.Columns[_encodedPreset].DisplayIndex = this.ColumnCount - 3;
            this.Columns[_encodedPreset].DefaultCellStyle.NullValue = null;
            this.Columns[_encodedPreset].HeaderText = "Preset";
            this.Columns[_encodedPreset].Width = 100;

            this.Columns["LastModified"].Width = 140;
            this.Columns["LastModified"].HeaderText = "Last modified";

            this.Columns["State"].Width = 75;
            this.Columns["Description"].Width = 110;

            WorkerRefreshChannels = new BackgroundWorker();
            WorkerRefreshChannels.WorkerSupportsCancellation = true;
            WorkerRefreshChannels.DoWork += new System.ComponentModel.DoWorkEventHandler(this.WorkerRefreshChannels_DoWork);

            _initialized = true;
        }


        public void DisplayPage(int page)
        {
            if (!_initialized) return;
            if (!_refreshedatleastonetime) return;

            if ((page <= _pagecount) && (page > 0))
            {
                _currentpage = page;
                this.DataSource = new BindingList<LiveEventEntry>(_MyObservLiveEvent.Skip(_channelsperpage * (page - 1)).Take(_channelsperpage).ToList());
            }
        }

        public void RefreshChannel(LiveEvent liveEventItem)
        {
            int index = -1;
            foreach (LiveEventEntry CE in _MyObservLiveEvent) // let's search for index
            {
                if (CE.Id == liveEventItem.Id)
                {
                    index = _MyObservLiveEvent.IndexOf(CE);
                    break;
                }
            }

            if (index >= 0) // we found it
            { // we update the observation collection
                liveEventItem = _client.LiveEvents.Get(_credentialsV3.ResourceGroup, _credentialsV3.AccountName, liveEventItem.Name); //refresh
                if (liveEventItem != null)
                {
                    _MyObservLiveEvent[index].State = liveEventItem.ResourceState;
                    _MyObservLiveEvent[index].Description = liveEventItem.Description;
                    _MyObservLiveEvent[index].LastModified = liveEventItem.LastModified != null ? (DateTime?)((DateTime)liveEventItem.LastModified).ToLocalTime() : null;
                    this.Refresh();
                }
            }
        }

        private void WorkerRefreshChannels_DoWork(object sender, DoWorkEventArgs e)
        {
            Debug.WriteLine("WorkerRefreshChannels_DoWork");
            BackgroundWorker worker = sender as BackgroundWorker;
            LiveEvent liveEventInputItem;

            foreach (LiveEventEntry CE in _MyObservLiveEvent)
            {

                liveEventInputItem = null;
                try
                {
                    liveEventInputItem = _client.LiveEvents.Get(_credentialsV3.ResourceGroup, _credentialsV3.AccountName, CE.Name);
                    if (liveEventInputItem != null)
                    {
                        CE.State = liveEventInputItem.ResourceState;
                        this.BeginInvoke(new Action(() => this.Refresh()), null);
                    }
                }
                catch // in some case, we have a timeout on Assets.Where...
                {

                }
                if (worker.CancellationPending == true)
                {
                    e.Cancel = true;
                    return;
                }
            }
            this.BeginInvoke(new Action(() => this.Refresh()), null);
        }

        private void RefreshChannels() // all assets are refreshed
        {
            RefreshChannels(_currentpage);
        }

        public void RefreshChannels(int pagetodisplay) // all assets are refreshed
        {

            if (!_initialized) return;

            this.BeginInvoke(new Action(() => this.FindForm().Cursor = Cursors.WaitCursor));

            /*
            IEnumerable<LiveEventEntry> channelquery;

            // DAYS
            bool filterStartDate = false;
            bool filterEndDate = false;

            DateTime dateTimeStart = DateTime.UtcNow;
            DateTime dateTimeRangeEnd = DateTime.UtcNow.AddDays(1);

            int days = FilterTime.ReturnNumberOfDays(_timefilter);

            if (days > 0)
            {
                filterStartDate = true;
                dateTimeStart = (DateTime.UtcNow.Add(-TimeSpan.FromDays(days)));
            }
            else if (days == -1) // TimeRange
            {
                filterStartDate = true;
                filterEndDate = true;
                dateTimeStart = _timefilterTimeRange.StartDate;
                if (_timefilterTimeRange.EndDate != null) // there is an end time
                {
                    dateTimeRangeEnd = (DateTime)_timefilterTimeRange.EndDate;
                }
            }

            // STATE
            bool filterstate = FilterState != "All";
            ChannelState channelstate = ChannelState.Running;
            if (filterstate)
            {
                channelstate = (ChannelState)Enum.Parse(typeof(ChannelState), FilterState);
            }

          //  IQueryable<LiveEvent> channelssrv =  _client.LiveEvents;

            // search
            if (_searchinname != null && !string.IsNullOrEmpty(_searchinname.Text))
            {
                bool Error = false;

                switch (_searchinname.SearchType)
                {
                    case SearchIn.ChannelName:
                        channelssrv = context.Channels.Where(c =>
                                                 (c.Name.ToLower().Contains(_searchinname.Text.ToLower()))
                                                 &&
                                                 (!filterStartDate || c.LastModified > dateTimeStart)
                                                 &&
                                                 (!filterEndDate || c.LastModified < dateTimeRangeEnd)
                                                 );
                        break;

                    case SearchIn.ChannelId:
                        string channelguid = _searchinname.Text;
                        if (channelguid.StartsWith(Constants.ChannelIdPrefix))
                        {
                            channelguid = channelguid.Substring(Constants.ChannelIdPrefix.Length);
                        }
                        try
                        {
                            var g = new Guid(channelguid);
                        }
                        catch
                        {
                            Error = true;
                            MessageBox.Show("Error with channel Id. Is it a valid GUID or channel Id ?", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        if (!Error)
                        {
                            channelssrv = context.Channels.Where(c =>
                                                    (c.Id == Constants.ChannelIdPrefix + channelguid)
                                                    &&
                                                    (!filterStartDate || c.LastModified > dateTimeStart)
                                                    &&
                                                    (!filterEndDate || c.LastModified < dateTimeRangeEnd)
                                                    );
                        }
                        break;

                    default:
                        break;

                }
            }
            else
            {
                channelssrv = context.Channels.Where(c =>
                                                 (!filterStartDate || c.LastModified > dateTimeStart)
                                                 &&
                                                 (!filterEndDate || c.LastModified < dateTimeRangeEnd)
                                                 );
            }

            /*
            switch (_orderitems)
            {
                case OrderChannels.LastModified:
                    channelssrv = channelssrv.OrderByDescending(p => p.LastModified);
                    break;

                case OrderChannels.Name:
                    channelssrv = channelssrv.OrderBy(p => p.Name);
                    break;

                case OrderChannels.State:
                    channelssrv = channelssrv.OrderBy(p => p.State);
                    break;

                default:
                    break;
            }
            */

            /*
            IEnumerable<IChannel> channels = channelssrv.AsEnumerable(); // local query now

            if (filterstate)
            {
                channels = channels.Where(c => c.State == channelstate); // this query has to be locally. Not supported on the server
            }

            if ((!string.IsNullOrEmpty(_timefilter)) && _timefilter == FilterTime.First50Items)
            {
                channels = channels.Take(50);
            }
            */

            var channelquery = _client.LiveEvents.List(_credentialsV3.ResourceGroup, _credentialsV3.AccountName).Select(c =>
                       new LiveEventEntry
                       {
                           Name = c.Name,
                           Id = c.Id,
                           Description = c.Description,
                           InputProtocol = string.Format("{0} ({1})", c.Input.StreamingProtocol.ToString() /*Program.ReturnNameForProtocol(c.Input.StreamingProtocol)*/, c.Input.Endpoints.Count),
                           Encoding = ReturnChannelBitmap(c),
                           EncodingPreset = (c.Encoding != null && c.Encoding.EncodingType != LiveEventEncodingType.None) ? c.Encoding.PresetName : string.Empty,
                           InputUrl = c.Input.Endpoints.Count > 0 ? c.Input.Endpoints.FirstOrDefault().Url : string.Empty,
                           PreviewUrl = c.Preview.Endpoints.Count > 0 ? c.Preview.Endpoints.FirstOrDefault().Url : string.Empty,
                           State = c.ResourceState,
                           LastModified = c.LastModified != null ? (DateTime?)((DateTime)c.LastModified).ToLocalTime() : null
                       });

            _MyObservLiveEvent = new SortableBindingList<LiveEventEntry>(channelquery.ToList());
            this.BeginInvoke(new Action(() => this.DataSource = _MyObservLiveEvent));
            _refreshedatleastonetime = true;
            this.BeginInvoke(new Action(() => this.FindForm().Cursor = Cursors.Default));
        }
    }

}
