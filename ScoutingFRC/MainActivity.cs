﻿using System;
using Android.App;
using Android.Widget;
using Android.OS;
using System.Collections.Generic;
using System.Linq;
using Android.Content;

namespace ScoutingFRC
{
    [Activity(Label = "FRC Scouting", Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private List<TeamData> teamDataList = new List<TeamData>();
        private int lastMatch = 0;
        private string lastName;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);
            FindViewById<Button>(Resource.Id.buttonSync).Click += ButtonSync_Click;
            FindViewById<Button>(Resource.Id.buttonPitScouting).Click += ButtonPitScouting_Click;
            FindViewById<Button>(Resource.Id.buttonCollect).Click += ButtonCollect_Click;
            FindViewById<Button>(Resource.Id.buttonView).Click += ButtonView_Click;
            FindViewById<ListView>(Resource.Id.listView1).ItemClick += OnItemClick;

            teamDataList = Storage.ReadFromFile("ScoutingData2017") ?? new List<TeamData>();
        }
        
        /// <summary>  
        /// When an Item is clicked in the list of team data, that team's data is displayed 
        /// </summary> 
        private void OnItemClick(object sender, AdapterView.ItemClickEventArgs itemClickEventArgs)
        {
            var item = FindViewById<ListView>(Resource.Id.listView1).Adapter.GetItem(itemClickEventArgs.Position);
            int num = int.Parse((string)item);
            DisplayTeamData(num);
        }

        protected override void OnResume()
        {
            base.OnResume();
            FindViewById<TextView>(Resource.Id.textView2).Text = "Matches Scouted: " + teamDataList.Count;

            List<int> teamsList = new List<int>();
            foreach (var matchData in teamDataList) {
                if (!teamsList.Contains(matchData.teamNumber)) {
                    teamsList.Add(matchData.teamNumber);
                }
            }

            FindViewById<TextView>(Resource.Id.textView3).Text = "Teams: " + teamsList.Count;
            var autocompleteTextView = FindViewById<AutoCompleteTextView>(Resource.Id.autoCompleteTextView1);
            teamsList.Sort();
            string[] autoCompleteOptions = teamsList.Select(i => i.ToString()).ToArray();
            ArrayAdapter autoCompleteAdapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleDropDownItem1Line, autoCompleteOptions);
            autocompleteTextView.Adapter = autoCompleteAdapter;

            var list = FindViewById<ListView>(Resource.Id.listView1);
            ArrayAdapter teamListAdapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleDropDownItem1Line, autoCompleteOptions);
            list.Adapter = teamListAdapter;
        }

        /// <summary>  
        ///  When the button to collect match data is clicked a new MatchData Activity is started and suplyed with the users name and last match
        /// </summary> 
        private void ButtonCollect_Click(object sender, EventArgs e)
        {
            var myIntent = new Intent(this, typeof(DataCollectionActivity));
            myIntent.PutExtra("name", lastName);
            var upcomingMatch = (lastMatch == 0) ? 0 : (lastMatch + 1);
            myIntent.PutExtra("match", upcomingMatch);
            StartActivityForResult(myIntent, 0);
        }
        
        /// <summary>  
        ///  When the button to View data is clicked a new DataViewingActivity is started.
        /// </summary>  
        private void ButtonView_Click(object sender, EventArgs e)
        {
            int number;
            if(int.TryParse(FindViewById<AutoCompleteTextView>(Resource.Id.autoCompleteTextView1).Text, out number)) {
                DisplayTeamData(number);
            }  
        }
        
        /// <summary>  
        ///  When the button to collect pit data is clicked a new PitData Activity is started and suplyed with the users name
        /// </summary> 
        private void ButtonPitScouting_Click(object sender, EventArgs eventArgs)
        {
            var myIntent = new Intent(this, typeof(PitScoutingActivity));
            myIntent.PutExtra("name", lastName);
            StartActivityForResult(myIntent, 2);
        }
        
        /// <summary>  
        ///  Finds the data for a given Team number and then starts a viewActivity with this data
        /// </summary> 
        private void DisplayTeamData(int number)
        {
            List<TeamData> teamData = new List<TeamData>();
            teamData.AddRange(teamDataList.Where(td => number == td.teamNumber));

            if (teamData.Count > 0) {
                var viewActivity = new Intent(Application.Context, typeof(DataViewingActivity));
                byte[] bytes = MatchData.Serialize(teamData);
                viewActivity.PutExtra("MatchBytes", bytes);

                StartActivity(viewActivity);
            }
        }
        
        /// <summary>  
        ///  When the button to sync data is clicked a new sync Activity is started and suplyed with the current data
        /// </summary> 
        private void ButtonSync_Click(object sender, EventArgs e)
        {
            var myIntent = new Intent(this, typeof(SyncDataActivity));
            byte[] bytes = MatchData.Serialize(teamDataList);
            myIntent.PutExtra("currentData", bytes);
            StartActivityForResult(myIntent, 1);
        }
        
        /// <summary>  
        ///  When data is recived from another activity it is added to the list and stored
        /// </summary> 
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (resultCode == Result.Ok) {
                switch(requestCode) {
                    case 0: {
                            var bytes = data.GetByteArrayExtra("newMatch");
                            var match = MatchData.Deserialize<MatchData>(bytes);
                            lastMatch = match.match;
                            lastName = match.scoutName;
                            teamDataList.Add(match);
                            break;
                        }
                    case 1: {
                            var bytes = data.GetByteArrayExtra("newMatches");
                            var matches = MatchData.Deserialize<List<TeamData>>(bytes);
                            teamDataList.AddRange(matches);
                            break;
                        }
                    case 2: {
                            var bytes = data.GetByteArrayExtra("newPitData");
                            var match = MatchData.Deserialize<TeamData>(bytes);
                            lastName = match.scoutName;
                            teamDataList.Add(match);
                            break;
                        }
                }

                Storage.Delete("ScoutingData2017");
                Storage.WriteToFile("ScoutingData2017", teamDataList);
            }
        }
    }
}
