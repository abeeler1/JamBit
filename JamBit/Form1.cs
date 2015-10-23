﻿using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JamBit
{
    public partial class Form1 : Form
    {
        private Timer checkTime;
        private OpenFileDialog openFileDialog;
        private SQLite.SQLiteConnection db;
        private RepeatMode playMode = RepeatMode.Loop;
        private Playlist currentPlaylist;
        private int playlistIndex = 1;

        enum RepeatMode { None, Loop, Repeat, Shuffle }

        public Form1()
        {
            InitializeComponent();

            db = new SQLiteConnection(Path.Combine(Application.UserAppDataPath, "jambit.db"));
            db.BeginTransaction();
            //db.DropTable<Song>();
            db.CreateTable<Song>();
            db.Commit();

            currentPlaylist = new Playlist();
            foreach (Song s in db.Table<Song>())
            {
                currentPlaylist.Songs.Add(s);
                lstPlaylist.Items.Add(new ListViewItem(new string[] {
                    s.Data.Tag.Title, s.Data.Tag.FirstPerformer, s.Data.Tag.Album
                }));
            }

            if (db.Table<Song>().Count() > 0)
            {
                MusicPlayer.OpenSong(db.Table<Song>().First());
                RefreshPlayer();
            }

            openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "MP3|*.mp3|" +
                "Music Files|*.mp3";
            openFileDialog.FileOk += openFileDialog_OnFileOk;

            checkTime = new Timer();
            checkTime.Interval = 1000;
            checkTime.Tick += new System.EventHandler(checkTime_Tick);
            
        }

        private void RefreshPlayer()
        {
            prgSongTime.SetValue(0);
            lblSongInformation.CycleText = new string[]{
                "Title: " + MusicPlayer.curSong.Data.Tag.Title,
                "Artist: " + MusicPlayer.curSong.Data.Tag.FirstPerformer,
                "Album: " + MusicPlayer.curSong.Data.Tag.Album 
            };
            lblCurrentTime.Text = "0:00";
            String format = MusicPlayer.curSong.Data.Properties.Duration.Hours > 0 ? @"h':'mm':'ss" : @"mm':'ss";
            lblSongLength.Text = (MusicPlayer.curSong.Data.Properties.Duration - new TimeSpan(0, 0, 1)).ToString(format);
        }

        private void checkTime_Tick(object sender, EventArgs e)
        {
            int seconds = (int)(long.Parse(MusicPlayer.CurrentTime()) / 1000);
            lblCurrentTime.Text = String.Format("{0}:{1:D2}", seconds / 60, seconds % 60);

            prgSongTime.SetValue((int)((double)seconds / MusicPlayer.curSong.Length * prgSongTime.Maximum));
            
            if (seconds >= MusicPlayer.curSong.Length)
            {
                switch(playMode)
                {
                    case RepeatMode.Loop:
                        if (playlistIndex == currentPlaylist.Count) playlistIndex = 0;
                        MusicPlayer.OpenSong(currentPlaylist.Songs[playlistIndex++]);
                        break;
                }
                RefreshPlayer();
            }
        }

        private void prgSongTime_SelecedValue(object sender, EventArgs e)
        {
            MusicPlayer.SeekTo(((int)((double)prgSongTime.Value / 1000 * MusicPlayer.curSong.Length)));
            int seconds = (int)(long.Parse(MusicPlayer.CurrentTime()) / 1000);
            lblCurrentTime.Text = String.Format("{0}:{1:D2}", seconds / 60, seconds % 60);
        }

        private void pgrVolume_ValueSlidTo(object sender, EventArgs e)
        {
            MusicPlayer.SetVolume(prgVolume.Value);
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (MusicPlayer.curSong != null)
            {
                checkTime.Start();
                MusicPlayer.PlaySong();
            }
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            checkTime.Stop();
            MusicPlayer.PauseSong();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            openFileDialog.ShowDialog();
        }

        private void openFileDialog_OnFileOk(object sender, CancelEventArgs e)
        {
            lblSongInformation.CycleText = new string[] { openFileDialog.FileName };
            MusicPlayer.OpenSong(openFileDialog.FileName);
            RefreshPlayer();

            db.BeginTransaction();
            foreach (string fileName in openFileDialog.FileNames)
            {
                Song s = new Song(fileName);
                try { db.Get<Song>(s.Checksum); }
                catch (System.InvalidOperationException) {
                    currentPlaylist.Songs.Add(s);
                    lstPlaylist.Items.Add(s.Data.Tag.Title);
                    db.Insert(s);
                }
                s.Data.Dispose();
            }
            db.Commit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            db.Dispose();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (currentPlaylist.Count > 0) {
                if (playlistIndex == currentPlaylist.Count) playlistIndex = 0;
                MusicPlayer.OpenSong(currentPlaylist.Songs[playlistIndex++]);
                RefreshPlayer();
            }
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            if (currentPlaylist.Count > 0)
            {
                playlistIndex = playlistIndex == 1 ? currentPlaylist.Count : playlistIndex - 1;
                MusicPlayer.OpenSong(currentPlaylist.Songs[playlistIndex - 1]);
                RefreshPlayer();
            }
        }

        private void lstPlaylist_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstPlaylist.SelectedIndices.Count == 1)
            {
                playlistIndex = lstPlaylist.SelectedIndices[0];
                MusicPlayer.OpenSong(currentPlaylist.Songs[playlistIndex++]);
                RefreshPlayer();
            }
        }
    }
}
