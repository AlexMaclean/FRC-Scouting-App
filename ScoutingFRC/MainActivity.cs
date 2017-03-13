﻿using System;
using Android.App;
using Android.Widget;
using Android.OS;
using System.Collections.Generic;
using Android.Bluetooth;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Android.Content;
using System.Text;

namespace ScoutingFRC
{
    [Activity(Label = "ScoutingFRC", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private List<MatchData> matchDataList = new List<MatchData>();

        private BluetoothCallbacks<BluetoothConnection> callbacks;

        private class BluetoothDataTransfer
        {
            public BluetoothDataTransfer(BluetoothConnection connection = null, int id = -1, bool received = false, bool sent = false)
            {
                this.connection = connection;
                this.id = id;
                this.received = received;
                this.sent = sent;
            }

            public bool Done()
            {
                return received && sent;
            }

            public BluetoothConnection connection;
            public int id;
            public bool received;
            public bool sent;
        }

        private List<BluetoothDataTransfer> btDataTransfers;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            FindViewById<Button>(Resource.Id.buttonCollect).Click += ButtonCollect_Click;
            FindViewById<Button>(Resource.Id.buttonView).Click += ButtonView_Click;
            FindViewById<Button>(Resource.Id.button1).Click += button1_Click;
            FindViewById<ListView>(Resource.Id.listView1).ItemClick += MainActivity_ItemClick;
            adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1);
            var listView = FindViewById<ListView>(Resource.Id.listView1);
            listView.Adapter = adapter;

            var bluetoothReceiver = new BluetoothReceiver(DiscoveryFinishedCallback);
            RegisterReceiver(bluetoothReceiver, new IntentFilter(BluetoothAdapter.ActionDiscoveryFinished));
            RegisterReceiver(bluetoothReceiver, new IntentFilter(BluetoothDevice.ActionFound));

            callbacks = new BluetoothCallbacks<BluetoothConnection>();
            callbacks.error = ErrorCallback;
            callbacks.dataReceived = DataCallback;
            callbacks.dataSent = DataSentCallback;
            callbacks.connected = ConnectedCallback;
            callbacks.disconnected = DisconnectedCallback;

            btDataTransfers = new List<BluetoothDataTransfer>();

            //Some testing
            List<MatchData> md = new List<MatchData> { RandomMatchData(), RandomMatchData(), RandomMatchData(), RandomMatchData(), RandomMatchData(), RandomMatchData() };

            byte[] test = MatchData.Serialize(md);

            List<MatchData> md2 = MatchData.Deserialize<List<MatchData>>(test);
            //

            bluetoothDevices = new List<BluetoothDevice>();

            bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            if (bluetoothAdapter != null) {
                bs = new BluetoothService(this, callbacks, bluetoothAdapter);
                SearchForDevices();
            }
            else {
                Toast.MakeText(this, "Bluetooth is disabled", ToastLength.Long).Show();
            }       
        }

        MatchData RandomMatchData()
        {
            MatchData md = new MatchData();
            Random r = new Random();

            md.teamNumber = r.Next();
            md.match = r.Next();

            md.automomous.gears.failedAttempts = r.Next();
            md.automomous.gears.successes = r.Next();

            md.automomous.highBoiler.failedAttempts = r.Next();
            md.automomous.highBoiler.successes = r.Next();

            md.automomous.lowBoiler.failedAttempts = r.Next();
            md.automomous.lowBoiler.successes = r.Next();

            md.automomous.oneTimePoints = r.Next() % 2 == 1;

            md.teleoperated.gears.failedAttempts = r.Next();
            md.teleoperated.gears.successes = r.Next();

            md.teleoperated.highBoiler.failedAttempts = r.Next();
            md.teleoperated.highBoiler.successes = r.Next();

            md.teleoperated.lowBoiler.failedAttempts = r.Next();
            md.teleoperated.lowBoiler.successes = r.Next();

            md.teleoperated.oneTimePoints = r.Next() % 2 == 1;

            md.teleoperated.gears.failedAttempts = r.Next();
            md.teleoperated.gears.successes = r.Next();

            return md;
        }

        void ErrorCallback(BluetoothConnection bluetoothConnection, Exception ex)
        {
            RunOnUiThread(() => {
                Toast.MakeText(this, "Error from " + (bluetoothConnection.bluetoothDevice.Name == null ? bluetoothConnection.bluetoothDevice.Address : bluetoothConnection.bluetoothDevice.Name) + ": " + ex.Message, ToastLength.Long).Show();
                var btDataTransfer = btDataTransfers.Find(btdt => btdt.connection == bluetoothConnection);
                if (btDataTransfer != null) {
                    btDataTransfers.Remove(btDataTransfer);
                }
            });
        }

        void DataCallback(BluetoothConnection bluetoothConnection, byte[] data)
        {
            RunOnUiThread(() => {
                List<MatchData> newMatchData = MatchData.Deserialize<List<MatchData>>(data);
                
                foreach(var md in newMatchData) {
                    var duplicate = matchDataList.Find(_md => _md.teamNumber == md.teamNumber && _md.match == md.match);
                    if(duplicate == null) {
                        matchDataList.Add(md);
                    }
                    else if (duplicate.timeCollected > md.timeCollected) {
                        matchDataList.Remove(duplicate);
                        matchDataList.Add(md);
                    }
                }

                var btDataTransfer = btDataTransfers.Find(btdt => btdt.connection == bluetoothConnection);
                if(btDataTransfer != null) {
                    btDataTransfer.received = true;

                    if (btDataTransfer.Done()) {
                        bluetoothConnection.Disconnect();
                        btDataTransfers.Remove(btDataTransfer);
                    }
                }

                //Update UI
            });
        }

        void DataSentCallback(BluetoothConnection bluetoothConnection, int id)
        {
            RunOnUiThread(() => {
                var btDataTransfer = btDataTransfers.Find(btdt => btdt.connection == bluetoothConnection);
                if (btDataTransfer != null) {
                    btDataTransfer.sent = true;

                    if (btDataTransfer.Done()) {
                        bluetoothConnection.Disconnect();
                        btDataTransfers.Remove(btDataTransfer);
                    }
                }
            });
        }

        void ConnectedCallback(BluetoothConnection bluetoothConnection)
        {
            RunOnUiThread(() => {
                var btdt = new BluetoothDataTransfer(bluetoothConnection);
                btDataTransfers.Add(btdt);
                bluetoothConnection.Write(MatchData.Serialize(matchDataList), ref btdt.id);
            });
        }

        void DisconnectedCallback(BluetoothConnection bluetoothConnection)
        {
            RunOnUiThread(() => {
                var btDataTransfer = btDataTransfers.Find(btdt => btdt.connection == bluetoothConnection);
                if (btDataTransfer != null) {
                    btDataTransfers.Remove(btDataTransfer);
                }
            });
        }

        private void MainActivity_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            BluetoothDevice device = bluetoothDevices[(int)(e.Id - 1)];

            lock (bs.connectionsLock) {
                if (bs.connections.Any(_bc => _bc.bluetoothDevice.Address == device.Address)) {
                    Toast.MakeText(this, "Already connected to this device. Disconnecting", ToastLength.Long).Show();
                    bs.Disconnect(device);
                }
                else {
                    bs.Connect(device);
                }
            }
        }

        private bool cancelled = false;

        private void SearchForDevices()
        {
            if(bluetoothAdapter != null) {
                if (bluetoothAdapter.IsDiscovering) {
                    bluetoothAdapter.CancelDiscovery();
                    cancelled = true;
                }

                if (bluetoothAdapter.StartDiscovery()) {
                    adapter.Add("---- Bluetooth Devices ---- ");
                    bluetoothDevices.Clear();

                    bluetoothDevices.AddRange(bluetoothAdapter.BondedDevices);
                    adapter.AddAll(bluetoothAdapter.BondedDevices.Select(bt => "Paired: " + ((bt.Name == null) ? "" : bt.Name) + " (" + bt.Address + ")").ToList());
                }
                else {
                    Debugger.Break();
                }
            }    
        }

        private void button1_Click(object sender, EventArgs e)
        {
            adapter.Clear();
            adapter.NotifyDataSetChanged();
            SearchForDevices();
        }

        private void ButtonCollect_Click(object sender, EventArgs e)
        {
            StartActivity(new Intent(Application.Context, typeof(DataCollectionActivity)));
        }

        private void ButtonView_Click(object sender, EventArgs e)
        {
            int number = Int32.Parse(FindViewById<TextView>(Resource.Id.editText2).Text);
            List<MatchData> goodData = new List<MatchData>();
            foreach (var matchData in matchDataList)
            {
                if(matchData.teamNumber == number) goodData.Add(matchData);
            }
            var viewActivity = new Intent(Application.Context, typeof(DataViewingActivity));

            var binFormatter = new BinaryFormatter();
            var mStream = new MemoryStream();
            binFormatter.Serialize(mStream, goodData);
            var bytes = mStream.ToArray();
            viewActivity.PutExtra("MatchBytes", bytes);
            
            StartActivity(viewActivity);
        }

        private void DiscoveryFinishedCallback(List<BluetoothDevice> devices)
        {
            if(!cancelled) {
                bluetoothDevices.AddRange(devices);

                adapter.AddAll(devices.Select(bt => ((bt.Name == null) ? "" : bt.Name) + " (" + bt.Address + ")").ToList());
                adapter.NotifyDataSetChanged();
            }

            cancelled = false;
        }

        private ArrayAdapter<string> adapter;
        private List<BluetoothDevice> bluetoothDevices;
        private BluetoothAdapter bluetoothAdapter;
        private BluetoothService bs;
    }
}

